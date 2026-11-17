using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class SynergySkillButtonVerification
    {
        private const string Folder = "ArtPreviews/SynergySkillButtons_20261117";
        private const string Key = "SquareTD.SynergySkillButtonProbe";
        private static TestRunnerApi s_runner;
        private static double s_next;
        private static ProbeResult s_result;

        static SynergySkillButtonVerification()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/SquareTD/시너지 스킬 버튼 검증 (플레이 포함)")]
        public static void RunTests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != "Assets/1. Scenes/3. Stage1.unity")
                throw new InvalidOperationException("Open Stage1 in Edit mode before running the button checks.");
            Directory.CreateDirectory(Folder);
            s_runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            s_runner.RegisterCallbacks(new Results());
            s_runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                testNames = new[] { "SquareTD.Tests.SynergySkillButtonTest", "SquareTD.Tests.SynergyTest" }
            }));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor test) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, Folder + "/editmode-results.xml");
                File.WriteAllText(Folder + "/editmode-summary.txt", $"Passed: {result.PassCount}; failed: {result.FailCount}; skipped: {result.SkipCount}");
                EditorApplication.delayCall += () => { if (s_runner != null) UnityEngine.Object.DestroyImmediate(s_runner); s_runner = null; };
                if (result.FailCount != 0) return;
                SessionState.SetInt(Key, 1);
                SessionState.SetFloat(Key + ".wait", (float)EditorApplication.timeSinceStartup + 3f);
                SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 90f);
            }
        }

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step == 0 || EditorApplication.timeSinceStartup < s_next) return;
            s_next = EditorApplication.timeSinceStartup + 0.2;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("Synergy button PlayMode probe timed out.");
                if (step == 1)
                {
                    if (EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".wait", 0)) return;
                    var running = typeof(TestRunnerApi).GetMethod("IsRunActive", BindingFlags.NonPublic | BindingFlags.Static);
                    if (running != null && (bool)running.Invoke(null, null)) return;
                    if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    if (SceneManager.GetActiveScene().path != "Assets/1. Scenes/3. Stage1.unity") throw new InvalidOperationException("The original Stage1 scene was not restored.");
                    SessionState.SetInt(Key, 2);
                    EditorApplication.isPlaying = true;
                    return;
                }
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (SimulationRunner.IsOn) throw new InvalidOperationException("A balance simulation is active; the UI probe will not change its settings.");
                SynergyBoard board = TowerManager.Instance != null ? TowerManager.Instance.Synergy : null;
                if (board == null || board.Entries.Count == 0) return;
                var buttons = board.Entries.Select(entry => entry.Tower.GetComponent<SynergySkillButton>()).ToArray();
                if (buttons.Any(button => button == null || button.View == null)) return;
                if (step == 2)
                {
                    SynergyBoard.TestAutoUse = false;
                    s_result = new ProbeResult { synergyTowers = buttons.Length };
                    foreach (SynergySkillButton button in buttons)
                    {
                        if (button.View.ActionButton.interactable) throw new InvalidOperationException("An uncharged skill was enabled.");
                        button.View.ActionButton.onClick.Invoke();
                    }
                    if (board.Entries.Any(entry => entry.Uses != 0)) throw new InvalidOperationException("An uncharged click consumed a skill.");
                    s_result.unchargedBlocked = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/charging.png"));
                    SessionState.SetInt(Key, 3);
                    return;
                }
                if (step == 3)
                {
                    if (!File.Exists(Folder + "/charging.png")) return;
                    board.FillAll();
                    SessionState.SetInt(Key, 4);
                    return;
                }
                if (step == 4)
                {
                    if (buttons.Any(button => !button.View.ActionButton.interactable)) return;
                    s_result.readyEnabled = true;
                    var pointer = new PointerEventData(EventSystem.current)
                    {
                        position = Camera.main.WorldToScreenPoint(buttons[0].View.ActionButton.transform.position)
                    };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    if (!hits.Any(hit => hit.gameObject.GetComponentInParent<SynergySkillButtonView>() == buttons[0].View))
                        throw new InvalidOperationException("The world-space button is not reachable by the UI raycaster.");
                    s_result.worldUiRaycast = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/ready.png"));
                    SessionState.SetInt(Key, 5);
                    return;
                }
                if (step == 5)
                {
                    if (!File.Exists(Folder + "/ready.png")) return;
                    SynergyBoard.Entry instant = board.Entries.First(entry => !SynergySkillButton.NeedsPathSelection(entry.Skill.Id));
                    int uses = instant.Uses;
                    instant.Tower.GetComponent<SynergySkillButton>().View.ActionButton.onClick.Invoke();
                    if (instant.Uses != uses + 1) throw new InvalidOperationException("The UI click did not invoke the synergy skill.");
                    s_result.skillClick = true;
                    SynergyBoard.Entry loki = board.Entries.FirstOrDefault(entry => entry.Skill.Id == SynergyEffects.LokiId);
                    if (loki != null)
                    {
                        loki.Tower.GetComponent<SynergySkillButton>().View.ActionButton.onClick.Invoke();
                        var input = UnityEngine.Object.FindAnyObjectByType<BoardInputController>();
                        if (!(input.Current is PickSynergyTargetMode)) throw new InvalidOperationException("Loki did not enter target selection.");
                        input.Current.OnPointerDown(new BoardPoint(Vector3.zero, 0, -1));
                        if (!loki.Skill.IsReady) throw new InvalidOperationException("Loki consumed stacks before the second cell.");
                        input.Current.OnPointerDown(new BoardPoint(Vector3.zero, 1, -1));
                        if (loki.Uses != 1 || loki.Skill.IsReady) throw new InvalidOperationException("Loki target selection did not cast.");
                        s_result.lokiSelection = true;
                    }
                    CurrencyManager.Instance.AddGold(TowerManager.FixedBuildCost);
                    if (!TowerManager.Instance.TryBuild()) throw new InvalidOperationException("Could not spawn a regular tower for the UI exclusion check.");
                    SessionState.SetInt(Key, 6);
                    return;
                }
                if (step == 6)
                {
                    TowerController regular = BoardManager.Instance.GetTowers(TowerPlace.Board).FirstOrDefault();
                    if (regular == null) return;
                    SynergySkillButton binding = regular.GetComponent<SynergySkillButton>();
                    if (binding == null || binding.enabled) return;
                    if (binding.View != null || regular.GetComponentInChildren<SynergySkillButtonView>(true) != null)
                        throw new InvalidOperationException("A regular tower received a synergy skill UI.");
                    s_result.regularTowerHasNoButton = true;
                    s_result.playModeVerified = true;
                    File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(s_result, true));
                    SessionState.SetInt(Key, 0);
                    EditorApplication.isPlaying = false;
                }
            }
            catch (Exception error)
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Folder + "/verification-error.txt", error.ToString());
                SessionState.SetInt(Key, 0);
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                Debug.LogException(error);
            }
        }

        [Serializable] private class ProbeResult
        {
            public int synergyTowers;
            public bool unchargedBlocked, readyEnabled, worldUiRaycast, skillClick, lokiSelection, regularTowerHasNoButton, playModeVerified;
        }
    }
}
