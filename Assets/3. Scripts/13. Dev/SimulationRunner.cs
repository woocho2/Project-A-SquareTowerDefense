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
        public bool CompareMergeCounts;
        public int MergeCountA = TowerProgression.MergeCount;
        public int MergeCountB = 2;
        public int StopWave;
        public int SupportVariant;

        public bool CompareDeckModes;
        public int NormalDeckCount = EmblemSelectPanel.NormalCount;
        public int HardDeckCount = EmblemSelectPanel.HardCount;
        public string CsvSha256 = "";

        public bool WaveProbe;
        public int ProbeTowers = 2;
        public int ProbeWaves = 2;
        public float ProbeSplashPower = 1f;
        public float ProbeSinglePower = 1f;
        public int ProbeSplashAction;
        public int ProbeSingleAction;
        public int ProbeEnemyAction;
        public int ProbeSplashMastery;
        public int ProbeRange;
        public int[] ProbeIds = new int[0];
        public bool ProbeOverkill;
        public bool ProbeDummy;

        public bool SearchStrategies;
        public bool SearchSmoke;
        public bool SearchComplete;
        public bool SearchFailed;
        public int SearchCandidates = 500;
        public int SearchRepeats = 10;
        public int SearchMinRepeats = 5;
        public float SearchRetainRatio = 0.8f;
        public int SearchCandidate;
        public int SearchTrial;
        public bool IsControlled => IsPlacementStudy || SearchStrategies;
        public int PlacementProfile = AutoPlanner.DefaultPlacementProfile;
        public int FocusEmblems = AutoPlanner.DefaultFocusCount;
        public float FixedGameStep = 1f / 30f;
        public int[] PlacementProfiles = new int[0];
        public int[] FixedDeck = { 3, 4, 11, 7 };
        public bool IsPlacementStudy => PlacementProfiles != null && PlacementProfiles.Length > 0;
        public int ScenarioCount => IsPlacementStudy ? PlacementProfiles.Length : CompareMergeCounts || CompareDeckModes ? 2 : 1;
        public int TotalRuns => SearchStrategies ? SearchCandidates * SearchRepeats : Runs * ScenarioCount;
        public int GetPlacementProfile(int run) => SearchStrategies ? AutoPlanner.DefaultPlacementProfile : IsPlacementStudy ? PlacementProfiles[run % ScenarioCount] : PlacementProfile;
        public static string PlacementLabel(int profile) => new[] { "Baseline", "MapBuffDebuff", "MapDebuffBuff", "BuffMapDebuff", "BuffDebuffMap", "DebuffMapBuff", "DebuffBuffMap" }[profile];
        public int GetPairIndex(int run) => SearchStrategies ? SearchTrial : run / ScenarioCount;
        public int GetSeed(int run) => SearchStrategies ? FirstSeed : unchecked(FirstSeed + GetPairIndex(run));
        public string GetVariant(int run) => SearchStrategies ? $"Strategy{SearchCandidate:D4}" : IsPlacementStudy
            ? $"S{run % ScenarioCount}_{PlacementLabel(GetPlacementProfile(run))}"
            : CompareDeckModes
            ? (run % 2 == 0 ? "Normal" : "Hard")
            : CompareMergeCounts ? (run % 2 == 0 ? "A" : "B") : "";
        public int GetMergeCount(int run) => CompareMergeCounts && !CompareDeckModes && !IsPlacementStudy && !SearchStrategies
            ? Mathf.Clamp(run % 2 == 0 ? MergeCountA : MergeCountB, 2, 5)
            : TowerProgression.MergeCount;
        public int GetMode(int run) => IsControlled ? (int)GameMode.Normal : CompareDeckModes ? (int)(run % 2 == 0 ? GameMode.Normal : GameMode.Hard) : Mode;
        public int GetDeckCount(int run) => IsControlled ? FixedDeck.Length : CompareDeckModes ? (run % 2 == 0 ? NormalDeckCount : HardDeckCount) : 0;
        public string GetResultName(int run) => ResultName + (ScenarioCount > 1 || IsControlled ? "_" + GetVariant(run) : "");

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
        public static int CurrentSeed { get; private set; }
        public static bool SuppressCosmetics { get; private set; }
        public const int SupportOff = 1;
        public const int AttackOnly = 2;
        public const int GlobalSupport = 3;
        public const int AuraSupport = 4;
        public const int AuraRange = 2;
        public static int SupportVariant { get; private set; }
        public static bool SupportsDisabled => IsOn && SupportVariant == SupportOff;
        public static bool SupportsGlobal => IsOn && SupportVariant == GlobalSupport;
        public static bool SupportsAura => IsOn && SupportVariant == AuraSupport;
        public static bool NoSupportZones => SupportsDisabled || SupportsGlobal || SupportsAura;
        public static bool AttackOnlyBuilds => IsOn && SupportVariant == AttackOnly;

        private SimulationSettings m_settings;
        private QuietLogHandler m_log;
        private ILogHandler m_originalLog;
        private float m_originalMaxDelta;
        private int m_originalVSync;
        private StrategySearch m_search;
        private StrategyRandomStreams m_randomStreams;
        private int m_originalFrameRate;
        private float m_originalCaptureDelta;
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
            if (settings.SearchStrategies) m_search = new StrategySearch(settings);
            m_run = 0;
            m_seed = settings.GetSeed(0);
            CurrentSeed = m_seed;
            UnityEngine.Random.InitState(m_seed);
            IsOn = true;
            SuppressCosmetics = settings.IsControlled;
            SupportVariant = settings.SupportVariant;
            BalanceRecorder.CountOverkill = settings.ProbeOverkill || settings.ProbeDummy;
            BalanceRecorder.DummyEnemies = settings.ProbeDummy;

            m_originalLog = Debug.unityLogger.logHandler;
            m_log = new QuietLogHandler(m_originalLog);
            Debug.unityLogger.logHandler = m_log;
            m_originalMaxDelta = Time.maximumDeltaTime;
            m_originalVSync = QualitySettings.vSyncCount;
            m_originalFrameRate = Application.targetFrameRate;
            m_originalCaptureDelta = Time.captureDeltaTime;
            if (settings.IsControlled) Time.captureDeltaTime = settings.FixedGameStep / settings.TimeScale;
            Time.maximumDeltaTime = 1f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

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
            SuppressCosmetics = false;
            SupportVariant = 0;
            BalanceRecorder.CountOverkill = false;
            BalanceRecorder.DummyEnemies = false;
            AutoPlanner.ResetPlacement();
            StrategySearch.Reset();
            Time.captureDeltaTime = m_originalCaptureDelta;
            if (TowerManager.Instance != null) TowerManager.Instance.ConfigureMergeCount(TowerProgression.MergeCount);
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
            m_probeEnemiesAdjusted = false;
            m_gameStartTime = Time.realtimeSinceStartup;
            m_log.LastReason = "";
            m_randomStreams = null;
            if (m_search != null) m_search.BeginGame();

            foreach (Camera camera in Camera.allCameras) camera.enabled = false;
            if (m_settings.IsControlled && Application.isBatchMode)
                foreach (Canvas canvas in FindObjectsByType<Canvas>()) canvas.enabled = false;

            string emblems = ApplyGameOptions();
            string modeName = GameSession.Instance != null ? GetField<GameMode>(GameSession.Instance, "m_mode").ToString() : "";
            BalanceRecorder.Open(SimulationSettings.Folder, m_settings.GetResultName(m_run));
            BalanceRecorder.BeginGame(m_settings.GetPairIndex(m_run), m_seed, modeName, emblems,
                m_settings.GetVariant(m_run), m_settings.GetMergeCount(m_run), m_search != null ? m_search.CombatSeed : 0);

            m_bossTrace.Clear();
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.StateChanged += OnStateChanged;
                TurnManager.Instance.EnemyTurnEnded += TraceBoss;
            }
        }

        private string ApplyGameOptions()
        {
            int modeValue = m_settings.GetMode(m_run);
            if (modeValue != 0 && GameSession.Instance != null) SetField(GameSession.Instance, "m_mode", (GameMode)modeValue);
            if (TowerManager.Instance == null) return "";
            TowerManager.Instance.ConfigureMergeCount(m_settings.GetMergeCount(m_run));
            AutoPlanner.ConfigurePlacement(m_settings.GetPlacementProfile(m_run));
            AutoPlanner.FocusCount = m_settings.FocusEmblems;
            AutoPlanner.ResetFocus();

            List<TowerEmblem> selected = GetField<List<TowerEmblem>>(TowerManager.Instance, "m_selectedEmblems");
            if (selected == null) return "";

            if (m_settings.IsControlled)
            {
                selected.Clear();
                foreach (int emblem in m_settings.FixedDeck) selected.Add((TowerEmblem)emblem);
            }
            else if (m_settings.RandomEmblems || m_settings.CompareDeckModes)
            {
                GameMode mode = GameSession.Instance != null ? GetField<GameMode>(GameSession.Instance, "m_mode") : GameMode.Normal;
                int requested = m_settings.GetDeckCount(m_run);
                int count = requested > 0 ? requested : EmblemSelectPanel.GetCount(mode);
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

            if (m_settings.WaveProbe) AdjustProbeEnemies();

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
            if (m_settings.WaveProbe)
            {
                ProbeStep(turns);
                return;
            }
            if (PassedStopWave())
            {
                Finish("Clear", $"{m_settings.StopWave}웨이브 통과");
                return;
            }

            bool ready;
            if (m_search == null) ready = AutoPlanner.PlayStep();
            else
            {
                if (m_randomStreams == null) m_randomStreams = new StrategyRandomStreams(m_settings.FirstSeed, m_search.CombatSeed);
                ready = m_randomStreams.RunPlanner(m_search.PlayStep);
            }
            if (!ready) return;

            int wave = WaveManager.Instance.CurrentWave;
            if (m_handledWave != wave)
            {
                m_handledWave = wave;
                if (!BoardManager.Instance.Slots.TryGetFirstEmpty(out _) && CurrencyManager.Instance.Gold >= towers.BuildCost)
                    BalanceRecorder.CountBlockedWave();
            }
            turns.OnClickEndTurn();
        }

        private readonly List<string> m_probeLines = new List<string>();
        private readonly List<(string ids, float rate)> m_probeResults = new List<(string, float)>();
        private string m_probeIds = "";
        private bool m_probeEnemiesAdjusted;

        private void AdjustProbeEnemies()
        {
            if (m_probeEnemiesAdjusted || m_settings.ProbeEnemyAction == 0 || WaveManager.Instance.CurrentWave != 1) return;
            if (EnemyManager.Instance == null || EnemyManager.Instance.Registry.Count == 0) return;
            m_probeEnemiesAdjusted = true;
            foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
            {
                int before = enemy.Stats.GetMoveRange().Action;
                enemy.Stats.Modifiers.Add(StatModifier.Flat("시뮬레이션 보정", StatType.MaxAction, m_settings.ProbeEnemyAction));
                enemy.Movement.AddActionProgress(before - enemy.Stats.GetMoveRange().Action);
            }
        }

        private void ProbeStep(TurnManager turns)
        {
            int wave = WaveManager.Instance.CurrentWave;
            if (wave == 1 && m_handledWave != 1)
            {
                m_handledWave = 1;
                m_probeIds = string.Join("+", PlaceProbeTowers());
                turns.OnClickEndTurn();
                return;
            }
            if (wave < 2) return;
            if (wave <= m_settings.ProbeWaves)
            {
                if (m_handledWave == wave) return;
                m_handledWave = wave;
                turns.OnClickEndTurn();
                return;
            }

            int spawned = WaveTable.GetNormalCount(1);
            int alive = 0;
            foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
            {
                if (enemy.SpawnWave == 1) alive++;
            }
            int killed = Mathf.Max(0, spawned - alive);
            float rate = spawned > 0 ? (float)killed / spawned : 0f;
            m_probeResults.Add((m_probeIds, rate));
            m_probeLines.Add($"{m_run},{m_seed},{(GameMode)m_settings.GetMode(m_run)},{m_settings.ProbeTowers},{m_probeIds},{spawned},{killed},{rate:0.###}");
            Finish("Probe", "");
        }

        private List<int> PlaceProbeTowers()
        {
            var random = new System.Random(unchecked(m_seed * 7919 + 17));
            var ids = new List<int>();
            BoardManager board = BoardManager.Instance;
            TowerManager towers = TowerManager.Instance;
            bool fixedId = m_settings.ProbeIds != null && m_settings.ProbeIds.Length > 0;
            if (fixedId && GameSession.Instance != null) GameSession.Instance.SetLimitModifier("시뮬레이션 보정", 10000);

            for (int i = 0; i < m_settings.ProbeTowers; i++)
            {
                TowerColor color = random.Next(2) == 0 ? TowerColor.Red : TowerColor.Blue;
                var emblem = (TowerEmblem)random.Next((int)TowerEmblem.Sword, (int)TowerEmblem.Darkness + 1);
                int id = fixedId ? m_settings.ProbeIds[m_run % m_settings.ProbeIds.Length] : TowerId.Make(1, color, emblem);
                color = TowerId.GetColor(id);
                if (!towers.Database.TryGet(id, out TowerDefinition definition)) continue;

                TowerController tower = towers.SpawnForDebug(definition);
                if (tower == null) continue;

                bool splash = color == TowerColor.Red;
                float power = splash ? m_settings.ProbeSplashPower : m_settings.ProbeSinglePower;
                int action = splash ? m_settings.ProbeSplashAction : m_settings.ProbeSingleAction;
                if (!Mathf.Approximately(power, 1f)) tower.State.Modifiers.Add(StatModifier.Percent("시뮬레이션 보정", StatType.Power, power - 1f));
                if (action != 0) tower.State.Modifiers.Add(StatModifier.Flat("시뮬레이션 보정", StatType.MaxAction, action));
                if (splash && m_settings.ProbeSplashMastery != 0) tower.State.Modifiers.Add(StatModifier.Flat("시뮬레이션 보정", StatType.Mastery, m_settings.ProbeSplashMastery));
                if (m_settings.ProbeRange != 0) tower.State.Modifiers.Add(StatModifier.Flat("시뮬레이션 보정", StatType.Range, m_settings.ProbeRange));

                int best = BestProbeSlot(board, tower);
                if (best >= 0 && best != tower.SlotIndex) towers.TryMoveOrSwap(tower, best);
                ids.Add(definition.Id);
            }
            return ids;
        }

        private static int BestProbeSlot(BoardManager board, TowerController tower)
        {
            int best = -1, bestCover = -1;
            for (int i = 0; i < board.Grid.SpawnCount; i++)
            {
                TowerController here = board.Slots.Get(i);
                if (here != null && here != tower) continue;

                int cover = board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(i), tower.Stats.Range).Count;
                if (cover > bestCover)
                {
                    bestCover = cover;
                    best = i;
                }
            }
            return best;
        }

        private void WriteProbeSummary()
        {
            if (m_probeLines.Count == 0) return;
            Directory.CreateDirectory(SimulationSettings.Folder);
            string name = m_settings.ResultName;
            File.WriteAllText(Path.Combine(SimulationSettings.Folder, name + "_probe.csv"),
                "run,seed,mode,towers,ids,spawned,killed,rate\n" + string.Join("\n", m_probeLines) + "\n");

            var text = new System.Text.StringBuilder();
            float sum = 0f;
            int over80 = 0, over50 = 0;
            foreach ((string _, float rate) in m_probeResults)
            {
                sum += rate;
                if (rate >= 0.8f - 0.0001f) over80++;
                if (rate >= 0.5f - 0.0001f) over50++;
            }
            int count = m_probeResults.Count;
            text.AppendLine($"1웨이브 적 섬멸률 ({m_settings.ProbeWaves}웨이브 종료까지) — {(GameMode)m_settings.Mode}, 브론즈·1레벨 공격 타워 {m_settings.ProbeTowers}기, {count}판");
            text.AppendLine($"보정: 광역 Power ×{m_settings.ProbeSplashPower:0.##} Action {m_settings.ProbeSplashAction:+0;-0;0}, 단일 Power ×{m_settings.ProbeSinglePower:0.##} Action {m_settings.ProbeSingleAction:+0;-0;0}, 적 Action {m_settings.ProbeEnemyAction:+0;-0;0}, 광역 Mastery {m_settings.ProbeSplashMastery:+0;-0;0}, Range {m_settings.ProbeRange:+0;-0;0}");
            text.AppendLine($"평균 {sum / count * 100f:0.#}%, 80% 이상 {over80 * 100f / count:0.#}%판, 50% 이상 {over50 * 100f / count:0.#}%판");

            var byMix = new Dictionary<string, (float sum, int n, int over80)>();
            foreach ((string ids, float rate) in m_probeResults)
            {
                int reds = 0, blues = 0;
                foreach (string part in ids.Split('+'))
                {
                    if (!int.TryParse(part, out int id)) continue;
                    if (TowerId.GetColor(id) == TowerColor.Red) reds++; else blues++;
                }
                string key = $"광역 {reds} + 단일 {blues}";
                byMix.TryGetValue(key, out var acc);
                byMix[key] = (acc.sum + rate, acc.n + 1, acc.over80 + (rate >= 0.8f - 0.0001f ? 1 : 0));
            }
            text.AppendLine("조합별");
            foreach (var pair in byMix)
                text.AppendLine($"  {pair.Key}: 평균 {pair.Value.sum / pair.Value.n * 100f:0.#}%, 80% 이상 {pair.Value.over80 * 100f / pair.Value.n:0.#}%판 ({pair.Value.n}판)");

            var byTower = new Dictionary<int, (float sum, int n)>();
            foreach ((string ids, float rate) in m_probeResults)
            {
                foreach (string part in ids.Split('+'))
                {
                    if (!int.TryParse(part, out int id)) continue;
                    byTower.TryGetValue(id, out var acc);
                    byTower[id] = (acc.sum + rate, acc.n + 1);
                }
            }
            var order = new List<int>(byTower.Keys);
            order.Sort((a, b) => (byTower[b].sum / byTower[b].n).CompareTo(byTower[a].sum / byTower[a].n));
            text.AppendLine("타워별 (그 타워가 들어간 판의 평균)");
            foreach (int id in order)
                text.AppendLine($"  {id} {EmblemNames.GetConceptName(id)}: {byTower[id].sum / byTower[id].n * 100f:0.#}% ({byTower[id].n}판)");

            File.WriteAllText(Path.Combine(SimulationSettings.Folder, name + "_probe_summary.txt"), text.ToString());
        }

        private void WriteBossLine(string result)
        {
            EnemyManager enemies = EnemyManager.Instance;
            if (enemies == null || WaveManager.Instance == null) return;
            string path = Path.Combine(SimulationSettings.Folder, m_settings.ResultName + "_boss.csv");
            if (!File.Exists(path)) File.WriteAllText(path, "seed,result,wave,kind,spawn_wave,hp_left,max_hp,left_ratio\n");
            var line = new System.Text.StringBuilder();
            foreach (EnemyController enemy in enemies.Registry.Alive)
            {
                if (enemy == null || !enemy.IsAlive || !enemy.IsBoss) continue;
                line.Append(FormattableString.Invariant($"{m_seed},{result},{WaveManager.Instance.CurrentWave},{enemy.Definition.Kind},{enemy.SpawnWave},{enemy.CurrentHp:0.#},{enemy.MaxHp:0.#},{enemy.CurrentHp / enemy.MaxHp:0.###}\n"));
            }
            if (line.Length == 0) line.Append($"{m_seed},{result},{WaveManager.Instance.CurrentWave},None,0,0,0,0\n");
            File.AppendAllText(path, line.ToString());

            if (line.ToString().Contains(",None,") || m_bossTrace.Length == 0) return;
            string tracePath = Path.Combine(SimulationSettings.Folder, m_settings.ResultName + "_bosstrace.csv");
            if (!File.Exists(tracePath)) File.WriteAllText(tracePath, "seed,wave,turn,boss_path,boss_hp,move,towers(slot:id:distance/range/remaining_action)\n");
            File.AppendAllText(tracePath, m_bossTrace.ToString());
        }

        private readonly System.Text.StringBuilder m_bossTrace = new System.Text.StringBuilder();

        private void TraceBoss(int turn)
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || EnemyManager.Instance == null) return;
            foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
            {
                if (enemy == null || !enemy.IsAlive || !enemy.IsBoss) continue;
                Vector3Int cell = board.Grid.GetPathCell(enemy.PathIndex);
                var towers = new List<string>();
                for (int i = 0; i < board.Slots.Count; i++)
                {
                    TowerController tower = board.Slots.Get(i);
                    if (tower == null || !tower.IsAttackTower) continue;
                    int distance = BoardGrid.GetTileDistance(board.Grid.GetSpawnCell(i), cell);
                    towers.Add($"{i}:{tower.Definition.Id}:{distance}/{tower.Stats.Range}/{tower.State.RemainingAction}");
                }
                MoveRange move = enemy.Stats.GetMoveRange();
                bool occupied = false;
                foreach (ITarget target in board.Occupancy.GetAt(enemy.PathIndex)) if (ReferenceEquals(target.Body, enemy)) occupied = true;
                var effects = new List<string>();
                foreach (StatusEffect effect in enemy.Status.Effects) effects.Add(effect.Key);
                float probe = enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f, Source = DamageSource.Basic });
                m_bossTrace.Append(FormattableString.Invariant(
                    $"{m_seed},{WaveManager.Instance.CurrentWave},{turn},{enemy.PathIndex},{enemy.CurrentHp:0.#},{move.Min}~{move.Max}/{move.Action},{string.Join("|", towers)},occ={occupied} taken={enemy.Stats.Get(StatType.DamageTaken):0.##} def={enemy.Stats.Get(StatType.Defense):0.#} probe100={probe:0.#} fx={string.Join("+", effects)}\n"));
            }
        }

        private bool PassedStopWave()
        {
            if (m_settings.StopWave <= 0) return false;
            int wave = WaveManager.Instance.CurrentWave;
            if (wave <= m_settings.StopWave) return false;
            if (wave > m_settings.StopWave + 1) return true;
            EnemyManager enemies = EnemyManager.Instance;
            return enemies == null || enemies.BossTimer.Count == 0;
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
            WriteBossLine(result);
            BalanceRecorder.EndGame(result, reason);
            bool searchNext = m_search == null || m_search.FinishGame(result, WaveManager.Instance.CurrentWave,
                BalanceRecorder.EnemyTurns, BalanceRecorder.Kills);

            m_run++;
            m_settings.Finished = m_run;
            if (m_search != null ? !searchNext : m_run >= m_settings.TotalRuns)
            {
                m_settings.Done = true;
                m_settings.Save();
                if (m_settings.WaveProbe) WriteProbeSummary();
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
            m_seed = m_settings.GetSeed(m_run);
            CurrentSeed = m_seed;
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
