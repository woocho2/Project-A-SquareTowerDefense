using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 방패 버프 — 프로텍트(스킬 1) · 이지스(스킬 2) (20번 4-7장) — 다른 문양과 방식이 다름
    //   스택은 존이 아니라 가디언 자신이 쌓음 (UsesOwnerStack). 다 쌓으면 이지스를 펼침 = 스킬 2
    //   존 위 타워의 최종 피해 +(0.001 × Power × AbilityValue × 스택 × 한도)% — 한도가 늘면 버프도 커짐 (다음 행동 때 다시 셈)
    //   이지스: 적 수 한도 + (사이클의 기본 한도 × 티어 비율). 한 번 펼치면 그 타워가 사라질 때까지
    //   타격 시너지: 받는 타워가 타격(방패·해머)이면 추가 피해 (한도 × 0.1)% / (한도 × 0.2)%
    public class ShieldBuffRule : BuffRule
    {
        // 티어(1 브론즈 ~ 5 다이아)마다 한도를 늘리는 비율의 분모: 1/40, 1/20, 1/10, 1/5, 1/2 (도끼와 거꾸로)
        private static readonly int[] s_limitDivisors = { 40, 20, 10, 5, 2 };

        private readonly Func<int> m_currentLimit;          // 지금 적 수 한도 (게임에서는 GameSession, 테스트에서는 정해 준 값)

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
                StatModifier.Flat(source, StatType.FinalDamage, SkillFormula.ZoneValue(zone) * limit),     // 브론즈 1.2% × 한도 20 = +24%
            };

            if (IsSynergy(zone, receiver))
            {
                float perLimit = isSkill2 ? 0.002f : 0.001f;                                             // 한도 × 0.2% / 0.1%
                modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, perLimit * limit));
            }
            return modifiers;
        }

        // 이지스를 펼친 뒤(타워 스택이 다 참)에만. 사이클 1(기본 한도 20): 브론즈 +0, 실버 +1, 골드 +2, 미스릴 +4, 다이아 +10
        public override int GetLimitModifier(TowerController owner, int baseLimit)
        {
            if (!owner.State.Stack.IsFull) return 0;

            int tier = Mathf.Clamp(owner.Definition.Tier, 1, s_limitDivisors.Length);            
            return (baseLimit / s_limitDivisors[tier - 1]);
        }
    }
}
