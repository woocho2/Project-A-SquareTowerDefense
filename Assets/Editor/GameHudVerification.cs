using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class GameHudVerification
    {
        private const string Key = "SquareTD.GameHudVerification", Folder = GameHudSetup.Folder;
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), Existing = new List<string>();
        private static double s_next;
        private static GameInfoPanel s_info;
        private static PlayerTurnPanel s_actions;
        private static TowerController s_tower;
        private static int s_beforeGold;
        [Serializable] private class Scenes { public SceneSetup[] values; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors; }
        static GameHudVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
        [MenuItem("Tools/SquareTD/게임 HUD 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("플레이를 종료하고 씬을 저장해 주세요.");
            SimulationSettings settings = SimulationSettings.Load(); if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("밸런스 시뮬레이션이 진행 중입니다.");
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new Scenes { values = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + ".background", Application.runInBackground); SessionState.SetBool(Key + ".listenerCaptured", false);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 180);
            Checks.Clear(); Errors.Clear(); Existing.Clear(); SessionState.SetInt(Key, 1);
            EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity", OpenSceneMode.Single);
            Application.runInBackground = true; EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
        }
        private static T Field<T>(Object source, string key) where T : Object => new SerializedObject(source).FindProperty(key).objectReferenceValue as T;
        private static void Check(bool valid, string label) { if (!valid) throw new InvalidOperationException(label); Checks.Add(label); }
        private static void Next(int step, double delay = .4) { SessionState.SetInt(Key, step); s_next = EditorApplication.timeSinceStartup + delay; }
        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) Existing.Add(text); else Errors.Add(text);
        }
        private static void Click(Button button)
        {
            Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)button.transform; Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = hits.Select(hit => ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(go => go != null);
            Check(target == button.gameObject, "실제 클릭 위치: " + button.name); ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }
        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0); if (step == 0 || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate(); if (EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(Key, 0); Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + ".scenes", ""));
                    if (scenes?.values != null) EditorSceneManager.RestoreSceneManagerSetup(scenes.values); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("게임 HUD 검증 시간 초과: " + step);
                if (!EditorApplication.isPlaying) return;
                Application.runInBackground = true; EditorApplication.isPaused = false;
                if (step == 1)
                {
                    if (!(TurnManager.Instance?.Current is PlayerTurnState)) return;
                    SessionState.SetFloat(Key + ".listener", AudioListener.volume); SessionState.SetBool(Key + ".listenerCaptured", true); AudioListener.volume = 0;
                    s_info = Object.FindAnyObjectByType<GameInfoPanel>(); s_actions = Object.FindAnyObjectByType<PlayerTurnPanel>();
                    Check(s_info != null && s_actions != null, "기존 HUD 컨트롤러 연결 유지");
                    Check(!s_info.GetComponentsInChildren<Transform>(true).Concat(s_actions.GetComponentsInChildren<Transform>(true)).Any(item => item.name.IndexOf("Gem", StringComparison.OrdinalIgnoreCase) >= 0), "상단·생성/경험치 영역의 젬 UI 제거");
                    Check(Field<TextMeshProUGUI>(s_info, "txt_playerLevel").text == "Lv. 1" && Field<TextMeshProUGUI>(s_info, "txt_playerXp").text == "0 / 20 EXP", "플레이어 레벨·경험치 초기 표시");
                    Check(Field<TextMeshProUGUI>(s_info, "txt_stage").text == GameSession.Instance.Stage.DisplayName, "스테이지 표시 이름 연결");
                    Check(Field<TextMeshProUGUI>(s_info, "txt_currentWave").text == "WAVE 1", "현재 웨이브 연결");
                    Check(Field<TextMeshProUGUI>(s_actions, "txt_createTowerValue").text == TowerManager.FixedBuildCost.ToString() && Field<TextMeshProUGUI>(s_actions, "txt_buyXpCost").text == BalanceTables.XpPurchaseCost.ToString(), "생성·경험치 실제 골드 비용 표시");
                    Capture("hud-initial-1920.png", 1920, 1080); Capture("hud-initial-16x10.png", 1728, 1080);
                    Next(11); return;
                }
                if (step == 11)
                {
                    s_beforeGold = CurrencyManager.Instance.Gold; Click(Field<Button>(s_actions, "btn_createTower"));
                    Check(CurrencyManager.Instance.Gold == s_beforeGold - TowerManager.FixedBuildCost && TowerManager.Instance.BuildCount == 1, "생성 버튼 실제 소환·골드 차감");
                    s_tower = BoardManager.Instance.GetBoardAndBenchTowers().Single();
                    s_beforeGold = CurrencyManager.Instance.Gold; Click(Field<Button>(s_actions, "btn_buyXp"));
                    Check(TowerManager.Instance.ColorLevels.Xp == 1 && CurrencyManager.Instance.Gold == s_beforeGold - BalanceTables.XpPurchaseCost, "EXP 버튼 실제 구매·경험치 증가");
                    Click(Field<Button>(s_actions, "btn_buyXp")); Next(2); return;
                }
                if (step == 2)
                {
                    Check(Field<TextMeshProUGUI>(s_info, "txt_playerXp").text == "2 / 20 EXP" && Mathf.Abs(Field<Image>(s_info, "img_playerXpFill").fillAmount - .1f) < .001f, "경험치 숫자·진행률 동기화");
                    Check(!Field<Button>(s_actions, "btn_createTower").interactable && !Field<Button>(s_actions, "btn_buyXp").interactable, "골드 부족 시 두 버튼 비활성화");
                    Check(Field<TextMeshProUGUI>(s_info, "txt_currentGold").text == CurrencyManager.Instance.Gold.ToString("N0"), "보유 골드 즉시 갱신");
                    CurrencyManager.Instance.AddGold(10000);
                    int remaining = TowerManager.Instance.ColorLevels.XpToNext - TowerManager.Instance.ColorLevels.Xp;
                    for (int i = 0; i < remaining; i++) Field<Button>(s_actions, "btn_buyXp").onClick.Invoke();
                    Next(3); return;
                }
                if (step == 3)
                {
                    Check(TowerManager.Instance.ColorLevels.Level == 2 && Field<TextMeshProUGUI>(s_info, "txt_playerLevel").text == "Lv. 2", "실제 경험치 구매로 플레이어 레벨업");
                    Check(s_tower.Stats.ColorLevel == 2 && new[] { TowerColor.Red, TowerColor.Blue, TowerColor.Yellow, TowerColor.Purple }.All(color => TowerManager.Instance.ColorLevels.GetLevel(color) == 2), "모든 타워 공통 강화 레벨 연결 유지");
                    Check(Field<TextMeshProUGUI>(s_info, "txt_playerXp").text == "0 / 40 EXP" && Field<Image>(s_info, "img_playerXpFill").fillAmount < .001f, "다음 레벨 경험치·바 초기화");
                    EnemyController enemy = EnemyManager.Instance.SpawnForDebug(1000);
                    Check(Field<TextMeshProUGUI>(s_info, "txt_currentLimit").text == $"{EnemyManager.Instance.Registry.Count} / {GameSession.Instance.CurrentLimit}", "LIFE의 적 수·한도 갱신");
                    Click(Field<Button>(s_info, "btn_gameSpeed")); Check(Time.timeScale == 2, "배속 ×2");
                    Click(Field<Button>(s_info, "btn_gameSpeed")); Check(Time.timeScale == 3, "배속 ×3");
                    Click(Field<Button>(s_info, "btn_gameSpeed")); Check(Time.timeScale == 1, "배속 ×1 복귀");
                    Click(Field<Button>(s_info, "btn_option")); Check(UIManager.Instance.IsPaused && Time.timeScale == 0, "설정 버튼·일시정지 연결 유지");
                    UIManager.Instance.CloseOptionPanel(); Check(!UIManager.Instance.IsPaused && Time.timeScale == 1, "옵션 복귀");
                    Capture("hud-level2-1920.png", 1920, 1080);
                    TowerManager.Instance.ColorLevels.AddXp(100000); Next(4); return;
                }
                if (step == 4)
                {
                    Check(Field<TextMeshProUGUI>(s_info, "txt_playerLevel").text == "Lv. 5" && Field<TextMeshProUGUI>(s_info, "txt_playerXp").text == "최대 레벨", "최대 플레이어 레벨 표시");
                    Check(!Field<Button>(s_actions, "btn_buyXp").interactable && Field<TextMeshProUGUI>(s_actions, "txt_buyXpCost").text == "MAX" && Field<Image>(s_info, "img_playerXpFill").fillAmount == 1, "최대 레벨 투자 비활성·진행률 100%");
                    Capture("hud-max-1920.png", 1920, 1080);
                    Click(Field<Button>(s_actions, "btn_turnEnd")); Next(5, .05); return;
                }
                if (step == 5)
                {
                    if (!(TurnManager.Instance.Current is EnemyTurnState)) return;
                    Check(!s_actions.gameObject.activeInHierarchy && s_info.gameObject.activeInHierarchy && Field<TextMeshProUGUI>(s_info, "txt_playerLevel").gameObject.activeInHierarchy, "적 턴에도 레벨·EXP 유지, 행동 버튼만 숨김");
                    Check(Errors.Count == 0, "새 HUD 오류 없음");
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
        private static Rect Bounds(RectTransform rect, Camera camera)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, points[0]), b = RectTransformUtility.WorldToScreenPoint(camera, points[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
        private static void Capture(string name, int width, int height)
        {
            Camera camera = Camera.main; Canvas[] canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            RenderTexture originalTarget = camera.targetTexture, original = RenderTexture.active;
            var target = new RenderTexture(width, height, 24); target.Create();
            try
            {
                camera.targetTexture = target;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; }
                Canvas.ForceUpdateCanvases(); camera.Render();
                foreach (TextMeshProUGUI text in s_info.GetComponentsInChildren<TextMeshProUGUI>().Concat(s_actions.GetComponentsInChildren<TextMeshProUGUI>()))
                { text.ForceMeshUpdate(); if (text.isTextOverflowing) throw new InvalidOperationException("HUD 텍스트 넘침: " + name + "/" + text.name + "/" + text.text); }
                Check(true, "HUD 텍스트 영역: " + name);
                RenderTexture.active = target; var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(Folder + "/" + name, image.EncodeToPNG()); Object.Destroy(image);
                Rect create = Bounds((RectTransform)Field<Button>(s_actions, "btn_createTower").transform, camera), xp = Bounds((RectTransform)Field<Button>(s_actions, "btn_buyXp").transform, camera);
                File.WriteAllText(Folder + "/layout-state.txt", $"{name}\ncreate={create}, xp={xp}\nactions={((RectTransform)s_actions.transform).rect}\ncanvas={s_actions.GetComponentInParent<Canvas>().pixelRect}");
                Check(create.xMin >= 0 && xp.xMax <= width && create.yMin >= 0 && xp.yMin >= 0, "하단 버튼 화면 내부: " + name);
                BoardManager board = BoardManager.Instance;
                for (int i = 0; i < board.Bench.Count; i++)
                {
                    Vector2 point = camera.WorldToScreenPoint(board.GetPlaceWorldPosition(TowerPlace.Bench, i));
                    if (create.Contains(point) || xp.Contains(point)) throw new InvalidOperationException("대기석과 행동 버튼이 겹칩니다: " + name + "/" + i);
                }
                Check(true, "대기석 칸 중심을 가리지 않음: " + name);
            }
            finally
            {
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
                camera.targetTexture = originalTarget; RenderTexture.active = original; target.Release(); Object.Destroy(target); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
