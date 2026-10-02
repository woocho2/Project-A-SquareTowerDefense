using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerInfoPanel : MonoBehaviour
{
    [Header("타워 정보")]
    [SerializeField] TextMeshProUGUI txt_level;
    [SerializeField] TextMeshProUGUI txt_name;
    [SerializeField] TextMeshProUGUI txt_tier;
    [SerializeField] TextMeshProUGUI txt_color;
    [SerializeField] TextMeshProUGUI txt_emblem;
    [SerializeField] Image img_tier;
    [SerializeField] Image img_color;
    [SerializeField] Image img_emblem;

    [Header("타겟 우선순위")]
    [SerializeField] TextMeshProUGUI txt_targetPriority;
    [SerializeField] Button btn_previousTarget;
    [SerializeField] Button btn_nextTarget;

    [Header("스탯")]
    [SerializeField] TextMeshProUGUI txt_power;
    [SerializeField] TextMeshProUGUI txt_range;
    [SerializeField] TextMeshProUGUI txt_action;
    [SerializeField] TextMeshProUGUI txt_attackCount;
    [SerializeField] TextMeshProUGUI txt_criticalRate;
    [SerializeField] TextMeshProUGUI txt_criticalDamage;

    [Header("설명")]
    [SerializeField] TextMeshProUGUI txt_default;
    [SerializeField] TextMeshProUGUI txt_skill;

    [Header("합성 및 판매")]
    [SerializeField] Button btn_combineColor;
    [SerializeField] Button btn_combineEmblem;
    [SerializeField] Button btn_combineExact;
    [SerializeField] Button btn_sellTower;
    [SerializeField] TextMeshProUGUI txt_sellTowerValue;

    private static readonly TargetPriority[] s_targetPriorityOrder =
    {
        TargetPriority.Closest,
        TargetPriority.Strongest,
        TargetPriority.Weakest,
        TargetPriority.First,
        TargetPriority.Last
    };

    private TowerController m_selectedTower;

    private void OnEnable()
    {
        BoardInputController.OnTowerClickedAction += HandleTowerClicked;

        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.TowerTierUpgraded += HandleTowerTierUpgraded;
        }

        // 패널이 꺼져 있는 동안 선택된 타워는 UIManager에서 가져옵니다.
        if (UIManager.Instance != null)
        {
            m_selectedTower = UIManager.Instance.SelectedTower;
        }

        UpdateTowerInfo();
    }

    private void OnDisable()
    {
        BoardInputController.OnTowerClickedAction -= HandleTowerClicked;

        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.TowerTierUpgraded -= HandleTowerTierUpgraded;
        }
    }

    private void Start()
    {
        if (GameManager.Instance == null || TowerManager.Instance == null || UIManager.Instance == null)
        {
            Debug.LogError("TowerInfoPanel : GameManager/TowerManager/UIManager가 없습니다.");
            return;
        }

        btn_previousTarget.onClick.AddListener(HandlePreviousTargetClick);
        btn_nextTarget.onClick.AddListener(HandleNextTargetClick);
        btn_combineColor.onClick.AddListener(HandleCombineColorClick);
        btn_combineEmblem.onClick.AddListener(HandleCombineEmblemClick);
        btn_combineExact.onClick.AddListener(HandleCombineExactClick);
        btn_sellTower.onClick.AddListener(HandleSellTowerClick);
    }

    private void OnDestroy()
    {
        if (btn_previousTarget != null)
        {
            btn_previousTarget.onClick.RemoveListener(HandlePreviousTargetClick);
        }
        if (btn_nextTarget != null)
        {
            btn_nextTarget.onClick.RemoveListener(HandleNextTargetClick);
        }
        if (btn_combineColor != null)
        {
            btn_combineColor.onClick.RemoveListener(HandleCombineColorClick);
        }
        if (btn_combineEmblem != null)
        {
            btn_combineEmblem.onClick.RemoveListener(HandleCombineEmblemClick);
        }
        if (btn_combineExact != null)
        {
            btn_combineExact.onClick.RemoveListener(HandleCombineExactClick);
        }
        if (btn_sellTower != null)
        {
            btn_sellTower.onClick.RemoveListener(HandleSellTowerClick);
        }
    }

    // 놓친 변경이 있어도 이 시간(초) 안에는 화면이 따라오도록 하는 안전 장치입니다.
    private const float FallbackRefreshInterval = 0.5f;

    private int m_shownStateVersion = -1;
    private float m_nextFallbackRefreshTime;

    private void Update()
    {
        // 게임 상태가 바뀌었을 때만 다시 그립니다. 매 프레임 최종 스탯을 계산하고 글자를 새로 쓰지 않습니다.
        if (m_shownStateVersion == GameStateVersion.Current && Time.unscaledTime < m_nextFallbackRefreshTime) return;

        UpdateTowerInfo();
    }

    private void HandleTowerClicked(TowerController tower)
    {
        m_selectedTower = tower;
        UpdateTowerInfo();
    }

    private void HandleTowerTierUpgraded(TowerController upgradedTower)
    {
        // 티어 강화로 교체된 기존 타워는 SpawnIndex가 -1이 됩니다.
        if (m_selectedTower == null || m_selectedTower.SpawnIndex >= 0) return;

        m_selectedTower = upgradedTower;
        UpdateTowerInfo();
    }

    private void UpdateTowerInfo()
    {
        m_shownStateVersion = GameStateVersion.Current;
        m_nextFallbackRefreshTime = Time.unscaledTime + FallbackRefreshInterval;

        if (m_selectedTower == null) return;

        TowerStats stats = m_selectedTower.GetFinalStats();
        TowerData data = m_selectedTower.GetTowerData();
        int towerID = data != null ? data.towerID : stats.ID;

        txt_level.text = $"{stats.Level}";
        txt_name.text = stats.Name;

        UpdateIdentityText(towerID);
        UpdateIdentityImage();
        UpdateTargetPriorityText();
        UpdateStatText(stats);
        UpdateDescriptionText(data, stats);
        UpdateCombineButton();
    }

    private void UpdateIdentityText(int towerID)
    {
        int tier = towerID / 1000;
        int color = (towerID % 1000) / 100;
        int emblem = towerID % 100;

        txt_tier.text = GetTierName(tier);
        txt_color.text = GetColorName(color);
        txt_emblem.text = GetEmblemName(emblem);
        txt_sellTowerValue.text = tier >= 1 ? $"{Mathf.RoundToInt(Mathf.Pow(3f, tier - 1))}" : "-";
    }

    private void UpdateIdentityImage()
    {
        TowerVisual towerVisual = m_selectedTower.GetComponentInChildren<TowerVisual>(true);

        UpdateImage(img_tier, towerVisual != null ? towerVisual.TierSprite : null);
        UpdateImage(img_color, towerVisual != null ? towerVisual.ColorSprite : null);
        UpdateImage(img_emblem, towerVisual != null ? towerVisual.EmblemSprite : null);
    }

    private void UpdateImage(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = sprite != null;
    }

    private void UpdateTargetPriorityText()
    {
        bool canChange = m_selectedTower.CanChangeTargetPriority && GameManager.Instance.CanPerformPlayerAction;

        btn_previousTarget.interactable = canChange;
        btn_nextTarget.interactable = canChange;
        txt_targetPriority.text = m_selectedTower.CanChangeTargetPriority
            ? GetTargetPriorityName(m_selectedTower.GetTargetPriority())
            : "-";
    }

    private void UpdateStatText(TowerStats stats)
    {
        txt_power.text = $"{stats.AttackPower:F2}";
        txt_range.text = $"{TowerAttackAction.ToTileRange(stats.Range)}칸";
        txt_action.text = $"{m_selectedTower.GetMaxAction()}";
        txt_attackCount.text = $"{stats.AttackCount}";
        txt_criticalRate.text = $"{stats.CriticalRate * 100f:F2}%";
        txt_criticalDamage.text = $"{(2f + stats.CriticalDamage) * 100f:F0}%";
    }

    private void UpdateDescriptionText(TowerData data, TowerStats stats)
    {
        if (data == null)
        {
            txt_default.text = "-";
            txt_skill.text = "-";
            return;
        }

        int tileRange = TowerAttackAction.ToTileRange(stats.Range);
        int totalHitCount = Mathf.Max(1, stats.AttackCount) * (1 + Mathf.Max(0, stats.AdditionalHitCount));

        switch (data.attackType)
        {
            case AttackType.Splash:
                txt_default.text = $"{stats.ProjectileRadius}칸 범위의 모든 적에게\n{stats.AttackPower:0.##}만큼의 피해";
                txt_skill.text = $"{stats.ProjectileRadius}칸 범위의 모든 적에게\n{stats.AttackPower * stats.AbilityValue:0.##}만큼의 피해 (쿨타임: {stats.Duration:0.##}턴)";
                break;

            case AttackType.Target:
                txt_default.text = $"대상에게 {stats.AttackPower:0.##}만큼의 피해\n총 {totalHitCount}회 적중";
                txt_skill.text = $"대상에게 {stats.Duration:0.##}회 공격 적중 시\n{stats.AttackPower * stats.AbilityValue:0.##} 피해";
                break;

            case AttackType.Buff:
                txt_default.text = $"{tileRange}칸 범위 아군 타워의\n{BuffTargetNames.GetBuffTargetName(data.buffTarget)} 능력 {stats.AttackPower:0.##} 증가";
                txt_skill.text = "스킬 미구현";
                break;

            case AttackType.Debuff:
                txt_default.text = $"디버프존 위 모든 적에게\n{BuffTargetNames.GetDebuffTargetName(data.debuffTarget)} 디버프 {stats.AttackPower:0.##} 부여 ({stats.Duration:0.##}턴)";
                txt_skill.text = "스킬 미구현";
                break;
        }
    }

    private void UpdateCombineButton()
    {
        int spawnIndex = m_selectedTower.SpawnIndex;

        btn_combineColor.interactable = TowerManager.Instance.CanCombine(spawnIndex, CombineMode.ColorMatch);
        btn_combineEmblem.interactable = TowerManager.Instance.CanCombine(spawnIndex, CombineMode.EmblemMatch);
        btn_combineExact.interactable = TowerManager.Instance.CanCombine(spawnIndex, CombineMode.ExactMatch);
    }

    private void HandlePreviousTargetClick()
    {
        ChangeTargetPriority(-1);
    }

    private void HandleNextTargetClick()
    {
        ChangeTargetPriority(1);
    }

    private void ChangeTargetPriority(int direction)
    {
        if (m_selectedTower == null || !m_selectedTower.CanChangeTargetPriority) return;
        if (!GameManager.Instance.CanPerformPlayerAction) return;

        int currentIndex = Mathf.Max(0, System.Array.IndexOf(s_targetPriorityOrder, m_selectedTower.GetTargetPriority()));
        int nextIndex = (currentIndex + direction + s_targetPriorityOrder.Length) % s_targetPriorityOrder.Length;

        m_selectedTower.SetTargetPriority(s_targetPriorityOrder[nextIndex]);
        UpdateTargetPriorityText();
    }

    private void HandleCombineColorClick()
    {
        CombineTower(CombineMode.ColorMatch);
    }

    private void HandleCombineEmblemClick()
    {
        CombineTower(CombineMode.EmblemMatch);
    }

    private void HandleCombineExactClick()
    {
        CombineTower(CombineMode.ExactMatch);
    }

    private void CombineTower(CombineMode mode)
    {
        if (m_selectedTower == null || !GameManager.Instance.CanPerformPlayerAction) return;

        TowerManager.Instance.ExecuteCombine(m_selectedTower.SpawnIndex, mode);
        CloseAfterTowerRemoved();
    }

    private void HandleSellTowerClick()
    {
        if (m_selectedTower == null || !GameManager.Instance.CanPerformPlayerAction) return;

        TowerManager.Instance.SellTower(m_selectedTower.SpawnIndex);
        CloseAfterTowerRemoved();
    }

    private void CloseAfterTowerRemoved()
    {
        // 선택한 타워가 사라지므로 선택을 풀고 패널을 닫습니다.
        BoardInputController.CancelAllInteractions();
        m_selectedTower = null;
        UIManager.Instance.CloseTowerInfoPanel();
    }

    private string GetTargetPriorityName(TargetPriority priority)
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

    private string GetTierName(int tier)
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

    private string GetColorName(int color)
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

    private string GetEmblemName(int emblem)
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
}
