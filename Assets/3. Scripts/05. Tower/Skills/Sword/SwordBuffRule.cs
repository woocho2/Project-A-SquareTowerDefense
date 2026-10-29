using System.Collections.Generic;

namespace SquareTD
{
    // 검 버프 — 블레싱 · 글로리
    public class SwordBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Percent(source, StatType.Power, SkillFormula.ZoneValue(zone)),
            };

            if (isSkill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, 0.1f));
            }

            if (IsSynergy(zone, receiver))
            {
                float critBonus = isSkill2 ? 0.1f : 0.05f;
                modifiers.Add(StatModifier.Flat(source, StatType.CritRate, critBonus));
            }
            return modifiers;
        }
    }
}
