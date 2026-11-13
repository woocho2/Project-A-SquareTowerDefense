using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 메뉴 씬의 스테이지 고르기 — 카드 5장이 고리처럼 돌며 가운데 카드가 고른 스테이지, 누르면 입장
    public class StageSelectPanel : MonoBehaviour
    {
        private const int CenterSlot = 2;

        [Header("스테이지 (1 아스가르드 ~ 10 라그나로크 순서)")]
        [SerializeField] StageDefinition[] m_stages = new StageDefinition[10];
        [SerializeField] int m_startStage = 1;

        [Header("카드 (왼쪽부터: Left1, Left2, Main, Right2, Right1)")]
        [SerializeField] RectTransform[] m_cards = new RectTransform[5];

        [Header("입장 버튼 (화살표·HARD 토글은 카드마다 안의 Btn_Next·Btn_previous·Toggle_Mode를 찾음)")]
        [SerializeField] Button btn_stageSelect;

        [Header("입장")]
        [SerializeField] EmblemSelectPanel m_emblemPanel;
        [SerializeField] string m_fallbackScene = "3. Stage1";

        [Header("연출")]
        [SerializeField] float m_slideDuration = 0.35f;
        [SerializeField] Ease m_slideEase = Ease.OutCubic;
        [SerializeField] Color m_sideTint = new Color(0.82f, 0.82f, 0.86f, 1f);
        [Tooltip("판 그림의 여백 (x 왼쪽, y 오른쪽, z 위, w 아래)")]
        [SerializeField] Vector4 m_previewPadding = new Vector4(75f, 75f, 150f, 75f);

        private struct SlotLayout
        {
            public Vector2 Position;
            public Vector2 Size;
            public Vector3 Scale;
        }

        private class Card
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public StageMapGraphic Preview;
            public TextMeshProUGUI Title;
            public Toggle Mode;
            public CanvasGroup[] Arrows;
            public Graphic[] Tintable;
            public Color[] BaseColors;
            public Vector2 BaseSize;
        }

        private static Vector3 ScaleFor(Card card, SlotLayout layout)
        {
            float ratio = card.BaseSize.x > 0f ? layout.Size.x / card.BaseSize.x : 1f;
            return layout.Scale * ratio;
        }

        private readonly List<SlotLayout> m_slots = new List<SlotLayout>();
        private readonly List<Card> m_slotCards = new List<Card>();
        private readonly List<RectTransform> m_controls = new List<RectTransform>();
        private int m_center;
        private bool m_sliding;
        private Sequence m_sequence;

        private bool m_hard;
        private GameMode Mode => m_hard ? GameMode.Hard : GameMode.Normal;
        private StageDefinition CenterStage => GetStage(m_center);

        private static readonly string[] s_cardParts = { "Select_BG", "Btn_Next", "Btn_previous", "Toggle_Mode", "Txt_StageName" };

        private void Awake()
        {
            MatchCardPartsToMain();

            foreach (RectTransform rect in m_cards)
            {
                if (rect == null) continue;
                m_slots.Add(new SlotLayout { Position = rect.anchoredPosition, Size = rect.sizeDelta, Scale = rect.localScale });
                m_slotCards.Add(MakeCard(rect));
            }

            DetachControl(btn_stageSelect != null ? btn_stageSelect.transform : null);

            if (btn_stageSelect != null) btn_stageSelect.onClick.AddListener(Enter);
            if (m_emblemPanel != null) m_emblemPanel.gameObject.SetActive(false);

            m_hard = false;
            m_center = Mathf.Clamp(m_startStage - 1, 0, Mathf.Max(0, m_stages.Length - 1));
            for (int slot = 0; slot < m_slotCards.Count; slot++) ShowStage(m_slotCards[slot], slot);
            ApplyOrder(false);
        }

        private void OnDestroy()
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            if (btn_stageSelect != null) btn_stageSelect.onClick.RemoveListener(Enter);
            foreach (Card card in m_slotCards)
            {
                Button button = card.Rect.GetComponent<Button>();
                if (button != null) button.onClick.RemoveAllListeners();
                if (card.Mode != null) card.Mode.onValueChanged.RemoveListener(HandleModeChanged);
                foreach (CanvasGroup arrow in card.Arrows)
                {
                    Button arrowButton = arrow.GetComponent<Button>();
                    if (arrowButton != null) arrowButton.onClick.RemoveAllListeners();
                }
            }
        }

        private void MatchCardPartsToMain()
        {
            if (m_cards.Length <= CenterSlot || m_cards[CenterSlot] == null) return;
            RectTransform main = m_cards[CenterSlot];

            foreach (string part in s_cardParts)
            {
                var source = main.Find(part) as RectTransform;
                if (source == null) continue;

                foreach (RectTransform card in m_cards)
                {
                    if (card == null || card == main) continue;
                    var target = card.Find(part) as RectTransform;
                    if (target == null) continue;

                    target.anchorMin = source.anchorMin;
                    target.anchorMax = source.anchorMax;
                    target.pivot = source.pivot;
                    target.sizeDelta = source.sizeDelta;
                    target.anchoredPosition = source.anchoredPosition;
                    target.localRotation = source.localRotation;
                    target.localScale = source.localScale;
                }
            }
        }

        public void Next() => Slide(1);
        public void Previous() => Slide(-1);

        private Card MakeCard(RectTransform rect)
        {
            var card = new Card { Rect = rect, BaseSize = rect.sizeDelta };
            card.Group = rect.GetComponent<CanvasGroup>();
            if (card.Group == null) card.Group = rect.gameObject.AddComponent<CanvasGroup>();

            Transform background = rect.Find("Select_BG");
            Transform title = rect.Find("Txt_StageName");
            card.Title = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            Transform mode = rect.Find("Toggle_Mode");
            card.Mode = mode != null ? mode.GetComponent<Toggle>() : null;
            if (card.Mode != null) card.Mode.onValueChanged.AddListener(HandleModeChanged);
            card.Arrows = new[] { MakeArrow(rect, "Btn_Next", Next), MakeArrow(rect, "Btn_previous", Previous) };
            card.Arrows = System.Array.FindAll(card.Arrows, arrow => arrow != null);
            Transform preview = rect.Find("Img_Map");
            card.Preview = preview != null && preview.GetComponent<StageMapGraphic>() != null ? preview.GetComponent<StageMapGraphic>() : MakePreview(rect, background);

            var tintable = new List<Graphic>();
            if (background != null) tintable.AddRange(background.GetComponents<Graphic>());
            if (card.Preview != null) tintable.Add(card.Preview);
            card.Tintable = tintable.ToArray();
            card.BaseColors = new Color[card.Tintable.Length];
            for (int i = 0; i < card.Tintable.Length; i++) card.BaseColors[i] = card.Tintable[i].color;

            Button button = rect.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }
            button.onClick.AddListener(() => HandleCardClick(card));
            return card;
        }

        private static CanvasGroup MakeArrow(RectTransform card, string name, UnityEngine.Events.UnityAction onClick)
        {
            Transform arrow = card.Find(name);
            Button button = arrow != null ? arrow.GetComponent<Button>() : null;
            if (button == null) return null;
            button.onClick.AddListener(onClick);
            CanvasGroup group = arrow.GetComponent<CanvasGroup>();
            return group != null ? group : arrow.gameObject.AddComponent<CanvasGroup>();
        }

        private StageMapGraphic MakePreview(RectTransform card, Transform background)
        {
            var image = new GameObject("Img_Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(StageMapGraphic)).GetComponent<StageMapGraphic>();
            RectTransform rect = image.rectTransform;
            rect.SetParent(card, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(m_previewPadding.x, m_previewPadding.w);
            rect.offsetMax = new Vector2(-m_previewPadding.y, -m_previewPadding.z);
            if (background != null) rect.SetSiblingIndex(background.GetSiblingIndex() + 1);
            image.raycastTarget = false;
            return image;
        }

        private void DetachControl(Transform control)
        {
            if (control == null || m_cards.Length <= CenterSlot || m_cards[CenterSlot] == null) return;
            control.SetParent(m_cards[CenterSlot].parent, true);
            m_controls.Add((RectTransform)control);
        }

        private StageDefinition GetStage(int index)
        {
            if (m_stages.Length == 0) return null;
            int wrapped = ((index % m_stages.Length) + m_stages.Length) % m_stages.Length;
            return m_stages[wrapped];
        }

        private void ShowStage(Card card, int slot)
        {
            StageDefinition stage = GetStage(m_center + slot - CenterSlot);
            if (card.Preview != null) card.Preview.SetStage(stage, m_stages.Length > 0 ? m_stages[0] : null);
            if (card.Title != null) card.Title.text = stage != null ? stage.CardTitle : "";
        }

        private void ApplyOrder(bool animate)
        {
            int[] drawOrder = { 0, 4, 1, 3, 2 };
            foreach (int slot in drawOrder)
            {
                if (slot < m_slotCards.Count) m_slotCards[slot].Rect.SetAsLastSibling();
            }
            foreach (RectTransform control in m_controls) control.SetAsLastSibling();

            SyncModeToggles();
            for (int slot = 0; slot < m_slotCards.Count; slot++)
            {
                if (m_slotCards[slot].Mode != null) m_slotCards[slot].Mode.interactable = slot == CenterSlot;
                Color tint = slot == CenterSlot ? Color.white : m_sideTint;
                Card card = m_slotCards[slot];
                for (int i = 0; i < card.Tintable.Length; i++) card.Tintable[i].color = card.BaseColors[i] * tint;
                ShowArrows(card, slot == CenterSlot, animate);
            }
        }

        private void ShowArrows(Card card, bool show, bool animate)
        {
            foreach (CanvasGroup arrow in card.Arrows)
            {
                arrow.interactable = show;
                arrow.blocksRaycasts = show;
                arrow.DOKill();
                if (!animate)
                {
                    arrow.alpha = show ? 1f : 0f;
                    continue;
                }
                if (show) arrow.DOFade(1f, m_slideDuration * 0.55f).SetDelay(m_slideDuration * 0.45f).SetLink(gameObject);
                else arrow.DOFade(0f, m_slideDuration * 0.4f).SetLink(gameObject);
            }
        }

        private void HandleCardClick(Card card)
        {
            if (m_sliding) return;
            int slot = m_slotCards.IndexOf(card);
            if (slot < 0) return;

            if (slot != CenterSlot) Slide(slot - CenterSlot);
        }

        private void Slide(int steps)
        {
            if (m_sliding || steps == 0 || m_slotCards.Count != m_cards.Length || m_slotCards.Count == 0) return;
            int direction = steps > 0 ? 1 : -1;
            int remaining = Mathf.Abs(steps);
            SlideOnce(direction, () => { if (remaining > 1) Slide(direction * (remaining - 1)); });
        }

        private void HandleModeChanged(bool hard)
        {
            m_hard = hard;
            SyncModeToggles();
            if (m_emblemPanel != null && m_emblemPanel.IsOpen) m_emblemPanel.SetMode(Mode);
        }

        private void SlideOnce(int direction, TweenCallback onDone)
        {
            if (m_emblemPanel != null && m_emblemPanel.IsOpen) m_emblemPanel.Close();
            m_sliding = true;
            m_center += direction;

            int last = m_slotCards.Count - 1;
            Card wrapping;
            if (direction > 0)
            {
                wrapping = m_slotCards[0];
                m_slotCards.RemoveAt(0);
                m_slotCards.Add(wrapping);
            }
            else
            {
                wrapping = m_slotCards[last];
                m_slotCards.RemoveAt(last);
                m_slotCards.Insert(0, wrapping);
            }
            int wrapSlot = direction > 0 ? last : 0;
            ApplyOrder(true);

            float duration = m_slideDuration;
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            for (int slot = 0; slot < m_slotCards.Count; slot++)
            {
                Card card = m_slotCards[slot];
                SlotLayout layout = m_slots[slot];
                if (card == wrapping)
                {
                    sequence.Insert(0f, card.Group.DOFade(0f, duration * 0.4f));
                    sequence.Insert(0f, card.Rect.DOScale(card.Rect.localScale * 0.7f, duration * 0.4f).SetEase(Ease.InQuad));
                    sequence.InsertCallback(duration * 0.4f, () =>
                    {
                        ShowStage(card, wrapSlot);
                        Vector3 scale = ScaleFor(card, layout);
                        card.Rect.anchoredPosition = layout.Position + new Vector2(direction * layout.Size.x * 0.4f, 0f);
                        card.Rect.localScale = scale * 0.7f;
                        card.Rect.DOScale(scale, duration * 0.6f).SetEase(m_slideEase).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                        card.Rect.DOAnchorPos(layout.Position, duration * 0.6f).SetEase(m_slideEase).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                        card.Group.DOFade(1f, duration * 0.6f).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                    });
                    continue;
                }

                sequence.Insert(0f, card.Rect.DOAnchorPos(layout.Position, duration).SetEase(m_slideEase));
                sequence.Insert(0f, card.Rect.DOScale(ScaleFor(card, layout), duration).SetEase(m_slideEase));
            }

            sequence.OnComplete(() =>
            {
                m_sliding = false;
                onDone?.Invoke();
            });
            m_sequence = sequence;
        }

        private void Enter()
        {
            StageDefinition stage = CenterStage;
            if (stage == null || m_sliding) return;

            if (m_emblemPanel != null)
            {
                if (m_emblemPanel.IsOpen) m_emblemPanel.Close();
                else m_emblemPanel.Open(stage, Mode);
                return;
            }

            StartSettings.Set(Mode, stage, new TowerEmblem[0]);
            Debug.Log($"[StageSelectPanel] {stage.CardTitle} · {Mode} 입장 (문양은 게임 씬 TowerManager 인스펙터 값)");
            if (!SceneFlow.LoadStage(stage, stage.Number == 1 ? m_fallbackScene : null)) ShakeCenter();
        }

        private void SyncModeToggles()
        {
            foreach (Card card in m_slotCards)
            {
                if (card.Mode != null && card.Mode.isOn != m_hard) card.Mode.SetIsOnWithoutNotify(m_hard);
            }
        }

        private void ShakeCenter()
        {
            Card center = m_slotCards[CenterSlot];
            center.Rect.DOKill(true);
            center.Rect.DOShakeAnchorPos(0.3f, new Vector2(14f, 0f), 20, 0f).SetLink(gameObject);
        }
    }
}
