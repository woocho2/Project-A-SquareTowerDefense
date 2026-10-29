using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 화면 흔들림
    public static class ScreenShake
    {
        private static Camera s_camera;
        private static Vector3 s_base;
        private static Tween s_tween;

        public static void Play(float strength, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null || strength <= 0f || duration <= 0f) return;

            if (s_tween != null && s_tween.IsActive()) s_tween.Kill();
            s_camera = camera;
            s_base = camera.transform.position;
            s_tween = camera.transform.DOShakePosition(duration, new Vector3(strength, strength, 0f), 30, 90f, false, true)
                .SetLink(camera.gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(Restore);
        }

        private static void Restore()
        {
            if (s_camera != null) s_camera.transform.position = s_base;
            s_camera = null;
        }
    }
}
