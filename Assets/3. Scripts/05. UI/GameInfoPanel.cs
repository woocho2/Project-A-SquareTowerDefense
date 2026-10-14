using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameInfoPanel : MonoBehaviour
{
    [Header("정보 표시")]
    [SerializeField] TextMeshProUGUI txt_stage;
    [SerializeField] TextMeshProUGUI txt_currentWave;
    [SerializeField] TextMeshProUGUI txt_currentLimit;
    [SerializeField] Image img_life;
    [SerializeField] Image img_shield;
    [SerializeField] TextMeshProUGUI txt_currentGold;
    [SerializeField] TextMeshProUGUI txt_currentGem;
    [SerializeField] TextMeshProUGUI txt_currentGameSpeed;


    [SerializeField] Button btn_option;
    [SerializeField] Button btn_gameSpeed;


    private void Start()
    {
        txt_stage.text = SceneManager.GetActiveScene().name;

        if (GameManager.Instance == null || WaveManager.Instance == null || CurrencyManager.Instance == null || TileManager.Instance == null)
        {
            Debug.LogError("GaInfoPanel : GameManager/WaveManager/CurrencyManager가 없습니다.");
            return;
        }

        UpdateLimitText();
        UpdateWaveText(WaveManager.Instance.CurrentWave);
        UpdateGoldText(CurrencyManager.Instance.CurrentGold);
        UpdateGemText(CurrencyManager.Instance.CurrentGem);
        UpdateGameSpeedText(GameManager.Instance.CurrentGameSpeed);

        GameManager.Instance.EnemyLimitChanged += HandleLimitChanged;
        TileManager.Instance.EnemyCountChanged += UpdateLimitText;
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
            GameManager.Instance.EnemyLimitChanged -= HandleLimitChanged;
            GameManager.Instance.CurrentGameSpeedChanged -= UpdateGameSpeedText;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.CurrentGoldChanged -= UpdateGoldText;
            CurrencyManager.Instance.CurrentGemChanged -= UpdateGemText;
        }

        if (TileManager.Instance != null)
        {
            TileManager.Instance.EnemyCountChanged -= UpdateLimitText;
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

    private void UpdateLimitText()
    {
        int count = TileManager.Instance.GetAllEnemiesOnPath().Count;
        int limit = GameManager.Instance.EnemyLimit;

        txt_currentLimit.text = $"{count}/{limit}";
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

    private void HandleLimitChanged(int limit)
    {
        UpdateLimitText();
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
