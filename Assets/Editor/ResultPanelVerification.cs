using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class ResultPanelVerification
    {
        private const string Key = "SquareTD.ResultPanelVerification", Folder = ResultPanelSetup.Output;
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), Existing = new List<string>();
        private static double s_next;
        private static TowerController s_first;
        private static BattleResults.Snapshot s_loss;
        private static string s_deck;
        [Serializable] private class Scenes { public SceneSetup[] values; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors; }
        static ResultPanelVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
        [MenuItem("Tools/SquareTD/승패 결과 패널 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("플레이를 종료하고 씬을 저장해 주세요.");
            SimulationSettings settings = SimulationSettings.Load();
            if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("진행 중인 밸런스 시뮬레이션을 보호하기 위해 검증하지 않았습니다.");
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new Scenes { values = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + ".background", Application.runInBackground); SessionState.SetBool(Key + ".listenerCaptured", false);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 180);
            Checks.Clear(); Errors.Clear(); Existing.Clear(); SessionState.SetInt(Key, 1);
            EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity", OpenSceneMode.Single); EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
        }
        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) Existing.Add(text); else Errors.Add(text);
        }
        private static void Check(bool valid, string label) { if (!valid) throw new InvalidOperationException(label); Checks.Add(label); }
        private static void Next(int step, double delay = .5) { SessionState.SetInt(Key, step); s_next = EditorApplication.timeSinceStartup + delay; }
        private static TowerController Spawn(int id) => TowerManager.Instance.SpawnForDebug(TowerManager.Instance.Database.Get(id));
        private static DamageInfo Hit(TowerController attacker, float amount, DamageSource source = DamageSource.Basic) => new DamageInfo { Attacker = attacker, BaseDamage = amount, ForceFixed = true, Source = source, Emblem = attacker.Definition.Emblem };
        private static BattleResultPanel View<T>() where T : Component => Object.FindAnyObjectByType<T>().GetComponent<BattleResultPanel>();
        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0); if (step == 0 || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(Key, 0); Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + ".scenes", ""));
                    if (scenes?.values != null) EditorSceneManager.RestoreSceneManagerSetup(scenes.values); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("승패 결과 검증 시간 초과: " + step);
                if (!EditorApplication.isPlaying) return;
                Application.runInBackground = true; EditorApplication.isPaused = false;
                if (step == 1)
                {
                    if (!(TurnManager.Instance?.Current is PlayerTurnState)) return;
                    SessionState.SetFloat(Key + ".listener", AudioListener.volume); SessionState.SetBool(Key + ".listenerCaptured", true); AudioListener.volume = 0;
                    s_first = Spawn(1101); TowerController second = Spawn(2202), bench = Spawn(3303);
                    Check(TowerManager.Instance.TryMoveTo(bench, TowerPlace.Bench, 0), "대기석 기물 준비");
                    EnemyController enemy = EnemyManager.Instance.SpawnForDebug(1000);
                    enemy.TakeDamage(Hit(s_first, 125)); enemy.TakeDamage(Hit(s_first, 25, DamageSource.Dot));
                    File.WriteAllText(Folder + "/damage-state.txt", $"HP={enemy.CurrentHp}, recorded={GameSession.Instance.Results.DamageFor(s_first)}, frozen={GameSession.Instance.Results.Final != null}, over={GameSession.Instance.IsOver}, simulation={SimulationRunner.IsOn}");
                    Check(Math.Abs(GameSession.Instance.Results.DamageFor(s_first) - 150) < .01, "기본·지속 피해 실제 체력 기준 누적");
                    enemy.TakeDamage(Hit(second, 80, DamageSource.Skill));
                    enemy.Execute(Hit(second, 0, DamageSource.Skill));
                    Check(Math.Abs(GameSession.Instance.Results.DamageFor(second) - 850) < .01, "스킬·처형의 남은 체력 기록");
                    EnemyController overkill = EnemyManager.Instance.SpawnForDebug(12); overkill.TakeDamage(Hit(second, 900));
                    Check(Math.Abs(GameSession.Instance.Results.DamageFor(second) - 862) < .01, "과잉 피해 제외");
                    overkill.TakeDamage(Hit(second, 900));
                    Check(Math.Abs(GameSession.Instance.Results.DamageFor(second) - 862) < .01, "사망한 적의 중복 피해 제외");
                    var ledger = new BattleResults(); ledger.Record(Hit(s_first, 10), 10); ledger.Record(Hit(second, 20), 20);
                    ledger.Merge(s_first, new[] { second }, bench); ledger.Record(Hit(s_first, 7), 7);
                    Check(Math.Abs(ledger.DamageFor(bench) - 37) < .01, "합성 계보와 이전 시전자 지속 피해 귀속");
                    TowerController material1 = Spawn(1101), material2 = Spawn(1101);
                    EnemyController target = EnemyManager.Instance.SpawnForDebug(500); target.TakeDamage(Hit(material1, 25)); target.TakeDamage(Hit(material2, 35));
                    Next(2, 2.5); return;
                }
                if (step == 2)
                {
                    if (TowerManager.Instance.IsMerging) return;
                    TowerController merged = BoardManager.Instance.GetBoardAndBenchTowers().FirstOrDefault(t => t.Definition.Id == 2101);
                    Check(merged != null && Math.Abs(GameSession.Instance.Results.DamageFor(merged) - 210) < .01, "실제 자동 합성 후 누적 피해 계승");
                    s_deck = string.Join(",", TowerManager.Instance.SelectedEmblems);
                    GameSession.Instance.GameOver("결과 화면 검증"); s_loss = GameSession.Instance.Results.Final; Next(3); return;
                }
                if (step == 3)
                {
                    BattleResultPanel view = View<GameOverPanel>();
                    Check(view != null && view.Data == s_loss && !s_loss.Won, "패배 패널 자동 표시와 종료 스냅샷 연결");
                    Check(s_loss.Rows.Any(row => row.Location.StartsWith("대기석")) && s_loss.Rows.Any(row => row.Location.StartsWith("시너지")), "판·대기석·시너지 보유 기물 포함");
                    Check(view.Rows.Count == s_loss.Rows.Count && view.Rows.All(row => row.Data.Portrait != null), "모든 보유 기물 Face 연결");
                    Check(view.Rows.Any(row => row.Data.Tier == 3) && view.Rows.Any(row => row.Data.Tier == 2), "종료 시점 개별 티어 표시");
                    double before = s_loss.TotalDamage; GameSession.Instance.Results.Record(new DamageInfo { Attacker = s_first }, 999);
                    Check(s_loss.TotalDamage == before && ReferenceEquals(GameSession.Instance.Results.Capture(true), s_loss), "종료 후 피해·승패 기록 고정");
                    Check(view.Rows[0].Data.Damage >= view.Rows[view.Rows.Count - 1].Data.Damage, "피해량 내림차순 정렬");
                    Check(view.PrimaryButton.GetComponentInChildren<TextMeshProUGUI>().text == "다시하기", "패배 다시하기 버튼");
                    Capture(view, "defeat-1920.png", 1920, 1080); Capture(view, "defeat-16x10.png", 1728, 1080);
                    view.PrimaryButton.onClick.Invoke(); view.PrimaryButton.onClick.Invoke();
                    Check(SceneFlow.PendingScene == "3. Stage1" && !view.PrimaryButton.interactable, "다시하기 연결·중복 입력 차단"); Next(4, .1); return;
                }
                if (step == 4)
                {
                    if (SceneManager.GetActiveScene().name != "3. Stage1" || !(TurnManager.Instance?.Current is PlayerTurnState)) return;
                    Check(GameSession.Instance.Results.Final == null && BoardManager.Instance.GetBoardAndBenchTowers().Count == 0, "재시작 후 결과·기물 초기화");
                    Check(string.Join(",", TowerManager.Instance.SelectedEmblems) == s_deck, "다시하기 덱 유지");
                    TowerController tower = Spawn(4104); GameSession.Instance.Results.Record(Hit(tower, 12345), 12345);
                    GameSession.Instance.GameClear(); Next(5); return;
                }
                if (step == 5)
                {
                    BattleResultPanel view = View<GameClearPanel>(); Check(view != null && view.Data.Won, "승리 패널 자동 표시");
                    Check(view.PrimaryButton.interactable && view.PrimaryButton.GetComponentInChildren<TextMeshProUGUI>().text == "다음 스테이지", "다음 스테이지 버튼 활성화");
                    Capture(view, "victory-1920.png", 1920, 1080);
                    BattleResults.Snapshot original = view.Data;
                    view.Display(new BattleResults.Snapshot(true, "아스가르드", "NORMAL", 10, new List<BattleResults.Row>()));
                    Check(view.Rows.Count == 0 && !double.IsNaN(view.Data.TotalDamage), "보유 기물 0기 결과 처리");
                    view.Display(original); view.PrimaryButton.onClick.Invoke();
                    Check(SceneFlow.PendingScene == "4. Stage2" && StartSettings.Stage.Number == 2, "Stage2 목적지와 StageDefinition 함께 전환"); Next(6, .1); return;
                }
                if (step == 6)
                {
                    if (SceneManager.GetActiveScene().name != "4. Stage2" || !(TurnManager.Instance?.Current is PlayerTurnState)) return;
                    Check(GameSession.Instance.Stage.Number == 2 && GameSession.Instance.Results.Final == null, "다음 스테이지 실제 진입·결과 초기화");
                    Check(string.Join(",", TowerManager.Instance.SelectedEmblems) == s_deck, "다음 스테이지 덱 유지");
                    GameSession.Instance.GameClear(); Next(7); return;
                }
                if (step == 7)
                {
                    BattleResultPanel view = View<GameClearPanel>();
                    Check(view.Data.Rows.All(row => row.Damage == 0) && view.Rows.All(row => !float.IsNaN(row.GetComponentsInChildren<Image>().First(image => image.name == "DamageFill").fillAmount)), "피해 0인 기물·비중 정상 표시");
                    view.StageSelectButton.onClick.Invoke(); Check(SceneFlow.PendingScene == "2. Menu", "스테이지 선택 버튼 연결"); Next(8, .1); return;
                }
                if (step == 8 && SceneManager.GetActiveScene().name == "2. Menu")
                {
                    Check(Object.FindAnyObjectByType<StageSelectPanel>() != null && SceneFlow.PendingScene == null, "스테이지 선택 화면 실제 복귀");
                    Check(Errors.Count == 0, "새 결과 패널 오류 없음");
                    File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = Existing.ToArray() }, true)); Finish();
                }
            }
            catch (Exception exception) { File.WriteAllText(Folder + "/verification-error.txt", exception.ToString()); Finish(); }
        }
        private static void Finish()
        {
            if (SessionState.GetBool(Key + ".listenerCaptured", false)) AudioListener.volume = SessionState.GetFloat(Key + ".listener", 1);
            Application.runInBackground = SessionState.GetBool(Key + ".background", false); Time.timeScale = 1;
            SessionState.SetInt(Key, 100); EditorApplication.isPlaying = false;
        }
        private static void Capture(BattleResultPanel view, string name, int width, int height)
        {
            Canvas canvas = view.GetComponentInParent<Canvas>(); RenderMode mode = canvas.renderMode; Camera previousCamera = canvas.worldCamera; float plane = canvas.planeDistance;
            var cameraRoot = new GameObject("Result Capture", typeof(Camera)); Camera camera = cameraRoot.GetComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(12, 17, 29, 255); camera.cullingMask = 1 << 5; camera.orthographic = true;
            var target = new RenderTexture(width, height, 24); target.Create(); RenderTexture previous = RenderTexture.active;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; camera.targetTexture = target;
                Canvas.ForceUpdateCanvases(); camera.Render();
                foreach (TextMeshProUGUI text in view.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.ForceMeshUpdate(); Check(!text.isTextOverflowing, "텍스트 영역: " + name + "/" + text.name);
                }
                RenderTexture.active = target; var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); File.WriteAllBytes(Folder + "/" + name, texture.EncodeToPNG()); Object.Destroy(texture);
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = previousCamera; canvas.planeDistance = plane;
                RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Object.Destroy(target); Object.Destroy(cameraRoot); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
