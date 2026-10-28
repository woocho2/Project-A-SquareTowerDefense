using System.Collections.Generic;

namespace SquareTD
{
    // 바람 버프 — 순풍(스킬 1) · 질풍(스킬 2) (20번 4-12장)
    //   버프 타워가 행동할 때마다 존 위 타워를 대상으로 (0.001 × Power × AbilityValue × 스택)% 확률로 터짐
    //   터지면 (버프 타워의 레벨 × 2)번 행동하는 동안 매 턴 행동 카운트 +2, 공격 횟수 +1(순풍) / +2(질풍). 또 터지면 처음부터
    //   존을 벗어나면 터진 버프도 바로 끝남
    //   질풍의 고유 효과: 질풍 존에 선 타워는 최대 Action −1 (존에 서 있는 동안 항상)
    //   바람 타워끼리 (존에 서 있는 동안 항상): 바람 광역은 치명 피해 +15% / +30%, 바람 단일은 공격 횟수 +1 / +2 (터진 것과 따로 더함)
    public class WindBuffRule : BuffRule
    {
        public const string BurstSource = "순풍·질풍";

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>();

            if (isSkill2)
            {
                // [C] 질풍: 최대 Action −1 — AxeBuffRule의 발할라의 찬가에 똑같은 줄이 있음
            }

            if (receiver.Definition.Emblem == TowerEmblem.Wind && receiver.IsSplash)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, isSkill2 ? 0.3f : 0.15f));
            }

            if (receiver.Definition.Emblem == TowerEmblem.Wind && receiver.IsTarget)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, isSkill2 ? 2f : 1f));
            }
            return modifiers;
        }

        public override void OnGiveBuff(Zone zone, TowerController receiver)
        {
            if (!Chance.Roll(SkillFormula.ZoneValue(zone))) return;

            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            TimedTowerBuff.Apply(receiver.State, BurstSource, zone.Owner.Stats.ColorLevel * 2,
                StatModifier.Flat(BurstSource, StatType.ActionGainPerTurn, 2f),
                StatModifier.Flat(BurstSource, StatType.AttackCount, isSkill2 ? 2f : 1f));
        }

        public override void OnTaken(Zone zone, TowerController receiver)
        {
            // [D] 존을 벗어나면 터진 버프도 끝 — 위 OnGiveBuff에서 Apply로 건 것을 같은 출처로 뗌 (TimedTowerBuff.Remove(받는 타워의 State, BurstSource))
        }
    }
}
