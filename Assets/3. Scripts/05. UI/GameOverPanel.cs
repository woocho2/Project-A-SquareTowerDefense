using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 오버 패널의 버튼(홈·메뉴·재시도)을 담당합니다.
/// 패널을 켜는 것은 UIManager가 합니다.
/// </summary>
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
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameOverPanel : GameManager가 없습니다.");
            return;
        }

        btn_home.onClick.AddListener(HandleHomeClick);
        btn_menu.onClick.AddListener(HandleMenuClick);
        btn_retry.onClick.AddListener(HandleRetryClick);
    }

    private void OnDestroy()
    {
        if (btn_home != null)
        {
            btn_home.onClick.RemoveListener(HandleHomeClick);
        }
        if (btn_menu != null)
        {
            btn_menu.onClick.RemoveListener(HandleMenuClick);
        }
        if (btn_retry != null)
        {
            btn_retry.onClick.RemoveListener(HandleRetryClick);
        }
    }

    private void HandleHomeClick()
    {
        GameManager.Instance?.OnQuitGame();
    }

    private void HandleMenuClick()
    {
        SceneLoader.StartLoad(m_sceneName_Menu);
    }

    private void HandleRetryClick()
    {
        // 배속 상태로 끝났더라도 다시 시작할 때는 기본 속도로 돌립니다.
        GameManager.Instance?.SetGameSpeed(1.0f);
        SceneLoader.StartLoad(m_sceneName_Restart);
    }
}
