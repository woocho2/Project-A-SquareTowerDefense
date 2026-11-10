using System.Collections.Generic;

namespace SquareTD
{
    // 검 버프 — 브레이브 소드 · 글로리 소드 (공격력, 글로리 소드 최종 피해 +10%, 베기 치명타 확률 +10%)
    public class SwordBuffRule : BuffRule
    {
        public const float GloryFinalDamage = 0.1f;
        public const float SlashCritRate = 0.1f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Percent(source, StatType.Power, cast.Rate) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, GloryFinalDamage));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.CritRate, SlashCritRate));
            return modifiers;
        }
    }
}
