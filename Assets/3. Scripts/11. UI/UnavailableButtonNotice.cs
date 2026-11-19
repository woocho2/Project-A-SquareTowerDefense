using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SquareTD
{
    // 비활성 구매 버튼도 클릭하면 부족한 골드·최대 레벨 이유를 안내
    public sealed class UnavailableButtonNotice : MonoBehaviour, IPointerClickHandler
    {
        private Button m_button;
        private bool m_experience;
        public static void Bind(Button button, bool experience)
        {
            if (button == null) return;
            var feedback = button.GetComponent<UnavailableButtonNotice>(); if (feedback == null) feedback = button.gameObject.AddComponent<UnavailableButtonNotice>();
            feedback.m_button = button; feedback.m_experience = experience;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || m_button == null || m_button.interactable || TowerManager.Instance == null || CurrencyManager.Instance == null) return;
            if (m_experience && TowerManager.Instance.ColorLevels.IsMax) { GameNotice.Warn("이미 최대 플레이어 레벨입니다."); return; }
            int cost = m_experience ? BalanceTables.XpPurchaseCost : BalanceTables.ShopRerollCost;
            GameNotice.Warn($"골드가 부족합니다. 필요 {cost:N0} / 보유 {CurrencyManager.Instance.Gold:N0}");
        }
    }
}
