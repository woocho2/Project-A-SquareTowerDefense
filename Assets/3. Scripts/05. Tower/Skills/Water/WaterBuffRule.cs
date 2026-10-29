using System.Collections.Generic;

namespace SquareTD
{
    // 물 버프 — 물결 · 해류
    public class WaterBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.StackGainRate, SkillFormula.ZoneValue(zone)),
            };

            if (zone.Stage == ZoneStage.Skill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.StackKeepHalf, 1f));
            }
            return modifiers;
        }
    }
}
