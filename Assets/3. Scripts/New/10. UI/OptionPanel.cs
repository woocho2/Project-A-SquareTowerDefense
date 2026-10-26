using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquareTD
{
    public class OptionPanel : MonoBehaviour
    {
        [SerializeField] Button btn_resume;
        [SerializeField] Button btn_restart;
        [SerializeField] Button btn_menu;

        [Header("씬 이름")]
        [SerializeField] string m_sceneName_Restart;
        [SerializeField] string m_sceneName_Menu = "2. Menu";

        [Header("스택 소모")]
        [SerializeField] Toggle tgl_autoConsumeStack;

        private void Start()
        {
            if (btn_resume != null) btn_resume.onClick.AddListener(HandleResumeClick);
            if (btn_restart != null) btn_restart.onClick.AddListener(HandleRestartClick);
            if (btn_menu != null) btn_menu.onClick.AddListener(HandleQuitClick);

            // 스택 전체 자동/수동 (켜짐 = 자동). 타워 종류(문양 + 컬러)별 예외 목록은 수업 47
            if (tgl_autoConsumeStack != null && TowerManager.Instance != null)
            {
                tgl_autoConsumeStack.isOn = !TowerManager.Instance.StackModes.ManualByDefault;
                tgl_autoConsumeStack.onValueChanged.AddListener(HandleAutoConsumeChanged);
            }
        }

        private void HandleAutoConsumeChanged(bool isAuto)
        {
            if (TowerManager.Instance != null) TowerManager.Instance.StackModes.ManualByDefault = !isAuto;
        }

        private void OnDestroy()
        {
            if (btn_resume != null) btn_resume.onClick.RemoveListener(HandleResumeClick);
            if (btn_restart != null) btn_restart.onClick.RemoveListener(HandleRestartClick);
            if (btn_menu != null) btn_menu.onClick.RemoveListener(HandleQuitClick);
            if (tgl_autoConsumeStack != null) tgl_autoConsumeStack.onValueChanged.RemoveListener(HandleAutoConsumeChanged);
        }

        private void HandleResumeClick()
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseOptionPanel();
            else gameObject.SetActive(false);
        }

        private void HandleRestartClick()
        {
            SceneFlow.Reload(m_sceneName_Restart);
        }

        private void HandleQuitClick()
        {
            SceneFlow.Load(m_sceneName_Menu);
        }
    }
}
