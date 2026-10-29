using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 단일 — 광폭화
    public class AxeTargetSkill : AttackSkill
    {
        public const string Source = "광폭화";

        public AxeTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);
            TowerFinalStats stats = owner.Stats;

            float percent = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            if (useStack) percent = SkillFormula.WithStack(percent, owner.State.Stack.Max, stats.StackValue);

            TimedTowerBuff.Apply(owner.State, Source, stats.ColorLevel * 2,
                StatModifier.Flat(Source, StatType.CritRate, 1f),
                StatModifier.Percent(Source, StatType.Power, percent / 100f),
                StatModifier.Flat(Source, StatType.ActionGainPerTurn, 1f));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
