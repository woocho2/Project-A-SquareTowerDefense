namespace SquareTD
{
    // 해머 디버프 — 원석(스킬 1) · 금맥(스킬 2) (20번 4-8장)
    //   그 적을 처치하면 골드 +(0.001 × Power × AbilityValue × 스택)% (소수는 버림). 적을 약하게 만드는 효과는 없음
    //   금맥: 그 적이 죽을 때 10% 확률로 젬 1개
    //   타격 시너지: 타격 타워(방패·해머)가 처치하면 골드 +5%(원석) / +10%(금맥). 고정 수치, 골드 증가끼리는 더함
    //   보상이라 저항력과 무관 (골드 증가도 젬 확률도 줄지 않음)
    public class HammerDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            return new HammerDebuffEffect(zone, SkillFormula.ZoneValue(zone));
        }
    }

    // 원석·금맥이 적에게 걸려 있는 동안. 보상 보정은 DeathResolver가 처치 때 물어봄
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
