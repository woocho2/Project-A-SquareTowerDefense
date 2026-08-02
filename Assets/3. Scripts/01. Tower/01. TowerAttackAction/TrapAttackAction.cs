using UnityEngine;

public class TrapAttackAction : TowerAttackAction
{
    public TrapAttackAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        Vector2 validSpawnPosition = GetValidTrapPosition(towerTransform.position, currentStats.range);

        if (float.IsInfinity(validSpawnPosition.x) || float.IsInfinity(validSpawnPosition.y))
        {
            return false;
        }

        DeployTrap(validSpawnPosition, currentStats);
        return true;
    }

    private Vector2 GetValidTrapPosition(Vector2 origin, float range)
    {
        for (int i = 0; i < m_data.maxPlacementAttempts; i++)
        {
            Vector2 randomPoint = origin + (Random.insideUnitCircle * range);
            Collider2D pathCollider = Physics2D.OverlapPoint(randomPoint, m_data.pathLayer);

            if (pathCollider != null)
            {
                return randomPoint;
            }
        }
        return Vector2.positiveInfinity;
    }

    private void DeployTrap(Vector2 spawnPosition, TowerStats stats)
    {
        if (GlobalProjectileManager.Instance == null) return;

        float trapSpeed = 0f;
        Vector3 dummyDirection = Vector3.up;

        ProjectileHit2D trapProjectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_data.towerID,
            spawnPosition,
            trapSpeed,
            false
        );

        if (trapProjectile != null)
        {
            ProjectileStats finalStats = new ProjectileStats
            {
                projectileID = m_data.towerID,
                projectileName = m_data.towerName,
                speed = trapSpeed,
                damage = m_data.damage,
                criticalRate = m_data.criticalRate,
                criticalDamage = m_data.criticalDamage,
                SplashRadius = m_data.splashRadius,
                hitEffectID = m_data.hitEffectID
            };

            trapProjectile.Init(finalStats);
            trapProjectile.IsSplash = (m_data.splashRadius > 0);
            trapProjectile.SetTargetPosition(spawnPosition);

            trapProjectile.Launch(dummyDirection, trapSpeed, false, m_data.trapLifeTime);
        }
    }
}