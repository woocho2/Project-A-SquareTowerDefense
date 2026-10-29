using System.Collections.Generic;

namespace SquareTD
{
    // 빛 버프 — 광채 · 광휘
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
                StatModifier.Flat(BurstSource, StatType.Mastery, 1f),
                StatModifier.Flat(BurstSource, StatType.CritRate, 0.05f),
                StatModifier.Flat(BurstSource, StatType.CritDamage, 0.1f),
                StatModifier.Flat(BurstSource, StatType.MaxAction, -1f),
            };
            if (zone.Stage == ZoneStage.Skill2) burst.Add(StatModifier.Percent(BurstSource, StatType.AbilityValue, 0.2f));

            TimedTowerBuff.Apply(receiver.State, BurstSource, zone.Owner.Stats.ColorLevel * 2, burst.ToArray());
        }

        public override void OnTaken(Zone zone, TowerController receiver)
        {
            TimedTowerBuff.Remove(receiver.State, BurstSource);
        }
    }
}
