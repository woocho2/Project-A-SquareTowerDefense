using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 버프 — 냉기(스킬 1) · 한기(스킬 2) (20번 4-13장)
    //   존 위 타워의 기본 공격에 맞은 적은 (0.001 × Power × AbilityValue × 스택)% 확률로 이동력 감소 — 냉기는 최대 −1, 한기는 최소·최대 −1
    //   확률은 저항할 수 있음. 깎이는 양(−1)은 정수형이라 그대로. (버프 타워의 레벨 × 2)번 행동하는 동안, 겹치지 않음
    //   한기의 고유 효과: 한기 존의 타워는 빙결된 적에게 +25% (FreezeEffect가 봄)
    //   얼음 타워끼리: 얼음 광역·단일은 Mastery +1(냉기) / +2(한기)
    public class IceBuffRule : BuffRule
    {
        public const string SlowKey = "냉기·한기";

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (receiver.Definition.Emblem == TowerEmblem.Ice)
            {
                // [E] 얼음 타워는 Mastery +1(냉기) / +2(한기) — modifiers.Add로 Flat 하나 (StatType.Mastery, 값은 zone.Stage == ZoneStage.Skill2 ? 2f : 1f)
            }
            return modifiers;
        }

        public override void OnReceiverHit(Zone zone, TowerController receiver, ITarget target)
        {
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive) return;
            if (!Chance.Roll(enemy.Resistance.ResistWeak(SkillFormula.ZoneValue(zone)))) return;

            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            int turns = zone.Owner.Stats.ColorLevel * 2;
            if (isSkill2)
            {
                enemy.Status.Add(new TimedModifierEffect(SlowKey, turns, 2f, "최소·최대 이동력 −1",
                    StatModifier.Flat(SlowKey, StatType.SpeedMin, -1f),
                    StatModifier.Flat(SlowKey, StatType.SpeedMax, -1f)));
            }
            else
            {
                enemy.Status.Add(new TimedModifierEffect(SlowKey, turns, 1f, "최대 이동력 −1",
                    StatModifier.Flat(SlowKey, StatType.SpeedMax, -1f)));
            }
        }
    }
}
