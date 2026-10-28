using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 해머 버프 — 제련(스킬 1) · 세공(스킬 2) (20번 4-8장)
    //   추가 피해 +(0.001 × Power × AbilityValue × 스택 × 현재 골드)% — 공통 식에 골드를 곱함 (방패가 한도를 곱하는 것과 같은 모양)
    //   골드는 버프 타워가 행동할 때 다시 셈 (그사이 골드가 바뀌면 다음 행동부터)
    //   세공: 존 위 타워가 행동할 때마다 10% 확률로 젬을 (사이클 번호)개
    //   타격 시너지: 받는 타워가 타격(방패·해머)이면 공격 횟수 +1(제련) / +2(세공)
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
                StatModifier.Flat(source, StatType.BonusDamage, SkillFormula.ZoneValue(zone) * m_currentGold()),   // 1.2% × 골드 500 = +600%
            };

            if (IsSynergy(zone, receiver))
            {
                // [C] 공격 횟수 +1(제련) / +2(세공) — 위 BonusDamage 줄과 같은 모양으로 modifiers.Add (StatType.AttackCount, 값은 isSkill2 ? 2f : 1f)
            }
            return modifiers;
        }

        // 세공만. 젬은 공격하는 타워의 행동을 기준으로 (20번 4-8장)
        public override void OnReceiverActed(Zone zone, TowerController receiver)
        {
            if (zone.Stage != ZoneStage.Skill2) return;
            if (Chance.Roll(GemChance)) m_addGem(m_currentCycle());
        }
    }
}
