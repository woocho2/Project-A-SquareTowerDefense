using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class DebuffZone : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera m_camera;

    [Header("Shield Tower Obstacle")]
    [SerializeField] private GameObject m_obstacleCollider;

    [Header("Interaction Settings")]
    [Tooltip("이 거리 이상 마우스가 이동해야 드래그로 판정합니다.")]
    [SerializeField] private float m_dragThreshold = 0.1f;

    public event Action<Vector3Int, Vector3> OnPlacedPathCellChanged;

    private Tilemap m_pathTilemap;

    private Vector3 m_towerOriginPos;
    private float m_maxRange = 1f;

    private bool m_isTracking;
    private bool m_isDragging;

    private Vector3 m_startMousePosition;
    private Vector3 m_offset;
    private float m_objectZ;
    private float m_cameraZDistance;
    private bool m_hasNotifiedPathCell;
    private Vector3Int m_lastNotifiedPathCell;

    private void Awake()
    {
        if (m_obstacleCollider != null)
        {
            m_obstacleCollider.SetActive(false);
        }

        if (m_camera == null)
        {
            m_camera = Camera.main;
        }

        m_objectZ = transform.position.z;
        m_cameraZDistance = Mathf.Abs(m_camera.transform.position.z - transform.position.z);

        GameObject pathObj = GameObject.Find("Tilemap_Path");
        if (pathObj != null)
        {
            m_pathTilemap = pathObj.GetComponent<Tilemap>();
        }

        if (m_pathTilemap == null)
        {
            TilePath tilePath = FindFirstObjectByType<TilePath>();
            if (tilePath != null)
            {
                m_pathTilemap = tilePath.GetComponent<Tilemap>();
            }
        }
    }

    /// <summary>
    /// 타워 최초 생성 시 원점 및 최대 사거리 초기화
    /// </summary>
    public void SetupBoundary(Vector3 towerPos, float maxRange)
    {
        m_towerOriginPos = towerPos;
        m_maxRange = maxRange;
    }

    /// <summary>
    /// 타워 이동 시 원점 및 사거리 갱신
    /// </summary>
    public void UpdateTowerPosition(Vector3 newTowerPos, float maxRange)
    {
        m_towerOriginPos = newTowerPos;
        m_maxRange = maxRange;

        float dist = Vector2.Distance(m_towerOriginPos, transform.position);
        if (dist > m_maxRange)
        {
            SnapToClosestPathTile();
        }
    }

    private void OnDisable()
    {
        m_isTracking = false;
        m_isDragging = false;
    }

    private void Update()
    {
        if (Mouse.current == null || m_camera == null) return;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

            // Physics Raycast는 Collider2D를 필요로 합니다.
            // 장판은 정확히 한 패스 타일에만 놓이므로, 클릭 좌표를 셀로 변환해 현재 장판 셀과 비교하는 편이 더 정확합니다.
            if (IsPointerOnPlacedPathCell(mouseWorldPosition))
            {
                m_isTracking = true;
                m_isDragging = false;

                m_startMousePosition = mouseWorldPosition;
                m_offset = transform.position - mouseWorldPosition;
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
                Vector3 targetPos = mouseWorldPosition + m_offset;

                Vector3 dirFromTower = targetPos - m_towerOriginPos;
                if (dirFromTower.magnitude > m_maxRange)
                {
                    targetPos = m_towerOriginPos + dirFromTower.normalized * m_maxRange;
                }

                targetPos.z = m_objectZ;

                // 장판은 드래그 중에도 경로 타일의 정중앙에서만 이동합니다.
                if (TryGetClosestPathTilePosition(targetPos, out Vector3 snappedPosition))
                {
                    transform.position = snappedPosition;
                    NotifyPlacedPathCellChanged();
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && m_isTracking)
        {
            if (m_isDragging)
            {
                SnapToClosestPathTile();
            }

            m_isTracking = false;
            m_isDragging = false;
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

    private void SnapToClosestPathTile()
    {
        if (TryGetClosestPathTilePosition(transform.position, out Vector3 snappedPosition))
        {
            transform.position = snappedPosition;
            NotifyPlacedPathCellChanged();
        }
    }

    public void CancelInteraction()
    {
        m_isTracking = false;
        m_isDragging = false;
    }

    public static void CancelAllInteractions()
    {
        DebuffZone[] zones = FindObjectsByType<DebuffZone>(FindObjectsSortMode.None);
        foreach (DebuffZone zone in zones)
        {
            zone.CancelInteraction();
        }
    }

    private bool TryGetClosestPathTilePosition(Vector3 currentPos, out Vector3 snappedPosition)
    {
        snappedPosition = transform.position;
        if (m_pathTilemap == null) return false;

        Vector3Int centerCell = m_pathTilemap.WorldToCell(currentPos);
        int cellRadius = Mathf.CeilToInt(Mathf.Max(m_maxRange, 1.5f));

        Vector3 bestWorldPos = currentPos;
        float minDistance = float.MaxValue;
        bool found = false;

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector3Int checkCell = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);

                if (m_pathTilemap.HasTile(checkCell))
                {
                    Vector3 cellWorldPos = m_pathTilemap.GetCellCenterWorld(checkCell);

                    float distFromTower = Vector2.Distance(m_towerOriginPos, cellWorldPos);
                    if (distFromTower <= m_maxRange)
                    {
                        float distFromMouse = Vector2.Distance(currentPos, cellWorldPos);
                        if (distFromMouse < minDistance)
                        {
                            minDistance = distFromMouse;
                            bestWorldPos = cellWorldPos;
                            found = true;
                        }
                    }
                }
            }
        }

        if (found)
        {
            bestWorldPos.z = m_objectZ;
            snappedPosition = bestWorldPos;
        }

        return found;
    }

    public void ToggleObstacle(bool isActive)
    {
        if (m_obstacleCollider != null)
        {
            m_obstacleCollider.SetActive(isActive);
        }
    }

    /// <summary>현재 장판이 스냅되어 있는 패스 타일 좌표를 반환합니다. 실제 디버프 판정은 이 좌표만 사용합니다.</summary>
    public bool TryGetPlacedPathCell(out Vector3Int cell)
    {
        cell = default;
        if (m_pathTilemap == null) return false;

        cell = m_pathTilemap.WorldToCell(transform.position);
        return m_pathTilemap.HasTile(cell);
    }

    private bool IsPointerOnPlacedPathCell(Vector3 worldPosition)
    {
        if (!TryGetPlacedPathCell(out Vector3Int zoneCell) || m_pathTilemap == null) return false;
        return m_pathTilemap.WorldToCell(worldPosition) == zoneCell;
    }

    private void NotifyPlacedPathCellChanged()
    {
        if (!TryGetPlacedPathCell(out Vector3Int cell)) return;
        if (m_hasNotifiedPathCell && cell == m_lastNotifiedPathCell) return;

        m_hasNotifiedPathCell = true;
        m_lastNotifiedPathCell = cell;
        OnPlacedPathCellChanged?.Invoke(cell, transform.position);
    }
}
