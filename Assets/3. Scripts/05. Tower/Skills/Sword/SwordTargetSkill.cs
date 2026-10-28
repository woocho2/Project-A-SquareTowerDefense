using System.Collections.Generic;

namespace SquareTD
{
    public class SwordTargetSkill : AttackSkill
    {
        public const int SliceCount = 24;
        public const float LastSliceMultiplier = 2.4f;      // 마지막 24번째 일격은 앞의 한 번의 2.4배 (2026-10-28 사용자)

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

            for (int i = 0; i < SliceCount - 1; i++)
            {
                ProjectileHitResolver.HitSingle(target, slice);
            }

            DamageInfo last = slice;
            last.BaseDamage = slice.BaseDamage * LastSliceMultiplier;
            ProjectileHitResolver.HitSingle(target, last);
        }
    }
}
