using System.Collections.Generic;

namespace SquareTD
{
    // 불 광역 — 메테오 (맵 전체, × (1 + 화상 스택 × 0.01))
    public class FireSplashSkill : AttackSkill
    {
        public FireSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        public static float BurnMultiplier(ITarget target) => 1f + BurnEffect.Get(target) * 0.01f;

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => BurnMultiplier(target);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) => ProjectileHitResolver.HitSingle(target, hit);
    }
}
