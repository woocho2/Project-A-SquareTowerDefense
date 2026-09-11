using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Turn Projection Size")]
    [SerializeField, Min(0.1f)] private float m_playerTurnProjectionSize = 7.5f;
    [SerializeField, Min(0.1f)] private float m_endTurnProjectionSize = 5f;
    [SerializeField, Min(0f)] private float m_projectionTransitionDuration = 0.3f;

    private Camera m_cam;
    private static Coroutine s_projectionTransitionRoutine;

    public static void SmoothToPlayerTurnProjectionSize(MonoBehaviour coroutineOwner)
    {
        StartProjectionTransition(coroutineOwner, controller => controller.m_playerTurnProjectionSize, 7.5f);
    }

    public static void SmoothToEndTurnProjectionSize(MonoBehaviour coroutineOwner)
    {
        StartProjectionTransition(coroutineOwner, controller => controller.m_endTurnProjectionSize, 5f);
    }

    private static void StartProjectionTransition(MonoBehaviour coroutineOwner, System.Func<CameraController, float> getSize, float fallbackSize)
    {
        CameraController controller = FindFirstObjectByType<CameraController>(FindObjectsInactive.Include);
        Camera targetCamera = controller != null ? controller.GetCamera() : Camera.main;

        if (targetCamera == null || !targetCamera.orthographic) return;

        float targetSize = controller != null ? getSize(controller) : fallbackSize;
        float duration = controller != null ? controller.m_projectionTransitionDuration : 0.3f;

        if (coroutineOwner == null || duration <= 0f)
        {
            targetCamera.orthographicSize = targetSize;
            return;
        }

        if (s_projectionTransitionRoutine != null)
        {
            coroutineOwner.StopCoroutine(s_projectionTransitionRoutine);
        }

        s_projectionTransitionRoutine = coroutineOwner.StartCoroutine(ProjectionTransitionRoutine(targetCamera, targetSize, duration));
    }

    private static IEnumerator ProjectionTransitionRoutine(Camera targetCamera, float targetSize, float duration)
    {
        float startSize = targetCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration && targetCamera != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t * t * (3f - 2f * t));
            yield return null;
        }

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = targetSize;
        }

        s_projectionTransitionRoutine = null;
    }

    private Camera GetCamera()
    {
        if (m_cam == null)
        {
            m_cam = GetComponent<Camera>();
        }

        return m_cam != null ? m_cam : Camera.main;
    }
}
