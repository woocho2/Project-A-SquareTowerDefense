using UnityEngine;

namespace SquareTD
{
    // 검 디버프 — 위더링 커스 · 둠 레퀴엠 (최대 체력 감소, 둠 레퀴엠 매 턴 최대 체력 1%, 베기 피해의 20% 고정)
    public class SwordDebuffRule : DebuffRule
    {
        public const float DoomTurnRate = 0.01f;
        public const float SlashFixedRatio = 0.2f;

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            float maxHpCut = Mathf.Min(Resist(enemy, cast.Rate), 0.95f);
            float turnRate = cast.IsSkill2 ? Resist(enemy, DoomTurnRate) : 0f;
            float fixedRatio = cast.IsSkill2 ? Resist(enemy, SlashFixedRatio) : 0f;
            return new SwordDebuffEffect(cast, maxHpCut, turnRate, fixedRatio);
        }
    }

    public class SwordDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_maxHpCut;
        private readonly float m_turnRate;
        private readonly float m_fixedRatio;

        public SwordDebuffEffect(SupportCast cast, float maxHpCut, float turnRate, float fixedRatio)
            : base(cast, $"최대 체력 −{maxHpCut * 100f:0.##}%" + (turnRate > 0f ? $", 매 턴 최대 체력 {turnRate * 100f:0.##}% 피해, 베기 피해의 {fixedRatio * 100f:0.#}% 고정" : ""))
        {
            m_maxHpCut = maxHpCut;
            m_turnRate = turnRate;
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
            if (m_turnRate <= 0f) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = Cast.Owner,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Sword,
                BaseDamage = enemy.MaxHp * m_turnRate,
                ForceFixed = true,
            });
        }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (m_fixedRatio <= 0f || info.Attribute != AttackAttribute.Slash || info.ForceFixed) return damage;
            float defense = enemy.Stats.Get(StatType.Defense);
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            float ratio = enemy.Stats.Get(StatType.FixedDamageRatio);
            float before = DamageCalculator.Calculate(info, defense, taken, ratio);
            float after = DamageCalculator.Calculate(info, defense, taken, Mathf.Min(1f, ratio + m_fixedRatio));
            return before > 0f ? damage * after / before : damage;
        }
    }
}
