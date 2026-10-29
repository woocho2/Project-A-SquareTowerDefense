using System.Collections.Generic;

namespace SquareTD
{
    // 활 버프 — 요정의 활 · 정령의 활
    public class BowBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.DistanceDamage, SkillFormula.ZoneValue(zone)),
            };

            if (isSkill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.Range, +1f));
            }

            if (IsSynergy(zone, receiver))
            {
                float pierceBonus = isSkill2 ? 0.1f : 0.05f;
                modifiers.Add(StatModifier.Flat(source, StatType.DistanceDamageBonus, pierceBonus));
            }
            return modifiers;
        }
    }
}
