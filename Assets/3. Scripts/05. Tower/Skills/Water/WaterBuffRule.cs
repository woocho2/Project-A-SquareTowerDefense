using System.Collections.Generic;

namespace SquareTD
{
    // 물 버프 — 물결(스킬 1) · 해류(스킬 2) (20번 4-10장)
    //   존 위 타워는 스택이 쌓이는 양 +(0.001 × Power × AbilityValue × 스택)% (공격할 때 쌓는 자기 스택. 최대 스택·StackValue는 그대로)
    //   해류의 고유 효과: 스택을 써서 스킬 2를 쏘면 0이 아니라 최대의 절반에서 다시 (StackKeepHalf)
    //   물이 아닌 공격 타워도 받음 (버프존은 문양을 가리지 않음). 물 광역의 쓰나미 ×1.25 / ×1.5는 WaterSplashSkill이 봄
    public class WaterBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.StackGainRate, SkillFormula.ZoneValue(zone)),
            };

            if (zone.Stage == ZoneStage.Skill2)
            {
                // [C] 해류: 스택을 써도 절반에서 다시 — 위 StackGainRate 줄과 같은 모양으로 modifiers.Add (StatType.StackKeepHalf, 1f)
            }
            return modifiers;
        }
    }
}
