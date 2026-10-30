using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class StartButton : MonoBehaviour
    {
        [SerializeField] private string m_menuSceneName = "2. Menu";

        [Header("타이틀 등장: 네모네모 → 디펜스 → 라그나로크")]
        [SerializeField] private RectTransform m_logoRoot;
        [SerializeField] private Image[] m_logoParts = new Image[0];
        [SerializeField, Min(0f)] private float m_introDelay = 0.25f;
        [SerializeField, Min(0.05f)] private float m_slamDuration = 0.22f;
        [SerializeField, Range(1f, 3f)] private float m_slamScale = 1.65f;
        [SerializeField, Min(0f)] private float m_logoGap = 0.12f;
        [SerializeField, Min(0f)] private float m_impactStrength = 7f;
        [SerializeField, Min(0.05f)] private float m_promptFadeDuration = 0.65f;

        [Header("Touch To Start 움직임")]
        [SerializeField] private Image m_promptImage;
        [SerializeField, Min(0.1f)] private float m_breatheDuration = 1.2f;
        [SerializeField, Range(1f, 1.1f)] private float m_scaleMultiplier = 1.035f;
        [SerializeField, Range(0f, 1f)] private float m_minAlpha = 0.55f;
        [SerializeField, Min(0f)] private float m_floatDistance = 4f;

        private bool m_loading;
        private bool m_ready;
        private Button m_button;
        private bool m_originalInteractable;
        private Sequence m_introTween;
        private Sequence m_promptTween;
        private Tween m_impactTween;
        private LogoPose[] m_logoPoses;
        private Vector2 m_logoRootPosition;
        private Vector3 m_promptScale;
        private Vector2 m_promptPosition;
        private float m_promptAlpha;
        private bool m_hasPromptPose;

        private void OnEnable()
        {
            m_loading = false;
            m_ready = false;
            m_button = GetComponent<Button>();
            m_originalInteractable = m_button.interactable;
            m_button.interactable = false;
            FindPromptImage();

            if (m_promptImage != null)
            {
                RectTransform prompt = m_promptImage.rectTransform;
                m_promptScale = prompt.localScale;
                m_promptPosition = prompt.anchoredPosition;
                m_promptAlpha = m_promptImage.color.a;
                m_hasPromptPose = true;
                SetAlpha(m_promptImage, 0f);
                prompt.localScale = m_promptScale * 0.96f;
                prompt.anchoredPosition = m_promptPosition + Vector2.down * 8f;
            }

            if (m_logoRoot != null) m_logoRootPosition = m_logoRoot.anchoredPosition;
            m_logoPoses = new LogoPose[m_logoParts == null ? 0 : m_logoParts.Length];
            for (int i = 0; i < m_logoPoses.Length; i++)
            {
                if (m_logoParts[i] == null) continue;
                LogoPose pose = new LogoPose(m_logoParts[i]);
                m_logoPoses[i] = pose;
                SetAlpha(pose.Image, 0f);
                pose.Rect.localScale = pose.Scale * m_slamScale;
                pose.Rect.anchoredPosition = pose.Position + Vector2.up * 70f;
            }

            PlayIntro();
        }

        private void FindPromptImage()
        {
            if (m_promptImage == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
                    {
                        if (image.name != "UI_TouchToStart") continue;
                        m_promptImage = image;
                        break;
                    }
                }
            }
        }

        private void PlayIntro()
        {
            m_introTween = DOTween.Sequence().SetUpdate(true);
            m_introTween.AppendInterval(m_introDelay);
            for (int i = 0; i < m_logoPoses.Length; i++)
            {
                LogoPose pose = m_logoPoses[i];
                if (pose == null) continue;
                int partIndex = i;
                Vector3 impactScale = Vector3.Scale(pose.Scale, new Vector3(1.07f, 0.91f, 1f));
                m_introTween.Append(pose.Rect.DOScale(impactScale, m_slamDuration).SetEase(Ease.InQuad));
                m_introTween.Join(pose.Rect.DOAnchorPos(pose.Position, m_slamDuration).SetEase(Ease.InQuad));
                m_introTween.Join(pose.Image.DOFade(pose.Alpha, m_slamDuration * 0.65f).SetEase(Ease.OutQuad));
                m_introTween.AppendCallback(() => PlayImpact(partIndex));
                m_introTween.Append(pose.Rect.DOScale(pose.Scale, 0.18f).SetEase(Ease.OutBack));
                m_introTween.AppendInterval(m_logoGap);
            }

            m_introTween.AppendInterval(0.15f);
            if (m_promptImage != null)
            {
                m_introTween.Append(m_promptImage.DOFade(m_promptAlpha, m_promptFadeDuration).SetEase(Ease.InOutSine));
                m_introTween.Join(m_promptImage.rectTransform.DOScale(m_promptScale, m_promptFadeDuration).SetEase(Ease.OutSine));
                m_introTween.Join(m_promptImage.rectTransform.DOAnchorPos(m_promptPosition, m_promptFadeDuration).SetEase(Ease.OutSine));
            }
            m_introTween.OnComplete(() =>
            {
                m_introTween = null;
                m_ready = true;
                m_button.interactable = m_originalInteractable;
                PlayPromptLoop();
            });
        }

        private void PlayImpact(int partIndex)
        {
            if (m_logoRoot == null || m_impactStrength <= 0f) return;
            m_impactTween?.Kill();
            m_logoRoot.anchoredPosition = m_logoRootPosition;
            float strength = m_impactStrength * (partIndex == m_logoPoses.Length - 1 ? 1.25f : 1f);
            m_impactTween = m_logoRoot.DOShakeAnchorPos(0.16f, new Vector2(strength * 0.35f, strength), 14, 45f, false, true)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    m_logoRoot.anchoredPosition = m_logoRootPosition;
                    m_impactTween = null;
                });
        }

        private void PlayPromptLoop()
        {
            if (m_promptImage == null) return;
            RectTransform prompt = m_promptImage.rectTransform;
            m_promptTween = DOTween.Sequence().SetUpdate(true);
            m_promptTween.Append(prompt.DOScale(m_promptScale * m_scaleMultiplier, m_breatheDuration).SetEase(Ease.InOutSine));
            m_promptTween.Join(prompt.DOAnchorPosY(m_promptPosition.y + m_floatDistance, m_breatheDuration).SetEase(Ease.InOutSine));
            m_promptTween.Join(m_promptImage.DOFade(m_promptAlpha * m_minAlpha, m_breatheDuration).SetEase(Ease.InOutSine));
            m_promptTween.SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDisable()
        {
            m_introTween?.Kill();
            m_promptTween?.Kill();
            m_impactTween?.Kill();
            m_introTween = null;
            m_promptTween = null;
            m_impactTween = null;
            m_ready = false;
            if (m_button != null) m_button.interactable = m_originalInteractable;
            if (m_logoRoot != null) m_logoRoot.anchoredPosition = m_logoRootPosition;
            if (m_logoPoses != null)
            {
                foreach (LogoPose pose in m_logoPoses) pose?.Restore();
                m_logoPoses = null;
            }
            if (m_hasPromptPose && m_promptImage != null)
            {
                m_promptImage.rectTransform.localScale = m_promptScale;
                m_promptImage.rectTransform.anchoredPosition = m_promptPosition;
                SetAlpha(m_promptImage, m_promptAlpha);
            }
            m_hasPromptPose = false;
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public void OpenMenu()
        {
            if (m_loading || !m_ready) return;
            m_loading = true;
            SceneFlow.Load(m_menuSceneName);
        }

        private sealed class LogoPose
        {
            public readonly Image Image;
            public readonly RectTransform Rect;
            public readonly Vector3 Scale;
            public readonly Vector2 Position;
            public readonly float Alpha;

            public LogoPose(Image image)
            {
                Image = image;
                Rect = image.rectTransform;
                Scale = Rect.localScale;
                Position = Rect.anchoredPosition;
                Alpha = image.color.a;
            }

            public void Restore()
            {
                if (Image == null) return;
                Rect.localScale = Scale;
                Rect.anchoredPosition = Position;
                SetAlpha(Image, Alpha);
            }
        }
    }
}
