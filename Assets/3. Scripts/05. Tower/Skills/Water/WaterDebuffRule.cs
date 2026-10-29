namespace SquareTD
{
    // 물 디버프 — 젖음 · 침수
    public class WaterDebuffRule : DebuffRule
    {
        public const float MapBuffCut = 0.5f;

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float damageTaken = Resist(enemy, SkillFormula.ZoneValue(zone));
            float mapCut = isSkill2 ? Resist(enemy, MapBuffCut) : 0f;
            return new WaterDebuffEffect(zone, damageTaken, mapCut);
        }
    }

    public class WaterDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_damageTaken;

        public WaterDebuffEffect(Zone zone, float damageTaken, float mapCut)
            : base(TowerEmblem.Water, zone.Stage, zone.Owner.Stats.Duration, damageTaken,
                   $"이동력을 바꾼 공격에 받는 피해 +{damageTaken * 100f:0.##}% (일반 공격 +{damageTaken * 50f:0.##}%)" + (mapCut > 0f ? $", 맵 버프 −{mapCut * 100f:0.#}%" : ""),
                   MapModifiers(mapCut))
        {
            m_damageTaken = damageTaken;
        }

        private static StatModifier[] MapModifiers(float mapCut) => mapCut > 0f
            ? new[] { StatModifier.Flat(KeyOf(TowerEmblem.Water), StatType.MapBuffRate, -mapCut) }
            : new StatModifier[0];

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            float increase = info.ChangesMovement ? m_damageTaken : m_damageTaken * 0.5f;
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            return taken > 0f ? damage * (taken + increase) / taken : damage;
        }
    }
}
