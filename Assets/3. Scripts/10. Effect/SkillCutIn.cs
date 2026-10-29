using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 스킬 2 직전 컷인 — 티어 색 띠 위로 그 타워의 일러스트, 스킬 이름, 시전자가 지나감 (모든 공격 타워 공통)
    [DisallowMultipleComponent]
    public sealed class SkillCutIn : MonoBehaviour
    {
        private static readonly Color s_band = new Color(0.07f, 0.09f, 0.24f, 0.94f);
        private const string FontPath = "Fonts & Materials/NotoSansKR-Black SDF";

        [SerializeField] private float m_inDuration = 0.18f;
        [SerializeField] private float m_hold = 0.6f;
        [SerializeField] private float m_outDuration = 0.12f;

        private static SkillCutIn s_instance;

        private Canvas m_overlay;
        private RectTransform m_band;
        private Image m_bandImage;
        private Image m_edgeTop;
        private Image m_edgeBottom;
        private Image m_shine;
        private Image m_illustration;
        private RectTransform m_textGroup;
        private TextMeshProUGUI m_skillName;
        private TextMeshProUGUI m_caster;
        private Image m_tierBadge;
        private Sequence m_sequence;

        public float Duration => m_inDuration + m_hold + m_outDuration;

        public static IEnumerator Play(TowerController tower)
        {
            if (!Application.isPlaying || tower == null || tower.Definition == null) yield break;

            TowerVisual visual = tower.GetComponent<TowerVisual>();
            Sprite illustration = visual != null ? visual.IllustrationSprite : null;
            if (illustration == null) yield break;

            TowerDefinition definition = tower.Definition;
            int tier = definition.Tier;
            bool synergy = TowerId.IsSynergy(definition.Id);
            //string tierName = !synergy && tier >= 1 && tier <= 5 ? EmblemNames.GetTierName(tier) + " " : "";
            Color tierColor = visual.TierColor;
            Sprite badge = !synergy && TowerIcons.Instance != null ? TowerIcons.Instance.GetTier(tier) : null;

            if (s_instance == null) s_instance = new GameObject("Skill Cut-in").AddComponent<SkillCutIn>();
            s_instance.Show(illustration, SkillDescriptions.GetName(definition), EmblemNames.GetConceptName(definition.Id),
                tierColor, badge, synergy ? 5 : Mathf.Clamp(tier, 1, 5));
            yield return new WaitForSeconds(s_instance.Duration);
        }

        private void Awake() => CreateOverlay();

        private void OnDisable()
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            if (m_overlay != null) m_overlay.enabled = false;
        }

        private void Show(Sprite illustration, string skillName, string casterName, Color tierColor, Sprite badge, int tier)
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();

            float width = Screen.width, height = Screen.height;
            float bandHeight = height * 0.42f;
            m_band.sizeDelta = new Vector2(width * 1.6f, bandHeight);
            m_band.localScale = new Vector3(1f, 0f, 1f);

            Color bandColor = Color.Lerp(s_band, tierColor * 0.55f, 0.12f + 0.04f * tier);
            bandColor.a = s_band.a;
            m_bandImage.color = bandColor;
            float edge = 3f + tier * 2f;
            foreach (Image line in new[] { m_edgeTop, m_edgeBottom })
            {
                line.color = tierColor;
                line.rectTransform.sizeDelta = new Vector2(0f, edge);
            }

            RectTransform shine = m_shine.rectTransform;
            Color shineColor = Color.Lerp(tierColor, Color.white, 0.4f);
            shineColor.a = 0.15f + 0.08f * tier;
            m_shine.color = shineColor;
            shine.sizeDelta = new Vector2(height * (0.06f + 0.02f * tier), bandHeight * 1.6f);
            shine.anchoredPosition = new Vector2(-width * 0.9f, 0f);

            RectTransform art = m_illustration.rectTransform;
            m_illustration.sprite = illustration;
            art.sizeDelta = new Vector2(height * 0.62f, height * 0.62f);
            art.anchoredPosition = new Vector2(-width * 0.7f, 0f);

            m_skillName.text = skillName;
            m_skillName.fontSize = height * 0.085f;
            m_caster.text = casterName;
            m_caster.fontSize = height * 0.036f;
            m_caster.color = Color.Lerp(tierColor, Color.white, 0.25f);
            m_tierBadge.sprite = badge;
            m_tierBadge.enabled = badge != null;
            m_tierBadge.color = tierColor;
            m_tierBadge.rectTransform.sizeDelta = new Vector2(height * 0.05f, height * 0.05f);
            m_tierBadge.rectTransform.anchoredPosition = new Vector2(-height * 0.035f, -bandHeight * 0.2f);
            m_skillName.rectTransform.anchoredPosition = new Vector2(0f, bandHeight * 0.06f);
            m_caster.rectTransform.anchoredPosition = new Vector2(0f, -bandHeight * 0.2f);
            m_textGroup.anchoredPosition = new Vector2(width * 0.45f, 0f);
            SetTextAlpha(0f);
            m_overlay.enabled = true;

            float outAt = m_inDuration + m_hold;
            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Insert(0f, m_band.DOScaleY(1f, m_inDuration).SetEase(Ease.OutBack));
            sequence.Insert(0f, AnchorX(art, -width * 0.12f, m_inDuration).SetEase(Ease.OutCubic));
            sequence.Insert(m_inDuration, AnchorX(art, -width * 0.06f, m_hold).SetEase(Ease.Linear));
            sequence.Insert(m_inDuration * 0.5f, AnchorX(m_textGroup, width * 0.17f, m_inDuration).SetEase(Ease.OutCubic));
            sequence.Insert(m_inDuration * 0.5f, DOTween.To(() => 0f, SetTextAlpha, 1f, m_inDuration));
            sequence.Insert(m_inDuration * 1.5f, AnchorX(m_textGroup, width * 0.13f, m_hold - m_inDuration * 0.5f).SetEase(Ease.Linear));
            sequence.Insert(m_inDuration, AnchorX(shine, width * 0.9f, m_hold).SetEase(Ease.InOutSine));
            sequence.Insert(outAt, AnchorX(art, width * 0.8f, m_outDuration).SetEase(Ease.InCubic));
            sequence.Insert(outAt, AnchorX(m_textGroup, -width * 0.6f, m_outDuration).SetEase(Ease.InCubic));
            sequence.Insert(outAt, m_band.DOScaleY(0f, m_outDuration).SetEase(Ease.InBack));
            sequence.OnComplete(() => m_overlay.enabled = false);
            m_sequence = sequence;
        }

        private void SetTextAlpha(float alpha)
        {
            m_skillName.alpha = alpha;
            m_caster.alpha = alpha;
            Color badge = m_tierBadge.color;
            badge.a = alpha;
            m_tierBadge.color = badge;
        }

        private static Tween AnchorX(RectTransform rect, float to, float duration) =>
            DOTween.To(() => rect.anchoredPosition.x, x => rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y), to, duration);

        private void CreateOverlay()
        {
            var canvasObject = new GameObject("Cut-in Overlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_overlay = canvasObject.GetComponent<Canvas>();
            m_overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            m_overlay.sortingOrder = short.MaxValue - 3;
            m_overlay.enabled = false;
            RectTransform root = (RectTransform)canvasObject.transform;

            m_bandImage = MakeImage(root, "Cut-in Band", s_band);
            m_band = m_bandImage.rectTransform;
            m_band.localRotation = Quaternion.Euler(0f, 0f, -8f);
            m_band.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            m_shine = MakeImage(m_band, "Band Shine", Color.white);
            m_shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            m_edgeTop = MakeEdge(m_band, "Band Edge Top", 1f);
            m_edgeBottom = MakeEdge(m_band, "Band Edge Bottom", 0f);

            m_illustration = MakeImage(root, "Cut-in Illustration", Color.white);
            m_illustration.preserveAspect = true;

            m_textGroup = new GameObject("Cut-in Text", typeof(RectTransform)).GetComponent<RectTransform>();
            m_textGroup.SetParent(m_band, false);
            m_textGroup.sizeDelta = Vector2.zero;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontPath);
            m_skillName = MakeText(m_textGroup, "Skill Name", font, Color.white, true);
            m_caster = MakeText(m_textGroup, "Caster", font, Color.white, false);
            m_tierBadge = MakeImage(m_textGroup, "Tier Badge", Color.white);
            m_tierBadge.preserveAspect = true;
            m_tierBadge.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, TMP_FontAsset font, Color color, bool title)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.pivot = new Vector2(0f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height * 0.15f);
            if (font != null) text.font = font;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            if (title)
            {
                text.fontStyle = FontStyles.Bold | FontStyles.Italic;
                text.outlineWidth = 0.2f;
                text.outlineColor = new Color32(10, 12, 30, 255);
            }
            return text;
        }

        private static Image MakeImage(RectTransform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image MakeEdge(RectTransform band, string name, float anchorY)
        {
            Image edge = MakeImage(band, name, Color.white);
            RectTransform rect = edge.rectTransform;
            rect.anchorMin = new Vector2(0f, anchorY);
            rect.anchorMax = new Vector2(1f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);
            rect.sizeDelta = new Vector2(0f, 6f);
            return edge;
        }
    }
}
