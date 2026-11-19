using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 행동 실패·배치 안내를 표시하고 미배치 존으로 바로 이동
    public sealed class GameNotice : MonoBehaviour
    {
        private static GameNotice s_current;
        private CanvasGroup m_group;
        private RectTransform m_panel;
        private TextMeshProUGUI m_text, m_symbol;
        private Button m_actionButton;
        private Action m_action;
        private Sequence m_motion;
        private float m_lastAt = -10;
        public static string CurrentMessage => s_current != null ? s_current.m_text.text : "";
        public static Button ActionButton => s_current != null ? s_current.m_actionButton : null;
        public static void Warn(string text) => Show(text, true);
        public static void Info(string text) => Show(text, false);
        public static void ZoneRequired(TowerController tower = null)
        {
            Show(tower != null ? EmblemNames.GetConceptName(tower.Definition.Id) + "의 존을 먼저 배치해 주세요." : "배치하지 않은 존이 있습니다. 존을 먼저 배치해 주세요.", true, () => BeginPlacement(tower));
        }
        private static void BeginPlacement(TowerController preferred)
        {
            if (TurnManager.Instance == null || !TurnManager.Instance.Current.CanPlayerAct) { Warn("존은 플레이어 턴에 배치할 수 있습니다."); return; }
            ZoneRegistry zones = BoardManager.Instance.Zones;
            TowerController tower = zones.NeedsPlacement(preferred) ? preferred : zones.FirstUnplacedOwner();
            if (tower == null) { Info("배치할 수 있는 미배치 존이 없습니다."); return; }
            BoardInputController input = FindAnyObjectByType<BoardInputController>();
            if (input == null) return;
            TowerManager.Instance.Select(tower); UIManager.Instance.ShowTowerPanel(tower);
            input.ChangeMode(new PlaceZoneMode(input, tower, zones));
        }
        private static void Show(string text, bool warning, Action action = null)
        {
            if (!Application.isPlaying || SimulationRunner.IsOn || SimulationRunner.SuppressCosmetics || (GameSession.Instance != null && GameSession.Instance.IsOver)) return;
            if (s_current == null) Create();
            GameNotice view = s_current;
            if (view.m_text.text == text && Time.unscaledTime - view.m_lastAt < .5f) return;
            view.m_lastAt = Time.unscaledTime; view.m_text.text = text; view.m_action = action;
            view.m_symbol.text = warning ? "!" : "i"; view.m_symbol.color = warning ? new Color32(255, 139, 126, 255) : new Color32(123, 204, 229, 255);
            view.m_actionButton.gameObject.SetActive(action != null);
            view.m_text.rectTransform.sizeDelta = new Vector2(action != null ? 552 : 672, 44);
            view.m_group.interactable = view.m_group.blocksRaycasts = action != null;
            view.m_motion?.Kill(); view.m_group.alpha = 0; view.m_panel.anchoredPosition = new Vector2(0, 210);
            view.m_motion = DOTween.Sequence().SetUpdate(true).Append(view.m_group.DOFade(1, .15f))
                .Join(view.m_panel.DOAnchorPosY(218, .18f).SetEase(Ease.OutCubic)).AppendInterval(action != null ? 4 : 2.4f).Append(view.m_group.DOFade(0, .2f))
                .OnComplete(() => { view.m_group.blocksRaycasts = false; view.m_action = null; });
            if (warning && SoundManager.Instance != null) SoundManager.Instance.Cue(SoundCue.Denied);
        }
        private static void Create()
        {
            var root = new GameObject("GameNotice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameNotice));
            root.layer = 5;
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            s_current = root.GetComponent<GameNotice>();
            var panel = new GameObject("Message", typeof(RectTransform), typeof(Image), typeof(CanvasGroup)); panel.transform.SetParent(root.transform, false);
            panel.layer = 5;
            s_current.m_panel = (RectTransform)panel.transform; s_current.m_panel.anchorMin = s_current.m_panel.anchorMax = s_current.m_panel.pivot = new Vector2(.5f, 0);
            s_current.m_panel.sizeDelta = new Vector2(740, 58); panel.GetComponent<Image>().color = new Color32(19, 29, 49, 249); panel.GetComponent<Image>().raycastTarget = false;
            s_current.m_group = panel.GetComponent<CanvasGroup>(); s_current.m_group.alpha = 0;
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-Black SDF"); if (font == null) font = TMP_Settings.defaultFontAsset;
            s_current.m_symbol = Text(panel.transform, "Symbol", font, new Vector2(14, 7), new Vector2(30, 44), 26); s_current.m_symbol.alignment = TextAlignmentOptions.Center;
            s_current.m_text = Text(panel.transform, "Text", font, new Vector2(52, 7), new Vector2(552, 44), 18);
            s_current.m_text.enableAutoSizing = true; s_current.m_text.fontSizeMin = 14; s_current.m_text.fontSizeMax = 18;
            var button = new GameObject("PlaceZone", typeof(RectTransform), typeof(Image), typeof(Button)); button.transform.SetParent(panel.transform, false);
            button.layer = 5;
            RectTransform rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(620, -8); rect.sizeDelta = new Vector2(108, 42);
            button.GetComponent<Image>().color = new Color32(224, 193, 119, 255);
            s_current.m_actionButton = button.GetComponent<Button>(); s_current.m_actionButton.targetGraphic = button.GetComponent<Image>();
            TextMeshProUGUI caption = Text(button.transform, "Label", font, Vector2.zero, new Vector2(108, 42), 18); caption.text = "존 배치"; caption.color = new Color32(25, 32, 48, 255); caption.alignment = TextAlignmentOptions.Center;
            s_current.m_actionButton.onClick.AddListener(() => { GameNotice view = s_current; Action action = view.m_action; view.m_motion?.Kill(); view.m_group.alpha = 0; view.m_group.blocksRaycasts = false; action?.Invoke(); });
        }
        private static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, Vector2 offset, Vector2 size, float fontSize)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); root.transform.SetParent(parent, false);
            root.layer = 5;
            RectTransform rect = (RectTransform)root.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(offset.x, -offset.y); rect.sizeDelta = size;
            TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false; return text;
        }
        private void OnDestroy() { m_motion?.Kill(); if (s_current == this) s_current = null; }
    }
}
