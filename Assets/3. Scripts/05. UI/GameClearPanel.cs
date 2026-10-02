using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 클리어 패널의 버튼(홈·메뉴·다음 스테이지)을 담당합니다.
/// 패널을 켜는 것은 UIManager가 합니다.
/// </summary>
public class GameClearPanel : MonoBehaviour
{
    [SerializeField] Button btn_home;
    [SerializeField] Button btn_menu;
    [SerializeField] Button btn_nextStage;

    [Header("씬 이름")]
    [SerializeField] string m_sceneName_Menu;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameClearPanel : GameManager가 없습니다.");
            return;
        }

        btn_home.onClick.AddListener(HandleHomeClick);
        btn_menu.onClick.AddListener(HandleMenuClick);
        btn_nextStage.onClick.AddListener(HandleNextStageClick);
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
        if (btn_nextStage != null)
        {
            btn_nextStage.onClick.RemoveListener(HandleNextStageClick);
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

    private void HandleNextStageClick()
    {
        // 다음 스테이지 로딩이 아직 없어 홈 버튼과 같은 동작을 합니다.
        GameManager.Instance?.OnQuitGame();
    }
}
