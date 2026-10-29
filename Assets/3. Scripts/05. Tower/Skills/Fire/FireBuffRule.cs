using System.Collections.Generic;

namespace SquareTD
{
    // 불 버프 — 불씨 · 불길
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
                modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, 0.3f));
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, 1f));
            }

            if (receiver.Definition.Emblem == TowerEmblem.Fire && receiver.IsTarget)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, 1f));
            }

            if (receiver.Definition.Emblem == TowerEmblem.Fire && receiver.IsSplash)
            {
                modifiers.Add(StatModifier.Percent(source, StatType.StackValue, 0.2f));
            }
            return modifiers;
        }

        public override bool OnZoneTurn(Zone zone)
        {
            if (zone.Stage != ZoneStage.Skill2) return false;

            if (zone.StageTurnsLeft == 0)
            {
                zone.StartStageTimer(zone.Owner.Stats.ColorLevel * BlazeTurnsPerLevel);
                return false;
            }

            if (!zone.TickStageTimer()) return false;
            zone.ResetToSkill1();
            return true;
        }
    }
}
