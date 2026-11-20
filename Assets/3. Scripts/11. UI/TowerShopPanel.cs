using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 프리팹에 배치된 상점 카드와 잠금 버튼을 게임 상태에 연결
    public sealed class TowerShopPanel : MonoBehaviour
    {
        [Serializable]
        private sealed class CardView
        {
            public Button Button;
            public Image Background, Band, PortraitFrame, Portrait, Emblem;
            public TextMeshProUGUI Tier, Role, Name, Price, Footer, EmptyStatus;
        }

        [SerializeField] private CardView[] m_cards = new CardView[BalanceTables.ShopSize];
        [SerializeField] private Button m_lockButton;
        [SerializeField] private TextMeshProUGUI[] m_tierChanceLabels = new TextMeshProUGUI[TowerId.MaxTier];

        [Header("상점 상태 색")]
        [SerializeField] private Color m_cardColor = new Color32(34, 46, 68, 255);
        [SerializeField] private Color m_unavailableColor = new Color32(24, 33, 50, 255);
        [SerializeField] private Color m_textColor = new Color32(247, 246, 235, 255);
        [SerializeField] private Color m_mutedColor = new Color32(167, 184, 208, 255);
        [SerializeField] private Color m_goldColor = new Color32(246, 210, 117, 255);
        [SerializeField] private Color m_shortColor = new Color32(255, 156, 152, 255);
        [SerializeField] private Color m_fullColor = new Color32(199, 183, 251, 255);
        [SerializeField] private Color m_lockOffColor = new Color32(128, 128, 128, 255);
        [SerializeField] private Color[] m_tierColors =
        {
            Color.clear, new Color32(215, 157, 110, 255), new Color32(203, 216, 234, 255),
            new Color32(248, 207, 98, 255), new Color32(105, 228, 202, 255), new Color32(132, 220, 255, 255),
        };

        private TowerCharacterCatalog m_catalog;
        private TowerShop m_shop;
        private CurrencyManager m_currency;
        private ColorUpgradeState m_playerLevel;
        private Image m_lockIcon;
        private bool? m_shownLocked;
        private bool m_hasSpace;

        private void Start()
        {
            m_catalog = Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog");
            TowerManager towers = TowerManager.Instance;
            m_currency = CurrencyManager.Instance;
            m_shop = towers.Shop;
            m_playerLevel = towers.ColorLevels;

            for (int i = 0; i < m_cards.Length; i++)
            {
                int index = i;
                m_cards[i].Button.onClick.AddListener(() => towers.TryBuyCard(index));
            }
            if (m_lockButton != null)
            {
                m_lockButton.onClick.AddListener(towers.ToggleShopLock);
                foreach (Image image in m_lockButton.GetComponentsInChildren<Image>(true))
                    if (image.gameObject != m_lockButton.gameObject) { m_lockIcon = image; break; }
            }
            else Debug.LogError("[TowerShopPanel] 잠금 버튼이 연결되지 않음 (UICanvas 프리팹의 m_lockButton)", this);

            m_shop.Changed += Refresh;
            m_currency.Changed += HandleGoldChanged;
            m_playerLevel.LevelChanged += RefreshTierChances;
            RefreshTierChances(m_playerLevel.Level);
            Refresh();
        }

        private void OnEnable()
        {
            if (m_shop != null) Refresh();
        }

        private void LateUpdate()
        {
            if (HasSpace() != m_hasSpace) Refresh();
        }

        private void OnDestroy()
        {
            if (m_shop != null) m_shop.Changed -= Refresh;
            if (m_currency != null) m_currency.Changed -= HandleGoldChanged;
            if (m_playerLevel != null) m_playerLevel.LevelChanged -= RefreshTierChances;
            if (m_lockIcon != null) m_lockIcon.DOKill();
        }

        private void HandleGoldChanged(int gold) => Refresh();

        private void RefreshTierChances(int level)
        {
            for (int i = 0; i < m_tierChanceLabels.Length; i++)
                if (m_tierChanceLabels[i] != null)
                    m_tierChanceLabels[i].text = BalanceTables.GetTierChance(level, i + 1).ToString("0.#%");
        }

        private static bool HasSpace() =>
            BoardManager.Instance != null && TowerManager.TryGetBuildSlot(BoardManager.Instance, out _, out _);

        private void Refresh()
        {
            m_hasSpace = HasSpace();
            int gold = m_currency.Gold;
            TowerIcons icons = TowerIcons.Instance;

            for (int i = 0; i < m_cards.Length; i++)
            {
                CardView view = m_cards[i];
                TowerShop.Card card = m_shop.Cards[i];
                bool available = card.IsAvailable;
                view.Button.interactable = available;
                view.Tier.enabled = view.Price.enabled = view.Name.enabled = view.Role.enabled = available;
                view.PortraitFrame.enabled = view.Footer.enabled = available;
                view.EmptyStatus.enabled = !available;

                if (!available)
                {
                    view.Background.color = m_unavailableColor;
                    view.Band.color = Color.clear;
                    view.Portrait.enabled = view.Emblem.enabled = false;
                    view.EmptyStatus.text = card.Sold ? "구매 완료" : "빈 상품";
                    view.EmptyStatus.color = m_mutedColor;
                    continue;
                }

                int tier = TowerId.GetTier(card.TowerId);
                TowerColor color = TowerId.GetColor(card.TowerId);
                TowerEmblem emblem = TowerId.GetEmblem(card.TowerId);
                bool affordable = gold >= card.Price;
                view.Background.color = m_hasSpace && affordable ? m_cardColor : m_unavailableColor;
                view.Band.color = view.Tier.color = m_tierColors[tier];
                view.Tier.text = EmblemNames.GetTierName(tier);
                view.Price.text = $"{card.Price:N0} G";
                view.Price.color = affordable ? m_goldColor : m_shortColor;
                view.Name.text = EmblemNames.GetEmblemName(emblem);
                view.Role.text = EmblemNames.GetColorName(color);
                view.Role.color = color == TowerColor.Purple ? m_fullColor : Color.Lerp(icons.GetColor(color), m_textColor, .35f);
                view.Emblem.sprite = icons.GetEmblem(emblem);
                view.Emblem.enabled = view.Emblem.sprite != null;
                view.Portrait.sprite = m_catalog != null ? m_catalog.GetTowerCharacter((int)emblem, (int)color) : null;
                view.Portrait.enabled = view.Portrait.sprite != null;
                view.Portrait.color = affordable && m_hasSpace ? Color.white : new Color(1, 1, 1, .52f);
                view.Footer.text = !m_hasSpace ? "배치할 자리 없음" : !affordable ? "골드 부족" : EmblemNames.GetConceptName(card.TowerId);
                view.Footer.color = !m_hasSpace ? m_fullColor : !affordable ? m_shortColor : m_mutedColor;
            }

            RefreshLock();
        }

        private void RefreshLock()
        {
            bool locked = m_shop.Locked;
            if (m_lockIcon == null || m_shownLocked == locked) return;

            Color color = locked ? m_goldColor : m_lockOffColor;
            m_lockIcon.DOKill();
            if (m_shownLocked == null) m_lockIcon.color = color;
            else m_lockIcon.DOColor(color, .14f).SetUpdate(true);
            m_shownLocked = locked;
        }
    }
}
