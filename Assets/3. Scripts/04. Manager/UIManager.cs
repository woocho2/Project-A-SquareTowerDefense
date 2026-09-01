using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Game Info")]
    [SerializeField] GameObject m_panelGameInfo;
    [SerializeField] Button m_btnGameSpeed;
    [SerializeField] TextMeshProUGUI m_txtGameSpeed;
    [SerializeField] Button m_btnMainStop;
    [SerializeField] Button m_btnCreateTower;
    [SerializeField] TextMeshProUGUI m_txtCreateCostGold;
    [SerializeField] Button m_btnColorUpgradeTower;
    [SerializeField] TextMeshProUGUI m_txtColorUpgradeCostGem;
    [SerializeField] Button m_btnTierUpgradeTower;
    [SerializeField] TextMeshProUGUI m_txtTierUpgradeCostGem;
    [SerializeField] Image[] m_LifePoints;
    [SerializeField] TextMeshProUGUI m_txtWave;
    [SerializeField] TextMeshProUGUI m_txtEnemy;

    [Header("UI Tower Info")]
    [SerializeField] GameObject m_towerInfoPanel;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoName;
    [SerializeField] Button m_btnCombineColor;
    [SerializeField] Button m_btnCombineEmblem;
    [SerializeField] Button m_btnCombineExact;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoLV;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoDamage;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoRange;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoSpeed;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoCriticalRate;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoCriticalDamage;

    [Header("UI Option")]
    [SerializeField] GameObject m_panelOption;
    [SerializeField] Button m_btnQuit;
    [SerializeField] Button m_btnRestart;
    [SerializeField] Button m_btnResume;

    [Header("Game Clear")]
    [SerializeField] GameObject m_panelGameclear;
    [SerializeField] Button m_btnGameClearHome;
    [SerializeField] Button m_btnGameClearMenu;
    [SerializeField] Button m_btnNextStage;

    [Header("Game Over")]
    [SerializeField] GameObject m_panelGameover;
    [SerializeField] Button m_btnGameOverHome;
    [SerializeField] Button m_btnGameOverMenu;
    [SerializeField] Button m_btnRetry;

    [Header("DebugLog")]
    [SerializeField] InGameDebugConsole m_debugconsole;
    [SerializeField] GameObject m_panelDebug;
    [SerializeField] TextMeshProUGUI m_txtDebug;
    [SerializeField] Button m_btnDebug;

    [Header("Trash")]
    public GameObject SellAreaPanel;

    [SerializeField] string m_sceneName_Restart;
    [SerializeField] string m_sceneName_Menu;

    [SerializeField] private Vector3 m_combineColorOffset = new Vector3(-50f, 0f, 50f);
    [SerializeField] private Vector3 m_combineEmblemOffset = new Vector3(0f, 0f, 50f);
    [SerializeField] private Vector3 m_combineExactOffset = new Vector3(50f, 0f, 50f);

    private TowerController m_selectedTower;
    private Vector3Int m_selectedTowerInfo;

    private int m_selectTowerID = 0;
    private int currentLifeIndex;
    private int m_lastWave = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        DragObjectOnGround.OnTowerClickedAction += ShowTowerpanel;
        GlobalClickDetector.OnGroundClickedAction += HideTowerPanel;
    }

    private void OnDisable()
    {
        DragObjectOnGround.OnTowerClickedAction -= ShowTowerpanel;
        GlobalClickDetector.OnGroundClickedAction -= HideTowerPanel;
    }

    private void Start()
    {
        InitUI();

        Button[] quitButtons = { m_btnQuit, m_btnGameOverHome, m_btnGameClearHome, m_btnNextStage };
        foreach (var btn in quitButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => GameManager.Instance.OnQuitGame());
            }
        }

        Button[] restartButtons = { m_btnRestart, m_btnRetry };
        foreach (var btn in restartButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(RestartGameLogic);
            }
        }

        Button[] MenuButtons = { m_btnGameOverMenu, m_btnGameClearMenu };
        foreach (var btn in MenuButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(MenuGameLogic);
            }
        }

        if (m_btnMainStop != null)
        {
            m_btnMainStop.onClick.RemoveAllListeners();
            m_btnMainStop.onClick.AddListener(() =>
            {
                m_btnMainStop.gameObject.SetActive(false);
                m_panelOption.SetActive(true);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnPauseGame();
                }
            });
        }

        if (m_btnResume != null)
        {
            m_btnResume.onClick.RemoveAllListeners();
            m_btnResume.onClick.AddListener(OnResumeButtonClick);
        }

        if (m_btnGameSpeed != null)
        {
            m_btnGameSpeed.onClick.RemoveAllListeners();
            m_btnGameSpeed.onClick.AddListener(() =>
            {
                OnGameSpeedButtonClick(GameManager.Instance.GetGameSpeed());
            });
        }

        if (m_btnNextStage != null)
        {
            m_btnNextStage.onClick.RemoveAllListeners();
            m_btnNextStage.onClick.AddListener(() =>
            {
                GameManager.Instance.OnQuitGame();
            });
        }

        if (m_btnCreateTower != null)
        {
            m_btnCreateTower.onClick.AddListener(() =>
            {
                TowerManager.Instance.BuildTower();

                if (m_btnCreateTower != null)
                {
                    RefreshCreateCostLabel();
                }
            });
        }

        if (m_btnColorUpgradeTower != null)
        {
            m_btnColorUpgradeTower.onClick.AddListener(() =>
            {
                if (m_selectTowerID <= 0)
                {
                    Debug.LogWarning("업그레이드할 타워가 선택되지 않았습니다.");
                    return;
                }

                TowerManager.Instance.UpgradeTower(m_selectTowerID);
                if (m_btnColorUpgradeTower != null)
                {
                    RefreshColorUpgradeCostLabel();
                }
            });
        }

        if (m_btnDebug != null)
        {
            m_btnDebug.onClick.AddListener(() =>
            {
                m_debugconsole.TestLog();
            });
        }

        RefreshCreateCostLabel();
        RefreshColorUpgradeCostLabel();
        RefreshTierUpgradeCostLabel();

        if (GameManager.Instance != null)
        {
            RefreshGameSpeedLabel(GameManager.Instance.GetGameSpeed());
        }
        if (m_btnCombineColor != null && m_btnCombineEmblem != null && m_btnCombineExact != null)
        {
            m_btnCombineColor.enabled = true;
            m_btnCombineEmblem.enabled = true;
            m_btnCombineExact.enabled = true;
        }

        if (m_btnCombineColor != null) m_btnCombineColor.onClick.AddListener(() => OnCombineClick(CombineMode.TypeMatch));
        if (m_btnCombineEmblem != null) m_btnCombineEmblem.onClick.AddListener(() => OnCombineClick(CombineMode.VariantMatch));
        if (m_btnCombineExact != null) m_btnCombineExact.onClick.AddListener(() => OnCombineClick(CombineMode.ExactMatch));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        UpdateWaveUI();
        UpdateEnemyCount();

        if (m_selectedTower != null && !m_selectedTower.Equals(null))
        {
            UpdateCombineButtons();
        }
    }

    void RestartGameLogic()
    {
        GameManager.Instance.SetGameSpeed(1.0f);
        SceneLoader.StartLoad(m_sceneName_Restart);
    }

    void MenuGameLogic()
    {
        SceneLoader.StartLoad(m_sceneName_Menu);
    }

    public void OnResumeButtonClick()
    {
        if (m_panelOption != null)
        {
            m_panelOption.SetActive(false);
        }

        if (m_btnMainStop != null)
        {
            m_btnMainStop.gameObject.SetActive(true);
        }

        GameManager.Instance.OnResumeGame();
    }

    public void OnGameSpeedButtonClick(float gamespeed)
    {
        GameManager.Instance.OnClickGameSpeed(gamespeed);
        ApplyGameSpeedFromButton();
    }

    void RefreshGameSpeedLabel(float speed)
    {
        if (m_txtGameSpeed != null)
        {
            m_txtGameSpeed.text = $"x{speed:F1}";
        }
    }

    void RefreshCreateCostLabel()
    {
        if (m_txtCreateCostGold != null)
        {
            m_txtCreateCostGold.text = $"{TowerManager.Instance.GetBuildCost()}";
        }
    }

    void RefreshColorUpgradeCostLabel()
    {
        if (m_txtColorUpgradeCostGem != null)
        {
            if (m_selectTowerID > 0)
            {
                TowerStats currentStats = TowerManager.Instance.GetGlobalStats(m_selectTowerID);

                if (currentStats.Level >= 5)
                {
                    m_txtColorUpgradeCostGem.text = "MAX";
                }
                else
                {
                    int currentColorUpgradeGem = (int)Mathf.Pow(2, currentStats.Level);
                    m_txtColorUpgradeCostGem.text = $"{currentColorUpgradeGem}";
                }
            }
            else
            {
                m_txtColorUpgradeCostGem.text = "-";
            }
        }
    }

    void RefreshTierUpgradeCostLabel()
    {
        if (m_txtTierUpgradeCostGem != null)
        {
            if (m_selectTowerID > 0)
            {
                TowerStats currentStats = TowerManager.Instance.GetGlobalStats(m_selectTowerID);
                float currentTier = currentStats.ID / 1000;

                if (currentTier >= 5)
                {
                    m_txtTierUpgradeCostGem.text = "MAX";
                }
                else
                {
                    int currentTierUpgradeGem = (int)Mathf.Pow(4, currentStats.Level);
                    m_txtTierUpgradeCostGem.text = $"{currentTierUpgradeGem}";
                }
            }
            else
            {
                m_txtTierUpgradeCostGem.text = "-";
            }
        }
    }

    public void RefreshUpgradeTowerInfoLabel()
    {
        if (m_selectedTower != null && m_towerInfoPanel.activeSelf)
        {
            TowerStats stats = m_selectedTower.GetFinalStats();
            TowerData data = m_selectedTower.GetTowerData();

            if (m_txtTowerInfoName != null) m_txtTowerInfoName.text = stats.Name;
            if (m_txtTowerInfoLV != null) m_txtTowerInfoLV.text = $"{stats.Level}";
            if (m_txtTowerInfoDamage != null)
            {
                if (data != null && data.attackType == AttackType.Buff)
                {
                    float buffPower = stats.AbilityValue;
                    m_txtTowerInfoDamage.text = $"+{buffPower * 100}%";
                }
                else
                {
                    m_txtTowerInfoDamage.text = $"{stats.AttackPower:F2}";
                }
            }
            if (m_txtTowerInfoRange != null) m_txtTowerInfoRange.text = $"{stats.Range * 100}";
            if (m_txtTowerInfoSpeed != null) m_txtTowerInfoSpeed.text = $"{stats.AttackSpeed:F2}";
            if (m_txtTowerInfoCriticalRate != null) m_txtTowerInfoCriticalRate.text = $"{(stats.CriticalRate * 100):F2}%";
            if (m_txtTowerInfoCriticalDamage != null) m_txtTowerInfoCriticalDamage.text = $"{((2 + stats.CriticalDamage) * 100):F0}%";
        }
    }

    private void UpdateWaveUI()
    {
        if (WaveManager.Instance != null && m_txtWave != null)
        {
            int currentWave = WaveManager.Instance.GetWave();
            //float remainingTime = WaveManager.Instance.currentWaveTimer;
            //int displaySeconds = Mathf.CeilToInt(remainingTime);

           // m_txtWave.text = $"Wave {currentWave}\nNext Wave : {displaySeconds}";
            m_lastWave = currentWave;
        }
    }

    public void InitUI()
    {
        if (m_panelGameInfo != null) m_panelGameInfo.SetActive(true);
        if (m_panelOption != null) m_panelOption.SetActive(false);
        if (m_panelGameover != null) m_panelGameover.SetActive(false);
        if (m_panelGameclear != null) m_panelGameclear.SetActive(false);
        if (m_btnMainStop != null) m_btnMainStop.gameObject.SetActive(true);
        if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(false);

        currentLifeIndex = m_LifePoints.Length - 1;

        foreach (var hpImage in m_LifePoints)
        {
            if (hpImage != null) hpImage.gameObject.SetActive(true);
        }
    }

    float GetGameButtonSpeed()
    {
        return GameManager.Instance.GetGameSpeed();
    }

    void ApplyGameSpeedFromButton()
    {
        float gameSpeed = GetGameButtonSpeed();
        RefreshGameSpeedLabel(gameSpeed);
    }

    public void RestartUI()
    {
        if (GameManager.Instance != null)
        {
            float gamespeed = GameManager.Instance.GetGameSpeed();
            RefreshGameSpeedLabel(gamespeed);
        }
    }

    private void ShowTowerpanel(TowerController clickedTower)
    {
        m_selectedTower = clickedTower;
        m_selectedTowerInfo = TowerManager.Instance.WorldToCell(clickedTower.transform.position);

        if (clickedTower.GetTowerData() != null)
        {
            m_selectTowerID = clickedTower.GetTowerData().towerID;
        }
        else
        {
            m_selectTowerID = 0;
        }

        RefreshColorUpgradeCostLabel();
        RefreshTierUpgradeCostLabel();

        if (m_towerInfoPanel != null)
        {
            m_towerInfoPanel.SetActive(true);
        }

        RefreshUpgradeTowerInfoLabel();
        UpdateCombineButtons();
        SelectTower(clickedTower);
    }

    private void HideTowerPanel()
    {
        if (m_towerInfoPanel != null && m_towerInfoPanel.activeSelf)
        {
            m_towerInfoPanel.SetActive(false);
        }
    }

    public void SelectTower(TowerController tower)
    {
        m_selectedTower = tower;

        if (m_btnCombineColor != null && m_btnCombineEmblem != null && m_btnCombineExact != null)
        {
            Vector3 towerWorldPos = tower.transform.position;
            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(towerWorldPos);

                m_btnCombineColor.transform.position = screenPos + m_combineColorOffset;
                m_btnCombineEmblem.transform.position = screenPos + m_combineEmblemOffset;
                m_btnCombineExact.transform.position = screenPos + m_combineExactOffset;
            }
        }
    }

    private void OnCombineClick(CombineMode type)
    {
        if (m_selectedTower != null && m_selectedTower.gameObject != null)
        {
            TowerManager.Instance.ExecuteCombine(m_selectedTowerInfo, type);
            HideTowerPanel();
            m_selectedTower = null;
        }
        else
        {
            Debug.LogWarning("[UIManager] 합성할 타워가 유효하지 않습니다.");
            HideTowerPanel();
        }
    }

    private void UpdateCombineButtons()
    {
        if (m_selectedTower == null) return;

        m_btnCombineColor.interactable = TowerManager.Instance.CanCombine(m_selectedTowerInfo, CombineMode.TypeMatch);
        m_btnCombineEmblem.interactable = TowerManager.Instance.CanCombine(m_selectedTowerInfo, CombineMode.VariantMatch);
        m_btnCombineExact.interactable = TowerManager.Instance.CanCombine(m_selectedTowerInfo, CombineMode.ExactMatch);
    }

    public void ShowButtons(bool isShow)
    {
        if (m_btnCreateTower != null) m_btnCreateTower.gameObject.SetActive(isShow);
        if (m_btnColorUpgradeTower != null) m_btnColorUpgradeTower.gameObject.SetActive(isShow);
    }

    public void OnPlayerHit(bool isBoss = false)
    {
        if (isBoss)
        {
            ShowGameOver();
            return;
        }

        if (currentLifeIndex >= 0)
        {
            if (m_LifePoints[currentLifeIndex] != null)
            {
                m_LifePoints[currentLifeIndex].gameObject.SetActive(false);
            }
            currentLifeIndex--;
        }

        if (currentLifeIndex < 0)
        {
            ShowGameOver();
        }
    }

    public void UpdateEnemyCount()
    {
        if (m_txtEnemy != null && WaveManager.Instance != null)
        {
            m_txtEnemy.text = $"Enemy : {WaveManager.Instance.GetEnemyCount()}";
        }
    }

    public void ShowGameOver()
    {
        // EnemyController 대신 EnemyHealthController를 탐색하여 체력바 숨김 처리
        EnemyHealthController[] allEnemies = FindObjectsByType<EnemyHealthController>(FindObjectsSortMode.None);
        for (int i = 0; i < allEnemies.Length; i++)
        {
            allEnemies[i].HideHPBar();
        }

        GameManager.Instance?.OnPauseGame();

        if (m_panelGameover != null)
        {
            if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(false);
            m_panelGameover.SetActive(true);
        }
    }

    public void ShowGameClear()
    {
        GameManager.Instance?.OnPauseGame();

        if (m_panelGameclear != null)
        {
            m_panelGameclear.SetActive(true);
        }
    }
}