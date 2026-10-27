using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    public class GameClearPanel : MonoBehaviour
    {
        [SerializeField] Button btn_home;
        [SerializeField] Button btn_menu;
        [SerializeField] Button btn_nextStage;

        [Header("씬 이름")]
        [SerializeField] string m_sceneName_Menu;

        private void Start()
        {
            if (btn_home != null) btn_home.onClick.AddListener(HandleHomeClick);
            if (btn_menu != null) btn_menu.onClick.AddListener(HandleMenuClick);
            if (btn_nextStage != null) btn_nextStage.onClick.AddListener(HandleNextStageClick);
        }

        private void OnDestroy()
        {
            if (btn_home != null) btn_home.onClick.RemoveListener(HandleHomeClick);
            if (btn_menu != null) btn_menu.onClick.RemoveListener(HandleMenuClick);
            if (btn_nextStage != null) btn_nextStage.onClick.RemoveListener(HandleNextStageClick);
        }

        private void HandleHomeClick() => SceneFlow.Quit();

        private void HandleMenuClick() => SceneFlow.Load(m_sceneName_Menu);

        private void HandleNextStageClick()
        {
            // 수업 42: 스테이지 해금이 생기면 StartSettings에 다음 스테이지를 적고 그 씬을 불러옴
            SceneFlow.Load(m_sceneName_Menu);
        }
    }
}
