using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class TowerAttackAction
{
    public const float WorldUnitsPerTile = 1.5f;

    protected TowerData m_data;
    private TargetPriority m_targetPriority;

    protected TargetPriority CurrentTargetPriority => m_targetPriority;

    public TowerAttackAction(TowerData data)
    {
        m_data = data;
        m_targetPriority = NormalizeTargetPriority(data != null ? data.targetPriority : TargetPriority.Closest);
    }

    public void SetTargetPriority(TargetPriority priority)
    {
        m_targetPriority = NormalizeTargetPriority(priority);
    }

    private static TargetPriority NormalizeTargetPriority(TargetPriority priority)
    {
        return priority == TargetPriority.Default ? TargetPriority.Closest : priority;
    }

    public abstract bool ExecuteAction(Transform towerTransform, TowerStats finalStats);

    public static int ToTileRange(float range)
    {
        return Mathf.Max(1, Mathf.RoundToInt(range / WorldUnitsPerTile));
    }

    public static float ToWorldRange(float tileRange)
    {
        return Mathf.Max(1f, tileRange) * WorldUnitsPerTile;
    }

    public bool HasTargetInRange(Transform towerTransform, TowerStats finalStats)
    {
        return TryFindTarget(
            towerTransform.position,
            finalStats.Range,
            m_data.targetLayer,
            out _,
            m_targetPriority);
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

        Vector3Int towerCell = Vector3Int.zero;
        if (towerTilemap != null)
        {
            towerCell = towerTilemap.WorldToCell(origin);
        }

        TilePath path = TilePath.Instance;
        if (path == null)
        {
            return false;
        }

        var enemies = path.GetAllActiveEnemies();

        float minSqrDistance = Mathf.Infinity;
        int maxTileIndex = -1;
        int minTileIndex = int.MaxValue;
        float maxHp = -1f;
        float minHp = Mathf.Infinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyHealthController health = enemies[i];

            if (towerTilemap != null)
            {
                Vector3Int enemyCell = towerTilemap.WorldToCell(health.transform.position);
                int cellDistanceX = Mathf.Abs(enemyCell.x - towerCell.x);
                int cellDistanceY = Mathf.Abs(enemyCell.y - towerCell.y);

                // Range 1 = 3x3, Range 2 = 5x5인 체비셰프 거리 판정입니다.
                if (cellDistanceX > tileRange || cellDistanceY > tileRange) continue;
            }
            else
            {
                Vector2 offset = (Vector2)health.transform.position - origin;
                if (Mathf.Abs(offset.x) > range || Mathf.Abs(offset.y) > range) continue;
            }

            health.TryGetComponent(out EnemyMovementController movement);

            switch (priority)
            {
                case TargetPriority.Closest:
                case TargetPriority.Default:
                    Vector2 direction = (Vector2)health.transform.position - origin;
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
