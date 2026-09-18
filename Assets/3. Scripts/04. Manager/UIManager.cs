using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Game Info")]
    [SerializeField] GameObject m_gameInfoPanel;
    [SerializeField] Image[] m_LifePoints;
    [SerializeField] Image[] m_ShieldPoints;
    [SerializeField] TextMeshProUGUI m_txtCurrentGold;
    [SerializeField] TextMeshProUGUI m_txtCurrentGem;
    [SerializeField] TextMeshProUGUI m_txtCurrentWaveIndex;
    [SerializeField] Button m_btnGameSpeed;
    [SerializeField] TextMeshProUGUI m_txtGameSpeed;
    [SerializeField] Button m_btnOption;


    [Header("UI Player Action")]
    [SerializeField] GameObject m_playerTurnPanel;
    [SerializeField] Button m_btnCreateTower;
    [SerializeField] TextMeshProUGUI m_txtCreateCostGold;
    [SerializeField] Button m_btnColorUpgradeTower;
    [SerializeField] TextMeshProUGUI m_txtColorUpgradeCostGem;
    [SerializeField] Button m_btnTierUpgradeTower;
    [SerializeField] TextMeshProUGUI m_txtTierUpgradeCostGem;
    [SerializeField] Button m_btnSpawnMiddleBoss;
    [SerializeField] Button m_btnEndPlayerTurn;

    [Header("UI Tower Info")]
    [SerializeField] GameObject m_towerInfoPanel;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoLevel;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoName;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoTier;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoColor;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoEmblem;
    [SerializeField] Image m_imgTowerInfoTier;
    [SerializeField] Image m_imgTowerInfoColor;
    [SerializeField] Image m_imgTowerInfoEmblem;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoTargetType;
    [SerializeField] Button m_btnPreviousTargetType;
    [SerializeField] Button m_btnNextTargetType;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoPowerLabel; 
    [SerializeField] TextMeshProUGUI m_txtTowerInfoPower;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoRange;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoAction;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoAttackCount;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoCriticalRate;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoCriticalDamage;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoDefault;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoSkill;
    [SerializeField] TextMeshProUGUI m_txtTowerInfoSellTowerGem;
    [SerializeField] Button m_btnCombineColor;
    [SerializeField] Button m_btnCombineEmblem;
    [SerializeField] Button m_btnCombineExact;
    [SerializeField] Button m_btnSellTower;

    [Header("UI Tile Info")]
    [Tooltip("타일을 클릭했을 때 표시할 패널")]
    [SerializeField] GameObject m_tileInfoPanel;
    [Tooltip("버프 항목을 생성할 부모 RectTransform. ScrollRect를 쓴다면 Content를 연결합니다.")]
    [SerializeField] RectTransform m_tileBuffContent;
    [Tooltip("디버프 항목을 생성할 부모 RectTransform. ScrollRect를 쓴다면 Content를 연결합니다.")]
    [SerializeField] RectTransform m_tileDebuffContent;
    [Tooltip("버프 한 줄을 표시하는 UI 프리팹. 하위에 TextMeshProUGUI 하나가 필요합니다.")]
    [SerializeField] GameObject m_tileBuffEntryPrefab;
    [Tooltip("디버프 한 줄을 표시하는 UI 프리팹. 하위에 TextMeshProUGUI 하나가 필요합니다.")]
    [SerializeField] GameObject m_tileDebuffEntryPrefab;
    [Tooltip("선택 사항: 버프 목록만 스크롤할 ScrollRect")]
    [SerializeField] ScrollRect m_tileBuffScrollRect;
    [Tooltip("선택 사항: 디버프 목록만 스크롤할 ScrollRect")]
    [SerializeField] ScrollRect m_tileDebuffScrollRect;
    [SerializeField] float m_tileBuffStartY = 150f;
    [SerializeField] float m_tileDebuffStartY = 105f;
    [SerializeField] float m_tileEntrySpacing = 75f;
    [Tooltip("두 정보 패널 안에 있는 '타워 정보로 전환' 버튼들을 모두 넣습니다.")]
    [SerializeField] Button[] m_btnChangeTowerInfoPanels = System.Array.Empty<Button>();
    [Tooltip("두 정보 패널 안에 있는 '타일 정보로 전환' 버튼들을 모두 넣습니다.")]
    [SerializeField] Button[] m_btnChangeTileInfoPanels = System.Array.Empty<Button>();

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

    [SerializeField] string m_sceneName_Restart;
    [SerializeField] string m_sceneName_Menu;

    private TowerController m_selectedTower;
    private Vector3Int m_selectedTowerInfo;

    private int m_selectTowerID = 0;
    private int currentLifeIndex;
    private int m_lastWave = -1;
    private bool m_isPlayerActionUIVisible;
    private WaveManager m_waveManager;
    private readonly List<GameObject> m_spawnedTileBuffEntries = new List<GameObject>();
    private readonly List<GameObject> m_spawnedTileDebuffEntries = new List<GameObject>();
    private Vector3Int m_selectedTileCell;
    private bool m_selectedTileIsTowerSpawn;
    private bool m_hasSelectedTile;

    private static readonly TargetPriority[] s_targetPriorityOrder =
    {
        TargetPriority.Closest,
        TargetPriority.Strongest,
        TargetPriority.Weakest,
        TargetPriority.First,
        TargetPriority.Last
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindUITexts();
    }

    // TowerInfoPanel 아래의 값 텍스트를 이름으로 자동 연결합니다.
    // 인스펙터에 직접 연결한 레퍼런스는 유지하고, 비어 있는 항목만 찾습니다.
    private void FindUITexts()
    {
        if (m_gameInfoPanel != null)
        {
            m_txtCurrentGold ??= FindGameInfoText("Txt_Gold");
            m_txtCurrentGem ??= FindGameInfoText("Txt_Gem");
            m_txtCurrentWaveIndex ??= FindGameInfoText("Txt_Wave");
            m_txtGameSpeed ??= FindGameInfoText("Txt_GameSpeed");
        }

        if (m_playerTurnPanel != null)
        {
            m_txtCreateCostGold ??= FindPlayerTurnText("Txt_CreateTowerValue");
            m_txtColorUpgradeCostGem ??= FindPlayerTurnText("Txt_ColorUpTowerValue");
            m_txtTierUpgradeCostGem ??= FindPlayerTurnText("Txt_TierUpTowerValue");
        }

        if (m_towerInfoPanel != null)
        {
            m_txtTowerInfoLevel ??= FindTowerInfoText("Txt_LV_Value");
            m_txtTowerInfoName ??= FindTowerInfoText("Txt_Name");
            m_txtTowerInfoTier ??= FindTowerInfoText("Txt_Tier");
            m_txtTowerInfoColor ??= FindTowerInfoText("Txt_Color");
            m_txtTowerInfoEmblem ??= FindTowerInfoText("Txt_Emblem");
            m_txtTowerInfoTargetType ??= FindTowerInfoText("Txt_Target");
            m_txtTowerInfoPower ??= FindTowerInfoText("Txt_PowerValue");
            m_txtTowerInfoRange ??= FindTowerInfoText("Txt_RangeValue");
            m_txtTowerInfoAction ??= FindTowerInfoText("Txt_ActionValue");
            m_txtTowerInfoAttackCount ??= FindTowerInfoText("Txt_AtteckCountValue");
            m_txtTowerInfoCriticalRate ??= FindTowerInfoText("Txt_CriticalRateValue");
            m_txtTowerInfoCriticalDamage ??= FindTowerInfoText("Txt_CriticalDamageValue");
            m_txtTowerInfoDefault ??= FindTowerInfoText("Txt_Default");
            m_txtTowerInfoSkill ??= FindTowerInfoText("Txt_Skill");
            m_txtTowerInfoSellTowerGem ??= FindTowerInfoText("Txt_SellTowerValueName");
        }
    }



    private TextMeshProUGUI FindTextInPanel(GameObject rootPanel, string objectName)
    {
        if (rootPanel == null) return null;

        TextMeshProUGUI[] texts =
            rootPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    private TextMeshProUGUI FindGameInfoText(string objectName)
    {
        return FindTextInPanel(m_gameInfoPanel, objectName);
    }

    private TextMeshProUGUI FindPlayerTurnText(string objectName)
    {
        return FindTextInPanel(m_playerTurnPanel, objectName);
    }

    private TextMeshProUGUI FindTowerInfoText(string objectName)
    {
        return FindTextInPanel(m_towerInfoPanel, objectName);
    }


    private void OnEnable()
    {
        DragObjectOnGround.OnTowerClickedAction += ShowTowerpanel;
        GlobalClickDetector.OnGroundWorldClickedAction += HandleGroundWorldClick;
    }

    private void OnDisable()
    {
        DragObjectOnGround.OnTowerClickedAction -= ShowTowerpanel;
        GlobalClickDetector.OnGroundWorldClickedAction -= HandleGroundWorldClick;
    }

    private void Start()
    {
        InitUI();

        if (GameManager.Instance != null)
        {
            SetPlayerActionUI(GameManager.Instance.CanPerformPlayerAction);
            GameManager.Instance.OnShieldChanged += RefreshShieldPoints;
            RefreshShieldPoints(GameManager.Instance.CurrentShield);
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

        if (m_btnOption != null)
        {
            m_btnOption.onClick.RemoveAllListeners();
            m_btnOption.onClick.AddListener(() =>
            {
                m_btnOption.gameObject.SetActive(false);
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

        BindMiddleBossSummonButton();

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

        if (m_btnSellTower != null)
        {
            m_btnSellTower.onClick.RemoveAllListeners();
            m_btnSellTower.onClick.AddListener(SellSelectedTower);
        }

        BindTargetPriorityButtons();
        BindInfoPanelSwitchButtons();

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShieldChanged -= RefreshShieldPoints;
        }

        if (m_waveManager != null)
        {
            m_waveManager.MiddleBossSummonAvailabilityChanged -= RefreshMiddleBossSummonButton;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BindMiddleBossSummonButton()
    {
        if (m_btnSpawnMiddleBoss != null)
        {
            m_btnSpawnMiddleBoss.onClick.RemoveAllListeners();
            m_btnSpawnMiddleBoss.onClick.AddListener(() =>
            {
                WaveManager.Instance?.TrySpawnMiddleBoss();
                RefreshMiddleBossSummonButton();
            });
        }

        m_waveManager = WaveManager.Instance;
        if (m_waveManager != null)
        {
            m_waveManager.MiddleBossSummonAvailabilityChanged += RefreshMiddleBossSummonButton;
        }

        RefreshMiddleBossSummonButton();
    }

    private void RefreshMiddleBossSummonButton()
    {
        if (m_btnSpawnMiddleBoss == null) return;

        bool canSummon = WaveManager.Instance != null && WaveManager.Instance.CanSummonMiddleBoss;
        m_btnSpawnMiddleBoss.gameObject.SetActive(canSummon);
        m_btnSpawnMiddleBoss.interactable = canSummon;
    }

    private void Update()
    {
        UpdateWaveUI();

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

        if (m_btnOption != null)
        {
            m_btnOption.gameObject.SetActive(true);
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
        if (m_selectedTower == null || m_towerInfoPanel == null || !m_towerInfoPanel.activeSelf)
        {
            return;
        }

        FindUITexts();

        TowerStats stats = m_selectedTower.GetFinalStats();
        TowerData data = m_selectedTower.GetTowerData();
        int towerID = data != null ? data.towerID : stats.ID;
        int tier = towerID / 1000;

        if (m_txtTowerInfoLevel != null) m_txtTowerInfoLevel.text = stats.Level.ToString();
        if (m_txtTowerInfoName != null) m_txtTowerInfoName.text = stats.Name;
        RefreshTowerIdentityInfo(towerID);
        RefreshTowerInfoVisuals();

        RefreshTargetPriorityUI();

        if (m_txtTowerInfoPower != null) m_txtTowerInfoPower.text = $"{stats.AttackPower:F2}";
        if (m_txtTowerInfoRange != null) m_txtTowerInfoRange.text = $"{TowerAttackAction.ToTileRange(stats.Range)}칸";
        if (m_txtTowerInfoAction != null) m_txtTowerInfoAction.text = m_selectedTower.GetMaxAction().ToString();
        if (m_txtTowerInfoAttackCount != null) m_txtTowerInfoAttackCount.text = stats.AttackCount.ToString();
        if (m_txtTowerInfoCriticalRate != null) m_txtTowerInfoCriticalRate.text = $"{stats.CriticalRate * 100f:F2}%";
        if (m_txtTowerInfoCriticalDamage != null) m_txtTowerInfoCriticalDamage.text = $"{(2f + stats.CriticalDamage) * 100f:F0}%";

        RefreshTowerInfoDescriptions(data, stats);

        if (m_txtTowerInfoSellTowerGem != null)
        {
            m_txtTowerInfoSellTowerGem.text = tier >= 1
                ? Mathf.RoundToInt(Mathf.Pow(3f, tier - 1)).ToString()
                : "-";
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

    private void RefreshTowerInfoVisuals()
    {
        TowerVisual towerVisual = m_selectedTower != null
            ? m_selectedTower.GetComponentInChildren<TowerVisual>(true)
            : null;

        SetTowerInfoImage(m_imgTowerInfoTier, towerVisual != null ? towerVisual.TierSprite : null);
        SetTowerInfoImage(m_imgTowerInfoColor, towerVisual != null ? towerVisual.ColorSprite : null);
        SetTowerInfoImage(m_imgTowerInfoEmblem, towerVisual != null ? towerVisual.EmblemSprite : null);
    }

    private static void SetTowerInfoImage(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = sprite != null;
    }

    private void RefreshTowerInfoDescriptions(TowerData data, TowerStats stats)
    {
        if (data == null)
        {
            if (m_txtTowerInfoDefault != null) m_txtTowerInfoDefault.text = "-";
            if (m_txtTowerInfoSkill != null) m_txtTowerInfoSkill.text = "-";
            return;
        }

        int tileRange = TowerAttackAction.ToTileRange(stats.Range);
        int totalTargetHitCount =
            Mathf.Max(1, stats.AttackCount) * (1 + Mathf.Max(0, stats.AdditionalHitCount));

        switch (data.attackType)
        {
            case AttackType.Splash:
                if (m_txtTowerInfoDefault != null)
                {
                    m_txtTowerInfoDefault.text =
                        $"{stats.ProjectileRadius}칸 범위의 모든 적에게\n{stats.AttackPower:0.##}만큼의 피해";
                }

                if (m_txtTowerInfoSkill != null)
                {
                    m_txtTowerInfoSkill.text =
                        $"{stats.ProjectileRadius}칸 범위의 모든 적에게\n{stats.AttackPower * stats.AbilityValue:0.##}만큼의 피해 (쿨타임: {stats.Duration:0.##}턴)";
                }
                break;

            case AttackType.Target:
                if (m_txtTowerInfoDefault != null)
                {
                    m_txtTowerInfoDefault.text =
                        $"대상에게 {stats.AttackPower:0.##}만큼의 피해\n총 {totalTargetHitCount}회 적중";
                }

                if (m_txtTowerInfoSkill != null)
                {
                    m_txtTowerInfoSkill.text =
                        $"대상에게 {stats.Duration:0.##}회 공격 적중 시\n{stats.AttackPower * stats.AbilityValue:0.##} 피해";
                }
                break;

            case AttackType.Buff:
                if (m_txtTowerInfoDefault != null)
                {
                    m_txtTowerInfoDefault.text =
                        $"{tileRange}칸 범위 아군 타워의\n{GetBuffTargetName(data.buffTarget)} 능력 {stats.AttackPower:0.##} 증가";
                }

                if (m_txtTowerInfoSkill != null) m_txtTowerInfoSkill.text = "스킬 미구현";
                break;

            case AttackType.Debuff:
                if (m_txtTowerInfoDefault != null)
                {
                    m_txtTowerInfoDefault.text =
                        $"디버프존 위 모든 적에게\n{GetDebuffTargetName(data.debuffTarget)} 디버프 {stats.AttackPower:0.##} 부여 ({stats.Duration:0.##}턴)";
                }

                if (m_txtTowerInfoSkill != null) m_txtTowerInfoSkill.text = "스킬 미구현";
                break;
        }
    }

    private static string GetBuffTargetName(BuffTarget buffTarget)
    {
        return buffTarget switch
        {
            BuffTarget.Sword => "공격력",
            BuffTarget.Bow => "사거리",
            BuffTarget.Shield => "방어막",
            BuffTarget.Spear => "치명타 확률",
            BuffTarget.Axe => "치명타 피해",
            BuffTarget.Hammer => "방어 관통",
            BuffTarget.Fire => "공격 횟수",
            BuffTarget.Ice => "스플래시/추가 타격",
            BuffTarget.Electricity => "추가 공격",
            BuffTarget.Wind => "행동력",
            BuffTarget.Earth => "티어 강화",
            BuffTarget.Light => "체인 공격",
            BuffTarget.Darkness => "다크니스 강화",
            _ => "버프"
        };
    }

    private static string GetDebuffTargetName(DebuffTarget debuffTarget)
    {
        return debuffTarget switch
        {
            DebuffTarget.Sword => "저주",
            DebuffTarget.Bow => "고정 피해",
            DebuffTarget.Shield => "속박",
            DebuffTarget.Spear => "투창",
            DebuffTarget.Axe => "취약",
            DebuffTarget.Hammer => "방어 파괴",
            DebuffTarget.Fire => "화상",
            DebuffTarget.Ice => "빙결",
            DebuffTarget.Electricity => "감전",
            DebuffTarget.Wind => "바람",
            DebuffTarget.Earth => "대지",
            DebuffTarget.Light => "빛",
            DebuffTarget.Darkness => "암흑",
            _ => "디버프"
        };
    }

    private void BindTargetPriorityButtons()
    {
        if (m_btnPreviousTargetType != null)
        {
            m_btnPreviousTargetType.onClick.RemoveAllListeners();
            m_btnPreviousTargetType.onClick.AddListener(() => ChangeSelectedTowerTargetPriority(-1));
        }

        if (m_btnNextTargetType != null)
        {
            m_btnNextTargetType.onClick.RemoveAllListeners();
            m_btnNextTargetType.onClick.AddListener(() => ChangeSelectedTowerTargetPriority(1));
        }
    }

    private void ChangeSelectedTowerTargetPriority(int direction)
    {
        if (m_selectedTower == null || !m_selectedTower.CanChangeTargetPriority)
        {
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction)
        {
            return;
        }

        int currentIndex = System.Array.IndexOf(s_targetPriorityOrder, m_selectedTower.GetTargetPriority());
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int nextIndex = (currentIndex + direction + s_targetPriorityOrder.Length) % s_targetPriorityOrder.Length;
        m_selectedTower.SetTargetPriority(s_targetPriorityOrder[nextIndex]);
        RefreshTargetPriorityUI();
    }

    private void RefreshTargetPriorityUI()
    {
        bool canChangeTarget = m_selectedTower != null &&
            m_selectedTower.CanChangeTargetPriority &&
            (GameManager.Instance == null || GameManager.Instance.CanPerformPlayerAction);

        if (m_btnPreviousTargetType != null)
        {
            m_btnPreviousTargetType.interactable = canChangeTarget;
        }

        if (m_btnNextTargetType != null)
        {
            m_btnNextTargetType.interactable = canChangeTarget;
        }

        if (m_txtTowerInfoTargetType != null)
        {
            m_txtTowerInfoTargetType.text = m_selectedTower != null && m_selectedTower.CanChangeTargetPriority
                ? GetTargetPriorityName(m_selectedTower.GetTargetPriority())
                : "-";
        }
    }

    private static string GetTargetPriorityName(TargetPriority priority)
    {
        return priority switch
        {
            TargetPriority.Strongest => "체력 많은 적",
            TargetPriority.Weakest => "체력 적은 적",
            TargetPriority.First => "결승점에 가까운 적",
            TargetPriority.Last => "결승점에서 먼 적",
            TargetPriority.Closest => "가까운 적",
            TargetPriority.Default => "가까운 적",
            _ => "-"
        };
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

    public void InitUI()
    {
        if (m_gameInfoPanel != null) m_gameInfoPanel.SetActive(true);
        if (m_panelOption != null) m_panelOption.SetActive(false);
        if (m_panelGameover != null) m_panelGameover.SetActive(false);
        if (m_panelGameclear != null) m_panelGameclear.SetActive(false);
        if (m_btnOption != null) m_btnOption.gameObject.SetActive(true);
        if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(false);
        if (m_tileInfoPanel != null) m_tileInfoPanel.SetActive(false);
        RefreshInfoPanelSwitchButtons();

        currentLifeIndex = m_LifePoints.Length - 1;

        foreach (var hpImage in m_LifePoints)
        {
            if (hpImage != null) hpImage.gameObject.SetActive(true);
        }

        // 게임 시작 시에는 쉴드가 없으므로 LifeArmor 아이콘을 모두 숨깁니다.
        RefreshShieldPoints(GameManager.Instance != null ? GameManager.Instance.CurrentShield : 0);
    }

    /// <summary>
    /// 현재 쉴드 수만큼 LifeArmor 아이콘을 왼쪽부터 표시합니다.
    /// 쉴드 타워의 행동 충전과 피해 흡수 시 GameManager 이벤트로 즉시 호출됩니다.
    /// </summary>
    private void RefreshShieldPoints(int shieldCount)
    {
        if (m_ShieldPoints == null) return;

        int visibleShieldCount = Mathf.Clamp(shieldCount, 0, m_ShieldPoints.Length);
        for (int index = 0; index < m_ShieldPoints.Length; index++)
        {
            Image shieldPoint = m_ShieldPoints[index];
            if (shieldPoint != null)
            {
                shieldPoint.gameObject.SetActive(index < visibleShieldCount);
            }
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
        HideTileInfoPanel();
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
        RefreshInfoPanelSwitchButtons();
    }

    private void HideTowerPanel()
    {
        if (m_towerInfoPanel != null && m_towerInfoPanel.activeSelf)
        {
            m_towerInfoPanel.SetActive(false);
        }
        RefreshInfoPanelSwitchButtons();
    }

    /// <summary>
    /// 빈 월드 클릭 중 실제 타일을 클릭한 경우, 타일 종류에 맞는 Dictionary 내용을 패널에 출력합니다.
    /// 타워 스폰 타일과 패스 타일은 서로 다른 Tilemap이므로 각각 먼저 확인합니다.
    /// </summary>
    private void HandleGroundWorldClick(Vector3 worldPosition)
    {
        HideTowerPanel();

        if (TryGetTowerSpawnTileAtWorldPosition(worldPosition, out Vector3Int towerCell))
        {
            ShowTileInfoPanel(towerCell, true);
            return;
        }

        if (TileManager.Instance != null && TileManager.Instance.TryGetPathCellAtWorldPosition(worldPosition, out Vector3Int pathCell))
        {
            ShowTileInfoPanel(pathCell, false);
            return;
        }

        HideTileInfoPanel();
    }

    private static bool TryGetTowerSpawnTileAtWorldPosition(Vector3 worldPosition, out Vector3Int cell)
    {
        cell = default;
        if (TowerManager.Instance == null) return false;

        Tilemap spawnTilemap = TowerManager.Instance.GetSpawnPointTilemap();
        if (spawnTilemap == null) return false;

        cell = spawnTilemap.WorldToCell(worldPosition);
        return spawnTilemap.HasTile(cell);
    }

    /// <summary>
    /// 선택한 타일의 고정 맵 효과와 동적 버프/디버프 효과를 각각 항목 프리팹으로 표시합니다.
    /// </summary>
    private void ShowTileInfoPanel(Vector3Int cell, bool isTowerSpawnTile)
    {
        if (m_tileInfoPanel == null) return;

        m_selectedTileCell = cell;
        m_selectedTileIsTowerSpawn = isTowerSpawnTile;
        m_hasSelectedTile = true;

        ClearTileInfoEntries(m_spawnedTileBuffEntries);
        ClearTileInfoEntries(m_spawnedTileDebuffEntries);

        List<string> buffDescriptions = new List<string>();
        List<string> debuffDescriptions = new List<string>();

        if (TileManager.Instance != null)
        {
            if (isTowerSpawnTile)
            {
                AddTowerMapTileDescription(TileManager.Instance.GetTowerTileBuffAt(cell), buffDescriptions);
                IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(cell);
                for (int i = 0; i < effects.Count; i++)
                {
                    TowerTileBuffEffect effect = effects[i];
                    buffDescriptions.Add($"{GetBuffTargetName(effect.Target)} 버프\n티어 {effect.Tier} · 지속 {effect.RemainingTurns}턴");
                }
            }
            else
            {
                AddPathMapTileDescription(TileManager.Instance.GetTileTypeAt(cell), buffDescriptions);
                IReadOnlyList<PathTileDebuffEffect> effects = TileManager.Instance.GetPathDebuffEffectsAt(cell);
                for (int i = 0; i < effects.Count; i++)
                {
                    PathTileDebuffEffect effect = effects[i];
                    string state = effect.ApplicationVersion <= 0
                        ? "첫 행동 대기"
                        : $"지속 {effect.Duration}턴";
                    debuffDescriptions.Add($"{GetDebuffTargetName(effect.Target)} 디버프\n티어 {effect.Tier} · {state}");
                }
            }
        }

        // ScrollRect가 연결되어 있다면 외부 Rect가 아니라 실제 Content에 생성해야
        // Viewport 마스크와 스크롤바가 정상적으로 목록을 제어합니다.
        RectTransform buffContent = m_tileBuffScrollRect != null && m_tileBuffScrollRect.content != null
            ? m_tileBuffScrollRect.content
            : m_tileBuffContent;
        RectTransform debuffContent = m_tileDebuffScrollRect != null && m_tileDebuffScrollRect.content != null
            ? m_tileDebuffScrollRect.content
            : m_tileDebuffContent;

        CreateTileInfoEntries(
            m_tileBuffEntryPrefab,
            buffContent,
            buffDescriptions,
            m_tileBuffStartY,
            m_spawnedTileBuffEntries);
        CreateTileInfoEntries(
            m_tileDebuffEntryPrefab,
            debuffContent,
            debuffDescriptions,
            m_tileDebuffStartY,
            m_spawnedTileDebuffEntries);

        m_tileInfoPanel.SetActive(true);
        if (m_tileBuffScrollRect != null) m_tileBuffScrollRect.verticalNormalizedPosition = 1f;
        if (m_tileDebuffScrollRect != null) m_tileDebuffScrollRect.verticalNormalizedPosition = 1f;
        RefreshInfoPanelSwitchButtons();
    }

    private void HideTileInfoPanel()
    {
        if (m_tileInfoPanel != null && m_tileInfoPanel.activeSelf)
        {
            m_tileInfoPanel.SetActive(false);
        }
        RefreshInfoPanelSwitchButtons();
    }

    private void BindInfoPanelSwitchButtons()
    {
        foreach (Button button in m_btnChangeTowerInfoPanels)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SwitchToTowerInfoPanel);
        }

        foreach (Button button in m_btnChangeTileInfoPanels)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SwitchToTileInfoPanel);
        }

        RefreshInfoPanelSwitchButtons();
    }

    private void SwitchToTowerInfoPanel()
    {
        if (m_selectedTower != null && !m_selectedTower.Equals(null))
        {
            ShowTowerpanel(m_selectedTower);
            return;
        }

        if (m_hasSelectedTile && m_selectedTileIsTowerSpawn &&
            TowerManager.Instance != null && TowerManager.Instance.TryGetTowerAt(m_selectedTileCell, out TowerController tower))
        {
            ShowTowerpanel(tower);
            return;
        }

        Debug.Log("[UIManager] 선택된 타워가 없어 타워 정보 패널로 전환할 수 없습니다.");
    }

    private void SwitchToTileInfoPanel()
    {
        if (m_selectedTower != null && !m_selectedTower.Equals(null) && TowerManager.Instance != null)
        {
            ShowTileInfoPanel(TowerManager.Instance.WorldToCell(m_selectedTower.transform.position), true);
            HideTowerPanel();
            return;
        }

        if (m_hasSelectedTile)
        {
            HideTowerPanel();
            ShowTileInfoPanel(m_selectedTileCell, m_selectedTileIsTowerSpawn);
        }
    }

    /// <summary>
    /// 현재 열려 있는 정보 패널에 해당하는 전환 버튼은 비활성화합니다.
    /// Button의 Transition이 Color Tint이면 interactable=false 상태가 자동으로 회색으로 표현됩니다.
    /// </summary>
    private void RefreshInfoPanelSwitchButtons()
    {
        bool isTowerInfoOpen = m_towerInfoPanel != null && m_towerInfoPanel.activeSelf;
        bool isTileInfoOpen = m_tileInfoPanel != null && m_tileInfoPanel.activeSelf;

        foreach (Button button in m_btnChangeTowerInfoPanels)
        {
            if (button != null) button.interactable = !isTowerInfoOpen;
        }

        foreach (Button button in m_btnChangeTileInfoPanels)
        {
            if (button != null) button.interactable = !isTileInfoOpen;
        }
    }

    private void CreateTileInfoEntries(
        GameObject entryPrefab,
        RectTransform content,
        List<string> descriptions,
        float startY,
        List<GameObject> spawnedEntries)
    {
        if (entryPrefab == null || content == null) return;

        for (int index = 0; index < descriptions.Count; index++)
        {
            GameObject entry = Instantiate(entryPrefab, content);
            entry.name = $"{entryPrefab.name}_{index + 1}";
            entry.SetActive(true);

            RectTransform entryRect = entry.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, startY - m_tileEntrySpacing * index);
            }

            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = descriptions[index];
            }
            else
            {
                Debug.LogWarning($"[UIManager] {entryPrefab.name} 프리팹에서 TextMeshProUGUI를 찾지 못했습니다.");
            }

            spawnedEntries.Add(entry);
        }

        // Content 높이를 늘려 ScrollRect가 항목 전체를 스크롤할 수 있게 합니다.
        if (descriptions.Count > 0)
        {
            float requiredHeight = Mathf.Abs(startY - m_tileEntrySpacing * (descriptions.Count - 1)) + m_tileEntrySpacing;
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(content.sizeDelta.y, requiredHeight));
        }
    }

    private static void ClearTileInfoEntries(List<GameObject> entries)
    {
        foreach (GameObject entry in entries)
        {
            if (entry != null) Destroy(entry);
        }
        entries.Clear();
    }

    private static void AddTowerMapTileDescription(TowerTileBuffType type, List<string> descriptions)
    {
        switch (type)
        {
            case TowerTileBuffType.AttackPowerUp:
                descriptions.Add("공격력 타일\n공격력 50% 증가");
                break;
            case TowerTileBuffType.ActionCountUp:
                descriptions.Add("행동력 타일\n행동력 1 감소");
                break;
            case TowerTileBuffType.AttackCountUp:
                descriptions.Add("공격 횟수 타일\n공격 횟수 1 증가");
                break;
        }
    }

    private static void AddPathMapTileDescription(SpecialTileType type, List<string> descriptions)
    {
        switch (type)
        {
            case SpecialTileType.DefendTile:
                descriptions.Add("방어 타일\n방어력 20% 증가");
                break;
            case SpecialTileType.SpeedTile:
                descriptions.Add("속도 타일\n행동 주기 1 감소 · 이동 주사위 최대값 +2");
                break;
            case SpecialTileType.HealTile:
                descriptions.Add("회복 타일\n현재 체력의 20% 회복");
                break;
        }
    }

    private void SellSelectedTower()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction)
        {
            return;
        }

        if (m_selectedTower == null || TowerManager.Instance == null)
        {
            return;
        }

        TowerManager.Instance.SellTower(m_selectedTowerInfo);
        m_selectedTower = null;
        m_selectTowerID = 0;
        HideTowerPanel();
    }

    public void SelectTower(TowerController tower)
    {
        m_selectedTower = tower;
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

    /// <summary>
    /// 턴 상태가 바뀔 때 행동 UI만 표시하거나 숨깁니다.
    /// 타워/적 정보 패널은 건드리지 않으므로 적 턴에도 정보를 확인할 수 있습니다.
    /// </summary>
    public void SetPlayerActionUI(bool isVisible)
    {
        m_isPlayerActionUIVisible = isVisible;

        // 턴 종료 버튼이 PlayerTurnPanel 밖에 배치되어 있어도
        // 플레이어 턴 상태와 항상 같은 표시 상태를 유지합니다.
        if (m_btnEndPlayerTurn != null)
        {
            m_btnEndPlayerTurn.gameObject.SetActive(isVisible);
        }

        if (m_playerTurnPanel != null)
        {
            m_playerTurnPanel.SetActive(isVisible);
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
