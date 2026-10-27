using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    // 큰 한 방의 "간지" 도구
    //   Slow: 잠깐 느려짐 (히트스톱·슬로우 모션) — 실제 시간으로 재고, 끝나면 원래 배속으로
    //   Frame: 1 ~ 2프레임 화면 전체 번쩍 (임팩트 프레임) — UI까지 덮음
    //   Punch: 카메라가 살짝 확대됐다 돌아옴 (화면 흔들림과 함께 쓰면 묵직함)
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

        // factor: 0.1이면 10배 느리게 / realSeconds: 실제 몇 초 동안
        public static void Slow(float factor, float realSeconds)
        {
            if (Time.timeScale <= 0f) return;                                       // 일시정지 중이면 건드리지 않음
            if (s_slowEnd != null && s_slowEnd.IsActive()) s_slowEnd.Kill();
            else s_restoreScale = Time.timeScale;

            Time.timeScale = s_restoreScale * Mathf.Clamp(factor, 0.01f, 1f);
            s_slowEnd = DOVirtual.DelayedCall(realSeconds, () =>
            {
                if (Time.timeScale > 0f) Time.timeScale = s_restoreScale;
            }).SetUpdate(true);
        }

        // 화면 전체가 color로 번쩍 (realSeconds 동안, 보통 0.03 ~ 0.06)
        public static void Frame(Color color, float realSeconds)
        {
            EnsureFrame();
            s_frameImage.color = color;
            s_frameImage.enabled = true;
            if (s_frameEnd != null && s_frameEnd.IsActive()) s_frameEnd.Kill();
            s_frameEnd = DOVirtual.DelayedCall(realSeconds, () => { if (s_frameImage != null) s_frameImage.enabled = false; }).SetUpdate(true);
        }

        // zoom: 0.04면 4% 확대 / duration: 돌아오는 데 걸리는 시간
        public static void Punch(float zoom, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null || !camera.orthographic) return;
            if (s_punch != null && s_punch.IsActive()) s_punch.Kill();          // OnKill에서 원래 크기로
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
            if (s_frameHolder != null) return;                                      // 씬을 다시 불러오면 지워졌으므로 새로
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
