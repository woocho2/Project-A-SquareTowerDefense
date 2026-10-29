namespace SquareTD
{
    // 어둠 디버프 — 잠식 · 침식
    public class DarknessDebuffRule : DebuffRule
    {
        public const float ErosionDamageTaken = 0.1f;

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            float countBonus = SkillFormula.ZoneValue(zone) * 100f;
            float damageTaken = zone.Stage == ZoneStage.Skill2 ? Resist(enemy, ErosionDamageTaken) : 0f;
            return new DarknessDebuffEffect(zone, countBonus, damageTaken);
        }
    }

    public class DarknessDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_countBonus;

        public DarknessDebuffEffect(Zone zone, float countBonus, float damageTaken)
            : base(TowerEmblem.Darkness, zone.Stage, zone.Owner.Stats.Duration, countBonus,
                   $"죽으면 카운트 +{countBonus:0.##}" + (damageTaken > 0f ? $", 받는 피해 +{damageTaken * 100f:0.#}%" : ""),
                   DamageModifiers(damageTaken))
        {
            m_countBonus = countBonus;
        }

        private static StatModifier[] DamageModifiers(float damageTaken) => damageTaken > 0f
            ? new[] { StatModifier.Flat(KeyOf(TowerEmblem.Darkness), StatType.DamageTaken, damageTaken) }
            : new StatModifier[0];

        public override void OnDeath(EnemyController enemy, KillInfo kill)
        {
            EmblemSharedState.Current.AddDarkness(m_countBonus);
        }
    }
}
