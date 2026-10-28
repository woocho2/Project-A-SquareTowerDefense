using System.Collections.Generic;

namespace SquareTD
{
    // 불 버프 — 불씨(스킬 1) · 불길(스킬 2) (20번 4-9장)
    //   공격력 +(0.001 × Power × AbilityValue × 스택)%. 불길은 치명 확률 +10%, 치명 피해 +30%, 공격 횟수 +1도
    //   불길은 영구가 아님 (불만의 예외): (컬러 강화 레벨 × 5)턴 뒤 0스택의 불씨로 돌아감 → 불씨 → 불길 → 불씨 …
    //   불 타워끼리의 맞물림: 불 단일은 공격 횟수 +1(불씨) / +2(불길, 모두가 받는 +1을 포함한 값), 불 광역은 StackValue +20%
    public class FireBuffRule : BuffRule
    {
        public const int BlazeTurnsPerLevel = 5;

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Percent(source, StatType.Power, SkillFormula.ZoneValue(zone)),
            };

            if (isSkill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.CritRate, 0.1f));
                // [C] 치명 피해 +30% — 위아래 줄과 같은 모양 (StatType.CritDamage, 0.3f)
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, 1f));
            }

            if (receiver.Definition.Emblem == TowerEmblem.Fire && receiver.IsTarget)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, 1f));     // 불씨 +1, 불길에서는 위의 +1과 합쳐 +2
            }

            if (receiver.Definition.Emblem == TowerEmblem.Fire && receiver.IsSplash)
            {
                modifiers.Add(StatModifier.Percent(source, StatType.StackValue, 0.2f));
            }
            return modifiers;
        }

        // 불길이 켜진 다음 턴에 시간을 재기 시작 → (레벨 × 5)턴이 지나면 0스택의 불씨로
        public override bool OnZoneTurn(Zone zone)
        {
            if (zone.Stage != ZoneStage.Skill2) return false;

            if (zone.StageTurnsLeft == 0)
            {
                zone.StartStageTimer(zone.Owner.Stats.ColorLevel * BlazeTurnsPerLevel);
                return false;
            }

            if (!zone.TickStageTimer()) return false;
            // [D] 불길이 끝남 → 0스택의 불씨로. Zone에 만들어 둔 함수 하나를 부름 (zone.ResetToSkill1)
            return true;
        }
    }
}
