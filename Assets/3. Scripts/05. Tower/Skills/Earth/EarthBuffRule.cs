using System.Collections.Generic;

namespace SquareTD
{
    // 대지 버프 — 성장(스킬 1) · 만개(스킬 2) (20번 4-11장)
    //   존 위 타워의 AbilityValue +(0.001 × Power × AbilityValue × 스택)% — 식의 Power·AV는 대지 버프 타워의 값
    //   만개의 고유 효과: 그 타워의 컬러 레벨 +1 (최대 6). 존을 벗어나면 사라짐. 영구 (불·전기처럼 돌지 않음)
    //   대지 광역·단일이 더 빨리 자라는 것(+1.5% / +2%)은 스킬 쪽이 봄 (EarthSplashSkill.GrowthPerUse)
    public class EarthBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Percent(source, StatType.AbilityValue, SkillFormula.ZoneValue(zone)),
            };

            if (zone.Stage == ZoneStage.Skill2)
            {
                // [C] 만개: 컬러 레벨 +1 — modifiers.Add로 Flat 하나 (StatType.ColorLevelBonus, 1f). 위 줄은 Percent라 모양만 참고
            }
            return modifiers;
        }
    }
}
