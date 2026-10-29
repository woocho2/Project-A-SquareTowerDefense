using System.Collections.Generic;

namespace SquareTD
{
    // 바람 버프 — 순풍 · 질풍
    public class WindBuffRule : BuffRule
    {
        public const string BurstSource = "순풍·질풍";

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            var modifiers = new List<StatModifier>();

            if (isSkill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, -1));
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
            TimedTowerBuff.Remove(receiver.State, BurstSource);
        }
    }
}
