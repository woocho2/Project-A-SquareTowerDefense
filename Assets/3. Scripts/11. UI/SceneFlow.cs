using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 씬 이동
    public static class SceneFlow
    {
        public static void Reload(string sceneName)
        {
            Load(string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName);
        }

        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneFlow] 불러올 씬 이름이 비어 있음 (패널의 씬 이름 칸을 확인)");
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
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
