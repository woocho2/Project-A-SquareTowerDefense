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

        int towerSpawnIndex = GetTowerSpawnIndex(towerTransform);
        if (!TryFindTarget(
                towerSpawnIndex,
                finalStats.Range,
                out _,
                out int targetPathIndex,
                CurrentTargetPriority))
        {
            CancelProjectiles(projectiles);
            return false;
        }

        // 에너미의 진형상 시각 위치가 아니라, 에너미가 등록된 패스 타일 인덱스를 착탄점으로 사용합니다.
        // 따라서 같은 타일에 여러 적이 흩어져 있어도 광역 공격은 항상 해당 타일 중앙에 떨어집니다.
        for (int i = 0; i < projectiles.Count; i++)
        {
            ProjectileHit2D projectile = projectiles[i];
            if (projectile == null) continue;
            ConfigureAndLaunch(projectile, towerTransform, towerSpawnIndex, targetPathIndex, finalStats);
        }

        return true;
    }

    public override bool ExecuteAction(Transform towerTransform, TowerStats finalStats)
    {
        ProjectileHit2D projectile = SpawnPreparedProjectile(towerTransform.position, 1f);
        if (projectile == null) return false;
        return LaunchSatelliteProjectiles(towerTransform, finalStats, new[] { projectile });
    }

    private void ConfigureAndLaunch(
        ProjectileHit2D projectile,
        Transform towerTransform,
        int towerSpawnIndex,
        int targetPathIndex,
        TowerStats finalStats)
    {
        if (TileManager.Instance == null ||
            !TileManager.Instance.TryGetPathWorldPosition(targetPathIndex, out Vector3 targetPosition))
        {
            projectile.CancelPreparedProjectile();
            return;
        }

        Vector3 direction = (targetPosition - projectile.transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.right;

        // 치명타는 발사 전에 탄마다 한 번 굴립니다. 치명타가 뜬 탄은 범위 안의 모든 적에게 치명타로 들어갑니다.
        bool isCritical = Random.Range(0f, 100f) < finalStats.CriticalRate * 100f;
        float calculatedDamage = finalStats.AttackPower;

        float distanceRatio = GetTileDistanceRatio(towerSpawnIndex, targetPathIndex, finalStats.Range);
        calculatedDamage *= 1f + finalStats.DistanceDamageBonusPercent / 100f * distanceRatio;

        // 치명타는 기본 2배에 치명타 피해를 합산합니다. 거리 비례 보너스 위에 곱해집니다.
        if (isCritical)
        {
            calculatedDamage *= 2f + finalStats.CriticalDamage;
        }

        projectile.Init(CreateProjectileStats(
            towerTransform, finalStats, calculatedDamage, isCritical,
            finalStats.ProjectileRadius, targetPathIndex));
        projectile.IsSplash = true;
        projectile.SetTargetPosition(targetPosition);
        projectile.Launch(direction, finalStats.ProjectileSpeed, true);
    }

    private ProjectileStats CreateProjectileStats(
        Transform towerTransform,
        TowerStats finalStats,
        float damage,
        bool isCritical,
        int splashRadius,
        int targetPathIndex)
    {
        return new ProjectileStats
        {
            projectileID = m_data.towerID,
            projectileName = m_data.towerName,
            damage = damage,
            speed = finalStats.ProjectileSpeed,
            isCritical = isCritical,
            criticalRate = finalStats.CriticalRate,
            criticalDamage = finalStats.CriticalDamage,
            SplashRadius = splashRadius,
            targetPathIndex = targetPathIndex,
            additionalHitCount = 0,
            armorPenetrationPercent = finalStats.ArmorPenetrationPercent,
            iceAdditionalTargetCount = 0,
            chainCount = finalStats.ChainCount,
            extraHitChance = finalStats.ExtraHitChance,
            ownerTower = towerTransform.GetComponent<TowerController>(),
            duration = finalStats.Duration,
            abilityValue = finalStats.AbilityValue,
            debuffTarget = m_data.debuffTarget,
            hitEffectID = m_data.hitEffectID
        };
    }

    private static void CancelProjectiles(IReadOnlyList<ProjectileHit2D> projectiles)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            projectiles[i]?.CancelPreparedProjectile();
        }
    }
}
