using System.Collections.Generic;

namespace SquareTD
{
    // 활 단일 — 은화살
    public class ArrowTargetSkill : AttackSkill
    {
        public ArrowTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo silver = hit;
            silver.BaseDamage = target.MaxHp * hit.BaseDamage / 100f;
            ProjectileHitResolver.HitSingle(target, silver);
        }
    }
}
