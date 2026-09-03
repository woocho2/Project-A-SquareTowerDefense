using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class TowerAttackAction
{
    protected TowerData m_data;

    public TowerAttackAction(TowerData data)
    {
        m_data = data;
    }

    public abstract bool ExecuteAction(Transform towerTransform, TowerStats finalStats);

    public static int ToTileRange(float range)
    {
        return Mathf.Max(1, Mathf.RoundToInt(range));
    }

    public bool HasTargetInRange(Transform towerTransform, TowerStats finalStats)
    {
        return TryFindTarget(
            towerTransform.position,
            finalStats.Range,
            m_data.targetLayer,
            out _,
            m_data.targetPriority);
    }

    // priority 매개변수에 기본값(= TargetPriority.Closest)을 지정하여 함수 1개로 통합
    protected virtual bool TryFindTarget(
        Vector2 origin,
        float range,
        LayerMask targetLayer,
        out EnemyHealthController targetEnemy,
        TargetPriority priority = TargetPriority.Closest)
    {
        targetEnemy = null;

        int tileRange = ToTileRange(range);
        Tilemap towerTilemap = TowerManager.Instance?.GetSpawnPointTilemap();

        Collider2D[] hitEnemies;
        Vector3Int towerCell = Vector3Int.zero;
        if (towerTilemap != null)
        {
            towerCell = towerTilemap.WorldToCell(origin);
            Vector3 cellSize = towerTilemap.cellSize;
            Vector2 squareSize = new Vector2(
                cellSize.x * (tileRange * 2 + 1),
                cellSize.y * (tileRange * 2 + 1));

            hitEnemies = Physics2D.OverlapBoxAll(origin, squareSize, 0f, targetLayer);
        }
        else
        {
            Debug.LogWarning("[TowerAttackAction] 타워 타일맵을 찾지 못해 원형 사거리 판정을 사용합니다.");
            hitEnemies = Physics2D.OverlapCircleAll(origin, tileRange, targetLayer);
        }

        if (hitEnemies.Length == 0)
        {
            return false;
        }

        float minSqrDistance = Mathf.Infinity;
        int maxTileIndex = -1;
        int minTileIndex = int.MaxValue;
        float maxHp = -1f;
        float minHp = Mathf.Infinity;

        for (int i = 0; i < hitEnemies.Length; i++)
        {
            Collider2D hit = hitEnemies[i];

            EnemyHealthController health = hit.GetComponentInParent<EnemyHealthController>();
            if (health == null || !health.gameObject.activeInHierarchy || health.CurrentHP <= 0f) continue;

            if (towerTilemap != null)
            {
                Vector3Int enemyCell = towerTilemap.WorldToCell(hit.transform.position);
                int cellDistanceX = Mathf.Abs(enemyCell.x - towerCell.x);
                int cellDistanceY = Mathf.Abs(enemyCell.y - towerCell.y);

                // Range 1 = 3x3, Range 2 = 5x5인 체비셰프 거리 판정입니다.
                if (cellDistanceX > tileRange || cellDistanceY > tileRange) continue;
            }

            hit.transform.parent.TryGetComponent<EnemyMovementController>(out var movement);
            if (movement == null)
            {
                health.TryGetComponent<EnemyMovementController>(out movement);
            }

            switch (priority)
            {
                case TargetPriority.Closest:
                case TargetPriority.Default:
                    Vector2 direction = (Vector2)hit.transform.position - origin;
                    float sqrDistance = direction.sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.First:
                    // 가장 앞서 나간 적(타일 인덱스가 가장 큰 적) 우선 타겟팅
                    if (movement != null && movement.CurrentTileIndex > maxTileIndex)
                    {
                        maxTileIndex = movement.CurrentTileIndex;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.Last:
                    // 가장 뒤처진 적(타일 인덱스가 가장 작은 적) 우선 타겟팅
                    if (movement != null && movement.CurrentTileIndex < minTileIndex)
                    {
                        minTileIndex = movement.CurrentTileIndex;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.Strongest:
                    if (health.IsBoss)
                    {
                        targetEnemy = health;
                        return true;
                    }

                    if (health.CurrentHP > maxHp)
                    {
                        maxHp = health.CurrentHP;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.Weakest:
                    if (health.CurrentHP < minHp)
                    {
                        minHp = health.CurrentHP;
                        targetEnemy = health;
                    }
                    break;
            }
        }

        return targetEnemy != null;
    }
}
