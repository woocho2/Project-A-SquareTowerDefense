using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 버프 — 냉기 · 한기
    public class IceBuffRule : BuffRule
    {
        public const string SlowKey = "냉기·한기";

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (receiver.Definition.Emblem == TowerEmblem.Ice)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.Mastery, zone.Stage == ZoneStage.Skill2 ? 2f : 1f));
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
