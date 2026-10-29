using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 큰 한 방의 연출 도구
    public static class ImpactFx
    {
        private static float s_restoreScale = -1f;
        private static Tween s_slowEnd;
        private static GameObject s_frameHolder;
        private static RawImage s_frameImage;
        private static Tween s_frameEnd;
        private static Camera s_punchCamera;
        private static float s_punchBase;
        private static Tween s_punch;

        public static void Slow(float factor, float realSeconds)
        {
            if (Time.timeScale <= 0f) return;
            if (s_slowEnd != null && s_slowEnd.IsActive()) s_slowEnd.Kill();
            else s_restoreScale = Time.timeScale;

            Time.timeScale = s_restoreScale * Mathf.Clamp(factor, 0.01f, 1f);
            s_slowEnd = DOVirtual.DelayedCall(realSeconds, () =>
            {
                if (Time.timeScale > 0f) Time.timeScale = s_restoreScale;
            }).SetUpdate(true);
        }

        public static void Frame(Color color, float realSeconds)
        {
            EnsureFrame();
            s_frameImage.color = color;
            s_frameImage.enabled = true;
            if (s_frameEnd != null && s_frameEnd.IsActive()) s_frameEnd.Kill();
            s_frameEnd = DOVirtual.DelayedCall(realSeconds, () => { if (s_frameImage != null) s_frameImage.enabled = false; }).SetUpdate(true);
        }

        public static void Punch(float zoom, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic) return;
            if (s_punch != null && s_punch.IsActive()) s_punch.Kill();
            s_punchCamera = camera;
            s_punchBase = camera.orthographicSize;
            camera.orthographicSize = s_punchBase * (1f - zoom);
            s_punch = DOTween.To(() => camera.orthographicSize, v => camera.orthographicSize = v, s_punchBase, duration)
                .SetEase(Ease.OutCubic)
                .SetLink(camera.gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => { if (s_punchCamera != null) s_punchCamera.orthographicSize = s_punchBase; s_punchCamera = null; });
        }

        private static void EnsureFrame()
        {
            if (s_frameHolder != null) return;
            s_frameHolder = new GameObject("Impact Frame", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = s_frameHolder.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 1;

            var imageObject = new GameObject("Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(s_frameHolder.transform, false);
            s_frameImage = imageObject.GetComponent<RawImage>();
            s_frameImage.texture = Texture2D.whiteTexture;
            s_frameImage.raycastTarget = false;
            s_frameImage.enabled = false;
            RectTransform rect = s_frameImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
