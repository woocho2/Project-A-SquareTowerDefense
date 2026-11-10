namespace SquareTD
{
    // 활 디버프 — 락 온 · 핀 포인트 (받는 최종 피해, 핀 포인트 받는 치명타 확률 +10%, 찌르기 피해의 20% 추가타)
    public class BowDebuffRule : DebuffRule
    {
        public const float PinPointCritRate = 0.1f;
        public const float PierceFollowUp = 0.2f;

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            float damageTaken = Resist(enemy, cast.Rate);
            float critRate = cast.IsSkill2 ? Resist(enemy, PinPointCritRate) : 0f;
            return new BowDebuffEffect(cast, damageTaken, critRate);
        }
    }

    public class BowDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_critRate;

        public BowDebuffEffect(SupportCast cast, float damageTaken, float critRate)
            : base(cast, $"받는 최종 피해 +{damageTaken * 100f:0.##}%" + (cast.IsSkill2 ? $", 받는 치명타 확률 +{critRate * 100f:0.#}%, 찌르기 피해의 20% 추가타" : ""),
                   StatModifier.Flat(KeyOf(TowerEmblem.Bow), StatType.DamageTaken, damageTaken))
        {
            m_critRate = critRate;
        }

        public override float GetCritRateBonus(TowerController attacker) => m_critRate;

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!IsSkill2 || !enemy.IsAlive || damage <= 0f) return;
            if (info.Attribute != AttackAttribute.Pierce) return;
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return;

            enemy.TakeDamage(new DamageInfo
            {
                Attacker = info.Attacker,
                Source = DamageSource.Relay,
                Emblem = TowerEmblem.Bow,
                Attribute = info.Attribute,
                BaseDamage = damage * BowDebuffRule.PierceFollowUp,
                ForceFixed = true,
            });
        }
    }
}
