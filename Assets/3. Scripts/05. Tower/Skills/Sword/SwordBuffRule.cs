using System.Collections.Generic;

namespace SquareTD
{
    // 검 버프 — 블레싱(스킬 1) · 글로리(스킬 2) (20번 4-3장)
    //   공격력 +(0.001 × Power × AbilityValue × 스택)%. 글로리는 최종 피해 +10%도
    //   베기 시너지: 받는 타워가 베기(검·도끼)면 치명타 확률 +5%(블레싱) / +10%(글로리)
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
                float critBonus = isSkill2 ? 0.1f : 0.05f;      // 치명 확률은 0 ~ 1로 들고 있음 (0.1 = 10%)
                modifiers.Add(StatModifier.Flat(source, StatType.CritRate, critBonus));
            }
            return modifiers;
        }
    }
}
