using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 버프 — 워 송 · 웰컴 투 발할라 (치명타 피해, 발할라 최대 Action -1, 베기 치명타 피해 +20%)
    public class AxeBuffRule : BuffRule
    {
        public const float SlashCritDamage = 0.2f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Flat(source, StatType.CritDamage, cast.Rate) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, -1f));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, SlashCritDamage));
            return modifiers;
        }
    }
}
