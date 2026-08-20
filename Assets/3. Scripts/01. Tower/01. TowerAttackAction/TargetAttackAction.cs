using UnityEngine;

public class TargetAttackAction : TowerAttackAction
{
    // 기본 생성자: 상위 클래스(TowerAttackAction)에 TowerData 전달
    public TargetAttackAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        // currentStats.Range 네이밍 규칙 적용
        if (TryFindTarget(towerTransform.position, currentStats.Range, m_data.targetLayer, out EnemyController targetEnemy))
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
            m_data.projectileSpeed,
            true
        );

        if (projectile != null)
        {
            // 치명타 확률 계산 (TowerStats의 CriticalRate 필드 적용)
            bool isCrit = UnityEngine.Random.Range(0f, 100f) <= (finalStats.CriticalRate * 100f);
            float calculatedDamage = finalStats.AttackPower;

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
                SplashRadius = 0f,
                duration = finalStats.Duration,
                abilityValue = finalStats.AbilityValue,
                debuffTarget = m_data.debuffTarget,
                hitEffectID = m_data.hitEffectID
            };

            // 투사체 초기화 및 타겟 추적 발사
            projectile.Init(projectileStats);
            projectile.IsSplash = false;
            projectile.SetTargetPosition(targetTransform.position);
            projectile.Launch(direction, m_data.projectileSpeed, true, -1f, targetTransform);

            // 시너지 효과: 귀속 화살 발사 로직
            if (TowerManager.Instance != null && TowerManager.Instance.IsAttributionArrowActive)
            {
                if (UnityEngine.Random.Range(0f, 100f) <= 30f)
                {
                    Vector3 extraFirePoint = firePoint.position + (Vector3.up * 0.3f);
                    int attributionArrowID = 6108;

                    ProjectileHit2D extraProjectile = GlobalProjectileManager.Instance.SpawnProjectile(
                        attributionArrowID,
                        extraFirePoint,
                        m_data.projectileSpeed,
                        true
                    );

                    if (extraProjectile != null)
                    {
                        // 기존 투사체 스탯 복사 후 귀속 화살 전용 스탯 덮어쓰기
                        ProjectileStats attributionStats = projectileStats;

                        attributionStats.projectileID = attributionArrowID;
                        attributionStats.damage = 200f;
                        attributionStats.criticalRate = 1f;
                        attributionStats.criticalDamage = 5.0f;

                        extraProjectile.Init(attributionStats);
                        extraProjectile.IsSplash = false;
                        extraProjectile.SetTargetPosition(targetTransform.position);
                        extraProjectile.Launch(direction, m_data.projectileSpeed, true, -1f, targetTransform);
                    }
                }
            }
        }
    }
}