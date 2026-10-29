namespace SquareTD
{
    // 얼음 디버프 — 동상 · 저체온증
    public class IceDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            float freezeChance = Resist(enemy, SkillFormula.ZoneValue(zone));
            return new IceDebuffEffect(zone, freezeChance);
        }
    }

    public class IceDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_freezeChance;

        public IceDebuffEffect(Zone zone, float freezeChance)
            : base(TowerEmblem.Ice, zone.Stage, zone.Owner.Stats.Duration, freezeChance,
                   $"턴마다 {freezeChance * 100f:0.##}%로 빙결" + (zone.Stage == ZoneStage.Skill2 ? ", 최소·최대 이동력 −1" : ""),
                   SpeedModifiers(zone.Stage == ZoneStage.Skill2))
        {
            m_freezeChance = freezeChance;
        }

        private static StatModifier[] SpeedModifiers(bool isSkill2)
        {
            if (!isSkill2) return new StatModifier[0];
            string source = KeyOf(TowerEmblem.Ice);
            return new[]
            {
                StatModifier.Flat(source, StatType.SpeedMin, -1f),
                StatModifier.Flat(source, StatType.SpeedMax, -1f),
            };
        }

        public override void OnTurn(EnemyController enemy)
        {
            if (Chance.Roll(m_freezeChance)) enemy.Status.Add(new FreezeEffect());
        }
    }
}
