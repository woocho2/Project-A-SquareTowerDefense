using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 옵션 패널의 버튼(계속하기·재시작·나가기)을 담당합니다.
/// 계속하기를 누르면 패널을 스스로 닫고 게임을 재개합니다.
/// </summary>
public class OptionPanel : MonoBehaviour
{
    [SerializeField] Button btn_resume;
    [SerializeField] Button btn_restart;
    [SerializeField] Button btn_menu;

    [Header("씬 이름")]
    [SerializeField] string m_sceneName_Restart;
    [SerializeField] string m_sceneName_Menu = "2. Menu";

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("OptionPanel : GameManager가 없습니다.");
            return;
        }

        btn_resume.onClick.AddListener(HandleResumeClick);
        btn_restart.onClick.AddListener(HandleRestartClick);
        btn_menu.onClick.AddListener(HandleQuitClick);
    }

    private void OnDestroy()
    {
        if (btn_resume != null)
        {
            btn_resume.onClick.RemoveListener(HandleResumeClick);
        }
        if (btn_restart != null)
        {
            btn_restart.onClick.RemoveListener(HandleRestartClick);
        }
        if (btn_menu != null)
        {
            btn_menu.onClick.RemoveListener(HandleQuitClick);
        }
    }

    private void HandleResumeClick()
    {
        gameObject.SetActive(false);
        GameManager.Instance?.OnResumeGame();
    }

    private void HandleRestartClick()
    {
        // 배속 상태였더라도 다시 시작할 때는 기본 속도로 돌립니다.
        GameManager.Instance?.SetGameSpeed(1.0f);
        SceneLoader.StartLoad(m_sceneName_Restart);
    }

    private void HandleQuitClick()
    {
        // 옵션을 여는 동안 멈춰 둔 시간을 되돌린 뒤 메인 메뉴 씬으로 나갑니다.
        GameManager.Instance?.SetGameSpeed(1.0f);
        SceneManager.LoadScene(m_sceneName_Menu);
    }
}
