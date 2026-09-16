using System.Collections.Generic;
using UnityEngine;

public class TargetAttackAction : TowerAttackAction
{
    public TargetAttackAction(TowerData data) : base(data) { }

    public override bool UsesProjectileSatellites => true;

    // additionalHitCount는 위성 자리마다 보이는 실제 탄환 수로 사용합니다.
    // CSV 값이 0인 기존 타워는 최소 1발을 발사합니다.
    public override int GetProjectilesPerSatellite(TowerStats finalStats)
    {
        return Mathf.Max(1, finalStats.AdditionalHitCount);
    }

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

        for (int i = 0; i < projectiles.Count; i++)
        {
            ProjectileHit2D projectile = projectiles[i];
            if (projectile == null) continue;
            ConfigureAndLaunch(projectile, towerTransform, targetEnemy.transform, finalStats);
        }

        return true;
    }

    // 위성 연출을 거치지 않는 외부 호출이 남아 있어도 즉시 공격할 수 있게 유지합니다.
    public override bool ExecuteAction(Transform towerTransform, TowerStats finalStats)
    {
        int count = GetProjectilesPerSatellite(finalStats);
        List<ProjectileHit2D> projectiles = new List<ProjectileHit2D>(count);
        for (int i = 0; i < count; i++)
        {
            ProjectileHit2D projectile = SpawnPreparedProjectile(towerTransform.position, 1f);
            if (projectile != null) projectiles.Add(projectile);
        }
        return LaunchSatelliteProjectiles(towerTransform, finalStats, projectiles);
    }

    private void ConfigureAndLaunch(
        ProjectileHit2D projectile,
        Transform towerTransform,
        Transform targetTransform,
        TowerStats finalStats)
    {
        Vector3 targetPosition = targetTransform.position;
        Vector3 direction = (targetPosition - projectile.transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.right;

        bool isCritical = Random.Range(0f, 100f) <= finalStats.CriticalRate * 100f;
        float calculatedDamage = finalStats.AttackPower;

        float distanceRatio = Mathf.Clamp01(
            Vector2.Distance(towerTransform.position, targetPosition) / Mathf.Max(0.01f, finalStats.Range));
        calculatedDamage *= 1f + finalStats.DistanceDamageBonusPercent / 100f * distanceRatio;

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
            SplashRadius = 0,
            // 실제 투사체를 여러 개 만들었으므로 투사체 하나는 한 번만 타격합니다.
            additionalHitCount = 0,
            armorPenetrationPercent = finalStats.ArmorPenetrationPercent,
            iceAdditionalTargetCount = finalStats.IceAdditionalTargetCount,
            chainCount = finalStats.ChainCount,
            extraHitChance = finalStats.ExtraHitChance,
            ownerTower = towerTransform.GetComponent<TowerController>(),
            duration = finalStats.Duration,
            abilityValue = finalStats.AbilityValue,
            debuffTarget = m_data.debuffTarget,
            hitEffectID = m_data.hitEffectID
        };

        projectile.Init(projectileStats);
        projectile.IsSplash = false;
        projectile.SetTargetPosition(targetPosition);
        projectile.Launch(direction, finalStats.ProjectileSpeed, true, -1f, targetTransform);
    }

    private static void CancelProjectiles(IReadOnlyList<ProjectileHit2D> projectiles)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            projectiles[i]?.CancelPreparedProjectile();
        }
    }
}
