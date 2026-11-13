using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 웨이브의 공용 주사위(적응형 능력치) 연출 — 화면 가운데서 굴러 눈금이 멈춤
    [DisallowMultipleComponent]
    public sealed class AdaptiveDiceRoll : MonoBehaviour
    {
        private const string FontPath = "Fonts & Materials/NotoSansKR-Black SDF";
        private static readonly Color s_dim = new Color(0.02f, 0.03f, 0.08f, 0.55f);
        private static readonly Color s_face = new Color(0.98f, 0.97f, 0.93f, 1f);
        private static readonly Color s_rim = new Color(0.95f, 0.76f, 0.3f, 1f);
        private static readonly Color s_pip = new Color(0.12f, 0.1f, 0.18f, 1f);

        private const float RollTime = 1.0f;
        private const float Hold = 0.55f;
        private const float OutTime = 0.22f;
        private const float Speed = 0.7f;

        private static AdaptiveDiceRoll s_instance;
        private static Sprite s_rounded;

        private Canvas m_overlay;
        private Image m_dim;
        private RectTransform m_die;
        private Image m_rim;
        private Image m_face;
        private TextMeshProUGUI m_number;
        private TextMeshProUGUI m_title;
        private TextMeshProUGUI m_range;
        private RawImage m_glow;
        private RawImage m_ring;
        private Sequence m_sequence;

        public static IEnumerator Play(int value, int min, int max)
        {
            if (SimulationRunner.IsOn || !Application.isPlaying) yield break;

            if (s_instance == null) s_instance = new GameObject("Adaptive Dice").AddComponent<AdaptiveDiceRoll>();
            yield return new WaitForSeconds(s_instance.Show(value, min, max));
        }

        private void Awake() => CreateOverlay();

        private void OnDisable()
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();
            if (m_overlay != null) m_overlay.enabled = false;
        }

        private float Show(int value, int min, int max)
        {
            if (m_sequence != null && m_sequence.IsActive()) m_sequence.Kill();

            float size = Screen.height * 0.2f;
            m_die.sizeDelta = new Vector2(size, size);
            m_face.rectTransform.sizeDelta = new Vector2(size * 0.86f, size * 0.86f);
            m_number.fontSize = size * 0.62f;
            m_title.fontSize = Screen.height * 0.042f;
            m_range.fontSize = Screen.height * 0.028f;
            m_title.rectTransform.anchoredPosition = new Vector2(0f, size * 0.95f);
            m_range.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.88f);
            m_glow.rectTransform.sizeDelta = new Vector2(size * 2.6f, size * 2.6f);
            m_ring.rectTransform.sizeDelta = new Vector2(size * 1.2f, size * 1.2f);

            m_title.text = "적응형 능력치";
            m_range.text = $"{min} ~ {max}";
            m_number.text = Random.Range(min, max + 1).ToString();
            m_number.color = s_pip;
            m_rim.color = s_rim;
            m_dim.color = Clear(s_dim);
            m_glow.color = Clear(s_rim);
            m_ring.color = Clear(Color.white);
            m_ring.rectTransform.localScale = Vector3.one;
            m_title.alpha = 0f;
            m_range.alpha = 0f;
            m_die.anchoredPosition = new Vector2(0f, -Screen.height * 0.6f);
            m_die.localScale = Vector3.one * 0.6f;
            m_die.localRotation = Quaternion.identity;
            m_overlay.enabled = true;

            Sequence sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            sequence.Insert(0f, m_dim.DOColor(s_dim, 0.18f));
            sequence.Insert(0f, m_die.DOAnchorPos(Vector2.zero, RollTime * 0.55f).SetEase(Ease.OutBack));
            sequence.Insert(0f, m_die.DOScale(1f, RollTime * 0.55f).SetEase(Ease.OutQuad));
            sequence.Insert(0f, m_die.DOLocalRotate(new Vector3(0f, 0f, -1080f - Random.Range(0f, 30f)), RollTime, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
            sequence.Insert(0.1f, DOTween.To(() => 0f, SetTextAlpha, 1f, 0.2f));

            float time = 0.04f;
            float interval = 0.045f;
            int last = -1;
            while (time < RollTime - 0.08f)
            {
                sequence.InsertCallback(time, () => last = SetRandomNumber(min, max, last));
                time += interval;
                interval *= 1.22f;
            }

            sequence.InsertCallback(RollTime, () => Land(value));
            sequence.Insert(RollTime, m_die.DOLocalRotate(Vector3.zero, 0.12f).SetEase(Ease.OutBack));
            sequence.Insert(RollTime, m_die.DOPunchScale(Vector3.one * 0.28f, 0.35f, 6, 0.6f));
            sequence.Insert(RollTime, m_glow.DOColor(WithAlpha(s_rim, 0.75f), 0.08f));
            sequence.Insert(RollTime + 0.08f, m_glow.DOColor(WithAlpha(s_rim, 0.3f), 0.4f));
            sequence.Insert(RollTime, m_ring.DOColor(WithAlpha(Color.white, 0.9f), 0.04f));
            sequence.Insert(RollTime, m_ring.rectTransform.DOScale(2.4f, 0.45f).SetEase(Ease.OutCubic));
            sequence.Insert(RollTime + 0.1f, m_ring.DOColor(Clear(Color.white), 0.35f));

            float outAt = RollTime + Hold;
            sequence.Insert(outAt, m_die.DOScale(0.4f, OutTime).SetEase(Ease.InBack));
            sequence.Insert(outAt, m_die.DOAnchorPos(new Vector2(0f, Screen.height * 0.08f), OutTime).SetEase(Ease.InQuad));
            sequence.Insert(outAt, m_dim.DOColor(Clear(s_dim), OutTime));
            sequence.Insert(outAt, m_glow.DOColor(Clear(s_rim), OutTime));
            sequence.Insert(outAt, DOTween.To(() => 1f, SetTextAlpha, 0f, OutTime));
            sequence.Insert(outAt, m_face.DOFade(0f, OutTime));
            sequence.Insert(outAt, m_rim.DOFade(0f, OutTime));
            sequence.Insert(outAt, m_number.DOFade(0f, OutTime));
            sequence.OnComplete(() => m_overlay.enabled = false);
            m_face.color = s_face;
            sequence.timeScale = Speed;
            m_sequence = sequence;
            return (outAt + OutTime) / Speed;
        }

        private int SetRandomNumber(int min, int max, int last)
        {
            int next = Random.Range(min, max + 1);
            if (max > min && next == last) next = next == max ? min : next + 1;
            m_number.text = next.ToString();
            RectTransform number = m_number.rectTransform;
            number.DOKill();
            number.localScale = Vector3.one * 1.18f;
            number.DOScale(1f, 0.06f / Speed).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            return next;
        }

        private void Land(int value)
        {
            m_number.text = value.ToString();
            m_number.color = Color.Lerp(s_pip, s_rim, 0.35f);
            m_number.DOColor(s_pip, 0.4f / Speed).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            ScreenShake.Play(0.08f, 0.18f);
        }

        private void SetTextAlpha(float alpha)
        {
            m_title.alpha = alpha;
            m_range.alpha = alpha;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color Clear(Color color) => WithAlpha(color, 0f);

        private void CreateOverlay()
        {
            var canvasObject = new GameObject("Dice Overlay", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            m_overlay = canvasObject.GetComponent<Canvas>();
            m_overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            m_overlay.sortingOrder = short.MaxValue - 4;
            m_overlay.enabled = false;
            RectTransform root = (RectTransform)canvasObject.transform;

            m_dim = MakeImage(root, "Dim", s_dim);
            RectTransform dim = m_dim.rectTransform;
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.sizeDelta = Vector2.zero;

            m_glow = MakeRaw(root, "Glow", EffectTextures.Get(EffectTextures.Glow));
            m_ring = MakeRaw(root, "Ring", EffectTextures.Get(EffectTextures.Ring));

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontPath);
            m_title = MakeText(root, "Title", font, Color.white);
            m_range = MakeText(root, "Range", font, new Color(0.85f, 0.87f, 0.95f));

            m_die = new GameObject("Die", typeof(RectTransform)).GetComponent<RectTransform>();
            m_die.SetParent(root, false);
            m_rim = MakeImage(m_die, "Rim", s_rim);
            m_rim.sprite = RoundedSprite();
            RectTransform rim = m_rim.rectTransform;
            rim.anchorMin = Vector2.zero;
            rim.anchorMax = Vector2.one;
            rim.sizeDelta = Vector2.zero;
            m_face = MakeImage(m_die, "Face", s_face);
            m_face.sprite = RoundedSprite();
            m_number = MakeText(m_die, "Number", font, s_pip);
            m_number.rectTransform.sizeDelta = new Vector2(Screen.height * 0.2f, Screen.height * 0.2f);
        }

        private static Sprite RoundedSprite()
        {
            if (s_rounded != null) return s_rounded;

            const int size = 128;
            const float radius = 30f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Dice Rounded", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            s_rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return s_rounded;
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, TMP_FontAsset font, Color color)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.sizeDelta = new Vector2(Screen.width * 0.6f, Screen.height * 0.12f);
            if (font != null) text.font = font;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
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

        private static RawImage MakeRaw(RectTransform parent, string name, Texture texture)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            image.rectTransform.SetParent(parent, false);
            image.texture = texture;
            image.color = Color.clear;
            image.raycastTarget = false;
            return image;
        }
    }
}
