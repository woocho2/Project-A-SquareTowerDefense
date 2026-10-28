using System.Collections.Generic;

namespace SquareTD
{
    // 빛 버프 — 광채(스킬 1) · 광휘(스킬 2) (20번 4-15장)
    //   버프 타워가 행동할 때마다 존 위 타워를 대상으로 (0.001 × Power × AbilityValue × 스택)% 확률로 터짐 (바람의 순풍·질풍과 같은 방식)
    //   터지면 (버프 타워의 레벨 × 2)번 행동하는 동안: 공격력 +10%, 공격 횟수 +1, Mastery +1, 치명 확률 +5%, 치명 피해 +10%, 최대 Action −1
    //   광휘의 고유 효과: 여기에 AbilityValue +20%. 또 터지면 처음부터. 존을 벗어나면 바로 끝남
    //   빛 타워끼리 (존에 서 있는 동안 항상): 빛 단일은 공격 횟수 +1(광채) / +2(광휘), 빛 광역의 프리즘 3 / 4스택은 LightSplashSkill이 봄
    public class LightBuffRule : BuffRule
    {
        public const string BurstSource = "광채·광휘";

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (receiver.Definition.Emblem == TowerEmblem.Light && receiver.IsTarget)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, zone.Stage == ZoneStage.Skill2 ? 2f : 1f));
            }
            return modifiers;
        }

        public override void OnGiveBuff(Zone zone, TowerController receiver)
        {
            if (!Chance.Roll(SkillFormula.ZoneValue(zone))) return;

            var burst = new List<StatModifier>
            {
                StatModifier.Percent(BurstSource, StatType.Power, 0.1f),
                StatModifier.Flat(BurstSource, StatType.AttackCount, 1f),
                // [D] Mastery +1 — 바로 위 AttackCount 줄과 같은 모양 (StatType.Mastery, 1f). 목록 안이라 끝에 쉼표
                StatModifier.Flat(BurstSource, StatType.CritRate, 0.05f),
                StatModifier.Flat(BurstSource, StatType.CritDamage, 0.1f),
                StatModifier.Flat(BurstSource, StatType.MaxAction, -1f),
            };
            if (zone.Stage == ZoneStage.Skill2) burst.Add(StatModifier.Percent(BurstSource, StatType.AbilityValue, 0.2f));

            TimedTowerBuff.Apply(receiver.State, BurstSource, zone.Owner.Stats.ColorLevel * 2, burst.ToArray());
        }

        public override void OnTaken(Zone zone, TowerController receiver)
        {
            // [C] 존을 벗어나면 터진 버프도 끝 — WindBuffRule.OnTaken과 같은 한 줄 (출처는 이 파일의 BurstSource)
        }
    }
}
