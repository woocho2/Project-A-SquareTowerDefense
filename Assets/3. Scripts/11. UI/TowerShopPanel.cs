using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 대기석 아래의 상점 카드 5장과 잠금 버튼 (카드를 누르면 구매)
    public sealed class TowerShopPanel : MonoBehaviour
    {
        private const float CardWidth = 136f, CardHeight = 172f, Gap = 10f;
        private static readonly Color s_cardColor = new Color32(23, 34, 58, 240);
        private static readonly Color s_text = new Color32(237, 241, 249, 255);
        private static readonly Color s_gold = new Color32(246, 205, 98, 255);
        private static readonly Color s_short = new Color32(246, 123, 119, 255);
        private static readonly Color[] s_tierColors =
        {
            Color.white, new Color32(205, 140, 82, 255), new Color32(200, 208, 220, 255), new Color32(246, 205, 98, 255), new Color32(132, 220, 255, 255),
        };

        private sealed class CardView
        {
            public Button Button;
            public CanvasGroup Group;
            public Image Band;
            public Image Portrait;
            public TextMeshProUGUI Role;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Price;
        }

        private readonly CardView[] m_cards = new CardView[BalanceTables.ShopSize];
        private TMP_FontAsset m_font;
        private TowerCharacterCatalog m_catalog;
        private TowerShop m_shop;
        private Button m_lock;
        private TextMeshProUGUI m_lockText;

        public static TowerShopPanel Create(Transform parent)
        {
            if (parent == null) return null;
            TowerShopPanel existing = parent.GetComponentInChildren<TowerShopPanel>(true);
            if (existing != null) return existing;
            var root = new GameObject("TowerShopPanel", typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(parent, false);
            return root.AddComponent<TowerShopPanel>();
        }

        private void Start()
        {
            m_font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-Black SDF");
            if (m_font == null) m_font = TMP_Settings.defaultFontAsset;
            m_catalog = Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog");
            Build();

            m_shop = TowerManager.Instance != null ? TowerManager.Instance.Shop : null;
            if (m_shop != null) m_shop.Changed += Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed += HandleGoldChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (m_shop != null) m_shop.Changed -= Refresh;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= HandleGoldChanged;
        }

        private void HandleGoldChanged(int gold) => Refresh();

        private void Build()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            float width = BalanceTables.ShopSize * CardWidth + (BalanceTables.ShopSize - 1) * Gap + Gap + 72f;
            rect.sizeDelta = new Vector2(width, CardHeight);
            rect.anchoredPosition = new Vector2(0f, 12f);

            for (int i = 0; i < m_cards.Length; i++)
            {
                int index = i;
                RectTransform card = Rect(rect, "Card_" + i, i * (CardWidth + Gap), 0f, CardWidth, CardHeight);
                Image background = card.gameObject.AddComponent<Image>();
                background.color = s_cardColor;
                var view = new CardView
                {
                    Button = card.gameObject.AddComponent<Button>(),
                    Group = card.gameObject.AddComponent<CanvasGroup>(),
                };
                view.Button.targetGraphic = background;
                view.Button.onClick.AddListener(() => TowerManager.Instance?.TryBuyCard(index));
                view.Band = Rect(card, "Tier", 0f, CardHeight - 6f, CardWidth, 6f).gameObject.AddComponent<Image>();
                view.Band.raycastTarget = false;
                view.Portrait = Rect(card, "Portrait", 8f, 54f, CardWidth - 16f, CardHeight - 66f).gameObject.AddComponent<Image>();
                view.Portrait.preserveAspect = true;
                view.Portrait.raycastTarget = false;
                view.Role = Text(card, "Role", 6f, 30f, CardWidth - 12f, 22f, 16);
                view.Name = Text(card, "Name", 6f, 52f, CardWidth - 12f, 18f, 11);
                view.Name.color = new Color32(168, 187, 209, 255);
                view.Price = Text(card, "Price", 6f, 4f, CardWidth - 12f, 24f, 18);
                m_cards[i] = view;
            }

            RectTransform lockRect = Rect(rect, "Lock", BalanceTables.ShopSize * (CardWidth + Gap), 0f, 72f, CardHeight);
            Image lockBackground = lockRect.gameObject.AddComponent<Image>();
            lockBackground.color = s_cardColor;
            m_lock = lockRect.gameObject.AddComponent<Button>();
            m_lock.targetGraphic = lockBackground;
            m_lock.onClick.AddListener(() => TowerManager.Instance?.ToggleShopLock());
            m_lockText = Text(lockRect, "Label", 4f, 0f, 64f, CardHeight, 15);
        }

        private void Refresh()
        {
            if (m_shop == null || m_cards[0] == null) return;
            int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0;
            for (int i = 0; i < m_cards.Length; i++)
            {
                CardView view = m_cards[i];
                TowerShop.Card card = i < m_shop.Cards.Count ? m_shop.Cards[i] : default;
                bool available = card.IsAvailable;
                view.Group.alpha = available ? 1f : 0.2f;
                view.Button.interactable = available;
                view.Portrait.enabled = available;
                if (!available)
                {
                    view.Role.text = view.Name.text = view.Price.text = "";
                    view.Band.color = Color.clear;
                    continue;
                }

                int tier = TowerId.GetTier(card.TowerId);
                TowerColor color = TowerId.GetColor(card.TowerId);
                TowerEmblem emblem = TowerId.GetEmblem(card.TowerId);
                view.Band.color = s_tierColors[Mathf.Clamp(tier, 0, s_tierColors.Length - 1)];
                view.Portrait.sprite = m_catalog != null ? m_catalog.GetTowerCharacter((int)emblem, (int)color) : null;
                view.Portrait.enabled = view.Portrait.sprite != null;
                view.Role.text = $"{EmblemNames.GetColorName(color)} / {EmblemNames.GetEmblemName(emblem)}";
                view.Name.text = $"{EmblemNames.GetTierName(tier)} {EmblemNames.GetConceptName(card.TowerId)}";
                view.Price.text = $"{card.Price:N0}G";
                view.Price.color = gold >= card.Price ? s_gold : s_short;
            }
            m_lockText.text = m_shop.Locked ? "잠김\n\n다음 턴에도\n유지" : "잠금";
            m_lockText.color = m_shop.Locked ? s_gold : s_text;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private TextMeshProUGUI Text(Transform parent, string name, float x, float y, float width, float height, int size)
        {
            TextMeshProUGUI text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = m_font;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size - 4;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = s_text;
            text.raycastTarget = false;
            return text;
        }
    }
}
