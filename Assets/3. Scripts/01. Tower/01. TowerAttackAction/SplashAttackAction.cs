using UnityEngine;

public class SplashAttackAction : TowerAttackAction
{
    // 기본 생성자: 상위 클래스(TowerAttackAction)에 TowerData 전달
    public SplashAttackAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats finalStats)
    {
        if (TryFindTarget(towerTransform.position, finalStats.Range, m_data.targetLayer, out EnemyHealthController targetEnemy, CurrentTargetPriority))
        {
            LaunchSplashProjectile(towerTransform, targetEnemy.transform.position, finalStats);
            return true;
        }
        return false;
    }

    private void LaunchSplashProjectile(Transform firePoint, Vector3 targetPosition, TowerStats finalStats)
    {
        // 전역 투사체 매니저 유효성 검사
        if (GlobalProjectileManager.Instance == null)
        {
            Debug.LogError("[SplashAttackAction] 글로벌 매니저가 누락되었습니다.");
            return;
        }

        // 목표 지점을 향한 발사 방향 벡터 계산
        Vector3 direction = (targetPosition - firePoint.position).normalized;

        // 투사체 풀에서 투사체 인스턴스 생성/소환
        ProjectileHit2D projectile = GlobalProjectileManager.Instance.SpawnProjectile(
            m_data.towerID,
            firePoint.position,
            m_data.projectileSpeed,
            true
        );

        if (projectile != null)
        {
            // 치명타 확률 계산 (TowerStats의 CriticalRate 필드 적용)
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= (finalStats.CriticalRate * 100f);
            float calculatedDamage = finalStats.AttackPower;

            // Bow 버프는 공격 지점까지의 거리에 비례해 피해를 증가시킵니다.
            float distanceRatio = Mathf.Clamp01(
                Vector2.Distance(firePoint.position, targetPosition) / Mathf.Max(0.01f, finalStats.Range));
            calculatedDamage *= 1f + (finalStats.DistanceDamageBonusPercent / 100f * distanceRatio);

            // 치명타 발생 시 데미지 증폭 (TowerStats의 CriticalDamage 필드 적용)
            if (isCrit)
            {
                float critMultiplier = 2.0f + finalStats.CriticalDamage;
                calculatedDamage = finalStats.AttackPower * critMultiplier;
            }

            // ProjectileStats 구조체 생성 및 네이밍 규칙에 맞춘 필드 할당
            ProjectileStats projectileStats = new ProjectileStats
            {
                projectileID = m_data.towerID,
                projectileName = m_data.towerName,
                damage = calculatedDamage,
                speed = m_data.projectileSpeed,
                isCritical = isCrit,
                criticalRate = finalStats.CriticalRate,
                criticalDamage = finalStats.CriticalDamage,
                SplashRadius = finalStats.ProjectileRadius,
                additionalHitCount = 0,
                armorPenetrationPercent = finalStats.ArmorPenetrationPercent,
                iceAdditionalTargetCount = 0,
                chainCount = finalStats.ChainCount,
                extraHitChance = finalStats.ExtraHitChance,
                ownerTower = firePoint.GetComponent<TowerController>(),
                duration = finalStats.Duration,
                abilityValue = finalStats.AbilityValue,
                debuffTarget = m_data.debuffTarget,
                hitEffectID = m_data.hitEffectID
            };

            // 투사체 초기화 및 발사 실행
            projectile.Init(projectileStats);
            projectile.IsSplash = true;
            projectile.SetTargetPosition(targetPosition);
            projectile.Launch(direction, m_data.projectileSpeed, true);
        }
    }
}
