using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager_Start : MonoBehaviour
{
    [SerializeField] Button Btn_GameStart;

    [SerializeField] string m_sceneName;


    private void Start()
    {
        if (Btn_GameStart != null)
        {
            Btn_GameStart.onClick.RemoveAllListeners();
            Btn_GameStart.onClick.AddListener(OnGameStartButtonClick);
        }
    }

    public void OnGameStartButtonClick()
    {
        SceneManager.LoadScene(m_sceneName);
    }
}
