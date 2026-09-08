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
    [SerializeField] Button m_btnEndPlayerTurn;
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
    [SerializeField] TextMeshProUGUI m_txtCurrentWaveIndex;
    [SerializeField] TextMeshProUGUI m_txtPlayerTurnRemainingTime;

    [Header("Turn State UI")]
    [Tooltip("타워 생성·컬러 강화·티어 강화 버튼만 포함하는 패널입니다.")]
    [SerializeField] GameObject m_playerActionPanel;

    [Header("UI Tower Info")]
    [SerializeField] GameObject m_towerInfoPanel;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoName;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoTier;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoColor;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoEmblem;
    [SerializeField] Button m_btnCombineColor;
    [SerializeField] Button m_btnCombineEmblem;
    [SerializeField] Button m_btnCombineExact;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoLV;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoPowerLabel;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoDamage;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoRange;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoAction;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoSpeedLabel;
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
    private bool m_isPlayerActionUIVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindTowerInfoValueTexts();
    }

    // TowerInfoPanel 아래의 값 텍스트를 이름으로 자동 연결합니다.
    // 인스펙터에 직접 연결한 레퍼런스는 유지하고, 비어 있는 항목만 찾습니다.
    private void FindTowerInfoValueTexts()
    {
        if (m_towerInfoPanel == null) return;

        m_txtTowerInfoTier ??= FindTowerInfoText("TowerTier");
        m_txtTowerInfoColor ??= FindTowerInfoText("TowerColor");
        m_txtTowerInfoEmblem ??= FindTowerInfoText("TowerEmblem");
        m_txtTowerInfoPowerLabel ??= FindTowerInfoText("Txt_TowerPower");
        m_txtTowerInfoDamage ??= FindTowerInfoText("Txt_TowerPowerValue");
        m_txtTowerInfoRange ??= FindTowerInfoText("Txt_TowerRangeValue");
        m_txtTowerInfoAction ??= FindTowerInfoText("Txt_TowerActionValue");
        m_txtTowerInfoSpeedLabel ??= FindTowerInfoText("Txt_TowerAttackSpeed");
        m_txtTowerInfoSpeed ??= FindTowerInfoText("Txt_TowerAttackSpeedValue");
        m_txtTowerInfoCriticalRate ??= FindTowerInfoText("Txt_TowerCriticalRateValue");
        m_txtTowerInfoCriticalDamage ??= FindTowerInfoText("Txt_TowerCriticalDamageValue");
    }

    private TextMeshProUGUI FindTowerInfoText(string objectName)
    {
        TextMeshProUGUI[] texts = m_towerInfoPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name == objectName)
            {
                return text;
            }
        }

        return null;
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

        SubscribeGameManagerEvents();
        if (GameManager.Instance != null)
        {
            SetPlayerActionUI(GameManager.Instance.CanPerformPlayerAction);
            RefreshPlayerTurnStateUI(GameManager.Instance.CurrentState);
        }

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

        if (m_btnEndPlayerTurn != null)
        {
            m_btnEndPlayerTurn.onClick.RemoveAllListeners();
            m_btnEndPlayerTurn.onClick.AddListener(() =>
            {
                GameManager.Instance?.OnClickSkipPlayerTurn();
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
                if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;
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
                if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;
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

        if (m_btnTierUpgradeTower != null)
        {
            m_btnTierUpgradeTower.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;
                if (m_selectedTower == null || m_selectTowerID <= 0)
                {
                    Debug.LogWarning("티어 강화할 타워가 선택되지 않았습니다.");
                    return;
                }

                if (!TowerManager.Instance.UpgradeTowerTier(m_selectedTowerInfo, out TowerController upgradedTower))
                {
                    return;
                }

                m_selectedTower = upgradedTower;
                m_selectTowerID = upgradedTower.GetTowerData().towerID;
                RefreshColorUpgradeCostLabel();
                RefreshTierUpgradeCostLabel();
                RefreshUpgradeTowerInfoLabel();
                SelectTower(upgradedTower);
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
        UnsubscribeGameManagerEvents();

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

            if (m_towerInfoPanel != null && m_towerInfoPanel.activeSelf)
            {
                RefreshUpgradeTowerInfoLabel();
            }
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
                int tierUpgradeGemCost = TowerManager.Instance.GetTierUpgradeCost(m_selectTowerID);
                if (tierUpgradeGemCost <= 0)
                {
                    m_txtTierUpgradeCostGem.text = "MAX";
                }
                else
                {
                    m_txtTierUpgradeCostGem.text = $"{tierUpgradeGemCost}";
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
            FindTowerInfoValueTexts();

            TowerStats stats = m_selectedTower.GetFinalStats();
            TowerData data = m_selectedTower.GetTowerData();

            if (m_txtTowerInfoName != null) m_txtTowerInfoName.text = stats.Name;
            RefreshTowerIdentityInfo(data != null ? data.towerID : stats.ID);
            if (m_txtTowerInfoLV != null) m_txtTowerInfoLV.text = $"UPGRADE : {stats.Level}";
            AttackType attackType = data != null ? data.attackType : AttackType.Target;
            bool isBuffTower = attackType == AttackType.Buff;
            bool isDebuffTower = attackType == AttackType.Debuff;

            if (m_txtTowerInfoPowerLabel != null)
            {
                m_txtTowerInfoPowerLabel.text = isBuffTower ? "버프력" : isDebuffTower ? "디버프력" : "공격력";
            }

            if (m_txtTowerInfoSpeedLabel != null)
            {
                m_txtTowerInfoSpeedLabel.text = (isBuffTower || isDebuffTower) ? "지속시간" : "공격횟수";
            }

            if (m_txtTowerInfoDamage != null)
            {
                if (isBuffTower)
                {
                    float buffPower = stats.AbilityValue;
                    m_txtTowerInfoDamage.text = $"+{buffPower * 100}%";
                }
                else if (isDebuffTower)
                {
                    m_txtTowerInfoDamage.text = $"{stats.AbilityValue:F2}";
                }
                else
                {
                    m_txtTowerInfoDamage.text = $"{stats.AttackPower:F2}";
                }
            }
            if (m_txtTowerInfoRange != null)
            {
                m_txtTowerInfoRange.text = isDebuffTower
                    ? "전체 지역"
                    : $"{TowerAttackAction.ToTileRange(stats.Range)}칸";
            }
            if (m_txtTowerInfoAction != null) m_txtTowerInfoAction.text = $"{m_selectedTower.GetRemainingAction()} / {m_selectedTower.GetMaxAction()}";
            if (m_txtTowerInfoSpeed != null)
            {
                m_txtTowerInfoSpeed.text = (isBuffTower || isDebuffTower)
                    ? $"{stats.Duration:F0}턴"
                    : $"x{stats.AttackCount}";
            }
            if (m_txtTowerInfoCriticalRate != null) m_txtTowerInfoCriticalRate.text = $"{(stats.CriticalRate * 100):F2}%";
            if (m_txtTowerInfoCriticalDamage != null) m_txtTowerInfoCriticalDamage.text = $"{((2 + stats.CriticalDamage) * 100):F0}%";
        }
    }

    private void RefreshTowerIdentityInfo(int towerID)
    {
        int tier = towerID / 1000;
        int color = (towerID % 1000) / 100;
        int emblem = towerID % 100;

        if (m_txtTowerInfoTier != null) m_txtTowerInfoTier.text = GetTowerTierName(tier);
        if (m_txtTowerInfoColor != null) m_txtTowerInfoColor.text = GetTowerColorName(color);
        if (m_txtTowerInfoEmblem != null) m_txtTowerInfoEmblem.text = GetTowerEmblemName(emblem);
    }

    private static string GetTowerTierName(int tier)
    {
        return tier switch
        {
            1 => "브론즈",
            2 => "실버",
            3 => "골드",
            4 => "미스릴",
            5 => "다이아몬드",
            _ => "-"
        };
    }

    private static string GetTowerColorName(int color)
    {
        return color switch
        {
            1 => "빨강",
            2 => "파랑",
            3 => "하양",
            4 => "검정",
            _ => "-"
        };
    }

    private static string GetTowerEmblemName(int emblem)
    {
        return emblem switch
        {
            1 => "검",
            2 => "활",
            3 => "방패",
            4 => "창",
            5 => "도끼",
            6 => "해머",
            7 => "불",
            8 => "얼음",
            9 => "전기",
            10 => "바람",
            11 => "대지",
            12 => "빛",
            13 => "어둠",
            _ => "-"
        };
    }

    private void UpdateWaveUI()
    {
        if (WaveManager.Instance != null)
        {
            int currentWave = WaveManager.Instance.GetWave();

            if (m_txtCurrentWaveIndex != null)
            {
                m_txtCurrentWaveIndex.text = $"Wave {currentWave}";
            }

            m_lastWave = currentWave;
        }
    }

    private void SubscribeGameManagerEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnPlayerTurnTimerUpdated += RefreshPlayerTurnRemainingTime;
        GameManager.Instance.OnTurnStateChanged += RefreshPlayerTurnStateUI;
    }

    private void UnsubscribeGameManagerEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnPlayerTurnTimerUpdated -= RefreshPlayerTurnRemainingTime;
        GameManager.Instance.OnTurnStateChanged -= RefreshPlayerTurnStateUI;
    }

    private void RefreshPlayerTurnRemainingTime(float remainingTime)
    {
        if (m_txtPlayerTurnRemainingTime == null) return;

        int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
        m_txtPlayerTurnRemainingTime.text = $"{displaySeconds}s";
    }

    private void RefreshPlayerTurnStateUI(TurnState turnState)
    {
        if (m_txtPlayerTurnRemainingTime == null) return;

        if (turnState != TurnState.PlayerTurn)
        {
            m_txtPlayerTurnRemainingTime.text = "-";
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
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

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
        if (!m_isPlayerActionUIVisible) return;

        if (m_playerActionPanel != null)
        {
            m_playerActionPanel.SetActive(isShow);
            return;
        }

        SetMainActionButtonsVisible(isShow);
    }

    /// <summary>
    /// 턴 상태가 바뀔 때 행동 UI만 표시하거나 숨깁니다.
    /// 타워/적 정보 패널은 건드리지 않으므로 적 턴에도 정보를 확인할 수 있습니다.
    /// </summary>
    public void SetPlayerActionUI(bool isVisible)
    {
        m_isPlayerActionUIVisible = isVisible;

        if (m_playerActionPanel != null)
        {
            m_playerActionPanel.SetActive(isVisible);
        }
        else
        {
            // 씬에 새 패널이 아직 반영되지 않은 경우의 안전장치입니다.
            SetMainActionButtonsVisible(isVisible);
        }
    }

    private void SetMainActionButtonsVisible(bool isVisible)
    {
        if (m_btnCreateTower != null) m_btnCreateTower.gameObject.SetActive(isVisible);
        if (m_btnColorUpgradeTower != null) m_btnColorUpgradeTower.gameObject.SetActive(isVisible);
        if (m_btnTierUpgradeTower != null) m_btnTierUpgradeTower.gameObject.SetActive(isVisible);
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
