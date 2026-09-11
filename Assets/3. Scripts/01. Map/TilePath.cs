using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilePath : MonoBehaviour
{
    public static TilePath Instance { get; private set; }
    [Tooltip("에디터에서 등록된 경로 타일 그리드 좌표 리스트")]
    public List<Vector3Int> pathGridPositions = new List<Vector3Int>();

    [Header("Enemy Visual Formation")]
    [SerializeField, Range(0.05f, 0.45f)] private float m_enemySlotSpacing = 0.28f;
    [SerializeField, Range(1, 5)] private int m_enemyFormationMaxColumns = 3;
    [SerializeField, Min(0.1f)] private float m_enemyFormationMoveSpeed = 1.5f;

    private Tilemap tilemap;
    private readonly Dictionary<int, List<EnemyHealthController>> m_enemiesByPathIndex =
        new Dictionary<int, List<EnemyHealthController>>();
    private readonly Dictionary<int, List<EnemyHealthController>> m_pendingArrivalsByPathIndex =
        new Dictionary<int, List<EnemyHealthController>>();
    private readonly Dictionary<EnemyHealthController, Vector3> m_enemyVisualTargets =
        new Dictionary<EnemyHealthController, Vector3>();

    // 경로의 마지막 인덱스 (자동 계산: 등록된 타일 개수 - 1)
    public int LastIndex => pathGridPositions.Count > 0 ? pathGridPositions.Count - 1 : 0;

    void Awake()
    {
        Instance = this;
        tilemap = GetComponent<Tilemap>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        List<EnemyHealthController> invalidEnemies = null;

        foreach (KeyValuePair<EnemyHealthController, Vector3> pair in m_enemyVisualTargets)
        {
            EnemyHealthController enemy = pair.Key;
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                invalidEnemies ??= new List<EnemyHealthController>();
                invalidEnemies.Add(enemy);
                continue;
            }

            if (enemy.TryGetComponent(out EnemyMovementController movement) && movement.IsMoving)
            {
                continue;
            }

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position,
                pair.Value,
                m_enemyFormationMoveSpeed * Time.deltaTime);
        }

        if (invalidEnemies == null) return;
        foreach (EnemyHealthController enemy in invalidEnemies)
        {
            m_enemyVisualTargets.Remove(enemy);
        }
    }

    // 인덱스 번호에 해당하는 타일의 월드 중앙 좌표 반환
    public Vector3 GetWorldPosition(int index)
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (pathGridPositions.Count == 0) return transform.position;

        // 인덱스가 범위를 벗어나지 않도록 클램프 처리
        int clampedIndex = Mathf.Clamp(index, 0, LastIndex);
        return tilemap.GetCellCenterWorld(pathGridPositions[clampedIndex]);
    }
    
    // 특정 인덱스의 그리드 좌표 반환 (타일 속성 체크용)
    public Vector3Int GetGridPosition(int index)
    {
        if (pathGridPositions.Count == 0) return Vector3Int.zero;
        int clampedIndex = Mathf.Clamp(index, 0, LastIndex);
        return pathGridPositions[clampedIndex];
    }

    /// <summary>적을 현재 서 있는 패스 타일의 목록에 등록합니다.</summary>
    public void RegisterEnemyAtIndex(EnemyHealthController enemy, int index)
    {
        if (enemy == null || index < 0 || index > LastIndex) return;
        RemovePendingArrival(enemy);
        UnregisterEnemy(enemy);

        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> enemies))
        {
            enemies = new List<EnemyHealthController>();
            m_enemiesByPathIndex.Add(index, enemies);
        }
        enemies.Add(enemy);
        ArrangeEnemiesOnTile(index);
    }

    /// <summary>
    /// 적이 타일에 도착하기 전에, 도착 순서에 맞는 시각 슬롯을 예약하고 좌표를 반환합니다.
    /// 예약 적은 아직 타일 판정 목록에 넣지 않으므로 이동 중 공격/디버프 대상이 되지 않습니다.
    /// </summary>
    public Vector3 ReserveArrivalPosition(EnemyHealthController enemy, int index)
    {
        if (enemy == null || index < 0 || index > LastIndex) return GetWorldPosition(index);

        RemovePendingArrival(enemy);

        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> enemies))
        {
            enemies = new List<EnemyHealthController>();
        }

        if (!m_pendingArrivalsByPathIndex.TryGetValue(index, out List<EnemyHealthController> pendingEnemies))
        {
            pendingEnemies = new List<EnemyHealthController>();
            m_pendingArrivalsByPathIndex.Add(index, pendingEnemies);
        }

        pendingEnemies.RemoveAll(pendingEnemy => pendingEnemy == null || !pendingEnemy.gameObject.activeInHierarchy);
        int totalEnemyCount = enemies.Count + pendingEnemies.Count + 1;
        int arrivalOrder = enemies.Count + pendingEnemies.Count;
        pendingEnemies.Add(enemy);

        return GetFormationPosition(index, arrivalOrder, totalEnemyCount);
    }

    /// <summary>풀 반환·사망·이동 직전에 이전 패스 타일 목록에서 제거합니다.</summary>
    public void UnregisterEnemy(EnemyHealthController enemy)
    {
        if (enemy == null) return;
        RemovePendingArrival(enemy);

        foreach (KeyValuePair<int, List<EnemyHealthController>> pair in m_enemiesByPathIndex)
        {
            if (pair.Value.Remove(enemy))
            {
                ArrangeEnemiesOnTile(pair.Key);
            }
        }

        m_enemyVisualTargets.Remove(enemy);
    }

    /// <summary>
    /// 같은 패스 타일에 도착한 순서대로 적을 격자 형태로 배치합니다.
    /// 적 목록의 순서는 유지하므로 0번째 적이 좌상단부터 차례로 놓입니다.
    /// </summary>
    private void ArrangeEnemiesOnTile(int pathIndex)
    {
        if (!m_enemiesByPathIndex.TryGetValue(pathIndex, out List<EnemyHealthController> enemies)) return;

        enemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
        if (enemies.Count == 0) return;

        for (int i = 0; i < enemies.Count; i++)
        {
            m_enemyVisualTargets[enemies[i]] = GetFormationPosition(pathIndex, i, enemies.Count);
        }
    }

    private Vector3 GetFormationPosition(int pathIndex, int slotIndex, int totalEnemyCount)
    {
        int columns = Mathf.Min(m_enemyFormationMaxColumns, totalEnemyCount);
        int rows = Mathf.CeilToInt(totalEnemyCount / (float)columns);
        float tileSize = tilemap != null ? Mathf.Min(tilemap.cellSize.x, tilemap.cellSize.y) : 1f;
        float spacing = tileSize * m_enemySlotSpacing;
        int column = slotIndex % columns;
        int row = slotIndex / columns;
        float xOffset = (column - ((columns - 1) * 0.5f)) * spacing;
        float yOffset = (((rows - 1) * 0.5f) - row) * spacing;
        return GetWorldPosition(pathIndex) + new Vector3(xOffset, yOffset, 0f);
    }

    private void RemovePendingArrival(EnemyHealthController enemy)
    {
        foreach (List<EnemyHealthController> pendingEnemies in m_pendingArrivalsByPathIndex.Values)
        {
            pendingEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// 투사체 착탄 지점을 중심으로 범위 안의 패스 타일 목록만 합쳐 반환합니다.
    /// 물리 콜라이더 검색과 무관한 실제 스플래시 피해 대상입니다.
    /// </summary>
    public List<EnemyHealthController> GetEnemiesInSquare(Vector3 worldPosition, int tileRadius)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tilemap == null) return result;

        Vector3Int impactCell = tilemap.WorldToCell(worldPosition);
        foreach (KeyValuePair<int, List<EnemyHealthController>> pair in m_enemiesByPathIndex)
        {
            Vector3Int pathCell = GetGridPosition(pair.Key);
            if (Mathf.Abs(pathCell.x - impactCell.x) > tileRadius || Mathf.Abs(pathCell.y - impactCell.y) > tileRadius) continue;

            foreach (EnemyHealthController enemy in pair.Value)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f)
                {
                    result.Add(enemy);
                }
            }
        }
        return result;
    }

    /// <summary>모든 패스 타일에 등록된 살아 있는 적을 반환합니다.</summary>
    public List<EnemyHealthController> GetAllActiveEnemies()
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        foreach (List<EnemyHealthController> enemies in m_enemiesByPathIndex.Values)
        {
            foreach (EnemyHealthController enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f)
                {
                    result.Add(enemy);
                }
            }
        }
        return result;
    }

    // 씬 뷰 시각화
    private void OnDrawGizmos()
    {
        if (pathGridPositions == null || pathGridPositions.Count == 0) return;

        Tilemap currentTilemap = GetComponent<Tilemap>();
        if (currentTilemap == null) return;

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        for (int i = 0; i < pathGridPositions.Count; i++)
        {
            Vector3 worldPos = currentTilemap.GetCellCenterWorld(pathGridPositions[i]);
            UnityEditor.Handles.Label(worldPos + new Vector3(-0.1f, 0.1f, 0), i.ToString(), style);

            if (i < pathGridPositions.Count - 1)
            {
                Vector3 nextWorldPos = currentTilemap.GetCellCenterWorld(pathGridPositions[i + 1]);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(worldPos, nextWorldPos);
            }
        }
#endif
    }
}
