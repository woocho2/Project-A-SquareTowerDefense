namespace SquareTD
{
    // 해머 디버프 — 원석 · 금맥
    public class HammerDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            return new HammerDebuffEffect(zone, SkillFormula.ZoneValue(zone));
        }
    }

    public class HammerDebuffEffect : EmblemDebuffEffect
    {
        public const float GemChance = 0.1f;

        private readonly float m_goldRate;

        public HammerDebuffEffect(Zone zone, float goldRate)
            : base(TowerEmblem.Hammer, zone.Stage, zone.Owner.Stats.Duration, goldRate,
                   $"처치 골드 +{goldRate * 100f:0.##}%" + (zone.Stage == ZoneStage.Skill2 ? ", 죽을 때 10%로 젬 1개" : "") +
                   $", 타격이 처치하면 +{(zone.Stage == ZoneStage.Skill2 ? 10 : 5)}%")
        {
            m_goldRate = goldRate;
        }

        public float BluntBonus => IsPermanent ? 0.1f : 0.05f;

        public override float GetGoldBonusRate(KillInfo kill)
        {
            float rate = m_goldRate;
            if (kill.Damage.Attacker != null && kill.Damage.Attribute == AttackAttribute.Blunt) rate += BluntBonus;
            return rate;
        }

        public override int RollGemBonus(KillInfo kill) => IsPermanent && Chance.Roll(GemChance) ? 1 : 0;
    }
}
