using System.Collections.Generic;
using System.Data;

namespace SquareTD
{
    // 창 버프 — 카우치(스킬 1) · 랜스 차지(스킬 2) (20번 4-6장, 2026-10-28 기획 변경)
    //   카우치: 돌격을 준비하는 대가만 — 행동력(행동 주기) +2. 좋은 효과 없음
    //   랜스 차지: 대가가 사라지고 방어 관통 +20%, 치명 확률·치명 피해 +(0.001 × Power × AbilityValue × maxStack)%
    //   찌르기 시너지: 받는 타워가 찌르기(활·창)면 랜스 차지의 방어 관통 +10% (합계 30%)
    public class SpearBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (zone.Stage != ZoneStage.Skill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, 2f));
                return modifiers;
            }

            float value = SkillFormula.ZoneValue(zone);
            modifiers.Add(StatModifier.Flat(source, StatType.ArmorPenetration, 0.2f));
            modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, value));

            if (IsSynergy(zone, receiver))
            {
                modifiers.Add(StatModifier.Flat(source, StatType.ArmorPenetration, 0.1f));
            }
            return modifiers;
        }
    }
}
