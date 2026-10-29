using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 방패 버프 — 프로텍트 · 이지스
    public class ShieldBuffRule : BuffRule
    {
        private static readonly int[] s_limitDivisors = { 40, 20, 10, 5, 2 };

        private readonly Func<int> m_currentLimit;

        public ShieldBuffRule(Func<int> currentLimit)
        {
            m_currentLimit = currentLimit;
        }

        public override bool UsesOwnerStack => true;

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            int limit = m_currentLimit();
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.FinalDamage, SkillFormula.ZoneValue(zone) * limit),
            };

            if (IsSynergy(zone, receiver))
            {
                float perLimit = isSkill2 ? 0.002f : 0.001f;
                modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, perLimit * limit));
            }
            return modifiers;
        }

        public override int GetLimitModifier(TowerController owner, int baseLimit)
        {
            if (!owner.State.Stack.IsFull) return 0;

            int tier = Mathf.Clamp(owner.Definition.Tier, 1, s_limitDivisors.Length);
            return (baseLimit / s_limitDivisors[tier - 1]);
        }
    }
}
