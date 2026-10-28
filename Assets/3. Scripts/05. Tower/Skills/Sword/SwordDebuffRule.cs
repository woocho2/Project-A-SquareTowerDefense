using UnityEngine;

namespace SquareTD
{
    // 검 디버프 — 커스(스킬 1) · 둠(스킬 2) (20번 4-3장)
    //   매 턴 최대 체력의 (0.001 × Power × AbilityValue × 스택)% 피해
    //   둠은 최대 체력 −20% (현재 체력도 같은 비율로)
    //   베기 시너지: 베기 공격에 맞으면 그 피해의 10%(커스) / 20%(둠)가 고정 피해(방어 무시)로 바뀜
    //   모든 수치는 적의 저항력만큼 줄어듦
    public class SwordDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float turnDamage = Resist(enemy, SkillFormula.ZoneValue(zone));
            float maxHpCut = isSkill2 ? Resist(enemy, 0.2f) : 0f;
            float fixedRatio = Resist(enemy, isSkill2 ? 0.2f : 0.1f);
            return new SwordDebuffEffect(zone, turnDamage, maxHpCut, fixedRatio);
        }
    }

    // 커스·둠이 적에게 걸려 있는 동안 하는 일. 값은 SwordDebuffRule이 저항까지 계산해서 넘김
    public class SwordDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_turnDamage;        // 매 턴 최대 체력의 이 비율 (0.012 = 1.2%)
        private readonly float m_maxHpCut;          // 둠: 최대 체력을 줄이는 비율 (커스는 0)
        private readonly float m_fixedRatio;        // 베기 피격 때 고정 피해로 바꾸는 비율

        public SwordDebuffEffect(Zone zone, float turnDamage, float maxHpCut, float fixedRatio)
            : base(TowerEmblem.Sword, zone.Stage, zone.Owner.Stats.Duration, turnDamage,
                   $"매 턴 최대 체력 {turnDamage * 100f:0.##}% 피해" + (maxHpCut > 0f ? $", 최대 체력 −{maxHpCut * 100f:0.#}%" : "") + $", 베기 피해의 {fixedRatio * 100f:0.#}% 고정")
        {
            m_owner = zone.Owner;
            m_turnDamage = turnDamage;
            m_maxHpCut = maxHpCut;
            m_fixedRatio = fixedRatio;
        }

        public override void OnAdded(EnemyController enemy)
        {
            if (m_maxHpCut > 0f) enemy.Health.ScaleMax(1f - m_maxHpCut);
        }

        public override void OnRemoved(EnemyController enemy)
        {
            if (m_maxHpCut > 0f) enemy.Health.ScaleMax(1f / (1f - m_maxHpCut));     // 정화 등으로 빠지면 되돌림 (비율 유지)
        }

        // ⑤ 매 턴: 최대 체력 비율 피해. 방어 무시
        public override void OnTurn(EnemyController enemy)
        {
            if (m_turnDamage <= 0f) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Sword,
                BaseDamage = enemy.MaxHp * m_turnDamage,
                ForceFixed = true,
            });
        }

        // 베기 공격이면 피해의 일부를 고정 피해로: "고정 비율만 올려서 다시 센 값 ÷ 원래 값"을 곱함 (다른 효과가 바꾼 몫은 그대로 둠)
        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (info.Attribute != AttackAttribute.Slash || info.ForceFixed) return damage;

            float defense = enemy.Stats.Get(StatType.Defense);
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            float ratio = enemy.Stats.Get(StatType.FixedDamageRatio);
            float before = DamageCalculator.Calculate(info, defense, taken, ratio);
            float after = DamageCalculator.Calculate(info, defense, taken, Mathf.Min(1f, ratio + m_fixedRatio));
            return before > 0f ? damage * after / before : damage;
        }
    }
}
