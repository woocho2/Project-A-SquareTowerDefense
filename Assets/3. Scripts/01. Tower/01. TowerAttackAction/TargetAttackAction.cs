using UnityEngine;


public class TargetAttackAction : TowerAttackAction
{
    public TargetAttackAction(TowerData Data, ProjectileData projectileData) : base(Data, projectileData) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        if (TryFindClosestTarget(towerTransform.position, currentStats.range, m_data.targetLayer, out EnemyController targetEnemy))
        {
            LaunchProjectile(towerTransform, targetEnemy.transform, currentStats);
            return true;
        }
        return false;
    }
    private void LaunchProjectile(Transform firePoint, Transform targetTransform, TowerStats finalstats)
    {
        if (m_projectileData == null || GlobalProjectileManager.Instance == null) return;

        Vector3 direction = (targetTransform.position - firePoint.position).normalized;

        ProjectileHit2D projectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_projectileData.projectileID,
            firePoint.position,
            m_projectileData.speed,
            true
            );

        if (projectile != null)
        {
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= (m_data.criticalRate * 100);
            float finalDamage = finalstats.damage;

            if (isCrit)
            {
                float critMultiplier = 2.0f + (m_data.criticalDamage);
                finalDamage = finalstats.damage * critMultiplier;
            }

            ProjectileStats finalStats = new ProjectileStats
            {
                projectileID = m_projectileData.projectileID,
                projectileName = m_projectileData.projectileName,
                damage = finalDamage,
                speed = m_projectileData.speed,
                isCritical = isCrit,
                criticalRate = finalstats.criticalRate,
                criticalDamage = finalstats.criticalDamage,
                SplashRadius = 0f,
                duration = finalstats.duration,
                abilityValue = finalstats.abilityValue,
                debuffTarget = m_data.debuffTarget,
                hitEffectID = m_projectileData.hiteffectID,
                dotDamage = finalstats.dotDamage,
                dotDuration = finalstats.dotDuration,
                chainDamage = finalstats.chainDamage,
                chainCount = finalstats.chainCount
            };

            projectile.Init(finalStats);
            projectile.IsSplash = false;
            projectile.SetTargetPosition(targetTransform.position);
            projectile.Launch(direction, m_projectileData.speed, true, -1f, targetTransform);

            if (TowerManager.Instance != null && TowerManager.Instance.IsAttributionArrowActive)
            {
                if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                {

                    Vector3 extraFirePoint = firePoint.position + (Vector3.up * 0.3f);

                    int attributionArrowID = 6108;

                    ProjectileHit2D extraProjectile = GlobalProjectileManager.Instance.SpawnProjectile(
                        attributionArrowID,
                        extraFirePoint,
                        m_projectileData.speed,
                        true
                    );

                    if (extraProjectile != null)
                    {
                        ProjectileStats attributionStats = finalStats;

                        attributionStats.projectileID = attributionArrowID;
                        attributionStats.damage = 200f;
                        attributionStats.criticalRate = 1f;
                        attributionStats.criticalDamage = 5.0f;

                        extraProjectile.Init(attributionStats);
                        extraProjectile.IsSplash = false;
                        extraProjectile.SetTargetPosition(targetTransform.position);
                        extraProjectile.Launch(direction, m_projectileData.speed, true, -1f, targetTransform);
                    }
                }
            }
        }
    }
}