using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System 기준 2D 오브젝트 드래그 스크립트입니다.
/// OnMouseDown을 사용하지 않고,
/// Mouse.current와 Collider2D.OverlapPoint를 사용합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DragObjectOnGround : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera m_camera;

    [Header("Interaction Settings")]
    [Tooltip("이 거리 이상 마우스가 이동해야 드래그로 판정합니다.")]
    [SerializeField] private float m_dragThreshold = 0.1f;

    private Collider2D m_collider2D;
    private TowerController m_towerController;

    private bool m_isTracking;
    private bool m_isDragging;

    private Vector3 m_startMousePosition;
    private Vector3 m_offset;
    private Vector3Int m_startCell;

    private float m_objectZ;
    private float m_cameraZDistance;

    public static event Action<bool> OnTowerDragStateChanged;

    [SerializeField] private GameObject m_highlight;

    public static event Action<TowerController> OnTowerClickedAction;

    private void Awake()
    {
        if (m_highlight != null)
        {
            m_highlight.SetActive(false);
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
            {
                m_collider2D = col;
                break;
            }
        }
        if (m_collider2D == null)
        {
            Debug.LogError($"[{gameObject.name}] isTrigger가 false인 타워 몸체용 콜라이더가 없습니다");
        }

        m_towerController = GetComponent<TowerController>();

        if (m_camera == null)
        {
            m_camera = Camera.main;
        }

        m_objectZ = transform.position.z;

        m_cameraZDistance = Mathf.Abs(m_camera.transform.position.z - transform.position.z);
    }

    private void Update()
    {
        if (Mouse.current == null || m_camera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (m_collider2D.OverlapPoint(mouseWorldPosition))
            {
                m_isTracking = true;
                m_isDragging = false;

                UIManager.Instance.ShowButtons(false);

                m_startMousePosition = mouseWorldPosition;
                m_offset = transform.position - mouseWorldPosition;

                if (TowerManager.Instance != null)
                {
                    m_startCell = TowerManager.Instance.WorldToCell(transform.position);
                }

                if (m_highlight != null)
                {
                    m_highlight.SetActive(true);
                }

                if (m_towerController != null) m_towerController.ShowRange(true);

                OnTowerDragStateChanged?.Invoke(true);
            }
            else
            {
                if (m_highlight != null && m_highlight.activeInHierarchy)
                {
                    m_highlight.SetActive(false);
                }

                if (m_towerController != null) m_towerController.ShowRange(false);
            }
        }

        if (Mouse.current.leftButton.isPressed && m_isTracking)
        {
            if (!m_isDragging)
            {
                float distance = Vector3.Distance(m_startMousePosition, mouseWorldPosition);
                if (distance > m_dragThreshold)
                {
                    m_isDragging = true;
                }
            }

            if (m_isDragging)
            {
                transform.position = mouseWorldPosition + m_offset;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && m_isTracking)
        {
            if (!m_isDragging)
            {
                OnTowerClickedAction?.Invoke(m_towerController);
            }
            else
            {
                Vector3 dropPosition = GetMouseWorldPosition();

                Collider2D hitCollider = Physics2D.OverlapPoint(dropPosition);

                bool isOverSellArea = false;
                if (hitCollider != null && hitCollider.CompareTag("SellArea"))
                {
                    isOverSellArea = true;
                }
                else
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointerData, results);

                    foreach (var result in results)
                    {
                        if (result.gameObject.CompareTag("SellArea"))
                        {
                            isOverSellArea = true;
                            break;
                        }
                    }
                }

                if (isOverSellArea)
                {
                    if (TowerManager.Instance != null)
                    {
                        TowerManager.Instance.SellTower(m_startCell);
                    }
                }
                else
                {
                    if (TowerManager.Instance != null)
                    {
                        Vector3Int endCell = TowerManager.Instance.WorldToCell(transform.position);
                        TowerManager.Instance.MoveTowerOnGrid(m_startCell, endCell);
                    }
                }
            }
            m_isTracking = false;
            m_isDragging = false;
            UIManager.Instance.ShowButtons(true);

            OnTowerDragStateChanged?.Invoke(false);
        }
    }


    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            m_cameraZDistance
        );

        Vector3 worldPosition = m_camera.ScreenToWorldPoint(screenPosition);

        worldPosition.z = m_objectZ;

        return worldPosition;
    }
}
