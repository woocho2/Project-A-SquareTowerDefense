using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    public class PlayerTurnPanel : MonoBehaviour
    {
        // 컬러마다 강화 칸 하나 (광역·단일·버프·디버프). 선택한 타워와 상관없이 플레이어가 고른 컬러를 강화
        [Serializable]
        private class ColorUpgradeSlot
        {
            public TowerColor color;
            public Button button;                   // Btn_SplashUp / Btn_TargetUp / Btn_BuffUp / Btn_DebuffUp
            public TextMeshProUGUI txt_level;       // Txt_B_…UpLv
            public TextMeshProUGUI txt_cost;        // Txt_A_…UpValue
        }

        [SerializeField] Button btn_createTower;
        [SerializeField] TextMeshProUGUI txt_createTowerValue;
        [SerializeField] ColorUpgradeSlot[] m_colorUpgrades =
        {
            new ColorUpgradeSlot { color = TowerColor.Red },
            new ColorUpgradeSlot { color = TowerColor.Blue },
            new ColorUpgradeSlot { color = TowerColor.Yellow },
            new ColorUpgradeSlot { color = TowerColor.Purple },
        };
        [SerializeField] Button btn_tierUpgrade;
        [SerializeField] TextMeshProUGUI txt_tierUpgradeValue;
        [SerializeField] Button btn_turnEnd;

        private TowerController m_selectedTower;

        private void Start()
        {
            TowerManager.Instance.BuildCostChanged += HandleBuildCostChanged;
            CurrencyManager.Instance.Changed += HandleCurrencyChanged;
            TowerManager.Instance.SelectionChanged += HandleTowerSelected;
            TowerManager.Instance.ColorLevels.LevelChanged += HandleColorLevelChanged;
            TowerManager.Instance.TowerReplaced += HandleTowerReplaced;
            TowerManager.Instance.TowerRemoved += HandleTowerRemoved;

            if (btn_createTower != null) btn_createTower.onClick.AddListener(HandleCreateTowerClick);
            foreach (ColorUpgradeSlot slot in m_colorUpgrades)
            {
                TowerColor color = slot.color;
                if (slot.button != null) slot.button.onClick.AddListener(() => HandleColorUpgradeClick(color));
            }
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
                TowerManager.Instance.SelectionChanged -= HandleTowerSelected;
                TowerManager.Instance.ColorLevels.LevelChanged -= HandleColorLevelChanged;
                TowerManager.Instance.TowerReplaced -= HandleTowerReplaced;
                TowerManager.Instance.TowerRemoved -= HandleTowerRemoved;
            }
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= HandleCurrencyChanged;

            if (btn_createTower != null) btn_createTower.onClick.RemoveListener(HandleCreateTowerClick);
            foreach (ColorUpgradeSlot slot in m_colorUpgrades)
            {
                if (slot.button != null) slot.button.onClick.RemoveAllListeners();     // 람다로 붙여서 하나씩 뗄 수 없음
            }
            if (btn_tierUpgrade != null) btn_tierUpgrade.onClick.RemoveListener(HandleTierUpgradeClick);
            if (btn_turnEnd != null) btn_turnEnd.onClick.RemoveListener(HandleTurnEndClick);
        }

        private void HandleBuildCostChanged(int cost) => Refresh();

        private void HandleCurrencyChanged(int gold, int gem) => Refresh();

        private void HandleColorLevelChanged(TowerColor color, int level) => Refresh();

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

            RefreshColorUpgrades();
            RefreshTierUpgrade();
        }

        private void RefreshColorUpgrades()
        {
            ColorUpgradeState levels = TowerManager.Instance.ColorLevels;
            int gem = CurrencyManager.Instance.Gem;

            foreach (ColorUpgradeSlot slot in m_colorUpgrades)
            {
                int cost = levels.GetCost(slot.color);
                if (slot.txt_level != null) slot.txt_level.text = $"Lv.{levels.GetLevel(slot.color)}";
                if (slot.txt_cost != null) slot.txt_cost.text = FormatUpgradeCost(cost);
                if (slot.button != null) slot.button.interactable = cost > 0 && gem >= cost;
            }
        }

        private void RefreshTierUpgrade()
        {
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

        private void HandleColorUpgradeClick(TowerColor color)
        {
            TowerManager.Instance.TryUpgradeColor(color);
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
