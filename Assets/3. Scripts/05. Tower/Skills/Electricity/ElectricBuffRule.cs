using System.Collections.Generic;

namespace SquareTD
{
    // 전기 버프 — 충전 · 과충전 · 방전
    public class ElectricBuffRule : BuffRule
    {
        public const int OverchargeTurnsPerLevel = 5;
        public const int DischargeTurns = 5;
        public const int SplashCharge = 2;
        public const int TargetCharge = 5;

        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>();
            if (zone.Stage == ZoneStage.Discharge) return modifiers;

            float value = SkillFormula.ZoneValue(zone);
            modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, value));
            return modifiers;
        }

        public override void OnReceiverHit(Zone zone, TowerController receiver, ITarget target)
        {
            if (zone.Stage != ZoneStage.Skill2) return;
            ChargeEffect.Add(target?.Body, receiver.IsSplash ? SplashCharge : TargetCharge);
        }

        public override bool OnZoneTurn(Zone zone)
        {
            if (zone.Stage == ZoneStage.Skill2)
            {
                if (zone.StageTurnsLeft == 0)
                {
                    zone.StartStageTimer(zone.Owner.Stats.ColorLevel * OverchargeTurnsPerLevel);
                    return false;
                }
                if (!zone.TickStageTimer()) return false;
                zone.StartDischarge(DischargeTurns);
                return true;
            }

            if (zone.Stage == ZoneStage.Discharge)
            {
                if (!zone.TickStageTimer()) return false;
                zone.ResetToSkill1();
                return true;
            }
            return false;
        }
    }
}
