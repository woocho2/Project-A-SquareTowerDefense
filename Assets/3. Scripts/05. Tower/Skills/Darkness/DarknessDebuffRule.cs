namespace SquareTD
{
    // 어둠 디버프 — 잠식(스킬 1) · 침식(스킬 2) (20번 4-16장)
    //   그 적이 죽으면 카운트 +(0.001 × Power × AbilityValue × 스택) — %가 아니라 그 수 그대로 (처치 기본 +1에 더함). 저항과 무관
    //   어둠 버프 타워가 없어도 쌓임
    //   침식의 고유 효과: 받는 피해 +10% (이것만 저항력만큼 줄어듦, 다른 받는 피해 증가와 더함)
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

    // 잠식·침식이 적에게 걸려 있는 동안
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
            // [D] 죽으면 카운트 + — 판의 카운트(EmblemSharedState.Current)에 AddDarkness(m_countBonus)
        }
    }
}
