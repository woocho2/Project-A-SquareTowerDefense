using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 입장 전 문양 밴픽 — 엠블럼을 누르면 아래 칸에 차례로 담기고, 다시 누르거나 아래 칸을 누르면 빠지며 앞으로 당겨짐
    public class EmblemSelectPanel : MonoBehaviour
    {
        [Header("엠블럼 (검, 도끼, 활, 창, 방패, 해머, 불, 물, 대지, 바람, 얼음, 전기, 빛, 어둠 순서)")]
        [SerializeField] Image[] img_emblems = new Image[14];

        [Header("고른 칸 (SelectEmblem1 ~ 8)")]
        [SerializeField] Image[] img_slots = new Image[HardCount];
        [SerializeField] TextMeshProUGUI txt_count;

        [Header("버튼")]
        [SerializeField] Button btn_emblemSelect;
        [SerializeField] Button btn_back;

        public const int NormalCount = 6;
        public const int HardCount = 8;

        [Header("연출")]
        [SerializeField] Color m_selectedTint = new Color(0.45f, 0.45f, 0.5f, 1f);
        [SerializeField] float m_popDuration = 0.22f;

        public event System.Action BackRequested;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsReady => m_selected.Count == RequiredCount;

        private readonly List<TowerEmblem> m_selected = new List<TowerEmblem>();
        private Color[] m_emblemColors;
        private StageDefinition m_stage;
        private GameMode m_mode = GameMode.Normal;
        private bool m_initialized;

        public static int GetCount(GameMode mode) => mode == GameMode.Hard ? HardCount : NormalCount;
        private int RequiredCount => GetCount(m_mode);

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (m_initialized) return;
            m_initialized = true;

            m_emblemColors = new Color[img_emblems.Length];
            for (int i = 0; i < img_emblems.Length; i++)
            {
                Image image = img_emblems[i];
                if (image == null) continue;
                m_emblemColors[i] = image.color;
                TowerEmblem emblem = ToEmblem(i);
                GetOrAddButton(image).onClick.AddListener(() => HandleEmblemClick(emblem));
            }

            for (int i = 0; i < img_slots.Length; i++)
            {
                Image slot = img_slots[i];
                if (slot == null) continue;
                int index = i;
                GetOrAddButton(slot).onClick.AddListener(() => HandleSlotClick(index));
            }

            if (btn_back != null) btn_back.onClick.AddListener(HandleBackClick);
            if (btn_emblemSelect != null) btn_emblemSelect.onClick.AddListener(HandleStartClick);
            RefreshAll();
        }

        private void OnDestroy()
        {
            foreach (Image image in img_emblems) RemoveListeners(image);
            foreach (Image slot in img_slots) RemoveListeners(slot);
            if (btn_back != null) btn_back.onClick.RemoveListener(HandleBackClick);
            if (btn_emblemSelect != null) btn_emblemSelect.onClick.RemoveListener(HandleStartClick);
        }

        private void HandleStartClick()
        {
            bool ready = IsReady;
            if (TryStart() || !ready || btn_emblemSelect == null) return;

            RectTransform rect = (RectTransform)btn_emblemSelect.transform;
            rect.DOKill(true);
            rect.DOShakeAnchorPos(0.3f, new Vector2(12f, 0f), 20, 0f).SetLink(gameObject);
        }

        private static Button GetOrAddButton(Image image)
        {
            Button button = image.GetComponent<Button>();
            if (button != null) return button;
            button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            image.raycastTarget = true;
            return button;
        }

        private static void RemoveListeners(Image image)
        {
            if (image == null) return;
            Button button = image.GetComponent<Button>();
            if (button != null) button.onClick.RemoveAllListeners();
        }

        private static TowerEmblem ToEmblem(int index) => (TowerEmblem)(index + 1);
        private static int ToIndex(TowerEmblem emblem) => (int)emblem - 1;

        public void Open(StageDefinition stage, GameMode mode)
        {
            Initialize();
            m_stage = stage;
            gameObject.SetActive(true);
            SetMode(mode);
        }

        public void Close() => gameObject.SetActive(false);

        public void SetStage(StageDefinition stage) => m_stage = stage;

        public void SetMode(GameMode mode)
        {
            Initialize();
            m_mode = mode;
            while (m_selected.Count > RequiredCount)
            {
                TowerEmblem removed = m_selected[m_selected.Count - 1];
                m_selected.RemoveAt(m_selected.Count - 1);
                SetEmblemSelected(removed, false);
            }
            RefreshAll();
        }

        public bool TryStart()
        {
            if (m_stage == null) return false;
            if (!IsReady)
            {
                ShakeEmptySlots();
                return false;
            }

            StartSettings.Set(m_mode, m_stage, m_selected);
            Debug.Log($"[EmblemSelectPanel] {m_stage.CardTitle} · {m_mode} · 문양 {string.Join(", ", m_selected)}");
            return SceneFlow.LoadStage(m_stage);
        }

        private void HandleEmblemClick(TowerEmblem emblem)
        {
            int at = m_selected.IndexOf(emblem);
            if (at >= 0)
            {
                Deselect(at);
                return;
            }

            if (m_selected.Count >= RequiredCount)
            {
                Image image = img_emblems[ToIndex(emblem)];
                image.rectTransform.DOKill(true);
                image.rectTransform.DOShakeAnchorPos(0.25f, new Vector2(8f, 0f), 18, 0f).SetLink(gameObject);
                ShakeCount();
                return;
            }

            m_selected.Add(emblem);
            SetEmblemSelected(emblem, true);
            RefreshSlots(m_selected.Count - 1);
        }

        private void HandleSlotClick(int index)
        {
            if (index < m_selected.Count) Deselect(index);
        }

        private void Deselect(int at)
        {
            TowerEmblem emblem = m_selected[at];
            m_selected.RemoveAt(at);
            SetEmblemSelected(emblem, false);
            RefreshSlots(at);
        }

        private void SetEmblemSelected(TowerEmblem emblem, bool selected)
        {
            int index = ToIndex(emblem);
            if (index < 0 || index >= img_emblems.Length || img_emblems[index] == null) return;

            Image image = img_emblems[index];
            Color baseColor = m_emblemColors[index];
            image.DOKill();
            image.DOColor(selected ? baseColor * m_selectedTint : baseColor, 0.15f).SetLink(gameObject);

            RectTransform rect = image.rectTransform;
            rect.DOKill(true);
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * (selected ? -0.12f : 0.1f), m_popDuration, 6, 0.6f).SetLink(gameObject);
        }

        private void RefreshAll()
        {
            for (int i = 0; i < img_emblems.Length; i++)
            {
                if (img_emblems[i] == null) continue;
                bool selected = m_selected.Contains(ToEmblem(i));
                img_emblems[i].color = selected ? m_emblemColors[i] * m_selectedTint : m_emblemColors[i];
            }
            RefreshSlots(int.MaxValue);
        }

        private void RefreshSlots(int animateFrom)
        {
            int required = RequiredCount;
            for (int i = 0; i < img_slots.Length; i++)
            {
                Image slot = img_slots[i];
                if (slot == null) continue;

                slot.gameObject.SetActive(i < required);
                RectTransform rect = slot.rectTransform;
                rect.DOKill(true);
                slot.DOKill(true);

                if (i < m_selected.Count)
                {
                    Image source = img_emblems[ToIndex(m_selected[i])];
                    slot.sprite = source != null ? source.sprite : null;
                    slot.preserveAspect = true;
                    if (i >= animateFrom)
                    {
                        slot.color = Clear(Color.white);
                        rect.localScale = Vector3.one * 0.6f;
                        slot.DOFade(1f, m_popDuration * 0.6f).SetLink(gameObject);
                        rect.DOScale(1f, m_popDuration).SetEase(Ease.OutBack).SetLink(gameObject);
                    }
                    else
                    {
                        slot.color = Color.white;
                        rect.localScale = Vector3.one;
                    }
                }
                else
                {
                    slot.color = Clear(Color.white);
                    rect.localScale = Vector3.one;
                }
            }

            if (txt_count != null) txt_count.text = $"{m_selected.Count} / {required}";
        }

        private void ShakeEmptySlots()
        {
            for (int i = m_selected.Count; i < RequiredCount && i < img_slots.Length; i++)
            {
                if (img_slots[i] == null) continue;
                RectTransform rect = img_slots[i].rectTransform;
                rect.DOKill(true);
                rect.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.6f).SetLink(gameObject);
            }
            ShakeCount();
        }

        private void ShakeCount()
        {
            if (txt_count == null) return;
            txt_count.rectTransform.DOKill(true);
            txt_count.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.6f).SetLink(gameObject);
        }

        private void HandleBackClick()
        {
            Close();
            BackRequested?.Invoke();
        }

        private static Color Clear(Color color)
        {
            color.a = 0f;
            return color;
        }
    }
}
