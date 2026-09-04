using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class DebuffZone : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera m_camera;

    [Header("Shield Tower Obstacle")]
    [SerializeField] private GameObject m_obstacleCollider;

    [Header("Interaction Settings")]
    [Tooltip("이 거리 이상 마우스가 이동해야 드래그로 판정합니다.")]
    [SerializeField] private float m_dragThreshold = 0.1f;

    public readonly List<EnemyHealthController> EnemiesInZone = new List<EnemyHealthController>();

    public event Action<EnemyHealthController> OnEnemyEnterEvent;
    public event Action<EnemyHealthController> OnEnemyExitEvent;

    private Collider2D m_collider2D;
    private Tilemap m_pathTilemap;
    private TilePath m_tilePath;

    private Vector3 m_towerOriginPos;
    private float m_maxRange = 1f;

    private bool m_isTracking;
    private bool m_isDragging;

    private Vector3 m_startMousePosition;
    private Vector3 m_offset;
    private float m_objectZ;
    private float m_cameraZDistance;

    private void Awake()
    {
        m_collider2D = GetComponent<Collider2D>();
        if (m_collider2D != null)
        {
            m_collider2D.isTrigger = true;
        }

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
            m_tilePath = FindFirstObjectByType<TilePath>();
            if (m_tilePath != null)
            {
                m_pathTilemap = m_tilePath.GetComponent<Tilemap>();
            }
        }
        else
        {
            m_tilePath = m_pathTilemap.GetComponent<TilePath>();
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
        EnemiesInZone.Clear();
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

            if (m_collider2D != null && m_collider2D.OverlapPoint(mouseWorldPosition))
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<EnemyHealthController>(out var enemyHealth))
        {
            if (!EnemiesInZone.Contains(enemyHealth))
            {
                EnemiesInZone.Add(enemyHealth);
                OnEnemyEnterEvent?.Invoke(enemyHealth);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<EnemyHealthController>(out var enemyHealth))
        {
            if (EnemiesInZone.Contains(enemyHealth))
            {
                EnemiesInZone.Remove(enemyHealth);
                OnEnemyExitEvent?.Invoke(enemyHealth);
            }
        }
    }

    public void ToggleObstacle(bool isActive)
    {
        if (m_obstacleCollider != null)
        {
            m_obstacleCollider.SetActive(isActive);
        }
    }

    /// <summary>
    /// 드래그로 장판을 적 위에 올렸을 때도 트리거 이벤트에 의존하지 않고,
    /// 현재 장판 내부의 활성 적을 즉시 다시 수집합니다.
    /// </summary>
    public List<EnemyHealthController> GetCurrentEnemiesInZone()
    {
        // 이전 TriggerEnter 목록은 드래그 후에도 남을 수 있으므로 사용하지 않습니다.
        EnemiesInZone.Clear();
        if (m_collider2D == null || EnemyManager.Instance == null) return EnemiesInZone;

        foreach (EnemyMovementController movement in EnemyManager.Instance.activeEnemies)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy) continue;
            if (!movement.TryGetComponent(out EnemyHealthController health) || health.CurrentHP <= 0f) continue;

            if (m_collider2D.OverlapPoint(health.transform.position) && !EnemiesInZone.Contains(health))
            {
                EnemiesInZone.Add(health);
            }
        }

        return EnemiesInZone;
    }

    /// <summary>
    /// 장판의 보이는 콜라이더 크기와 무관하게, 패스 타일 인덱스가 같은 적만 반환합니다.
    /// </summary>
    public List<EnemyHealthController> GetEnemiesOnPlacedPathTile()
    {
        EnemiesInZone.Clear();
        if (EnemyManager.Instance == null || m_tilePath == null || m_pathTilemap == null) return EnemiesInZone;

        Vector3Int zoneCell = m_pathTilemap.WorldToCell(transform.position);
        int zoneIndex = m_tilePath.pathGridPositions.IndexOf(zoneCell);
        if (zoneIndex < 0) return EnemiesInZone;

        foreach (EnemyMovementController movement in EnemyManager.Instance.activeEnemies)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy || movement.CurrentTileIndex != zoneIndex) continue;
            if (movement.TryGetComponent(out EnemyHealthController health) && health.CurrentHP > 0f)
            {
                EnemiesInZone.Add(health);
            }
        }
        return EnemiesInZone;
    }
}
