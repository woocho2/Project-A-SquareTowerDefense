namespace SquareTD
{
    // 전기 디버프 — 누전(스킬 1) · 감전(스킬 2) (20번 4-14장)
    //   누전: 누전 존을 밟을 때마다 전하 +(0.001 × Power × AbilityValue × 지금 스택) — %가 아니라 그 수 그대로 (브론즈 최대 1.2)
    //   감전: 매 턴 전하 +(0.001 × Power × AbilityValue × maxStack). 과부하가 감전인 적의 전하를 2배로 셈 (ElectricTargetSkill)
    //   적을 직접 깎는 효과는 없음. 전기는 저항력과 무관
    public class ElectricDebuffRule : DebuffRule
    {
        // SkillFormula.ZoneValue는 % 값(0.012 = 1.2%)이라 100을 곱해 "전하 수"로
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

    // 누전·감전이 적에게 걸려 있는 동안
    public class ElectricDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_chargePerTurn;     // 감전: 매 턴 쌓는 전하 (누전은 0 — 밟을 때 OnStep에서 줌)

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
