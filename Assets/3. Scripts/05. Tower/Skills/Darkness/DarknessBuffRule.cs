using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 버프 — 포식(스킬 1) · 폭식(스킬 2) (20번 4-16장) — 카운트가 닿은 단계까지 전부 받음 (누적)
    //   "식"인 단계의 증가량 = (0.001 × Power × AbilityValue × 스택)% (포식은 지금 스택, 폭식은 최대 스택)
    //     100 공격력 · 200 치명 확률 · 300 치명 피해 · 700 AbilityValue · 1000 최종 피해
    //   고정 단계 (포식·폭식 같음): 400 공격 횟수 +1 · 500 행동 주기 −1 · 600 처치 보상 +150% · 800 Mastery +1 · 900 모든 공격이 고정 피해
    //   폭식의 고유 효과: 사거리 +1. 카운트는 버프 타워가 행동할 때 다시 읽음
    public class DarknessBuffRule : BuffRule
    {
        private readonly Func<float> m_count;

        public DarknessBuffRule(Func<float> count)
        {
            m_count = count;
        }

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            float value = SkillFormula.ZoneValue(zone);
            float count = m_count();
            var modifiers = new List<StatModifier>();

            if (count >= 100f) modifiers.Add(StatModifier.Percent(source, StatType.Power, value));
            if (count >= 200f) modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            if (count >= 300f) modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, value));
            // [C] 카운트 400: 공격 횟수 +1 — 위아래 줄과 같은 모양 (if (count >= 400f) …, StatType.AttackCount, 1f)
            if (count >= 500f) modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, -1f));
            if (count >= 600f) modifiers.Add(StatModifier.Flat(source, StatType.RewardBonus, 1.5f));
            if (count >= 700f) modifiers.Add(StatModifier.Percent(source, StatType.AbilityValue, value));
            if (count >= 800f) modifiers.Add(StatModifier.Flat(source, StatType.Mastery, 1f));
            if (count >= 900f) modifiers.Add(StatModifier.Flat(source, StatType.ForceFixedDamage, 1f));
            if (count >= 1000f) modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, value));

            if (zone.Stage == ZoneStage.Skill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.Range, 1f));
            }
            return modifiers;
        }
    }
}
