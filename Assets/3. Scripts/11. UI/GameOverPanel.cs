using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 게임 오버 패널
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] Button btn_home;
        [SerializeField] Button btn_menu;
        [SerializeField] Button btn_retry;

        [Header("씬 이름")]
        [SerializeField] string m_sceneName_Restart;
        [SerializeField] string m_sceneName_Menu;

        private void Start()
        {
            if (btn_home != null) btn_home.onClick.AddListener(HandleHomeClick);
            if (btn_menu != null) btn_menu.onClick.AddListener(HandleMenuClick);
            if (btn_retry != null) btn_retry.onClick.AddListener(HandleRetryClick);
        }

        private void OnDestroy()
        {
            if (btn_home != null) btn_home.onClick.RemoveListener(HandleHomeClick);
            if (btn_menu != null) btn_menu.onClick.RemoveListener(HandleMenuClick);
            if (btn_retry != null) btn_retry.onClick.RemoveListener(HandleRetryClick);
        }

        private void HandleHomeClick() => SceneFlow.Quit();

        private void HandleMenuClick() => SceneFlow.Load(m_sceneName_Menu);

        private void HandleRetryClick() => SceneFlow.Reload(m_sceneName_Restart);
    }
}
