using System.Collections.Generic;

namespace SquareTD
{
    // 바람 단일 — 폭풍
    public class WindTargetSkill : AttackSkill
    {
        public const string Source = "폭풍";

        public WindTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;

        public static float GetChance(TowerController owner, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float percent = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            if (useStack) percent = SkillFormula.WithStack(percent, owner.State.Stack.Max, stats.StackValue);
            return percent / 100f;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            if (!Chance.Roll(GetChance(owner, useStack))) return true;

            TimedTowerBuff.Apply(owner.State, Source, owner.Stats.ColorLevel * 2,
                StatModifier.Flat(Source, StatType.ActionGainPerTurn, 2f),
                StatModifier.Flat(Source, StatType.AttackCount, 1f));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
