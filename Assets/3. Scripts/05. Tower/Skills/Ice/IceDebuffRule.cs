namespace SquareTD
{
    // 얼음 디버프 — 동상(스킬 1) · 저체온증(스킬 2) (20번 4-13장)
    //   턴마다 (0.001 × Power × AbilityValue × 스택)% 확률로 빙결 — 확률은 저항력만큼 줄고, 빙결 자체도 강한 CC라 지금 저항력%로 한 번 더 막음
    //   저체온증의 고유 효과: 최소·최대 이동력 −1. 정수형이라 저항할 수 없음
    public class IceDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            float freezeChance = Resist(enemy, SkillFormula.ZoneValue(zone));
            return new IceDebuffEffect(zone, freezeChance);
        }
    }

    // 동상·저체온증이 적에게 걸려 있는 동안
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

        // ⑤ 턴마다 한 번 굴림
        public override void OnTurn(EnemyController enemy)
        {
            if (Chance.Roll(m_freezeChance)) enemy.Status.Add(new FreezeEffect());
        }
    }
}
