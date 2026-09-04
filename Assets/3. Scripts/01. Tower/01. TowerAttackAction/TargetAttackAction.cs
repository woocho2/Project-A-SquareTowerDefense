using UnityEngine;

public class TargetAttackAction : TowerAttackAction
{
    // 기본 생성자: 상위 클래스(TowerAttackAction)에 TowerData 전달
    public TargetAttackAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        if (TryFindTarget(towerTransform.position, currentStats.Range, m_data.targetLayer, out EnemyHealthController targetEnemy, m_data.targetPriority))
        {
            LaunchProjectile(towerTransform, targetEnemy.transform, currentStats);
            return true;
        }
        return false;
    }

    private void LaunchProjectile(Transform firePoint, Transform targetTransform, TowerStats finalStats)
    {
        // 전역 투사체 매니저 유효성 검사
        if (GlobalProjectileManager.Instance == null) return;

        // 목표 지점을 향한 발사 방향 벡터 계산
        Vector3 direction = (targetTransform.position - firePoint.position).normalized;

        // 기본 투사체 인스턴스 생성
        ProjectileHit2D projectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_data.towerID,
            firePoint.position,
            finalStats.ProjectileSpeed,
            true
        );

        if (projectile != null)
        {
            // 치명타 확률 계산 (TowerStats의 CriticalRate 필드 적용)
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= (finalStats.CriticalRate * 100f);
            float calculatedDamage = finalStats.AttackPower;

            // Bow 버프: 사거리 끝에서 최대 보너스가 되도록, 타워와 적의 실제 거리에 비례해 증가합니다.
            float distanceRatio = Mathf.Clamp01(
                Vector2.Distance(firePoint.position, targetTransform.position) / Mathf.Max(0.01f, finalStats.Range));
            calculatedDamage *= 1f + (finalStats.DistanceDamageBonusPercent / 100f * distanceRatio);

            // 치명타 발생 시 데미지 증폭 (기본 2배 + CriticalDamage 배율 추가)
            if (isCrit)
            {
                float critMultiplier = 2.0f + finalStats.CriticalDamage;
                calculatedDamage = finalStats.AttackPower * critMultiplier;
            }

            // ProjectileStats 구조체 생성 및 버프가 적용된 finalStats 값 주입
            ProjectileStats projectileStats = new ProjectileStats
            {
                projectileID = m_data.towerID,
                projectileName = m_data.towerName,
                damage = calculatedDamage,
                speed = finalStats.ProjectileSpeed,
                isCritical = isCrit,
                criticalRate = finalStats.CriticalRate,
                criticalDamage = finalStats.CriticalDamage,
                SplashRadius = 0,
                additionalHitCount = finalStats.AdditionalHitCount,
                armorPenetrationPercent = finalStats.ArmorPenetrationPercent,
                iceAdditionalTargetCount = finalStats.IceAdditionalTargetCount,
                chainCount = finalStats.ChainCount,
                extraHitChance = finalStats.ExtraHitChance,
                ownerTower = firePoint.GetComponent<TowerController>(),
                duration = finalStats.Duration,
                abilityValue = finalStats.AbilityValue,
                debuffTarget = m_data.debuffTarget,
                hitEffectID = m_data.hitEffectID
            };

            // 투사체 초기화 및 타겟 추적 발사
            projectile.Init(projectileStats);
            projectile.IsSplash = false;
            projectile.SetTargetPosition(targetTransform.position);
            projectile.Launch(direction, finalStats.ProjectileSpeed, true, -1f, targetTransform);
        }
    }
}
