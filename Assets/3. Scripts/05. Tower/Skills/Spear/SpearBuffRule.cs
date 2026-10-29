using System.Collections.Generic;

namespace SquareTD
{
    // 창 버프 — 카우치 · 랜스 차지
    public class SpearBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (zone.Stage != ZoneStage.Skill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, 2f));
                return modifiers;
            }

            float value = SkillFormula.ZoneValue(zone);
            modifiers.Add(StatModifier.Flat(source, StatType.ArmorPenetration, 0.2f));
            modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, value));

            if (IsSynergy(zone, receiver))
            {
                modifiers.Add(StatModifier.Flat(source, StatType.ArmorPenetration, 0.1f));
            }
            return modifiers;
        }
    }
}
