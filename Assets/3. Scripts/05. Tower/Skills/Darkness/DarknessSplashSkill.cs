using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 광역 — 블랙홀 (맞은 적의 타일에 (레벨 × Tier) 턴, 매 턴 Power × AbilityValue × 0.3)
    public class DarknessSplashSkill : AttackSkill
    {
        public const float TurnRatio = 0.3f;

        private readonly GroundEffectRegistry m_grounds;

        public DarknessSplashSkill(BoardGrid grid, PathOccupancy occupancy, GroundEffectRegistry grounds)
            : base(grid, occupancy)
        {
            m_grounds = grounds;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            if (mainTarget == null) return false;
            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * TurnRatio * stats.FinalDamageMultiplier;
            m_grounds?.Add(new BlackHoleGround(owner, m_grid, m_occupancy, mainTarget.PathIndex, stats.Mastery, damage, EffectDuration(owner)));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
