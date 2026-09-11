using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        if (GameManager.Instance != null)
        {
            SetPlayerActionUI(GameManager.Instance.CanPerformPlayerAction);
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
        int totalTargetHitCount = 1 + Mathf.Max(0, stats.AdditionalHitCount);

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
                        $"{stats.ProjectileRadius}칸 범위의 모든 적에게\n{stats.AbilityValue:0.##}만큼의 피해 (쿨타임: {stats.Duration:0.##}턴)";
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
                        $"대상에게 {stats.Duration:0.##}회 공격 적중 시\n{stats.AbilityValue:0.##} 적용";
                }
                break;

            case AttackType.Buff:
                if (m_txtTowerInfoDefault != null)
                {
                    m_txtTowerInfoDefault.text =
                        $"{tileRange}칸 범위 아군 타워의\n{GetBuffTargetName(data.buffTarget)} 능력 {stats.AbilityValue:0.##} 증가";
                }

                if (m_txtTowerInfoSkill != null) m_txtTowerInfoSkill.text = "스킬 미구현";
                break;

            case AttackType.Debuff:
                if (m_txtTowerInfoDefault != null)
                {
                    m_txtTowerInfoDefault.text =
                        $"디버프존 위 모든 적에게\n{GetDebuffTargetName(data.debuffTarget)} 디버프 부여";
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
            DebuffTarget.Fire => "화상",
            DebuffTarget.Ice => "빙결",
            DebuffTarget.Wind => "바람",
            DebuffTarget.Darkness => "암흑",
            _ => "일반"
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
