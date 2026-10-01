using UnityEngine;
using System.Collections.Generic;

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

    /// <summary>
    /// 투사체를 사용하는 공격은 타워 주위에 투사체를 미리 배치한 뒤 순차 발사할 수 있습니다.
    /// 버프/디버프처럼 투사체를 사용하지 않는 행동은 기본값(false)을 그대로 사용합니다.
    /// </summary>
    public virtual bool UsesProjectileSatellites => false;

    /// <summary>위성 자리 하나에 생성할 실제 투사체 개수입니다.</summary>
    public virtual int GetProjectilesPerSatellite(TowerStats finalStats) => 1;

    /// <summary>지정한 위성 위치에 아직 발사되지 않은 투사체를 준비합니다.</summary>
    public virtual ProjectileHit2D PrepareSatelliteProjectile(Vector3 spawnPosition, float preparationLifetime)
    {
        return null;
    }

    /// <summary>준비된 한 자리의 투사체들을 현재 우선순위 대상에게 발사합니다.</summary>
    public virtual bool LaunchSatelliteProjectiles(
        Transform towerTransform,
        TowerStats finalStats,
        IReadOnlyList<ProjectileHit2D> projectiles)
    {
        return false;
    }

    protected ProjectileHit2D SpawnPreparedProjectile(Vector3 spawnPosition, float preparationLifetime)
    {
        if (m_data == null || GlobalProjectileManager.Instance == null) return null;

        ProjectileHit2D projectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_data.towerID,
            spawnPosition,
            0f,
            false);

        projectile?.PrepareAsSatellite(preparationLifetime);
        return projectile;
    }

    public static int ToTileRange(float range)
    {
        return Mathf.Max(1, Mathf.RoundToInt(range / WorldUnitsPerTile));
    }

    public static float ToWorldRange(float tileRange)
    {
        return Mathf.Max(1f, tileRange) * WorldUnitsPerTile;
    }

    protected static int GetTowerSpawnIndex(Transform towerTransform)
    {
        return towerTransform != null && towerTransform.TryGetComponent(out TowerController tower)
            ? tower.SpawnIndex
            : -1;
    }

    /// <summary>거리 비례 보너스에 쓰는 비율입니다. 타일 거리 ÷ 사거리 칸 수로 계산합니다.</summary>
    protected static float GetTileDistanceRatio(int towerSpawnIndex, int pathIndex, float range)
    {
        if (TileManager.Instance == null) return 0f;

        int tileDistance = TileManager.Instance.GetTileDistance(towerSpawnIndex, pathIndex);
        return tileDistance < 0 ? 0f : Mathf.Clamp01(tileDistance / (float)ToTileRange(range));
    }

    public bool HasTargetInRange(Transform towerTransform, TowerStats finalStats)
    {
        return TryFindTarget(
            GetTowerSpawnIndex(towerTransform),
            finalStats.Range,
            out _,
            out _,
            m_targetPriority);
    }

    // 사거리와 거리는 TileManager의 타일 인덱스로만 판정합니다. 적의 화면상 위치는 사용하지 않습니다.
    protected virtual bool TryFindTarget(
        int towerSpawnIndex,
        float range,
        out EnemyHealthController targetEnemy,
        out int targetPathIndex,
        TargetPriority priority = TargetPriority.Closest)
    {
        targetEnemy = null;
        targetPathIndex = -1;

        TileManager tileManager = TileManager.Instance;
        if (tileManager == null)
        {
            return false;
        }

        // Range 1 = 3x3, Range 2 = 5x5인 타일 거리 판정입니다.
        List<int> pathIndices = tileManager.GetPathIndicesInTowerRange(towerSpawnIndex, ToTileRange(range));

        int minTileDistance = int.MaxValue;
        float maxHp = -1f;
        float minHp = Mathf.Infinity;

        for (int i = 0; i < pathIndices.Count; i++)
        {
            int pathIndex = pathIndices[i];
            int tileDistance = tileManager.GetTileDistance(towerSpawnIndex, pathIndex);
            List<EnemyHealthController> enemies = tileManager.GetEnemyOccupantsAtPathIndex(pathIndex);

            for (int j = 0; j < enemies.Count; j++)
            {
                EnemyHealthController health = enemies[j];
                if (health.CurrentHP <= 0f) continue;

                switch (priority)
                {
                    case TargetPriority.Closest:
                    case TargetPriority.Default:
                        // 타일 거리가 같으면 결승점에 더 가까운 타일의 적을 고릅니다.
                        if (tileDistance < minTileDistance ||
                            (tileDistance == minTileDistance && pathIndex > targetPathIndex))
                        {
                            minTileDistance = tileDistance;
                            targetEnemy = health;
                            targetPathIndex = pathIndex;
                        }
                        break;

                    case TargetPriority.First:
                        // 가장 앞서 나간 적(타일 인덱스가 가장 큰 적) 우선 타겟팅
                        if (targetEnemy == null || pathIndex > targetPathIndex)
                        {
                            targetEnemy = health;
                            targetPathIndex = pathIndex;
                        }
                        break;

                    case TargetPriority.Last:
                        // 가장 뒤처진 적(타일 인덱스가 가장 작은 적) 우선 타겟팅
                        if (targetEnemy == null || pathIndex < targetPathIndex)
                        {
                            targetEnemy = health;
                            targetPathIndex = pathIndex;
                        }
                        break;

                    case TargetPriority.Strongest:
                        if (health.IsBoss)
                        {
                            targetEnemy = health;
                            targetPathIndex = pathIndex;
                            return true;
                        }

                        if (health.CurrentHP > maxHp)
                        {
                            maxHp = health.CurrentHP;
                            targetEnemy = health;
                            targetPathIndex = pathIndex;
                        }
                        break;

                    case TargetPriority.Weakest:
                        if (health.CurrentHP < minHp)
                        {
                            minHp = health.CurrentHP;
                            targetEnemy = health;
                            targetPathIndex = pathIndex;
                        }
                        break;
                }
            }
        }

        return targetEnemy != null;
    }
}
