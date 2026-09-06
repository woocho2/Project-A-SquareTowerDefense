using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager_Start : MonoBehaviour
{
    [SerializeField] Button Btn_GameStart;
    [SerializeField] string m_sceneName;

    [Header("Game Start Text Effect")]
    [Tooltip("반짝임을 적용할 시작 화면 텍스트")]
    [SerializeField] private TextMeshProUGUI m_txtGameStart;
    [SerializeField, Min(0.1f)] private float m_gameStartSparkleSpeed = 2.2f;
    [SerializeField, Range(0f, 1f)] private float m_gameStartMinAlpha = 0.45f;
    [SerializeField, Min(1f)] private float m_gameStartMaxScale = 1.06f;

    private Color m_gameStartBaseColor;
    private Vector3 m_gameStartBaseScale;

    private void Start()
    {
        if (Btn_GameStart != null)
        {
            Btn_GameStart.onClick.RemoveAllListeners();
            Btn_GameStart.onClick.AddListener(OnGameStartButtonClick);
        }

        CacheGameStartTextStyle();
    }

    private void Update()
    {
        UpdateGameStartTextSparkle();
    }

    private void CacheGameStartTextStyle()
    {
        if (m_txtGameStart == null) return;

        m_gameStartBaseColor = m_txtGameStart.color;
        m_gameStartBaseScale = m_txtGameStart.transform.localScale;
    }

    private void UpdateGameStartTextSparkle()
    {
        if (m_txtGameStart == null) return;

        float pulse = (Mathf.Sin(Time.unscaledTime * m_gameStartSparkleSpeed) + 1f) * 0.5f;
        Color color = m_gameStartBaseColor;
        color.a = m_gameStartBaseColor.a * Mathf.Lerp(m_gameStartMinAlpha, 1f, pulse);
        m_txtGameStart.color = color;

        float scale = Mathf.Lerp(1f, m_gameStartMaxScale, pulse);
        m_txtGameStart.transform.localScale = m_gameStartBaseScale * scale;
    }

    public void OnGameStartButtonClick()
    {
        SceneManager.LoadScene(m_sceneName);
    }
}
