using System.Collections.Generic;

namespace SquareTD
{
    // 물 버프 — 웰스프링 · 오버플로우 (스킬 카운트가 쌓이는 양 +브론즈 5%, 오버플로우 동안 스킬 2 피해 1.5배, 물 타워는 잃은 체력 비례 피해 최대 +10%)
    public class WaterBuffRule : BuffRule
    {
        public const float BronzeSkillCountGain = 0.05f;
        public const float OverflowSkillDamage = 0.5f;
        public const float WaterLostHpDamage = 0.1f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Flat(source, StatType.SkillCountGainRate, cast.Bronze(BronzeSkillCountGain)) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.SkillDamageBonus, OverflowSkillDamage));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.LostHpDamage, WaterLostHpDamage));
            return modifiers;
        }
    }
}
