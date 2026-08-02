using UnityEngine;

public class SplashAttackAction : TowerAttackAction
{
    public SplashAttackAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats finalStats)
    {
        if (TryFindClosestTarget(towerTransform.position, finalStats.range, m_data.targetLayer, out EnemyController targetEnemy))
        {
            LaunchSplashProjectile(towerTransform, targetEnemy.transform.position, finalStats);
            return true;
        }
        return false;
    }

    private void LaunchSplashProjectile(Transform firePoint, Vector3 targetPosition, TowerStats finalstats)
    {
        if (GlobalProjectileManager.Instance == null)
        {
            Debug.LogError("[SplashAttackAction] 글로벌 매니저가 누락되었습니다.");
            return;
        }

        Vector3 direction = (targetPosition - firePoint.position).normalized;

        ProjectileHit2D projectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_data.towerID,
            firePoint.position,
            m_data.projectileSpeed,
            true
        );

        if (projectile != null)
        {
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= (finalstats.criticalRate * 100);
            float finalDamage = finalstats.damage;

            if (isCrit)
            {
                float critMultiplier = 2.0f + (finalstats.criticalDamage);
                finalDamage = finalstats.damage * critMultiplier;
            }

            ProjectileStats finalStats = new ProjectileStats
            {
                projectileID = m_data.towerID,
                projectileName = m_data.towerName,
                damage = finalDamage,
                speed = m_data.projectileSpeed,
                isCritical = isCrit,
                criticalRate = finalstats.criticalRate,
                criticalDamage = finalstats.criticalDamage,
                SplashRadius = m_data.splashRadius,
                duration = finalstats.duration,
                abilityValue = finalstats.abilityValue,
                debuffTarget = m_data.debuffTarget,
                hitEffectID = m_data.hitEffectID,
                dotDamage = finalstats.dotDamage,
                dotDuration = finalstats.dotDuration,
                chainDamage = finalstats.chainDamage,
                chainCount = finalstats.chainCount
            };

            projectile.Init(finalStats);
            projectile.IsSplash = true;
            projectile.SetTargetPosition(targetPosition);
            projectile.Launch(direction, m_data.projectileSpeed, true);
        }
    }
}