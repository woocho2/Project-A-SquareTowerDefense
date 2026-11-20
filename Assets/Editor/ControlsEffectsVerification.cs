using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class ControlsEffectsVerification
    {
        private const string KeyName = "SquareTD.ControlsEffectsVerification", Folder = ControlsEffectsSetup.Folder;
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), Existing = new List<string>();
        private static double s_next;
        private static int s_gold, s_build, s_xp, s_slot;
        private static TowerController s_tower, s_support;
        private static GlobalEffectsPanel s_effects;
        private static TMP_InputField s_input;
        private static EnemyController s_enemy, s_hel;
        private static InputSettings s_originalInput, s_testInput;
        [Serializable] private class Scenes { public SceneSetup[] values; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors; }
        static ControlsEffectsVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
        [MenuItem("Tools/SquareTD/단축키·전장 효과 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("플레이를 종료하고 씬을 저장해 주세요.");
            SimulationSettings settings = SimulationSettings.Load(); if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("밸런스 시뮬레이션이 진행 중입니다.");
            Checks.Clear(); Errors.Clear(); Existing.Clear();
            SessionState.SetString(KeyName + ".scenes", JsonUtility.ToJson(new Scenes { values = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(KeyName + ".background", Application.runInBackground); SessionState.SetBool(KeyName + ".listenerCaptured", false);
            SessionState.SetFloat(KeyName + ".deadline", (float)EditorApplication.timeSinceStartup + 240); SessionState.SetInt(KeyName, 1);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameHudSetup.Prefab); Check(prefab.GetComponentInChildren<GlobalEffectsPanel>(true) != null, "공용 HUD에 전장 효과 컴포넌트 저장");
            foreach (string scene in Directory.GetFiles("Assets/1. Scenes", "*Stage*.unity")) Check(AssetDatabase.GetDependencies(scene).Contains(GameHudSetup.Prefab), "스테이지 공용 HUD 연결: " + Path.GetFileName(scene));
            EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity", OpenSceneMode.Single);
            Application.runInBackground = true; EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
        }
        private static T Field<T>(object source, string key) => (T)source.GetType().GetField(key, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
        private static void Check(bool valid, string label) { if (!valid) throw new InvalidOperationException(label); Checks.Add(label); }
        private static void Next(int step, double delay = .3) { SessionState.SetInt(KeyName, step); s_next = EditorApplication.timeSinceStartup + delay; }
        private static void Press(Key key, int next) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key)); Next(next); }
        private static void Release() { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); }
        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(KeyName, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) Existing.Add(text); else Errors.Add(text);
        }
        private static void Tick()
        {
            int step = SessionState.GetInt(KeyName, 0); if (step == 0 || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate(); if (EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(KeyName, 0); Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(KeyName + ".scenes", ""));
                    if (scenes?.values != null) EditorSceneManager.RestoreSceneManagerSetup(scenes.values); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(KeyName + ".deadline", 0)) throw new TimeoutException("검증 시간 초과: " + step);
                if (!EditorApplication.isPlaying) return;
                Application.runInBackground = true;
                if (step == 1)
                {
                    if (!(TurnManager.Instance?.Current is PlayerTurnState)) return;
                    SessionState.SetFloat(KeyName + ".listener", AudioListener.volume); SessionState.SetBool(KeyName + ".listenerCaptured", true); AudioListener.volume = 0;
                    s_originalInput = InputSystem.settings; s_testInput = Object.Instantiate(s_originalInput); s_testInput.hideFlags = HideFlags.HideAndDontSave;
                    s_testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    s_testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    InputSystem.settings = s_testInput; InputSystem.EnableDevice(Keyboard.current);
                    foreach (DebugCheat cheat in Object.FindObjectsByType<DebugCheat>()) cheat.enabled = false;
                    Check(Object.FindObjectsByType<GameplayShortcuts>().Length == 1, "개발자 도구를 꺼도 정식 단축키 1개 활성");
                    CurrencyManager.Instance.AddGold(10000); s_gold = CurrencyManager.Instance.Gold; s_build = TowerManager.Instance.BuildCount; s_xp = TowerManager.Instance.ColorLevels.Xp;
                    s_effects = Object.FindAnyObjectByType<GlobalEffectsPanel>(); Check(s_effects != null && s_effects.StateText.Contains("초월") && s_effects.StateText.Contains("침식"), "왼쪽 상단 기본 공용 카운트 표시");
                    Press(Key.D, 2); return;
                }
                if (step == 2)
                {
                    File.WriteAllText(Folder + "/input-state.txt", $"Build={TowerManager.Instance.BuildCount}, Expected={s_build + 1}, Gold={CurrencyManager.Instance.Gold}, ExpectedGold={s_gold - TowerManager.FixedBuildCost}, keyboard={Keyboard.current.enabled}, D={Keyboard.current.dKey.isPressed}, Focus={Application.isFocused}, Capture={GameplayShortcuts.InputCaptured?.Invoke()}, State={TurnManager.Instance.Current.Name}");
                    Release(); Check(TowerManager.Instance.BuildCount == s_build + 1 && CurrencyManager.Instance.Gold == s_gold - TowerManager.FixedBuildCost, "D 실제 키 입력으로 타워 1기 생성·비용 차감");
                    s_tower = BoardManager.Instance.GetBoardAndBenchTowers().Single(); s_slot = s_tower.SlotIndex; Press(Key.F, 3); return;
                }
                if (step == 3)
                {
                    Release(); Check(TowerManager.Instance.ColorLevels.Xp == s_xp + BalanceTables.XpPerPurchase && CurrencyManager.Instance.Gold == s_gold - TowerManager.FixedBuildCost - BalanceTables.XpPurchaseCost, "F 실제 키 입력으로 경험치 1회 구매·비용 차감");
                    TowerManager.Instance.Select(s_tower); Next(4); return;
                }
                if (step == 4)
                {
                    PlayerTurnPanel actions = Object.FindAnyObjectByType<PlayerTurnPanel>(); GameInfoPanel info = Object.FindAnyObjectByType<GameInfoPanel>();
                    Check(Field<Button>(actions, "btn_createTower").transform.Find("Hint").GetComponent<TextMeshProUGUI>().text.Contains("[D]") && Field<Button>(actions, "btn_buyXp").transform.Find("Hint").GetComponent<TextMeshProUGUI>().text.Contains("[F]"), "생성·EXP UI에 D/F 표시");
                    Check(Field<TextMeshProUGUI>(info, "txt_skip").text.Contains("R"), "스킵 UI R 표시");
                    TowerInfoPanel towerPanel = Object.FindAnyObjectByType<TowerInfoPanel>(); Check(Field<Button>(towerPanel, "btn_sellTower").GetComponentsInChildren<TextMeshProUGUI>().Any(text => text.text.Contains("[E]")), "선택 타워 판매 UI E 표시");
                    Capture("shortcuts-and-counters.png", 1920, 1080); Next(40); return;
                }
                if (step == 40) { s_gold = CurrencyManager.Instance.Gold; Press(Key.E, 5); return; }
                if (step == 5)
                {
                    Release(); Check(BoardManager.Instance.Slots.Get(s_slot) == null && CurrencyManager.Instance.Gold == s_gold + TowerManager.GetSellGold(1) && UIManager.Instance.SelectedTower == null, "E 실제 키 입력으로 선택 타워 판매·환급·패널 닫기");
                    Press(Key.E, 6); return;
                }
                if (step == 6)
                {
                    Release(); Check(GameNotice.CurrentMessage.Contains("먼저 선택"), "선택 없는 E는 판매 안내"); s_build = TowerManager.Instance.BuildCount;
                    UIManager.Instance.ShowOptionPanel(); Press(Key.D, 7); return;
                }
                if (step == 7)
                {
                    Release(); Check(TowerManager.Instance.BuildCount == s_build, "옵션·일시정지 중 D 차단"); UIManager.Instance.CloseOptionPanel();
                    GameplayShortcuts.InputCaptured = () => true; Press(Key.F, 8); return;
                }
                if (step == 8)
                {
                    Release(); Check(TowerManager.Instance.ColorLevels.Xp == s_xp + BalanceTables.XpPerPurchase, "개발자 ID 입력 중 정식 단축키 차단"); GameplayShortcuts.InputCaptured = null;
                    var root = new GameObject("VerificationInput", typeof(RectTransform), typeof(Image)); root.SetActive(false); root.transform.SetParent(Object.FindAnyObjectByType<GameInfoPanel>().transform, false);
                    s_input = root.AddComponent<TMP_InputField>(); s_input.textViewport = (RectTransform)root.transform; var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(root.transform, false); s_input.textComponent = text.GetComponent<TextMeshProUGUI>(); s_input.textComponent.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-Black SDF");
                    root.SetActive(true); s_input.Select(); s_input.ActivateInputField(); Next(9, .6); return;
                }
                if (step == 9) { File.WriteAllText(Folder + "/focus-state.txt", $"Focus={s_input.isFocused}, Active={s_input.isActiveAndEnabled}, Interactable={s_input.IsInteractable()}, Font={s_input.textComponent.font}, Selected={EventSystem.current.currentSelectedGameObject}, Time={Time.timeScale}"); Check(s_input.isFocused, "텍스트 입력란 실제 포커스"); Press(Key.D, 10); return; }
                if (step == 10)
                {
                    Release(); Check(TowerManager.Instance.BuildCount == s_build, "텍스트 입력 중 D 단축키 차단"); s_input.DeactivateInputField(); EventSystem.current.SetSelectedGameObject(null); Object.Destroy(s_input.gameObject);
                    EmblemSharedState shared = GameSession.Instance.Shared; shared.AddTranscend(6.5f); shared.AddDarkness(150.75f); s_effects.Refresh();
                    Check(s_effects.StateText.Contains("7.5 / 20") && s_effects.StateText.Contains("침식: 150"), "초월 소수·침식 정수 표시 동기화"); shared.AddTranscend(20); s_effects.Refresh(); Check(s_effects.StateText.Contains("초월 예약"), "초월 달성 후 다음 웨이브 예약 표시");
                    shared.OnWaveStarted(); shared.ExtendTranscend(); shared.AddDarkness(249.25f); shared.StartGoldDouble(3); shared.AddLimitShield(); shared.AddStopTile(0, 4); GameSession.Instance.SetLimitModifier("UI verification", 2);
                    TowerManager.Instance.Database.TryGet(1102, out TowerDefinition def); s_tower = TowerManager.Instance.SpawnForDebug(def); s_tower.State.BossEffects.AddFenrir(3, 0); s_tower.State.BossEffects.AddFenrir(5, 0); s_tower.State.BossEffects.AddSurtr(4, 0);
                    s_enemy = EnemyManager.Instance.SpawnForDebug(5000); s_enemy.Status.Add(new BossSpiritBlessing(3, 0));
                    StageDefinition hel = AssetDatabase.FindAssets("t:StageDefinition").Select(guid => AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid))).First(stage => stage.Number == 9);
                    EnemyManager enemies = EnemyManager.Instance; s_hel = new EnemySpawner(BoardManager.Instance, enemies.Registry, Field<EnemySpawnEntry>(enemies, "m_normal"), Field<EnemySpawnEntry>(enemies, "m_middleBoss"), Field<EnemySpawnEntry>(enemies, "m_finalBoss"), enemies.BossTimer, hel).SpawnWave(10).Single(); s_hel.AddDeathCount(7, GameSession.Instance.HelDeathTarget);
                    s_effects.Refresh(); Next(11); return;
                }
                if (step == 11)
                {
                    string text = s_effects.StateText;
                    File.WriteAllText(Folder + "/global-effects-state.txt", text);
                    foreach (string label in new[] { "발동 중", "골드 2배: 3턴", "한도 보호: 1회", "멈춤 타일: 1칸", "적 수 한도: +2", "펜리르: +2 행동", "수르트: -20%", "적 정령 가호: +50%", "죽음 카운트: 7 / 70" }) Check(text.Contains(label), "실제 상태에 맞춘 전장 효과: " + label);
                    Capture("global-effects-active.png", 1920, 1080); Next(12); return;
                }
                if (step == 12)
                {
                    s_effects.PanelRect.GetComponent<ScrollRect>().verticalNormalizedPosition = 0; Next(13); return;
                }
                if (step == 13)
                {
                    Capture("global-effects-bosses-16x10.png", 1728, 1080);
                    EmblemSharedState shared = GameSession.Instance.Shared; for (int i = 0; i < 4; i++) { shared.CountDownGoldDouble(); shared.CountDownStopTiles(); } shared.TryUseLimitShield(); GameSession.Instance.RemoveLimitModifier("UI verification");
                    s_tower.State.BossEffects.EndTurn(100); s_enemy.Status.Remove(BossSpiritBlessing.Source); Remove(s_enemy); Remove(s_hel); TowerManager.Instance.TrySell(s_tower);
                    s_effects.Refresh(); Check(!s_effects.StateText.Contains("골드 2배") && !s_effects.StateText.Contains("펜리르") && !s_effects.StateText.Contains("수르트") && !s_effects.StateText.Contains("한도 보호") && !s_effects.StateText.Contains("죽음 카운트"), "효과 만료·보스 제거 시 행 숨김");
                    TowerManager.Instance.Database.TryGet(4301, out TowerDefinition support); s_support = TowerManager.Instance.SpawnForDebug(support); Press(Key.R, 14); return;
                }
                if (step == 14)
                {
                    Release(); Check(TurnManager.Instance.Current is PlayerTurnState && !TurnManager.Skipping && GameNotice.CurrentMessage.Contains("존"), "R 스킵도 미배치 존을 무시하지 않고 안내"); TowerManager.Instance.TrySell(s_support);
                    s_build = TowerManager.Instance.BuildCount; EventSystem.current.SetSelectedGameObject(Field<Button>(Object.FindAnyObjectByType<PlayerTurnPanel>(), "btn_createTower").gameObject);
                    Press(Key.R, 15); return;
                }
                if (step == 15)
                {
                    Release(); Check(TurnManager.Skipping && Time.timeScale == 30 && !(TurnManager.Instance.Current is PlayerTurnState), "R 실제 입력으로 적 턴 스킵 시작"); Check(TowerManager.Instance.BuildCount == s_build, "리롤 버튼이 선택된 상태의 R도 리롤 없이 스킵"); Next(16); return;
                }
                if (step == 16)
                {
                    if (!(TurnManager.Instance.Current is PlayerTurnState)) { s_next = EditorApplication.timeSinceStartup + .2; return; }
                    Check(!TurnManager.Skipping && Time.timeScale == 1 && WaveManager.Instance.CurrentWave == 2, "다음 플레이어 턴 도착·배속 자동 복구");
                    SceneFlow.Reload(SceneManager.GetActiveScene().name); Next(17); return;
                }
                if (step == 17)
                {
                    if (!(TurnManager.Instance?.Current is PlayerTurnState) || SceneManager.GetActiveScene().name.Contains("Loading")) return;
                    s_effects = Object.FindAnyObjectByType<GlobalEffectsPanel>(); if (s_effects == null || s_effects.PanelRect == null) return;
                    Check(GameSession.Instance.Shared.DarknessCount == 0 && !GameSession.Instance.Shared.IsTranscendWave && s_effects.StateText.Contains("침식: 0"), "재시작 시 공용 카운트·UI 초기화");
                    Check(Object.FindObjectsByType<GameplayShortcuts>().Length == 1, "재시작 후 단축키 중복 없음"); Check(Errors.Count == 0, "새 런타임 오류 없음");
                    File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = Existing.ToArray() }, true)); Finish();
                }
            }
            catch (Exception exception) { File.WriteAllText(Folder + "/verification-error.txt", exception.ToString()); File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = false, checks = Checks.ToArray(), errors = Errors.Concat(new[] { exception.Message }).ToArray(), existingErrors = Existing.ToArray() }, true)); Finish(); }
        }
        private static void Remove(EnemyController enemy)
        {
            BoardManager.Instance.Occupancy.Remove(enemy, enemy.PathIndex); EnemyManager.Instance.Registry.Unregister(enemy); if (enemy.IsBoss) EnemyManager.Instance.BossTimer.Remove(enemy); enemy.Pool.Release(enemy);
        }
        private static void Capture(string name, int width, int height)
        {
            Camera camera = Camera.main; Canvas[] canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            Camera[] cameras = canvases.Select(canvas => canvas.worldCamera).ToArray(); float[] distances = canvases.Select(canvas => canvas.planeDistance).ToArray(); int[] layers = canvases.Select(canvas => canvas.sortingLayerID).ToArray();
            RenderTexture previous = camera.targetTexture, active = RenderTexture.active; var target = new RenderTexture(width, height, 24); target.Create();
            try
            {
                camera.targetTexture = target; foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; canvas.sortingLayerName = "UI"; }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(Folder + "/" + name, image.EncodeToPNG()); Object.Destroy(image);
                foreach (TextMeshProUGUI text in s_effects.GetComponentsInChildren<TextMeshProUGUI>().Concat(Object.FindAnyObjectByType<PlayerTurnPanel>().GetComponentsInChildren<TextMeshProUGUI>())) { text.ForceMeshUpdate(); Check(!text.isTextOverflowing, "전장 효과·단축키 텍스트 넘침 없음: " + name + "/" + text.name); }
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; canvases[i].sortingLayerID = layers[i]; }
                camera.targetTexture = previous; RenderTexture.active = active; target.Release(); Object.Destroy(target); Canvas.ForceUpdateCanvases();
            }
        }
        private static void Finish()
        {
            Release(); GameplayShortcuts.InputCaptured = null;
            if (s_originalInput != null) InputSystem.settings = s_originalInput;
            if (s_testInput != null) Object.Destroy(s_testInput);
            if (SessionState.GetBool(KeyName + ".listenerCaptured", false)) AudioListener.volume = SessionState.GetFloat(KeyName + ".listener", 1);
            Application.runInBackground = SessionState.GetBool(KeyName + ".background", false); Time.timeScale = 1; SessionState.SetInt(KeyName, 100); EditorApplication.isPlaying = false;
        }
    }
}
