using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 턴의 행동 버튼(타워 생성·컬러 강화·티어 강화·중간 보스 소환·턴 종료)과 비용 표시를 담당합니다.
/// 패널을 켜고 끄는 것은 UIManager가 하고, 표시 내용은 각 매니저의 이벤트를 구독해 갱신합니다.
/// </summary>
public class PlayerTurnPanel : MonoBehaviour
{
    [SerializeField] Button btn_createTower;
    [SerializeField] TextMeshProUGUI txt_createTowerValue;
    [SerializeField] Button btn_colorUpgrade;
    [SerializeField] TextMeshProUGUI txt_colorUpgradeValue;
    [SerializeField] Button btn_tierUpgrade;
    [SerializeField] TextMeshProUGUI txt_tierUpgradeValue;
    [SerializeField] Button btn_summonMiddleBoss;
    [SerializeField] Button btn_turnEnd;

    // 강화 대상은 마지막으로 클릭한 타워입니다.
    private TowerController m_selectedTower;

    private void Start()
    {
        if (GameManager.Instance == null || TowerManager.Instance == null || WaveManager.Instance == null)
        {
            Debug.LogError("PlayerTurnPanel : GameManager/TowerManager/WaveManager가 없습니다.");
            return;
        }

        TowerManager.Instance.CurrentCreateTowerValueChanged += UpdateCreateTowerText;
        TowerManager.Instance.CurrentColorUpgradeValueChanged += HandleUpgradeCostChanged;
        TowerManager.Instance.CurrentTierUpgradeValueChanged += HandleUpgradeCostChanged;
        WaveManager.Instance.MiddleBossSummonableChanged += RefreshMiddleBossButton;
        BoardInputController.OnTowerClickedAction += HandleTowerClicked;

        btn_createTower.onClick.AddListener(HandleCreateTowerClick);
        btn_colorUpgrade.onClick.AddListener(HandleColorUpgradeClick);
        btn_tierUpgrade.onClick.AddListener(HandleTierUpgradeClick);
        btn_summonMiddleBoss.onClick.AddListener(HandleSummonMiddleBossClick);
        btn_turnEnd.onClick.AddListener(HandleTurnEndClick);

        UpdateCreateTowerText(TowerManager.Instance.CreateTowerGold);
        RefreshUpgradeCostTexts();
        RefreshMiddleBossButton();
    }

    private void OnDestroy()
    {
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.CurrentCreateTowerValueChanged -= UpdateCreateTowerText;
            TowerManager.Instance.CurrentColorUpgradeValueChanged -= HandleUpgradeCostChanged;
            TowerManager.Instance.CurrentTierUpgradeValueChanged -= HandleUpgradeCostChanged;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.MiddleBossSummonableChanged -= RefreshMiddleBossButton;
        }

        BoardInputController.OnTowerClickedAction -= HandleTowerClicked;

        if (btn_createTower != null)
        {
            btn_createTower.onClick.RemoveListener(HandleCreateTowerClick);
        }
        if (btn_colorUpgrade != null)
        {
            btn_colorUpgrade.onClick.RemoveListener(HandleColorUpgradeClick);
        }
        if (btn_tierUpgrade != null)
        {
            btn_tierUpgrade.onClick.RemoveListener(HandleTierUpgradeClick);
        }
        if (btn_summonMiddleBoss != null)
        {
            btn_summonMiddleBoss.onClick.RemoveListener(HandleSummonMiddleBossClick);
        }
        if (btn_turnEnd != null)
        {
            btn_turnEnd.onClick.RemoveListener(HandleTurnEndClick);
        }
    }

    private void UpdateCreateTowerText(int amount)
    {
        txt_createTowerValue.text = $"{amount}";
    }

    // 강화 비용은 선택한 타워에 따라 달라지므로, 이벤트 값 대신 현재 선택 기준으로 다시 계산합니다.
    private void HandleUpgradeCostChanged(int amount)
    {
        RefreshUpgradeCostTexts();
    }

    private void HandleTowerClicked(TowerController tower)
    {
        m_selectedTower = tower;
        RefreshUpgradeCostTexts();
    }

    private void RefreshUpgradeCostTexts()
    {
        TowerData data = m_selectedTower != null ? m_selectedTower.GetTowerData() : null;
        if (data == null || TowerManager.Instance == null)
        {
            // 선택한 타워가 없으면 비용을 표시하지 않습니다.
            if (txt_colorUpgradeValue != null) txt_colorUpgradeValue.text = "-";
            if (txt_tierUpgradeValue != null) txt_tierUpgradeValue.text = "-";
            return;
        }

        if (txt_colorUpgradeValue != null)
        {
            txt_colorUpgradeValue.text = FormatUpgradeCost(TowerManager.Instance.GetColorUpgradeCost(data.towerID));
        }
        if (txt_tierUpgradeValue != null)
        {
            txt_tierUpgradeValue.text = FormatUpgradeCost(TowerManager.Instance.GetTierUpgradeCost(data.towerID));
        }
    }

    private static string FormatUpgradeCost(int cost)
    {
        return cost > 0 ? $"{cost}" : "MAX";
    }

    private void RefreshMiddleBossButton()
    {
        if (btn_summonMiddleBoss == null) return;

        bool canSummon = WaveManager.Instance != null && WaveManager.Instance.CanSummonMiddleBoss;
        btn_summonMiddleBoss.gameObject.SetActive(canSummon);
        btn_summonMiddleBoss.interactable = canSummon;
    }

    private void HandleCreateTowerClick()
    {
        TowerManager.Instance?.BuildTower();
    }

    private void HandleColorUpgradeClick()
    {
        TowerData data = m_selectedTower != null ? m_selectedTower.GetTowerData() : null;
        if (data == null || TowerManager.Instance == null)
        {
            Debug.LogWarning("업그레이드할 타워가 선택되지 않았습니다.");
            return;
        }

        TowerManager.Instance.UpgradeTower(data.towerID);
    }

    private void HandleTierUpgradeClick()
    {
        if (m_selectedTower == null || TowerManager.Instance == null)
        {
            Debug.LogWarning("티어 강화할 타워가 선택되지 않았습니다.");
            return;
        }

        // 강화에 성공하면 기존 타워가 새 타워로 교체되므로 선택도 새 타워로 옮깁니다.
        if (TowerManager.Instance.UpgradeTowerTier(m_selectedTower.SpawnIndex, out TowerController upgradedTower))
        {
            m_selectedTower = upgradedTower;
            RefreshUpgradeCostTexts();
        }
    }

    private void HandleSummonMiddleBossClick()
    {
        WaveManager.Instance?.TrySpawnMiddleBoss();
    }

    private void HandleTurnEndClick()
    {
        GameManager.Instance?.OnClickSkipPlayerTurn();
    }
}
