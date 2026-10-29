using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 해머 버프 — 제련 · 세공
    public class HammerBuffRule : BuffRule
    {
        public const float GemChance = 0.1f;

        private readonly Func<int> m_currentGold;
        private readonly Func<int> m_currentCycle;
        private readonly Action<int> m_addGem;

        public HammerBuffRule(Func<int> currentGold, Func<int> currentCycle, Action<int> addGem)
        {
            m_currentGold = currentGold;
            m_currentCycle = currentCycle;
            m_addGem = addGem;
        }

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.BonusDamage, SkillFormula.ZoneValue(zone) * m_currentGold()),
            };

            if (IsSynergy(zone, receiver))
            {
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, zone.Stage == ZoneStage.Skill2 ? 2f : 1f));
            }
            return modifiers;
        }

        public override void OnReceiverActed(Zone zone, TowerController receiver)
        {
            if (zone.Stage != ZoneStage.Skill2) return;
            if (Chance.Roll(GemChance)) m_addGem(m_currentCycle());
        }
    }
}
