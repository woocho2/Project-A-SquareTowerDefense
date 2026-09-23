using System.Collections.Generic;
using UnityEngine;

public class SplashAttackAction : TowerAttackAction
{
    public SplashAttackAction(TowerData data) : base(data) { }

    public override bool UsesProjectileSatellites => true;

    public override ProjectileHit2D PrepareSatelliteProjectile(Vector3 spawnPosition, float preparationLifetime)
    {
        return SpawnPreparedProjectile(spawnPosition, preparationLifetime);
    }

    public override bool LaunchSatelliteProjectiles(
        Transform towerTransform,
        TowerStats finalStats,
        IReadOnlyList<ProjectileHit2D> projectiles)
    {
        if (towerTransform == null || projectiles == null || projectiles.Count == 0) return false;

        if (!TryFindTarget(
                towerTransform.position,
                finalStats.Range,
                m_data.targetLayer,
                out EnemyHealthController targetEnemy,
                CurrentTargetPriority))
        {
            CancelProjectiles(projectiles);
            return false;
        }

        // 에너미의 진형상 시각 위치가 아니라, 에너미가 등록된 패스 타일의 정중앙을 착탄점으로 사용합니다.
        // 따라서 같은 타일에 여러 적이 흩어져 있어도 광역 공격은 항상 해당 타일 중앙에 떨어집니다.
        Vector3 targetPosition = GetTargetTileCenter(targetEnemy);
        for (int i = 0; i < projectiles.Count; i++)
        {
            ProjectileHit2D projectile = projectiles[i];
            if (projectile == null) continue;
            ConfigureAndLaunch(projectile, towerTransform, targetPosition, finalStats);
        }

        return true;
    }

    private static Vector3 GetTargetTileCenter(EnemyHealthController targetEnemy)
    {
        if (targetEnemy != null && TilePath.Instance != null &&
            targetEnemy.TryGetComponent(out EnemyMoveController movement))
        {
            return TilePath.Instance.GetWorldPosition(movement.CurrentTileIndex);
        }

        return targetEnemy != null ? targetEnemy.transform.position : Vector3.zero;
    }

    public override bool ExecuteAction(Transform towerTransform, TowerStats finalStats)
    {
        ProjectileHit2D projectile = SpawnPreparedProjectile(towerTransform.position, 1f);
        if (projectile == null) return false;
        return LaunchSatelliteProjectiles(towerTransform, finalStats, new[] { projectile });
    }

    /// <summary>
    /// Fire Splash skill 2: launches one visual meteor at every path tile inside
    /// range.  Damage still resolves only on projectile impact.
    /// </summary>
    public bool LaunchFireMeteor(Transform towerTransform, TowerStats finalStats)
    {
        if (towerTransform == null || TilePath.Instance == null) return false;

        List<Vector3> targetPositions = TilePath.Instance.GetPathWorldPositionsInRange(
            towerTransform.position,
            finalStats.Range);
        bool launchedAny = false;

        for (int i = 0; i < targetPositions.Count; i++)
        {
            ProjectileHit2D projectile = SpawnPreparedProjectile(towerTransform.position, 1f);
            if (projectile == null) continue;

            ConfigureAndLaunch(projectile, towerTransform, targetPositions[i], finalStats, true);
            launchedAny = true;
        }

        return launchedAny;
    }

    private void ConfigureAndLaunch(
        ProjectileHit2D projectile,
        Transform towerTransform,
        Vector3 targetPosition,
        TowerStats finalStats,
        bool isFireMeteor = false)
    {
        Vector3 direction = (targetPosition - projectile.transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.right;

        bool isCritical = !isFireMeteor && Random.Range(0f, 100f) <= finalStats.CriticalRate * 100f;
        float calculatedDamage = isFireMeteor
            ? finalStats.AttackPower * finalStats.AbilityValue
            : finalStats.AttackPower;

        if (!isFireMeteor)
        {
            float distanceRatio = Mathf.Clamp01(
                Vector2.Distance(towerTransform.position, targetPosition) / Mathf.Max(0.01f, finalStats.Range));
            calculatedDamage *= 1f + finalStats.DistanceDamageBonusPercent / 100f * distanceRatio;
        }

        if (isCritical)
        {
            calculatedDamage = finalStats.AttackPower * (2f + finalStats.CriticalDamage);
        }

        ProjectileStats projectileStats = new ProjectileStats
        {
            projectileID = m_data.towerID,
            projectileName = m_data.towerName,
            damage = calculatedDamage,
            speed = finalStats.ProjectileSpeed,
            isCritical = isCritical,
            criticalRate = finalStats.CriticalRate,
            criticalDamage = finalStats.CriticalDamage,
            SplashRadius = finalStats.ProjectileRadius,
            additionalHitCount = 0,
            armorPenetrationPercent = finalStats.ArmorPenetrationPercent,
            iceAdditionalTargetCount = 0,
            chainCount = finalStats.ChainCount,
            extraHitChance = finalStats.ExtraHitChance,
            ownerTower = towerTransform.GetComponent<TowerController>(),
            duration = finalStats.Duration,
            abilityValue = finalStats.AbilityValue,
            debuffTarget = m_data.debuffTarget,
            hitEffectID = m_data.hitEffectID,
            isFireMeteor = isFireMeteor
        };

        projectile.Init(projectileStats);
        projectile.IsSplash = true;
        projectile.SetTargetPosition(targetPosition);
        projectile.Launch(direction, finalStats.ProjectileSpeed, true);
    }

    private static void CancelProjectiles(IReadOnlyList<ProjectileHit2D> projectiles)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            projectiles[i]?.CancelPreparedProjectile();
        }
    }
}
