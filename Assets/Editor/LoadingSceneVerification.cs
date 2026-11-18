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
    public static class LoadingSceneVerification
    {
        private const string Key = "SquareTD.LoadingVerification";
        private const string Folder = LoadingSceneSetup.Output;
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), ExistingErrors = new List<string>();
        private static float s_lastProgress, s_letterY;
        private static bool s_motion, s_complete;
        private static string s_deck;
        [Serializable] private class SceneState { public SceneSetup[] Scenes; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors; }

        static LoadingSceneVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }

        [MenuItem("Tools/SquareTD/로딩 화면 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 종료 후 검증해 주세요.");
            AuditAndCapture();
            SimulationSettings settings = SimulationSettings.Load();
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty) || (settings != null && settings.Active && !settings.Done))
            {
                File.WriteAllText(Folder + "/playmode-skipped.txt", "저장하지 않은 씬 변경 또는 진행 중인 밸런스 시뮬레이션을 보호하기 위해 실제 씬 전환 검증은 실행하지 않았습니다.");
                return;
            }
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new SceneState { Scenes = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + ".background", Application.runInBackground);
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 150);
            EditorSceneManager.OpenScene(LoadingSceneSetup.ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        public static void AuditAndCapture()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var rows = new List<string>();
            try
            {
                SceneManager.SetActiveScene(preview);
                GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LoadingSceneSetup.Prefab), preview);
                LoadingSceneController controller = root.GetComponent<LoadingSceneController>();
                root.GetComponent<CanvasGroup>().alpha = 1;
                var cameraObject = new GameObject("Verification Camera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.orthographic = true; camera.cullingMask = 1 << 5;
                Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                var target = new RenderTexture(1920, 1080, 24); target.Create(); camera.targetTexture = target;
                for (int emblem = 1; emblem <= 14; emblem++)
                    for (int color = 1; color <= 4; color++)
                    {
                        int id = TowerId.Make(1, (TowerColor)color, (TowerEmblem)emblem);
                        controller.ShowTower(id); Canvas.ForceUpdateCanvases(); camera.Render();
                        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                        foreach (TextMeshProUGUI text in texts)
                        {
                            text.ForceMeshUpdate();
                            if (text.isTextOverflowing) { Capture(camera, target, "overflow.png"); throw new InvalidOperationException($"타워 {id} / {text.name} 텍스트가 넘칩니다: {text.text}"); }
                        }
                        Image portrait = root.transform.Find("SafeArea/TowerSpotlight/Portrait").GetComponent<Image>();
                        if (portrait.sprite == null || !portrait.sprite.name.EndsWith("_Full")) throw new InvalidOperationException("Full 스프라이트 누락: " + id);
                        rows.Add(id + " | " + controller.TowerName + " | 설명·전신·넘침 확인");
                    }
                controller.ShowTower(1101); root.GetComponentInChildren<Slider>().value = .64f;
                root.transform.Find("SafeArea/ProgressArea/Percent").GetComponent<TextMeshProUGUI>().text = "64%";
                Capture(camera, target, "loading-sword.png");
                controller.ShowTower(1301); Capture(camera, target, "loading-buff.png");
                camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target);
                var targetWide = new RenderTexture(1728, 1080, 24); targetWide.Create(); camera.targetTexture = targetWide;
                controller.ShowTower(1101); Capture(camera, targetWide, "loading-16x10.png");
                camera.targetTexture = null; targetWide.Release(); Object.DestroyImmediate(targetWide);
                File.WriteAllLines(Folder + "/file-verification.txt", rows);
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(preview, true); }
            foreach (string path in new[] { LoadingSceneSetup.ScenePath })
            {
                Scene scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    GameObject[] roots = scene.GetRootGameObjects();
                    if (roots.SelectMany(go => go.GetComponentsInChildren<LoadingSceneController>(true)).Count() != 1) throw new InvalidOperationException("로딩 컨트롤러 수 오류: " + path);
                    foreach (GameObject root in roots)
                        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0) throw new InvalidOperationException("Missing Script: " + path);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            File.AppendAllText(Folder + "/file-verification.txt", "\n단일 로딩 씬: 컨트롤러·프리팹·Missing Script 검사 통과\n");
        }

        private static void Capture(Camera camera, RenderTexture target, string name)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture original = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Folder + "/" + name, image.EncodeToPNG());
            Object.DestroyImmediate(image); RenderTexture.active = original;
        }

        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) ExistingErrors.Add(text);
            else Errors.Add(text);
        }
        private static void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); Checks.Add(text); }
        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step == 0 || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SceneState state = JsonUtility.FromJson<SceneState>(SessionState.GetString(Key + ".scenes", ""));
                    SessionState.SetInt(Key, 0);
                    if (state?.Scenes != null) EditorSceneManager.RestoreSceneManagerSetup(state.Scenes);
                    return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("로딩 씬 검증 시간 초과: " + step);
                if (!EditorApplication.isPlaying) return;
                Application.runInBackground = true; EditorApplication.isPaused = false;
                LoadingSceneController controller = Object.FindAnyObjectByType<LoadingSceneController>();
                if (step == 1 && controller != null)
                {
                    Checks.Clear(); Errors.Clear(); ExistingErrors.Clear();
                    Time.timeScale = 0;
                    Check(SceneManager.GetActiveScene().name == SceneFlow.LoadingSceneName, "공용 로딩 씬에서 메뉴 준비");
                    Check(!string.IsNullOrEmpty(controller.TowerName), "랜덤 타워 이름과 소개 표시");
                    BeginSample(controller); SessionState.SetInt(Key, 2);
                }
                else if (step == 2)
                {
                    if (controller != null) Sample(controller);
                    else if (SceneManager.GetActiveScene().name == "2. Menu")
                    {
                        Check(s_motion, "Loading 글자 개별 움직임"); Check(s_complete, "100% 표시 후 메뉴 이동");
                        Check(SceneFlow.PendingScene == null, "이동 후 목적지 요청 해제");
                        s_deck = string.Join(",", StartSettings.Emblems);
                        SceneFlow.Load("3. Stage1"); SceneFlow.Load("3. Stage1"); SessionState.SetInt(Key, 3);
                    }
                }
                else if (step == 3 && controller != null)
                {
                    Check(SceneManager.GetActiveScene().name == SceneFlow.LoadingSceneName, "스테이지 이동도 같은 로딩 씬 경유");
                    Time.timeScale = 0; BeginSample(controller); SessionState.SetInt(Key, 4);
                }
                else if (step == 4)
                {
                    if (controller != null) Sample(controller);
                    else if (SceneManager.GetActiveScene().name == "3. Stage1")
                    {
                        Check(s_complete, "100% 표시 후 스테이지 이동");
                        Check(SceneFlow.PendingScene == null, "중복 이동 없이 요청 해제");
                        Check(string.Join(",", StartSettings.Emblems) == s_deck, "선택한 문양 정보 유지");
                        Check(Time.timeScale == 0, "일시정지 상태에서도 로딩·연출 완료");
                        SceneFlow.Reload("3. Stage1"); SessionState.SetInt(Key, 5);
                    }
                }
                else if (step == 5 && controller != null)
                {
                    if (controller.Destination == null) return;
                    Check(SceneManager.GetActiveScene().name == SceneFlow.LoadingSceneName && controller.Destination == "3. Stage1", "재시작도 공용 로딩 씬 경유");
                    BeginSample(controller); SessionState.SetInt(Key, 6);
                }
                else if (step == 6)
                {
                    if (controller != null) Sample(controller);
                    else if (SceneManager.GetActiveScene().name == "3. Stage1")
                    {
                        Check(s_complete && SceneFlow.PendingScene == null, "스테이지 재시작 완료");
                        SceneFlow.Load("2. Menu"); SessionState.SetInt(Key, 7);
                    }
                }
                else if (step == 7 && controller != null)
                {
                    if (controller.Destination == null) return;
                    Check(SceneManager.GetActiveScene().name == SceneFlow.LoadingSceneName && controller.Destination == "2. Menu", "메뉴 복귀도 공용 로딩 씬 경유");
                    BeginSample(controller); SessionState.SetInt(Key, 8);
                }
                else if (step == 8)
                {
                    if (controller != null) Sample(controller);
                    else if (SceneManager.GetActiveScene().name == "2. Menu")
                    {
                        Check(s_complete && SceneFlow.PendingScene == null, "메뉴 복귀 완료");
                        Time.timeScale = 1;
                        Check(Errors.Count == 0, "새 로딩 오류 없음");
                        File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = ExistingErrors.ToArray() }, true));
                        Application.runInBackground = SessionState.GetBool(Key + ".background", false);
                        SessionState.SetInt(Key, 100); EditorApplication.isPlaying = false;
                    }
                }
            }
            catch (Exception exception)
            {
                File.WriteAllText(Folder + "/verification-error.txt", exception.ToString());
                Application.runInBackground = SessionState.GetBool(Key + ".background", false);
                SessionState.SetInt(Key, 100); Time.timeScale = 1; EditorApplication.isPlaying = false;
            }
        }

        private static void BeginSample(LoadingSceneController controller)
        {
            s_lastProgress = 0; s_motion = s_complete = false;
            s_letterY = controller.transform.Find("SafeArea/ProgressArea/Letter_0").GetComponent<RectTransform>().anchoredPosition.y;
        }
        private static void Sample(LoadingSceneController controller)
        {
            if (controller.DisplayProgress + .001f < s_lastProgress) throw new InvalidOperationException("진행률이 역행했습니다.");
            s_lastProgress = controller.DisplayProgress;
            s_complete |= s_lastProgress >= .999f;
            float y = controller.transform.Find("SafeArea/ProgressArea/Letter_0").GetComponent<RectTransform>().anchoredPosition.y;
            s_motion |= Mathf.Abs(y - s_letterY) > .3f;
        }
    }
}
