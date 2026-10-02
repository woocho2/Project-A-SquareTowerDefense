using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>투사체를 타워 둘레에 띄웠다가 쏘는 연출의 설정값입니다. 값은 타워 프리팹(TowerController)에서 정합니다.</summary>
public struct ProjectileSatelliteSettings
{
    public float Interval;              // 자리 하나를 만들고 다음 자리를 만들거나 쏘기까지의 간격
    public float LaunchDelay;           // 전부 띄운 뒤 첫 발사까지의 대기
    public float Radius;                // 타워 중심에서 투사체까지의 거리
    public float GroupSpacing;          // 한 자리에 여러 발일 때 서로 벌리는 간격
    public float PreparationSafetyTime; // 연출이 중단됐을 때 대기 중인 투사체를 회수하는 여유 시간
}

/// <summary>
/// 타워의 기본 행동(스킬 1)을 정하는 공통 규약입니다. 광역·단일·버프·디버프가 이 클래스를 상속합니다.
/// 타워(TowerController)는 최종 스탯을 넘기기만 하고, 대상 선택과 피해 계산은 각 행동 스크립트가 합니다.
/// </summary>
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

    // 12시 → 6시 → 9시 → 3시 → 1시 → 7시 → 11시 → 5시 순으로 투사체를 띄웁니다.
    private static readonly Vector2[] s_satelliteDirections =
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2(0.70710678f, 0.70710678f),
        new Vector2(-0.70710678f, -0.70710678f),
        new Vector2(-0.70710678f, 0.70710678f),
        new Vector2(0.70710678f, -0.70710678f)
    };

    /// <summary>
    /// 투사체 공격 한 번의 전체 과정입니다. AttackCount만큼 타워 둘레에 투사체를 띄우고,
    /// 잠시 보여준 뒤 한 자리씩 발사합니다. 대상 선택과 피해 계산은 LaunchSatelliteProjectiles가 합니다.
    /// 타워가 코루틴으로 실행하며, 이 코루틴이 끝나면 발사가 모두 끝난 것입니다.
    /// </summary>
    public IEnumerator SatelliteAttackRoutine(Transform towerTransform, TowerStats finalStats, ProjectileSatelliteSettings settings)
    {
        int satelliteCount = Mathf.Max(1, finalStats.AttackCount);
        int projectilesPerSatellite = Mathf.Max(1, GetProjectilesPerSatellite(finalStats));
        float sequenceDuration = settings.Interval * Mathf.Max(0, (satelliteCount - 1) * 2) + settings.LaunchDelay;
        float preparationLifetime = sequenceDuration + settings.PreparationSafetyTime;

        List<List<ProjectileHit2D>> satelliteGroups = new List<List<ProjectileHit2D>>(satelliteCount);

        for (int satelliteIndex = 0; satelliteIndex < satelliteCount; satelliteIndex++)
        {
            Vector3 satelliteOffset = GetSatelliteOffset(satelliteIndex, settings.Radius);
            Vector3 satelliteCenter = towerTransform.position + satelliteOffset;
            Vector3 tangentDirection = new Vector3(-satelliteOffset.y, satelliteOffset.x, 0f).normalized;
            List<ProjectileHit2D> group = new List<ProjectileHit2D>(projectilesPerSatellite);

            for (int projectileIndex = 0; projectileIndex < projectilesPerSatellite; projectileIndex++)
            {
                float centeredIndex = projectileIndex - (projectilesPerSatellite - 1) * 0.5f;
                Vector3 spawnPosition = satelliteCenter + tangentDirection * (centeredIndex * settings.GroupSpacing);
                ProjectileHit2D projectile = PrepareSatelliteProjectile(spawnPosition, preparationLifetime);
                if (projectile != null) group.Add(projectile);
            }

            satelliteGroups.Add(group);

            if (satelliteIndex < satelliteCount - 1 && settings.Interval > 0f)
            {
                yield return new WaitForSeconds(settings.Interval);
            }
        }

        // 완성된 위성 배치를 잠시 보여준 뒤 발사를 시작해 전투 흐름을 읽기 쉽게 합니다.
        if (settings.LaunchDelay > 0f)
        {
            yield return new WaitForSeconds(settings.LaunchDelay);
        }

        // 생성된 자리 순서대로 같은 위치의 투사체를 한 묶음씩 발사합니다.
        for (int satelliteIndex = 0; satelliteIndex < satelliteGroups.Count; satelliteIndex++)
        {
            LaunchSatelliteProjectiles(towerTransform, finalStats, satelliteGroups[satelliteIndex]);

            if (satelliteIndex < satelliteGroups.Count - 1 && settings.Interval > 0f)
            {
                yield return new WaitForSeconds(settings.Interval);
            }
        }
    }

    private static Vector3 GetSatelliteOffset(int satelliteIndex, float baseRadius)
    {
        // 8개를 넘는 공격횟수는 같은 방향의 다음 바깥 고리에 배치합니다.
        int ring = satelliteIndex / s_satelliteDirections.Length;
        float radius = baseRadius * (1f + ring * 0.5f);
        return s_satelliteDirections[satelliteIndex % s_satelliteDirections.Length] * radius;
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

    /// <summary>
    /// 버프·디버프 타워의 스킬 2가 발동하는 스택 수입니다. 60 ÷ 컬러 강화 레벨 (60 / 30 / 20 / 15 / 12).
    /// 60은 1~5의 최소공배수라 어느 레벨에서도 정수로 떨어집니다.
    /// </summary>
    public static int GetStackSkillThreshold(int colorUpgradeLevel)
    {
        return 60 / Mathf.Clamp(colorUpgradeLevel, 1, 5);
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
        TargetPriority priority = TargetPriority.Closest,
        bool skipDoomedEnemies = false)
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
                // 이미 날아가는 탄만으로 죽을 적은 건너뛰어, 남는 탄이 다음 우선순위의 적에게 가게 합니다.
                if (skipDoomedEnemies && health.HasLethalDamageReserved) continue;

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
