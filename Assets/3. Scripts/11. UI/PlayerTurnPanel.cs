using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 플레이어 턴의 버튼들 (생성, 경험치 구매, 턴 종료)
    public class PlayerTurnPanel : MonoBehaviour
    {
        [SerializeField] Button btn_createTower;
        [SerializeField] TextMeshProUGUI txt_createTowerValue;
        [SerializeField] Button btn_buyXp;
        [SerializeField] TextMeshProUGUI txt_buyXpCost;
        [SerializeField] TextMeshProUGUI txt_level;
        [SerializeField] TextMeshProUGUI txt_xp;
        [SerializeField] Image img_xpFill;
        [SerializeField] Button btn_turnEnd;

        private void Start()
        {
            CurrencyManager.Instance.Changed += HandleCurrencyChanged;
            TowerManager.Instance.ColorLevels.XpChanged += Refresh;

            if (btn_createTower != null) btn_createTower.onClick.AddListener(HandleCreateTowerClick);
            if (btn_buyXp != null) btn_buyXp.onClick.AddListener(HandleBuyXpClick);
            if (btn_turnEnd != null) btn_turnEnd.onClick.AddListener(HandleTurnEndClick);

            Refresh();
        }

        private void OnEnable()
        {
            if (TowerManager.Instance != null && CurrencyManager.Instance != null) Refresh();
        }

        private void OnDestroy()
        {
            if (TowerManager.Instance != null) TowerManager.Instance.ColorLevels.XpChanged -= Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= HandleCurrencyChanged;

            if (btn_createTower != null) btn_createTower.onClick.RemoveListener(HandleCreateTowerClick);
            if (btn_buyXp != null) btn_buyXp.onClick.RemoveListener(HandleBuyXpClick);
            if (btn_turnEnd != null) btn_turnEnd.onClick.RemoveListener(HandleTurnEndClick);
        }

        private void HandleCurrencyChanged(int gold) => Refresh();

        private void Refresh()
        {
            int buildCost = TowerManager.Instance.BuildCost;
            if (txt_createTowerValue != null) txt_createTowerValue.text = $"{buildCost}";
            if (btn_createTower != null) btn_createTower.interactable = CurrencyManager.Instance.Gold >= buildCost;

            ColorUpgradeState level = TowerManager.Instance.ColorLevels;
            int cost = level.IsMax ? 0 : BalanceTables.XpPurchaseCost;

            if (txt_level != null) txt_level.text = $"Lv.{level.Level}";
            if (txt_xp != null) txt_xp.text = level.IsMax ? "MAX" : $"{level.Xp} / {level.XpToNext}";
            if (img_xpFill != null) img_xpFill.fillAmount = level.IsMax ? 1f : (float)level.Xp / level.XpToNext;
            if (txt_buyXpCost != null) txt_buyXpCost.text = cost > 0 ? $"{cost}" : "MAX";
            if (btn_buyXp != null) btn_buyXp.interactable = cost > 0 && CurrencyManager.Instance.Gold >= cost;
        }

        private void HandleCreateTowerClick()
        {
            TowerManager.Instance.TryBuild();
        }

        private void HandleBuyXpClick()
        {
            TowerManager.Instance.TryBuyXp();
        }

        private void HandleTurnEndClick()
        {
            TurnManager.Instance.OnClickEndTurn();
        }
    }
}
