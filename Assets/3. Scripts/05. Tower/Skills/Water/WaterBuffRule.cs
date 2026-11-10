using System.Collections.Generic;

namespace SquareTD
{
    // 물 버프 — 웰스프링 · 오버플로우 (차지가 쌓이는 양, 오버플로우 동안 차지 배율 1.5배, 물 타워는 잃은 체력 비례 피해 최대 +10%)
    public class WaterBuffRule : BuffRule
    {
        public const float OverflowChargeBonus = 0.5f;
        public const float WaterLostHpDamage = 0.1f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Flat(source, StatType.StackGainRate, cast.Rate) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.ChargeBonus, OverflowChargeBonus));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.LostHpDamage, WaterLostHpDamage));
            return modifiers;
        }
    }
}
