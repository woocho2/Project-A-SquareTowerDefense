using System.Collections.Generic;

namespace SquareTD
{    
    public class SwordSplashSkill : AttackSkill
    {
        public SwordSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.WholeMap(m_grid, m_occupancy);            
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
