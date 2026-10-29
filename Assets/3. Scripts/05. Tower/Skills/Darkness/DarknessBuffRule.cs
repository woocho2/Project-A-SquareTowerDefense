using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 버프 — 포식 · 폭식
    public class DarknessBuffRule : BuffRule
    {
        private readonly Func<float> m_count;

        public DarknessBuffRule(Func<float> count)
        {
            m_count = count;
        }

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            float value = SkillFormula.ZoneValue(zone);
            float count = m_count();
            var modifiers = new List<StatModifier>();

            if (count >= 100f) modifiers.Add(StatModifier.Percent(source, StatType.Power, value));
            if (count >= 200f) modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            if (count >= 300f) modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, value));
            if (count >= 400f) modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, 1f));
            if (count >= 500f) modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, -1f));
            if (count >= 600f) modifiers.Add(StatModifier.Flat(source, StatType.RewardBonus, 1.5f));
            if (count >= 700f) modifiers.Add(StatModifier.Percent(source, StatType.AbilityValue, value));
            if (count >= 800f) modifiers.Add(StatModifier.Flat(source, StatType.Mastery, 1f));
            if (count >= 900f) modifiers.Add(StatModifier.Flat(source, StatType.ForceFixedDamage, 1f));
            if (count >= 1000f) modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, value));

            if (zone.Stage == ZoneStage.Skill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.Range, 1f));
            }
            return modifiers;
        }
    }
}
