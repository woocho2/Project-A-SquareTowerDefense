using UnityEngine;


public class TrapAttackAction : TowerAttackAction
{
    public TrapAttackAction(TowerData data, ProjectileData projectileData) : base(data, projectileData) { }

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
        // m_data의 트랩 전용 설정 사용
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

    /// <summary>
    /// 글로벌 매니저를 호출하여 트랩(속도가 0인 투사체)을 생성합니다.
    /// </summary>
    private void DeployTrap(Vector2 spawnPosition, TowerStats stats)
    {
        if (m_projectileData == null || GlobalProjectileManager.Instance == null) return;

        float trapSpeed = 0f;
        Vector3 dummyDirection = Vector3.up;

        ProjectileHit2D trapProjectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_projectileData.projectileID,
            spawnPosition,
            trapSpeed,
            false
        );

        if (trapProjectile != null)
        {
            ProjectileStats finalStats = new ProjectileStats
            {
                projectileID = m_projectileData.projectileID,
                projectileName = m_projectileData.projectileName,
                speed = trapSpeed,
                damage = m_data.damage,
                criticalRate = m_data.criticalRate,
                criticalDamage = m_data.criticalDamage,
                SplashRadius = m_projectileData.splashradius,
                hitEffectID = m_projectileData.hiteffectID
            };

            trapProjectile.Init(finalStats);
            trapProjectile.IsSplash = (m_projectileData.splashradius > 0);
            trapProjectile.SetTargetPosition(spawnPosition);

            // 유지 시간은 m_data.trapLifeTime을 참조
            trapProjectile.Launch(dummyDirection, trapSpeed, false, m_data.trapLifeTime);
        }
    }
}