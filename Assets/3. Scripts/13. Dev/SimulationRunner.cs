using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 밸런스 시뮬레이션 진행기 — 설정 파일이 켜져 있으면 화면·로그를 끄고 빨리 감아 자동 진행으로 여러 판을 이어 돌림
    [Serializable]
    public class SimulationSettings
    {
        public bool Active;
        public bool Done;
        public int Runs = 20;
        public int FirstSeed = 1;
        public int Mode;
        public bool RandomEmblems;
        public float TimeScale = 100f;
        public float GameTimeoutSeconds = 900f;
        public string ResultName = "";
        public int Finished;

        public static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Balance"));
        public static string FilePath => Path.Combine(Folder, "simulation_settings.json");

        public static SimulationSettings Load()
        {
            if (!File.Exists(FilePath)) return null;
            try { return JsonUtility.FromJson<SimulationSettings>(File.ReadAllText(FilePath)); }
            catch (Exception) { return null; }
        }

        public void Save()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
        }
    }

    public sealed class SimulationRunner : MonoBehaviour
    {
        private const int StuckFrames = 3000;
        private const string GameOverMarker = "[GameSession] 게임 오버: ";

        public static bool IsOn { get; private set; }

        private SimulationSettings m_settings;
        private QuietLogHandler m_log;
        private ILogHandler m_originalLog;
        private float m_originalMaxDelta;
        private int m_originalVSync;
        private int m_originalFrameRate;
        private int m_run;
        private int m_seed;
        private bool m_ending;
        private int m_playerTurnFrames;
        private int m_handledWave;
        private float m_gameStartTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            IsOn = false;
            SimulationSettings settings = SimulationSettings.Load();
            if (settings == null || !settings.Active || settings.Done) return;

            var runner = new GameObject("Simulation Runner").AddComponent<SimulationRunner>();
            DontDestroyOnLoad(runner.gameObject);
            runner.Begin(settings);
        }

        private void Begin(SimulationSettings settings)
        {
            m_settings = settings;
            m_run = 0;
            m_seed = settings.FirstSeed;
            UnityEngine.Random.InitState(m_seed);
            IsOn = true;

            m_originalLog = Debug.unityLogger.logHandler;
            m_log = new QuietLogHandler(m_originalLog);
            Debug.unityLogger.logHandler = m_log;
            m_originalMaxDelta = Time.maximumDeltaTime;
            m_originalVSync = QualitySettings.vSyncCount;
            m_originalFrameRate = Application.targetFrameRate;
            Time.maximumDeltaTime = 1f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            BalanceRecorder.Open(SimulationSettings.Folder, settings.ResultName);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (IsOn) Restore();
        }

        private void Restore()
        {
            IsOn = false;
            if (m_originalLog != null) Debug.unityLogger.logHandler = m_originalLog;
            Time.maximumDeltaTime = m_originalMaxDelta;
            QualitySettings.vSyncCount = m_originalVSync;
            Application.targetFrameRate = m_originalFrameRate;
            Time.timeScale = 1f;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            m_ending = false;
            m_playerTurnFrames = 0;
            m_handledWave = -1;
            m_gameStartTime = Time.realtimeSinceStartup;
            m_log.LastReason = "";

            foreach (Camera camera in Camera.allCameras) camera.enabled = false;

            string emblems = ApplyGameOptions();
            string modeName = GameSession.Instance != null ? GetField<GameMode>(GameSession.Instance, "m_mode").ToString() : "";
            BalanceRecorder.BeginGame(m_run, m_seed, modeName, emblems);

            if (TurnManager.Instance != null) TurnManager.Instance.StateChanged += OnStateChanged;
        }

        private string ApplyGameOptions()
        {
            if (m_settings.Mode != 0 && GameSession.Instance != null) SetField(GameSession.Instance, "m_mode", (GameMode)m_settings.Mode);
            if (TowerManager.Instance == null) return "";

            List<TowerEmblem> selected = GetField<List<TowerEmblem>>(TowerManager.Instance, "m_selectedEmblems");
            if (selected == null) return "";

            if (m_settings.RandomEmblems)
            {
                GameMode mode = GameSession.Instance != null ? GetField<GameMode>(GameSession.Instance, "m_mode") : GameMode.Normal;
                int count = mode == GameMode.Hard ? 8 : 6;
                var pool = new List<TowerEmblem>();
                for (int i = (int)TowerEmblem.Sword; i <= (int)TowerEmblem.Darkness; i++) pool.Add((TowerEmblem)i);

                var random = new System.Random(m_seed);
                selected.Clear();
                while (selected.Count < count && pool.Count > 0)
                {
                    int pick = random.Next(pool.Count);
                    selected.Add(pool[pick]);
                    pool.RemoveAt(pick);
                }
            }
            return string.Join("|", selected);
        }

        private void Update()
        {
            if (!IsOn || m_ending) return;
            Time.timeScale = m_settings.TimeScale;

            TurnManager turns = TurnManager.Instance;
            TowerManager towers = TowerManager.Instance;
            if (turns == null || towers == null) return;

            if (Time.realtimeSinceStartup - m_gameStartTime > m_settings.GameTimeoutSeconds)
            {
                Finish("Timeout", $"{m_settings.GameTimeoutSeconds:0}초 안에 끝나지 않음");
                return;
            }

            if (!turns.Current.CanPlayerAct)
            {
                m_playerTurnFrames = 0;
                return;
            }
            if (++m_playerTurnFrames > StuckFrames)
            {
                Finish("Stuck", "플레이어 턴을 넘기지 못함 (놓지 못한 존 등)");
                return;
            }
            if (!AutoPlanner.PlayStep()) return;

            int wave = WaveManager.Instance.CurrentWave;
            if (m_handledWave != wave)
            {
                m_handledWave = wave;
                if (!BoardManager.Instance.Slots.TryGetFirstEmpty(out _) && CurrencyManager.Instance.Gold >= towers.BuildCost)
                    BalanceRecorder.CountBlockedWave();
            }
            turns.OnClickEndTurn();
        }

        private void OnStateChanged(ITurnState state)
        {
            if (state is GameOverState) Finish("GameOver", m_log.LastReason);
            else if (state is GameClearState) Finish("Clear", "");
        }

        private void Finish(string result, string reason)
        {
            if (m_ending) return;
            m_ending = true;

            if (TurnManager.Instance != null) TurnManager.Instance.StateChanged -= OnStateChanged;
            BalanceRecorder.EndGame(result, reason);

            m_run++;
            m_settings.Finished = m_run;
            if (m_run >= m_settings.Runs)
            {
                m_settings.Done = true;
                m_settings.Save();
                Restore();
                Time.timeScale = 0f;
                Debug.Log($"[Simulation] {m_run}판 끝 — 결과: {SimulationSettings.Folder}");
                return;
            }

            m_settings.Save();
            StartCoroutine(NextGame());
        }

        private IEnumerator NextGame()
        {
            yield return null;
            DOTween.KillAll();
            m_seed = m_settings.FirstSeed + m_run;
            UnityEngine.Random.InitState(m_seed);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static T GetField<T>(object owner, string name)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field != null ? (T)field.GetValue(owner) : default;
        }

        private static void SetField(object owner, string name, object value)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null) field.SetValue(owner, value);
        }

        private sealed class QuietLogHandler : ILogHandler
        {
            private readonly ILogHandler m_inner;
            public string LastReason = "";

            public QuietLogHandler(ILogHandler inner) => m_inner = inner;

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (logType == LogType.Log)
                {
                    string message = args != null && args.Length > 0 ? string.Format(format, args) : format;
                    if (message.StartsWith(GameOverMarker)) LastReason = message.Substring(GameOverMarker.Length);
                    return;
                }
                m_inner.LogFormat(logType, context, format, args);
            }

            public void LogException(Exception exception, UnityEngine.Object context) => m_inner.LogException(exception, context);
        }
    }
}
