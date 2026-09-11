using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Unity Input System 기반의 전역 클릭 감지기입니다.
/// UI 클릭과 타워 클릭은 무시하고, 빈 월드 지점을 클릭했을 때만 이벤트를 발행합니다.
/// </summary>
public class GlobalClickDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera m_camera;
    [SerializeField] private LayerMask m_towerLayer;

    public static event Action OnGroundClickedAction;

    private void Awake()
    {
        if (m_camera == null)
        {
            m_camera = Camera.main;
        }
    }

    private void Update()
    {
        if (m_camera == null || !TryGetPrimaryPointerPress(out Vector2 screenPosition))
        {
            return;
        }

        if (IsPointerOverUI(screenPosition))
        {
            return;
        }

        DetectGroundClick(screenPosition);
    }

    private static bool TryGetPrimaryPointerPress(out Vector2 screenPosition)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }

    private void DetectGroundClick(Vector2 screenPosition)
    {
        Ray ray = m_camera.ScreenPointToRay(screenPosition);
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);
        if (!gamePlane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector2 worldPosition = ray.GetPoint(distance);
        Collider2D towerHit = Physics2D.OverlapPoint(worldPosition, m_towerLayer);

        if (towerHit == null)
        {
            OnGroundClickedAction?.Invoke();
        }
    }
}
