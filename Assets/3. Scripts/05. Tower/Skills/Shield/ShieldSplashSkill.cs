using System.Collections.Generic;

namespace SquareTD
{
    // 방패 광역 — 배쉬
    public class ShieldSplashSkill : AttackSkill
    {
        public const float StunChance = 0.2f;

        public ShieldSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            return SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo bash = hit;
            bash.StunChance = StunChance;
            ProjectileHitResolver.HitSingle(target, bash);
        }
    }
}
