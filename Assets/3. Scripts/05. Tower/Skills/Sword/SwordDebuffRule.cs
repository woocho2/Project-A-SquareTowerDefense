using UnityEngine;

namespace SquareTD
{
    // 검 디버프 — 커스 · 둠
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

    public class SwordDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_turnDamage;
        private readonly float m_maxHpCut;
        private readonly float m_fixedRatio;

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
            if (m_maxHpCut > 0f) enemy.Health.ScaleMax(1f / (1f - m_maxHpCut));
        }

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
