using System.Collections.Generic;

namespace SquareTD
{
    // 활 버프 — 요정의 활(스킬 1) · 정령의 활(스킬 2) (20번 4-5장)
    //   거리 비례 추가 피해: 날아간 타일 하나당 +(0.001 × Power × AbilityValue × 스택)%, 사거리 끝에서 최대 (TowerFinalStats.GetDistanceMultiplier)
    //   정령의 활은 사거리 +1칸 (그만큼 추가 피해의 최대치도 오름)
    //   찌르기 시너지: 받는 타워가 찌르기(활·창)면 거리와 무관하게 늘 최대치 + 5%(요정) / 10%(정령)
    public class BowBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.DistanceDamage, SkillFormula.ZoneValue(zone)),     // 타일 하나당 (0.012 = +1.2%)
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
