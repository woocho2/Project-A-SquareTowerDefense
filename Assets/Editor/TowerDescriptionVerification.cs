using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class TowerDescriptionVerification
    {
        private const string Folder = "ArtPreviews/SkillDescriptions_20261118";
        private const string Key = "SquareTD.DescriptionVerification";
        private static TestRunnerApi s_runner;
        private static double s_next;
        private static TowerInfoPanel s_panel;
        private static SkillDescriptionView s_view;
        private static TowerController s_sword, s_buff, s_debuff;
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), ExistingErrors = new List<string>(), Overflows = new List<string>();
        static TowerDescriptionVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }

        public static void RunBatch()
        {
            SessionState.SetBool(Key + ".batch", true);
            try { TowerDescriptionSetup.Apply(); Run(); } catch (Exception error) { Fail(error); }
        }

        [MenuItem("Tools/SquareTD/타워 설명·계산 링크 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit mode.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(index => SceneManager.GetSceneAt(index).isDirty)) throw new InvalidOperationException("Save scene edits before running the description probe.");
            SimulationSettings settings = SimulationSettings.Load();
            if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("Finish the balance simulation before running the UI probe.");
            Directory.CreateDirectory(Folder);
            foreach (string name in new[] { "verification-error.txt", "collected-errors.txt" }) if (File.Exists(Folder + "/" + name)) File.Delete(Folder + "/" + name);
            s_runner = ScriptableObject.CreateInstance<TestRunnerApi>(); s_runner.RegisterCallbacks(new TestResults());
            s_runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "SquareTD.Tests.TowerDescriptionTest" } }));
        }

        private sealed class TestResults : ICallbacks
        {
            public void RunStarted(ITestAdaptor test) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, Folder + "/editmode-results.xml");
                File.WriteAllText(Folder + "/editmode-summary.txt", $"Passed: {result.PassCount}; failed: {result.FailCount}; skipped: {result.SkipCount}");
                EditorApplication.delayCall += () => { if (s_runner != null) Object.DestroyImmediate(s_runner); s_runner = null; };
                if (result.FailCount > 0) { Fail(new InvalidOperationException("Description tests failed.")); return; }
                SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 180);
                SessionState.SetFloat(Key + ".start", (float)EditorApplication.timeSinceStartup + 4);
                SessionState.SetInt(Key, 1);
            }
        }

        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) ExistingErrors.Add(text);
            else Errors.Add(text);
        }
        private static T Field<T>(Object owner, string name) where T : Object => new SerializedObject(owner).FindProperty(name).objectReferenceValue as T;
        private static void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); Checks.Add(text); }
        private static void Next(int step, double wait = .7) { SessionState.SetInt(Key, step); s_next = EditorApplication.timeSinceStartup + wait; }
        private static void Open(TowerController tower)
        {
            UIManager.Instance.ShowTowerPanel(tower);
            s_panel.transform.Find("Front/ShowSkillDetails").GetComponent<Button>().onClick.Invoke();
        }
        private static void Scroll(bool top)
        {
            Canvas.ForceUpdateCanvases();
            s_panel.transform.Find("Back/Descriptions").GetComponent<ScrollRect>().verticalNormalizedPosition = top ? 1 : 0;
        }

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step == 0 || EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(Key, 0); if (SessionState.GetBool(Key + ".batch", false)) EditorApplication.Exit(0); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("Description probe timed out at " + step);
                if (step == 1)
                {
                    if (EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".start", 0) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                    MethodInfo running = typeof(TestRunnerApi).GetMethod("IsRunActive", BindingFlags.NonPublic | BindingFlags.Static);
                    if (running != null && (bool)running.Invoke(null, null)) return;
                    EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity", OpenSceneMode.Single);
                    Next(2, 2); EditorApplication.isPlaying = true; return;
                }
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (SimulationRunner.IsOn) throw new InvalidOperationException("Balance simulation active.");
                switch (step)
                {
                    case 2:
                        if (TowerManager.Instance == null || TowerManager.Instance.Database == null || UIManager.Instance == null || TurnManager.Instance?.Current == null) return;
                        s_panel = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TowerInfoPanel>(true)).Single();
                        s_view = s_panel.GetComponent<SkillDescriptionView>();
                        Check(s_view != null, "Description view is connected to the live panel");
                        TowerManager.Instance.Database.TryGet(TowerId.Make(1, TowerColor.Red, TowerEmblem.Sword), out TowerDefinition sword);
                        TowerManager.Instance.Database.TryGet(TowerId.Make(1, TowerColor.Yellow, TowerEmblem.Sword), out TowerDefinition buff);
                        TowerManager.Instance.Database.TryGet(TowerId.Make(1, TowerColor.Purple, TowerEmblem.Hammer), out TowerDefinition debuff);
                        s_sword = TowerManager.Instance.SpawnForDebug(sword); s_buff = TowerManager.Instance.SpawnForDebug(buff); s_debuff = TowerManager.Instance.SpawnForDebug(debuff);
                        UIManager.Instance.ShowTowerPanel(s_sword); Next(3, 2); break;
                    case 3:
                        Time.timeScale = 0;
                        s_panel.transform.Find("Front/ShowSkillDetails").GetComponent<Button>().onClick.Invoke();
                        Next(31); break;
                    case 31:
                        Scroll(true); Next(4); break;
                    case 4:
                        Check(s_view.Document.Values["skill.damage"].Amount > 0, "Attack skill shows a computed numeric damage");
                        Capture("attack-description.png"); Next(5); break;
                    case 5:
                        ClickLink(Field<TextMeshProUGUI>(s_panel, "txt_skill"), "skill.damage", true);
                        Check(!s_view.IsValueOpen, "Dragging a number does not open its calculation");
                        ClickLink(Field<TextMeshProUGUI>(s_panel, "txt_skill"), "skill.damage");
                        Check(s_view.IsValueOpen && s_view.ValueText.Contains("공격력 x 적용 계수") && s_view.ValueText.Contains("="), "Clicking damage opens its real formula");
                        Next(6); break;
                    case 6:
                        Capture("attack-calculation.png"); Next(7); break;
                    case 7:
                        Field<Button>(s_view, "m_close").onClick.Invoke();
                        Check(!s_view.IsValueOpen, "Calculation back button returns to effect text");
                        Open(s_buff); Next(8); break;
                    case 8:
                        Check(s_view.Document.BasicTitle.Contains("기본 버프") && s_view.Document.SkillTitle.Contains("스킬 버프"), "Buff basic and skill names are separate");
                        Check(s_view.Document.Basic.Contains("공격력") && !s_view.Document.Basic.Contains("스킬 카운트"), "Basic support description shows the actual zone buff");
                        Scroll(true); Next(9); break;
                    case 9:
                        Capture("buff-description.png"); Next(10); break;
                    case 10:
                        ClickLink(Field<TextMeshProUGUI>(s_panel, "txt_default"), "basic.amount");
                        Check(s_view.IsValueOpen && s_view.ValueText.Contains("현재 공격력"), "Basic buff number opens the power-based calculation");
                        Next(11); break;
                    case 11:
                        Capture("buff-calculation.png"); Next(12); break;
                    case 12:
                        Open(s_debuff);
                        Check(!s_view.IsValueOpen, "Changing towers closes the previous calculation");
                        Next(13); break;
                    case 13:
                        Check(s_view.Document.BasicTitle.Contains("기본 디버프") && s_view.Document.SkillTitle.Contains("스킬 디버프"), "Debuff basic and skill names are separate");
                        Check(s_view.Document.Skill.Contains("골드") && !s_view.Document.Skill.Contains("젬"), "Debuff description uses current gold reward rules");
                        Scroll(false); Next(14); break;
                    case 14:
                        Capture("debuff-description.png"); Next(15); break;
                    case 15:
                        ClickLink(Field<TextMeshProUGUI>(s_panel, "txt_skill"), "skill.gold");
                        Check(s_view.IsValueOpen && s_view.ValueText.Contains("100"), "Fixed reward number explains its rule");
                        Next(16); break;
                    case 16:
                        Capture("debuff-calculation.png"); Next(17); break;
                    case 17:
                        UIManager.Instance.HideInfoPanels(); Check(!s_view.IsValueOpen, "Closing the panel also closes calculations");
                        Check(Errors.Count == 0, "No new description UI errors");
                        File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Result { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = ExistingErrors.ToArray(), textOverflows = Overflows.Distinct().ToArray() }, true));
                        Time.timeScale = 1; Next(100, .2); EditorApplication.isPlaying = false; break;
                }
            }
            catch (Exception error) { Fail(error); }
        }

        private static void ClickLink(TextMeshProUGUI text, string key, bool dragging = false)
        {
            Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate();
            Check(text.gameObject.activeInHierarchy, "Description page is visible for numeric input");
            TMP_LinkInfo info = text.textInfo.linkInfo.Take(text.textInfo.linkCount).Single(link => link.GetLinkID() == key);
            TMP_CharacterInfo character = text.textInfo.characterInfo[info.linkTextfirstCharacterIndex];
            Vector3 world = text.transform.TransformPoint((character.bottomLeft + character.topRight) * .5f);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, world), dragging = dragging };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Any(hit => hit.gameObject == text.gameObject), "Raycast reaches linked text: " + key);
            ExecuteEvents.Execute(text.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static void Capture(string file)
        {
            Camera camera = Camera.main;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
            float oldAspect = camera.aspect;
            var target = new RenderTexture(1920, 1080, 24); Texture2D pixels = null;
            try
            {
                camera.targetTexture = target; camera.aspect = 16f / 9;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
                Canvas.ForceUpdateCanvases();
                foreach (TextMeshProUGUI text in s_panel.GetComponentsInChildren<TextMeshProUGUI>()) { text.ForceMeshUpdate(); if (text.isTextOverflowing) Overflows.Add(file + ": " + text.name + " / " + text.text); }
                camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(1920, 1080, TextureFormat.RGBA32, false); pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                File.WriteAllBytes(Folder + "/" + file, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive; camera.targetTexture = oldTarget; camera.aspect = oldAspect;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
                if (pixels != null) Object.DestroyImmediate(pixels); Object.DestroyImmediate(target); Canvas.ForceUpdateCanvases();
            }
        }

        private static void Fail(Exception error)
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/verification-error.txt", error.ToString());
            File.WriteAllLines(Folder + "/collected-errors.txt", Errors.Concat(ExistingErrors)); SessionState.SetInt(Key, 0); Time.timeScale = 1;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            Debug.LogException(error); if (SessionState.GetBool(Key + ".batch", false)) EditorApplication.Exit(1);
        }
        [Serializable] private class Result { public bool verified; public string[] checks, errors, existingErrors, textOverflows; }
    }
}
