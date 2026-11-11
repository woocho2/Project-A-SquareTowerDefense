using System.Collections.Generic;

namespace SquareTD
{
    // 바람 단일 — 템페스트 ((Power × AbilityValue (+ 가속))% 확률로 (레벨 × Tier)번 행동 동안 AttackCount +2, 가속 상태면 +4)
    public class WindTargetSkill : AttackSkill
    {
        public const string Source = "템페스트";
        public const int AttackCountBonus = 2;
        public const int AcceleratedBonus = 4;

        public WindTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        public override bool NeedsLivingTarget => false;

        public static float GetChance(TowerController owner, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float percent = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) + Acceleration.Get(owner);
            return percent * SkillFormula.PercentRate;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            if (!Chance.Roll(GetChance(owner, useStack))) return true;
            int bonus = Acceleration.IsAccelerated(owner) ? AcceleratedBonus : AttackCountBonus;
            TimedTowerBuff.Apply(owner.State, Source, EffectDuration(owner), StatModifier.Flat(Source, StatType.AttackCount, bonus));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
