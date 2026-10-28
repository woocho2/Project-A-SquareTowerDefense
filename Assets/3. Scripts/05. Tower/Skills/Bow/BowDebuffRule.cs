namespace SquareTD
{
    // 활 디버프 — 락 온(스킬 1) · 핀 포인트(스킬 2) (20번 4-5장)
    //   받는 최종 피해 +(0.001 × Power × AbilityValue × 스택)% (다른 받는 피해 증가와 합연산 → DamageTaken에 더함)
    //   핀 포인트는 받는 치명타 피해 +50% (치명 배율 2 → 2.5)
    //   찌르기 시너지: 찌르기 타워(활·창)가 이 적을 치면 치명 확률 +5%(락 온) / +10%(핀 포인트)
    //   모든 수치는 적의 저항력만큼 줄어듦
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

    // 락 온·핀 포인트가 적에게 걸려 있는 동안 하는 일. 값은 BowDebuffRule이 저항까지 계산해서 넘김
    public class BowDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_critDamageTaken;   // 핀 포인트: 치명 배율에 더함 (락 온은 0)
        private readonly float m_critRateBonus;     // 찌르기 공격의 치명 확률에 더함

        public BowDebuffEffect(Zone zone, float damageTaken, float critDamageTaken, float critRateBonus)
            : base(TowerEmblem.Bow, zone.Stage, zone.Owner.Stats.Duration, damageTaken,
                   $"받는 최종 피해 +{damageTaken * 100f:0.##}%" + (critDamageTaken > 0f ? $", 받는 치명 피해 +{critDamageTaken * 100f:0.#}%" : "") + $", 찌르기의 치명 확률 +{critRateBonus * 100f:0.#}%",
                   StatModifier.Flat(KeyOf(TowerEmblem.Bow), StatType.DamageTaken, damageTaken))
        {
            m_critDamageTaken = critDamageTaken;
            m_critRateBonus = critRateBonus;
        }

        // 치명으로 들어온 공격이면 배율을 다시: 피해 × (배율 + 0.5) ÷ 배율
        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (m_critDamageTaken <= 0f || !info.IsCritical || info.CritMultiplier <= 0f) return damage;
            return damage * (info.CritMultiplier + m_critDamageTaken) / info.CritMultiplier;
        }

        public override float GetCritRateBonus(TowerController attacker)
        {
            if (attacker == null) return 0f;                                            // 장판·지속 피해 등 타워가 없는 피해
            if (attacker.Definition.Attribute != AttackAttribute.Pierce) return 0f;     // 찌르기(활·창)만

            return m_critRateBonus;            
        }
    }
}
