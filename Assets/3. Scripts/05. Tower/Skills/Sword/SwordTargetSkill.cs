using System.Collections.Generic;

namespace SquareTD
{
    public class SwordTargetSkill : AttackSkill
    {
        public const int SliceCount = 24;

        public SwordTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo slice = hit;
            slice.BaseDamage = hit.BaseDamage / SliceCount;

            for (int i = 0; i < SliceCount; i++)
            {
                ProjectileHitResolver.HitSingle(target, slice);
            }
        }
    }
}
