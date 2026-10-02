using System.Collections;
using UnityEngine;

/// <summary>
/// 타워 스킬의 공통 규약입니다.
/// TowerAttackAction이 기본 공격 방식을 정하듯, 문양별 스킬은 이 클래스를 상속해서 만듭니다.
/// TowerController는 스킬의 종류를 모른 채, 정해진 시점에 아래 함수만 호출합니다.
/// 새 스킬을 추가할 때는 이 클래스를 상속한 파일 하나를 만들고 Create에 한 줄을 등록합니다.
/// </summary>
public abstract class TowerSkill
{
    protected readonly TowerController m_owner;
    protected readonly TowerData m_data;

    protected TowerSkill(TowerController owner, TowerData data)
    {
        m_owner = owner;
        m_data = data;
    }

    /// <summary>
    /// 공격 방식과 문양에 맞는 스킬을 만듭니다. 스킬이 없는 타워는 null을 반환합니다.
    /// </summary>
    public static TowerSkill Create(TowerController owner, TowerData data, TowerAttackAction attackAction)
    {
        if (owner == null || data == null) return null;

        switch (data.attackType)
        {
            case AttackType.Splash:
                if (data.EmblemId == TowerEmblem.FIRE)
                {
                    return new FireSplashSkill(owner, data);
                }
                break;

            case AttackType.Target:
                if (data.EmblemId == TowerEmblem.FIRE)
                {
                    return new FireTargetSkill(owner, data);
                }
                break;
        }

        return null;
    }

    /// <summary>
    /// 적 턴이 시작될 때 한 번 호출됩니다. 스킬이 가진 지속 턴을 줄일 때 사용합니다.
    /// </summary>
    public virtual void OnEnemyTurnStart() { }

    /// <summary>
    /// 적 턴마다 공격 타워의 기본 공격 직전에 한 번 호출됩니다. 행동력과 무관합니다.
    /// 턴을 세다가 발동하는 스킬은 여기서 처리합니다. 스킬이 투사체를 발사했으면 true를 반환합니다.
    /// finalStats는 버프가 반영된 최종 스탯입니다 (스킬도 기본 공격처럼 버프를 받습니다).
    /// </summary>
    public virtual bool OnAttackPhase(TowerStats finalStats) => false;

    /// <summary>
    /// 이 타워의 기본 공격 투사체가 노린 대상에 명중할 때마다 호출됩니다.
    /// 적중 횟수를 세는 스킬은 여기서 처리합니다.
    /// </summary>
    public virtual void OnPrimaryTargetHit() { }

    /// <summary>
    /// 최종 스탯을 계산할 때 버프 적용 뒤에 호출됩니다.
    /// 스킬이 쌓아 둔 보너스를 스탯에 더할 때 사용합니다.
    /// </summary>
    public virtual void ModifyFinalStats(ref TowerStats finalStats) { }

    /// <summary>
    /// 이 스킬이 발사한 투사체(ProjectileStats.skill에 자신을 넣은 것)가 목적지에 닿았을 때 호출됩니다.
    /// 누구에게 얼마의 피해를 줄지, 어떤 이펙트를 낼지는 스킬이 여기서 직접 처리합니다.
    /// 명중 뒤에 시간을 두고 이어지는 처리가 있으면 그 코루틴을 반환합니다.
    /// 투사체는 그 코루틴이 끝날 때까지 남아 있어서, 다음 타워가 도중에 행동하지 않습니다. 없으면 null을 반환합니다.
    /// </summary>
    public virtual IEnumerator OnSkillProjectileImpact(ProjectileHit2D projectile) => null;

    /// <summary>이 타워의 투사체 프리팹으로 스킬용 투사체를 하나 꺼냅니다. Launch를 부르기 전까지는 제자리에 대기합니다.</summary>
    protected ProjectileHit2D SpawnSkillProjectile(Vector3 spawnPosition)
    {
        if (m_data == null || GlobalProjectileManager.Instance == null) return null;

        ProjectileHit2D projectile = GlobalProjectileManager.Instance.SpawnProjectile(m_data.towerID, spawnPosition, 0f, false);
        if (projectile != null) projectile.PrepareAsSatellite(1f);
        return projectile;
    }
}
