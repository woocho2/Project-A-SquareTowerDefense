namespace SquareTD
{
    // 대지 디버프 — 진흙 · 석화
    public class EarthDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float bindChance = Resist(enemy, SkillFormula.ZoneValue(zone));
            return new EarthDebuffEffect(zone, bindChance, isSkill2 ? 0.1f : 0.2f, isSkill2 ? 2 : 0);
        }
    }

    public class EarthDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_bindChance;

        public EarthDebuffEffect(Zone zone, float bindChance, float defenseUp, int actionUp)
            : base(TowerEmblem.Earth, zone.Stage, zone.Owner.Stats.Duration, bindChance,
                   $"턴마다 {bindChance * 100f:0.##}%로 속박, 방어력 +{defenseUp * 100f:0}%" + (actionUp > 0 ? $", 행동 주기 +{actionUp}" : ""),
                   Modifiers(defenseUp, actionUp))
        {
            m_bindChance = bindChance;
        }

        private static StatModifier[] Modifiers(float defenseUp, int actionUp)
        {
            string source = KeyOf(TowerEmblem.Earth);
            StatModifier defense = StatModifier.Percent(source, StatType.Defense, defenseUp);
            if (actionUp <= 0) return new[] { defense };
            return new[] { defense, StatModifier.Flat(source, StatType.MaxAction, actionUp) };
        }

        public override void OnTurn(EnemyController enemy)
        {
            if (Chance.Roll(m_bindChance)) enemy.Status.Add(new ImmobilizeEffect("속박"));
        }
    }
}
