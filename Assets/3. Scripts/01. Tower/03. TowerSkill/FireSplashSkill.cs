using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 불 광역 타워의 스킬: 메테오.
/// Duration 턴마다(컬러 강화가 레벨만큼 줄여 둔 값) 화면 중앙에 큰 메테오 하나를 떨어뜨립니다.
/// 메테오가 닿는 순간 타워의 사거리와 무관하게 맵의 모든 적이 한 번씩 피해를 받습니다.
/// 기본 공격과 별개로 발동하며, 같은 턴에 기본 공격도 합니다.
/// 불 디버프와의 시너지: 화상 중인 적에게 2배, 연소된 적에게 4배의 피해를 줍니다.
/// </summary>
public class FireSplashSkill : TowerSkill
{
    // 연출 값입니다. 카메라 위쪽 화면 밖에서 화면 중앙으로 떨어집니다.
    private const float MeteorVisualScale = 4f;
    private const float MeteorSpawnMargin = 2f;
    private const float ExplosionEffectScale = 8f;
    private const float ChainInterval = 0.12f;
    private const float ChainEffectScale = 1f;
    private const float ChainDamageRatio = 0.2f;

    private const float BurnedDamageMultiplier = 2f;
    private const float IgnitedDamageMultiplier = 4f;

    private int m_turnCounter;

    public FireSplashSkill(TowerController owner, TowerData data) : base(owner, data) { }

    public override bool OnAttackPhase(TowerStats finalStats)
    {
        m_turnCounter++;
        // 컬러 강화가 Duration을 레벨만큼 나눠 두므로, 레벨이 오를수록 발동 주기가 짧아집니다 (올림).
        int threshold = Mathf.Max(1, Mathf.CeilToInt(finalStats.Duration - 0.0001f));
        if (m_turnCounter < threshold) return false;

        m_turnCounter = 0;
        return LaunchMeteor(finalStats);
    }

    private bool LaunchMeteor(TowerStats finalStats)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Vector3 targetPosition = mainCamera.transform.position;
        targetPosition.z = m_owner.transform.position.z;
        float fallHeight = (mainCamera.orthographic ? mainCamera.orthographicSize : 5f) + MeteorSpawnMargin;

        ProjectileHit2D projectile = SpawnSkillProjectile(targetPosition + Vector3.up * fallHeight);
        if (projectile == null) return false;

        // 스킬은 치명타가 없고, 활 버프의 거리 비례 보너스를 거리와 무관하게 항상 최대치로 받습니다.
        float damage = finalStats.AttackPower * finalStats.AbilityValue;
        damage *= 1f + finalStats.DistanceDamageBonusPercent / 100f;

        projectile.Init(new ProjectileStats
        {
            projectileID = m_data.towerID,
            projectileName = m_data.towerName,
            damage = damage,
            speed = finalStats.ProjectileSpeed,
            armorPenetrationPercent = finalStats.ArmorPenetrationPercent,
            chainCount = finalStats.ChainCount,
            extraHitChance = finalStats.ExtraHitChance,
            ownerTower = m_owner,
            hitEffectID = m_data.hitEffectID,
            targetPathIndex = -1,
            skill = this
        });
        projectile.SetVisualScale(MeteorVisualScale);
        projectile.SetTargetPosition(targetPosition);

        // 기본 수명(1.5초)보다 오래 떨어질 수 있으므로 낙하 시간에 맞춰 수명을 정합니다.
        float speed = Mathf.Max(0.01f, finalStats.ProjectileSpeed);
        projectile.Launch(Vector3.down, speed, true, fallHeight / speed + 1f);
        return true;
    }

    public override IEnumerator OnSkillProjectileImpact(ProjectileHit2D projectile)
    {
        ProjectileStats stats = projectile.Stats;
        EffectManager.Instance?.PlayEffect(stats.hitEffectID, projectile.transform.position, Quaternion.identity, ExplosionEffectScale);

        if (TileManager.Instance == null) return null;

        List<EnemyHealthController> hitEnemies = TileManager.Instance.GetAllLivingEnemies();
        for (int i = 0; i < hitEnemies.Count; i++)
        {
            EnemyHealthController health = hitEnemies[i];
            DealMeteorDamage(health, stats, 1f);

            // 전기 버프의 추가 타격
            if (Random.Range(0f, 100f) < stats.extraHitChance && health.CurrentHP > 0f)
            {
                DealMeteorDamage(health, stats, 1f);
            }
        }

        if (stats.chainCount <= 0 || hitEnemies.Count == 0) return null;
        return ChainRoutine(hitEnemies, stats);
    }

    // 빛 버프: 메테오에 맞은 모든 적에게 연쇄 횟수만큼 메테오 피해의 20%가 차례로 다시 들어갑니다.
    private IEnumerator ChainRoutine(List<EnemyHealthController> hitEnemies, ProjectileStats stats)
    {
        for (int round = 0; round < stats.chainCount; round++)
        {
            yield return new WaitForSeconds(ChainInterval);

            for (int i = 0; i < hitEnemies.Count; i++)
            {
                EnemyHealthController health = hitEnemies[i];
                if (health == null || health.CurrentHP <= 0f || !health.gameObject.activeInHierarchy) continue;

                EffectManager.Instance?.PlayEffect(stats.hitEffectID, health.transform.position, Quaternion.identity, ChainEffectScale);
                DealMeteorDamage(health, stats, ChainDamageRatio);
            }
        }
    }

    private void DealMeteorDamage(EnemyHealthController health, ProjectileStats stats, float ratio)
    {
        if (health == null || health.CurrentHP <= 0f) return;

        float damage = stats.damage * ratio * GetFireDebuffMultiplier(health);
        health.ApplyDamage(damage, false, stats.armorPenetrationPercent, m_owner);
    }

    // 불 문양끼리의 시너지: 불 디버프 타워가 걸어 둔 화상·연소 상태를 읽어 메테오 피해를 키웁니다.
    private static float GetFireDebuffMultiplier(EnemyHealthController health)
    {
        if (!health.TryGetComponent(out EnemyDebuffController debuff)) return 1f;
        if (debuff.IsDebuffTriggered(DebuffTarget.Fire)) return IgnitedDamageMultiplier;
        return debuff.HasActiveDebuff(DebuffTarget.Fire) ? BurnedDamageMultiplier : 1f;
    }
}
