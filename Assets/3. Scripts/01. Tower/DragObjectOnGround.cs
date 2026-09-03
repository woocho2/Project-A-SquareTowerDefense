using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

        // 본체 게임오브젝트에 직접 붙은 논-트리거 콜라이더를 정확히 탐색
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
            {
                m_collider2D = col;
                break;
            }
        }

        // 논-트리거가 없다면 첫 번째 콜라이더 할당
        if (m_collider2D == null)
        {
            m_collider2D = GetComponent<Collider2D>();
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
        if (Mouse.current == null || m_camera == null) return;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // 2D 마우스 좌표 기준 충돌 검사 (Z축 영향 배제)
            Vector2 mousePos2D = new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);

            if (m_collider2D != null && m_collider2D.OverlapPoint(mousePos2D))
            {
                m_isTracking = true;
                m_isDragging = false;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowButtons(false);
                }

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

                // 타워 사거리 범위 표시 켜기
                if (m_towerController != null)
                {
                    m_towerController.ShowRange(true);
                }

                OnTowerDragStateChanged?.Invoke(true);
            }
            else
            {
                // 타워 외부 클릭 시 사거리 및 하이라이트 끄기
                if (m_highlight != null && m_highlight.activeInHierarchy)
                {
                    m_highlight.SetActive(false);
                }

                if (m_towerController != null)
                {
                    m_towerController.ShowRange(false);
                }
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
                Collider2D hitCollider = Physics2D.OverlapPoint(new Vector2(dropPosition.x, dropPosition.y));

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

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowButtons(true);
            }

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