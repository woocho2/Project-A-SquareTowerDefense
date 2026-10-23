using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    public class PlayerTurnPanel : MonoBehaviour
    {
        [SerializeField] Button btn_createTower;
        [SerializeField] TextMeshProUGUI txt_createTowerValue;
        [SerializeField] Button btn_colorUpgrade;
        [SerializeField] TextMeshProUGUI txt_colorUpgradeValue;
        [SerializeField] Button btn_tierUpgrade;
        [SerializeField] TextMeshProUGUI txt_tierUpgradeValue;
        [SerializeField] Button btn_turnEnd;

        private TowerController m_selectedTower;

        private void Start()
        {
            TowerManager.Instance.BuildCostChanged += HandleBuildCostChanged;
            CurrencyManager.Instance.Changed += HandleCurrencyChanged;
            if (UIManager.Instance != null) UIManager.Instance.SelectedTowerChanged += HandleTowerSelected;
            // 수업 20: ColorUpgradeState.LevelChanged += HandleUpgradeChanged
            TowerManager.Instance.TowerReplaced += HandleTowerReplaced;
            TowerManager.Instance.TowerRemoved += HandleTowerRemoved;

            if (btn_createTower != null) btn_createTower.onClick.AddListener(HandleCreateTowerClick);
            if (btn_colorUpgrade != null) btn_colorUpgrade.onClick.AddListener(HandleColorUpgradeClick);
            if (btn_tierUpgrade != null) btn_tierUpgrade.onClick.AddListener(HandleTierUpgradeClick);
            if (btn_turnEnd != null) btn_turnEnd.onClick.AddListener(HandleTurnEndClick);

            Refresh();
        }

        private void OnEnable()
        {
            if (TowerManager.Instance != null && CurrencyManager.Instance != null) Refresh();
        }

        private void OnDestroy()
        {
            if (TowerManager.Instance != null)
            {
                TowerManager.Instance.BuildCostChanged -= HandleBuildCostChanged;
                TowerManager.Instance.TowerReplaced -= HandleTowerReplaced;
                TowerManager.Instance.TowerRemoved -= HandleTowerRemoved;
            }
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= HandleCurrencyChanged;
            if (UIManager.Instance != null) UIManager.Instance.SelectedTowerChanged -= HandleTowerSelected;

            if (btn_createTower != null) btn_createTower.onClick.RemoveListener(HandleCreateTowerClick);
            if (btn_colorUpgrade != null) btn_colorUpgrade.onClick.RemoveListener(HandleColorUpgradeClick);
            if (btn_tierUpgrade != null) btn_tierUpgrade.onClick.RemoveListener(HandleTierUpgradeClick);
            if (btn_turnEnd != null) btn_turnEnd.onClick.RemoveListener(HandleTurnEndClick);
        }

        private void HandleBuildCostChanged(int cost) => Refresh();

        private void HandleCurrencyChanged(int gold, int gem) => Refresh();

        private void HandleTowerSelected(TowerController tower)
        {
            m_selectedTower = tower;
            Refresh();
        }

        private void HandleTowerReplaced(TowerController old, TowerController created)
        {
            if (m_selectedTower != old) return;
            m_selectedTower = created;
            Refresh();
        }

        private void HandleTowerRemoved(TowerController tower)
        {
            if (m_selectedTower != tower) return;
            m_selectedTower = null;
            Refresh();
        }

        private void Refresh()
        {
            int buildCost = TowerManager.Instance.BuildCost;
            if (txt_createTowerValue != null) txt_createTowerValue.text = $"{buildCost}";
            if (btn_createTower != null) btn_createTower.interactable = CurrencyManager.Instance.Gold >= buildCost;

            RefreshUpgradeCostTexts();
        }

        private void RefreshUpgradeCostTexts()
        {
            // 수업 20: 컬러 강화 비용 = ColorUpgradeState.GetCost(선택한 타워의 컬러), 레벨 5면 "MAX", 젬이 모자라면 버튼을 끔
            if (txt_colorUpgradeValue != null) txt_colorUpgradeValue.text = "-";
            if (btn_colorUpgrade != null) btn_colorUpgrade.interactable = false;

            if (m_selectedTower == null)
            {
                if (txt_tierUpgradeValue != null) txt_tierUpgradeValue.text = "-";
                if (btn_tierUpgrade != null) btn_tierUpgrade.interactable = false;
                return;
            }

            int tierCost = TowerId.IsSynergy(m_selectedTower.Definition.Id) ? 0 : TowerProgression.GetTierUpgradeCost(m_selectedTower.Definition.Tier);
            if (txt_tierUpgradeValue != null) txt_tierUpgradeValue.text = FormatUpgradeCost(tierCost);
            if (btn_tierUpgrade != null) btn_tierUpgrade.interactable = tierCost > 0 && CurrencyManager.Instance.Gem >= tierCost;
        }

        private static string FormatUpgradeCost(int cost)
        {
            return cost > 0 ? $"{cost}" : "MAX";
        }

        private void HandleCreateTowerClick()
        {
            TowerManager.Instance.TryBuild();
        }

        private void HandleColorUpgradeClick()
        {
            if (m_selectedTower == null)
            {
                Debug.LogWarning("[PlayerTurnPanel] 컬러 강화할 타워가 선택되지 않음");
                return;
            }

            // 수업 20: TowerManager.Instance.TryUpgradeColor(m_selectedTower.Definition.Color)
        }

        private void HandleTierUpgradeClick()
        {
            if (m_selectedTower == null)
            {
                Debug.LogWarning("[PlayerTurnPanel] 티어 강화할 타워가 선택되지 않음");
                return;
            }

            TowerManager.Instance.TryTierUpgrade(m_selectedTower);      // 성공하면 TowerReplaced로 새 타워를 이어서 고름
        }

        private void HandleTurnEndClick()
        {
            TurnManager.Instance.OnClickEndTurn();
        }
    }
}
