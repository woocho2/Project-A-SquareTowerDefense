using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 도끼 버프 — 출정가(스킬 1) · 발할라의 찬가(스킬 2) (20번 4-4장)
    //   치명타 피해 +(0.001 × Power × AbilityValue × 스택)% — 배율이 아니라 더하는 값
    //   발할라의 찬가는 최대 Action −1, Mastery +1 (한도를 내놓는 리스크의 보상)
    //   베기 시너지: 받는 타워가 베기(검·도끼)면 치명타 피해 +25%(출정가) / +50%(발할라의 찬가)
    //   대가: 판 위에 있는 동안 적 수 한도를 깎음 (출정가 때부터). 한도 × 티어 비율, 소수 버림
    public class AxeBuffRule : BuffRule
    {
        // 티어(1 브론즈 ~ 5 다이아)마다 한도를 깎는 비율의 분모: 1/2, 1/5, 1/10, 1/20, 1/40
        private static readonly int[] s_limitDivisors = { 2, 5, 10, 20, 40 };

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.CritDamage, SkillFormula.ZoneValue(zone)),     // 치명 피해는 0.012 = +1.2%로 들고 있음
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

        // 사이클 1(한도 20): 브론즈 −10, 실버 −4, 골드 −2, 미스릴 −1, 다이아 −0. 하드(15) 브론즈 −7
        public override int GetLimitModifier(TowerController owner, int baseLimit)
        {
            int tier = Mathf.Clamp(owner.Definition.Tier, 1, s_limitDivisors.Length);
            return -(baseLimit / s_limitDivisors[tier - 1]);       // int ÷ int는 소수를 버림
        }
    }
}
