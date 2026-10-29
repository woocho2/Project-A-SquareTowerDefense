using System.Collections.Generic;

namespace SquareTD
{
    // 활 광역 — 화살비
    public class ArrowSplashSkill : AttackSkill
    {
        private readonly GroundEffectRegistry m_grounds;

        public ArrowSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes, GroundEffectRegistry grounds)
            : base(grid, occupancy, stackModes)
        {
            m_grounds = grounds;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue) * ArrowRainGround.TurnRatio;
            if (useStack) damage = SkillFormula.WithStack(damage, owner.State.Stack.Max, stats.StackValue);
            damage *= stats.FinalDamageMultiplier;

            m_grounds?.Add(new ArrowRainGround(owner, m_grid, m_occupancy, damage, stats.ColorLevel * 2));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
