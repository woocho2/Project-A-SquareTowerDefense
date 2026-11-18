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
    public static class EmblemMenuVerification
    {
        private const string Folder = EmblemMenuSetup.Folder, Key = "SquareTD.EmblemMenuVerification";
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), Existing = new List<string>(), Overflows = new List<string>();
        private static double s_next;
        private static EmblemSelectPanel s_panel;
        private static int[] s_expected;
        private static TowerEmblem[] s_deck;
        private static int s_clickFrame;
        [Serializable] private class SceneState { public SceneSetup[] scenes; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors, textOverflows; public int selectFrame, firstLoadFrame; }
        static EmblemMenuVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
        [MenuItem("Tools/SquareTD/엠블럼 선택 화면 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 종료 후 검증해 주세요.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("열린 씬을 먼저 저장해 주세요.");
            SimulationSettings settings = SimulationSettings.Load();
            if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("밸런스 시뮬레이션이 진행 중입니다.");
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new SceneState { scenes = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetInt(Key, 1); SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 150);
            EditorSceneManager.OpenScene(EmblemMenuSetup.MenuScene, OpenSceneMode.Single); EditorApplication.isPlaying = true;
        }
        public static void CapturePreview()
        {
            if (!EditorApplication.isPlaying || s_panel == null) throw new InvalidOperationException("플레이 검증 중 미리보기를 사용해 주세요.");
            Capture("selection-ready.png");
        }
        private static T Field<T>(string key) where T : Object => new SerializedObject(s_panel).FindProperty(key).objectReferenceValue as T;
        private static void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); Checks.Add(label); }
        private static void Next(int state, double wait = .25) { SessionState.SetInt(Key, state); s_next = EditorApplication.timeSinceStartup + wait; }
        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) Existing.Add(text);
            else Errors.Add(text);
        }
        private static void Click(Transform target)
        {
            RectTransform rect = (RectTransform)target; Canvas.ForceUpdateCanvases();
            Canvas canvas = target.GetComponentInParent<Canvas>().rootCanvas;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            GameObject receiver = hits.Select(hit => ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(go => go != null);
            if (receiver != target.gameObject)
            {
                Canvas local = s_panel.GetComponent<Canvas>();
                File.WriteAllText(Folder + "/raycast-debug.txt", "Target=" + target.name + " point=" + point + " rect=" + rect.rect + " canvas=" + canvas.renderMode + " screen=" + Screen.width + "x" + Screen.height + "\nPanel active=" + s_panel.gameObject.activeInHierarchy + " canvas=" + local.isActiveAndEnabled + " sorting=" + local.sortingOrder + " graphics=" + GraphicRegistry.GetGraphicsForCanvas(local).Count + " raycaster=" + s_panel.GetComponent<GraphicRaycaster>().isActiveAndEnabled + "\n" + string.Join("\n", hits.Select(hit => hit.gameObject.name + " handler=" + ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)?.name)));
            }
            Check(receiver == target.gameObject, "실제 클릭 위치 확인: " + target.name);
            ExecuteEvents.Execute(receiver, data, ExecuteEvents.pointerClickHandler);
        }
        private static Transform Choice(int number) => s_panel.transform.Find("Window/Emblem_" + number.ToString("00"));
        private static void MatchPreview()
        {
            int[] expected = SynergyRecipes.Pick(s_panel.Selected.ToArray(), 6).Select(recipe => recipe.Id).ToArray();
            Check(s_panel.Preview.Select(recipe => recipe.Id).SequenceEqual(expected), "예상 목록이 실제 조합 우선순위와 일치");
        }
        private static void Tick()
        {
            int state = SessionState.GetInt(Key, 0);
            if (state == 0 || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (state == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SceneState restore = JsonUtility.FromJson<SceneState>(SessionState.GetString(Key + ".scenes", "")); SessionState.SetInt(Key, 0);
                    if (restore?.scenes != null) EditorSceneManager.RestoreSceneManagerSetup(restore.scenes); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("엠블럼 화면 검증 시간 초과: " + state);
                if (!EditorApplication.isPlaying) return;
                switch (state)
                {
                    case 1:
                        s_panel = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EmblemSelectPanel>(true)).FirstOrDefault();
                        if (s_panel == null) return;
                        Checks.Clear(); Errors.Clear(); Existing.Clear(); Overflows.Clear();
                        StageDefinition stage = AssetDatabase.FindAssets("t:StageDefinition").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<StageDefinition>).First(item => item.Number == 1);
                        s_panel.Open(stage, GameMode.Normal); Time.timeScale = 0; Next(2); break;
                    case 2:
                        Check(Field<TextMeshProUGUI>("txt_count").text == "0 / 6 선택", "노말 초기 선택 개수");
                        Check(!Field<Button>("btn_emblemSelect").interactable, "선택 미완료 시 SELECT 비활성화");
                        Check(!s_panel.TryStart(), "미완료 상태에서 입장 요청 거절");
                        Check(s_panel.Preview.Count == 0, "미선택 상태에 빈 목록 안내");
                        Capture("selection-empty.png"); Next(3); break;
                    case 3:
                        Click(Choice(1));
                        Check(s_panel.Selected.SequenceEqual(new[] { TowerEmblem.Sword }), "문양 선택과 선택 순서 저장");
                        Check(Choice(1).Find("Selected").gameObject.activeSelf, "선택 체크 표시");
                        Check(Field<TextMeshProUGUI>("txt_count").text == "1 / 6 선택", "클릭 후 개수 갱신"); MatchPreview();
                        for (int i = 2; i <= 6; i++) Click(Choice(i));
                        Check(s_panel.IsReady && Field<Button>("btn_emblemSelect").interactable, "6개 선택하면 입장 활성화");
                        Click(Choice(7)); Check(s_panel.Selected.Count == 6, "노말 6개 초과 방지");
                        Click(Choice(1)); Check(s_panel.Selected.Count == 5, "재클릭으로 선택 해제");
                        Click(s_panel.transform.Find("Window/Selected_1"));
                        Check(s_panel.Selected.Count == 4 && s_panel.Selected[1] == TowerEmblem.Spear, "선택 칸 클릭 해제와 순서 압축");
                        Next(4); break;
                    case 4:
                        s_panel.SetMode(GameMode.Hard);
                        for (int i = 1; i <= 14 && s_panel.Selected.Count < 8; i++) if (!s_panel.Selected.Contains((TowerEmblem)i)) Click(Choice(i));
                        Check(s_panel.RequiredCount == 8 && s_panel.Selected.Count == 8 && s_panel.IsReady, "하드 8개 선택");
                        Check(Field<TextMeshProUGUI>("txt_count").text == "8 / 8 선택", "하드 선택 개수 표시");
                        s_panel.SetMode(GameMode.Normal); Check(s_panel.Selected.Count == 6, "노말 복귀 시 앞의 6개 유지");
                        foreach (TowerEmblem emblem in s_panel.Selected.ToArray()) s_panel.ToggleEmblem(emblem);
                        foreach (TowerEmblem emblem in new[] { TowerEmblem.Sword, TowerEmblem.Shield, TowerEmblem.Electricity, TowerEmblem.Axe, TowerEmblem.Ice, TowerEmblem.Darkness }) s_panel.ToggleEmblem(emblem);
                        MatchPreview(); Next(5); break;
                    case 5:
                        Field<Button>("btn_back").onClick.Invoke(); Check(!s_panel.IsOpen, "뒤로 가기 동작");
                        StageDefinition selectedStage = AssetDatabase.FindAssets("t:StageDefinition").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<StageDefinition>).First(item => item.Number == 1);
                        s_panel.Open(selectedStage, GameMode.Normal); Check(s_panel.Selected.Count == 6, "다시 열면 선택 유지");
                        s_expected = s_panel.Preview.Select(item => item.Id).ToArray(); s_deck = s_panel.Selected.ToArray(); Next(55); break;
                    case 55:
                        AuditText(); Capture("selection-ready.png");
                        Check(Overflows.Count == 0, "선택·시너지 본문 넘침 없음"); Next(6); break;
                    case 6:
                        s_clickFrame = Time.frameCount; Click(Field<Button>("btn_emblemSelect").transform);
                        Check(Object.FindAnyObjectByType<SceneTransitionOverlay>() != null, "SELECT 클릭한 프레임에 로딩 화면 표시");
                        Check(SceneTransitionOverlay.LastLoadFrame == -1, "로딩 UI를 그리기 전에 무거운 씬 로드를 시작하지 않음");
                        Check(s_panel.IsStarting && !s_panel.TryStart(), "중복 입장 요청 방지");
                        Capture("select-immediate-loading.png"); Next(7, .01); break;
                    case 7:
                        if (SceneManager.GetActiveScene().name != SceneFlow.LoadingSceneName) return;
                        Check(SceneTransitionOverlay.LastLoadFrame >= s_clickFrame + 2, "화면 갱신 후 로딩 씬을 비동기로 읽음");
                        Check(StartSettings.Emblems.SequenceEqual(s_deck), "선택한 문양 전달"); Next(8, .05); break;
                    case 8:
                        if (SceneManager.GetActiveScene().name != "3. Stage1" || TowerManager.Instance?.Synergy == null) return;
                        Check(TowerManager.Instance.Synergy.Entries.Select(entry => entry.Recipe.Id).SequenceEqual(s_expected), "실제로 생성된 시너지 타워가 메뉴 예상 목록과 일치");
                        Check(Errors.Count == 0, "새 메뉴·로딩 오류 없음");
                        File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = Existing.ToArray(), textOverflows = Overflows.ToArray(), selectFrame = s_clickFrame, firstLoadFrame = SceneTransitionOverlay.LastLoadFrame }, true));
                        SessionState.SetInt(Key, 100); Time.timeScale = 1; EditorApplication.isPlaying = false; break;
                }
            }
            catch (Exception exception)
            {
                File.WriteAllText(Folder + "/verification-error.txt", exception.ToString());
                SessionState.SetInt(Key, 100); Time.timeScale = 1; EditorApplication.isPlaying = false;
            }
        }
        private static void AuditText()
        {
            Canvas.ForceUpdateCanvases();
            foreach (TextMeshProUGUI text in s_panel.GetComponentsInChildren<TextMeshProUGUI>()) { text.ForceMeshUpdate(); if (text.isTextOverflowing) Overflows.Add(text.name + ": " + text.text); }
        }
        private static void Capture(string name, int width = 1920, int height = 1080)
        {
            Camera camera = Camera.main; if (camera == null) throw new InvalidOperationException("캡처용 카메라가 없습니다.");
            Canvas[] canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var target = new RenderTexture(width, height, 24); target.Create(); RenderTexture previous = RenderTexture.active; RenderTexture previousCamera = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(Folder + "/" + name, image.EncodeToPNG()); Object.DestroyImmediate(image);
            }
            finally
            {
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
                camera.targetTexture = previousCamera; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
