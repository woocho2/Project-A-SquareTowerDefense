using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 도끼 버프 — 출정가 · 발할라의 찬가
    public class AxeBuffRule : BuffRule
    {
        private static readonly int[] s_limitDivisors = { 2, 5, 10, 20, 40 };

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.CritDamage, SkillFormula.ZoneValue(zone)),
            };

            if (isSkill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, -1f));
                modifiers.Add(StatModifier.Flat(source, StatType.Mastery, +1f));
            }

            if (IsSynergy(zone, receiver))
            {
                float critDamageBonus = isSkill2 ? 0.5f : 0.25f;
                modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, critDamageBonus));
            }
            return modifiers;
        }

        public override int GetLimitModifier(TowerController owner, int baseLimit)
        {
            int tier = Mathf.Clamp(owner.Definition.Tier, 1, s_limitDivisors.Length);
            return -(baseLimit / s_limitDivisors[tier - 1]);
        }
    }
}
