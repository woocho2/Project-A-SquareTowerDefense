namespace SquareTD
{
    // 활 디버프 — 락 온 · 핀 포인트
    public class BowDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float damageTaken = Resist(enemy, SkillFormula.ZoneValue(zone));
            float critDamageTaken = isSkill2 ? Resist(enemy, 0.5f) : 0f;
            float critRateBonus = Resist(enemy, isSkill2 ? 0.1f : 0.05f);
            return new BowDebuffEffect(zone, damageTaken, critDamageTaken, critRateBonus);
        }
    }

    public class BowDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_critDamageTaken;
        private readonly float m_critRateBonus;

        public BowDebuffEffect(Zone zone, float damageTaken, float critDamageTaken, float critRateBonus)
            : base(TowerEmblem.Bow, zone.Stage, zone.Owner.Stats.Duration, damageTaken,
                   $"받는 최종 피해 +{damageTaken * 100f:0.##}%" + (critDamageTaken > 0f ? $", 받는 치명 피해 +{critDamageTaken * 100f:0.#}%" : "") + $", 찌르기의 치명 확률 +{critRateBonus * 100f:0.#}%",
                   StatModifier.Flat(KeyOf(TowerEmblem.Bow), StatType.DamageTaken, damageTaken))
        {
            m_critDamageTaken = critDamageTaken;
            m_critRateBonus = critRateBonus;
        }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (m_critDamageTaken <= 0f || !info.IsCritical || info.CritMultiplier <= 0f) return damage;
            return damage * (info.CritMultiplier + m_critDamageTaken) / info.CritMultiplier;
        }

        public override float GetCritRateBonus(TowerController attacker)
        {
            if (attacker == null) return 0f;
            if (attacker.Definition.Attribute != AttackAttribute.Pierce) return 0f;

            return m_critRateBonus;
        }
    }
}
