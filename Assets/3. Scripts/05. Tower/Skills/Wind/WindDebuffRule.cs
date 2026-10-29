using UnityEngine;

namespace SquareTD
{
    // 바람 디버프 — 역풍 · 난기류
    public class WindDebuffRule : DebuffRule
    {
        public const float TurbulenceRate = 0.04f;

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float chance = Resist(enemy, SkillFormula.ZoneValue(zone));
            float hpRate = isSkill2 ? Resist(enemy, TurbulenceRate) : 0f;
            return new WindDebuffEffect(zone, chance, hpRate);
        }
    }

    public class WindDebuffEffect : EmblemDebuffEffect
    {
        private readonly TowerController m_owner;
        private readonly float m_chance;
        private readonly float m_hpRate;

        public WindDebuffEffect(Zone zone, float chance, float hpRate)
            : base(TowerEmblem.Wind, zone.Stage, zone.Owner.Stats.Duration, chance,
                   $"이동할 때 {chance * 100f:0.##}%로 −최대 ~ +최대" + (hpRate > 0f ? $", 이동한 칸마다 현재 체력 {hpRate * 100f:0.#}%" : ""))
        {
            m_owner = zone.Owner;
            m_chance = chance;
            m_hpRate = hpRate;
        }

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range)
        {
            if (range.Min == 0 && range.Max == 0) return range;
            if (!Chance.Roll(m_chance)) return range;
            return new MoveRange(-range.Max, range.Max, range.Action);
        }

        public override void OnMoved(EnemyController enemy, int steps)
        {
            if (m_hpRate <= 0f || steps == 0) return;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner != null ? m_owner : null,
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Wind,
                BaseDamage = enemy.CurrentHp * m_hpRate * Mathf.Abs(steps),
                ForceFixed = true,
            });
        }

        public override float GetCritRateBonus(TowerController attacker)
        {
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Wind || !attacker.IsTarget) return 0f;
            return IsPermanent ? 0.2f : 0.1f;
        }
    }
}
