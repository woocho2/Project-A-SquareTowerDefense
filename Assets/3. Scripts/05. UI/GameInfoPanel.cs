using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameInfoPanel : MonoBehaviour
{
    [Header("정보 표시")]
    [SerializeField] TextMeshProUGUI txt_stage;
    [SerializeField] TextMeshProUGUI txt_currentWave;
    [SerializeField] Image[] m_currentLifes;
    [SerializeField] Image[] m_currentShields;
    [SerializeField] TextMeshProUGUI txt_currentGold;
    [SerializeField] TextMeshProUGUI txt_currentGem;
    [SerializeField] TextMeshProUGUI txt_currentGameSpeed;


    [SerializeField] Button btn_option;
    [SerializeField] Button btn_gameSpeed;


    private void Start()
    {
        txt_stage.text = SceneManager.GetActiveScene().name;
        
        if (GameManager.Instance == null || WaveManager.Instance == null || CurrencyManager.Instance == null)
        {
            Debug.LogError("GaInfoPanel : GameManager/WaveManager/CurrencyManager가 없습니다.");
            return;   
        }
        
        UpdateLifeImage(GameManager.Instance.CurrentLife);
        UpdateShieldImage(GameManager.Instance.CurrentShield);
        UpdateWaveText(WaveManager.Instance.CurrentWave);
        UpdateGoldText(CurrencyManager.Instance.CurrentGold);
        UpdateGemText(CurrencyManager.Instance.CurrentGem);
        UpdateGameSpeedText(GameManager.Instance.CurrentGameSpeed);
        
        GameManager.Instance.CurrentLifeChanged += UpdateLifeImage;
        GameManager.Instance.CurrentShieldChanged += UpdateShieldImage;
        WaveManager.Instance.CurrentWaveChanged += UpdateWaveText;
        CurrencyManager.Instance.CurrentGoldChanged += UpdateGoldText;
        CurrencyManager.Instance.CurrentGemChanged += UpdateGemText;
        GameManager.Instance.CurrentGameSpeedChanged += UpdateGameSpeedText;

        btn_option.onClick.AddListener(HandleOptionClick);
        btn_gameSpeed.onClick.AddListener(HandleGameSpeedClick);
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.CurrentWaveChanged -= UpdateWaveText;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentLifeChanged -= UpdateLifeImage;
            GameManager.Instance.CurrentShieldChanged -= UpdateShieldImage;
            GameManager.Instance.CurrentGameSpeedChanged -= UpdateGameSpeedText;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.CurrentGoldChanged -= UpdateGoldText;
            CurrencyManager.Instance.CurrentGemChanged -= UpdateGemText;
        }

        if (btn_option != null)
        {
            btn_option.onClick.RemoveListener(HandleOptionClick);
        }

        if (btn_gameSpeed != null)
        {
            btn_gameSpeed.onClick.RemoveListener(HandleGameSpeedClick);
        }
    }

    private void UpdateWaveText(int wave)
    {
        txt_currentWave.text = $"Wave {wave}";
    }

    private void UpdateLifeImage(int life)
    {
        for (int i = 0; i < m_currentLifes.Length; i++)
        {
            if (m_currentLifes[i] != null)
            {
                m_currentLifes[i].gameObject.SetActive(i < life);
            }
        }
    }

    private void UpdateShieldImage(int shield)
    {
        for (int i = 0; i < m_currentShields.Length; i++)
        {
            if (m_currentShields[i] != null)
            {
                m_currentShields[i].gameObject.SetActive(i < shield);
            }
        }
    }

    private void UpdateGoldText(int amount)
    {
        txt_currentGold.text = $"{amount}";
    }

    private void UpdateGemText(int amount)
    {
        txt_currentGem.text = $"{amount}"; 
    }

    private void UpdateGameSpeedText(float amount)
    {
        txt_currentGameSpeed.text = $"{amount}";
    }

    private void HandleOptionClick()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("GameInfoPanel : UIManager가 없어 옵션 패널을 열 수 없습니다.");
            return;
        }

        UIManager.Instance.ShowOptionPanel();
    }

    private void HandleGameSpeedClick()
    {
        GameManager.Instance?.OnClickGameSpeed();
    }
}
