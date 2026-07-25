using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class GlobalClickDetector : MonoBehaviour
{
    [Header("Camera Configuration")]
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
        if (Mouse.current == null || m_camera == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            DetectGroundClick();
        }
    }

    private void DetectGroundClick()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = m_camera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Mathf.Abs(m_camera.transform.position.z)));

        Vector2 mousePosition2D = new Vector2(worldPosition.x, worldPosition.y);

        Collider2D towerHit = Physics2D.OverlapPoint(mousePosition2D, m_towerLayer);

        if (towerHit == null)
        {
            OnGroundClickedAction?.Invoke();
        }
    }
}