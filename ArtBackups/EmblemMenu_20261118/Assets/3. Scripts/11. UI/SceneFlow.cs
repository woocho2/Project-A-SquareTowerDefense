using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 씬 이동
    public static class SceneFlow
    {
        public static string PendingScene { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => PendingScene = null;

        public static void FinishLoading() => PendingScene = null;

        public static void Reload(string sceneName)
        {
            Load(string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName);
        }

        public static void Load(string sceneName)
        {
            if (PendingScene != null) return;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneFlow] 불러올 씬 이름이 비어 있음 (패널의 씬 이름 칸을 확인)");
                return;
            }

            Time.timeScale = 1f;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneFlow] '{sceneName}' 씬이 Build Settings 목록에 없습니다.");
                return;
            }
            bool loadingScene = sceneName == "LoadingScene1" || sceneName == "LoadingScene2";
            string loading = sceneName == "2. Menu" ? "LoadingScene1" : "LoadingScene2";
            if (loadingScene || !Application.CanStreamedLevelBeLoaded(loading)) { SceneManager.LoadScene(sceneName); return; }
            PendingScene = sceneName;
            try { SceneManager.LoadScene(loading); }
            catch { PendingScene = null; throw; }
        }

        public static bool LoadStage(StageDefinition stage, string fallbackScene = null)
        {
            string sceneName = stage != null && !string.IsNullOrEmpty(stage.SceneName) ? stage.SceneName : fallbackScene;
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[SceneFlow] '{sceneName}' 씬이 아직 없거나 Build Settings 목록에 없어 입장하지 않음");
                return false;
            }

            Load(sceneName);
            return true;
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
