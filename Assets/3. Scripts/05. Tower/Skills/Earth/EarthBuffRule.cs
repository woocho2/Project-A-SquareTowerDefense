using System.Collections.Generic;

namespace SquareTD
{
    // 대지 버프 — 성장 · 만개
    public class EarthBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Percent(source, StatType.AbilityValue, SkillFormula.ZoneValue(zone)),
            };

            if (zone.Stage == ZoneStage.Skill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.ColorLevelBonus, 1f));
            }
            return modifiers;
        }
    }
}
