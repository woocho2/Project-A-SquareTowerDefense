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

            // 수업 23 · 47: StackModeSettings — 전체 자동/수동 토글과 타워 종류(문양 + 컬러)별 예외 목록
            //   tgl_autoConsumeStack.isOn = StackModeSettings.Instance.DefaultAuto;
            //   tgl_autoConsumeStack.onValueChanged.AddListener(StackModeSettings.Instance.SetDefaultAuto);
            if (tgl_autoConsumeStack != null) tgl_autoConsumeStack.interactable = false;
        }

        private void OnDestroy()
        {
            if (btn_resume != null) btn_resume.onClick.RemoveListener(HandleResumeClick);
            if (btn_restart != null) btn_restart.onClick.RemoveListener(HandleRestartClick);
            if (btn_menu != null) btn_menu.onClick.RemoveListener(HandleQuitClick);
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
