namespace SquareTD
{
    // 전기 디버프 — 누전 · 감전
    public class ElectricDebuffRule : DebuffRule
    {
        public static float ChargeOf(Zone zone) => SkillFormula.ZoneValue(zone) * 100f;

        public override void OnStep(Zone zone, EnemyController enemy)
        {
            if (zone.Stage == ZoneStage.Skill1) ChargeEffect.Add(enemy, ChargeOf(zone));
        }

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            return new ElectricDebuffEffect(zone, zone.Stage == ZoneStage.Skill2 ? ChargeOf(zone) : 0f);
        }
    }

    public class ElectricDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_chargePerTurn;

        public ElectricDebuffEffect(Zone zone, float chargePerTurn)
            : base(TowerEmblem.Electricity, zone.Stage, zone.Owner.Stats.Duration, ElectricDebuffRule.ChargeOf(zone),
                   zone.Stage == ZoneStage.Skill2
                       ? $"매 턴 전하 +{chargePerTurn:0.##}, 과부하가 전하를 2배로 셈"
                       : $"밟을 때 전하 +{ElectricDebuffRule.ChargeOf(zone):0.##}")
        {
            m_chargePerTurn = chargePerTurn;
        }

        public override void OnTurn(EnemyController enemy)
        {
            if (m_chargePerTurn > 0f) ChargeEffect.Add(enemy, m_chargePerTurn);
        }
    }
}
