using System.Collections.Generic;

namespace SquareTD
{
    // 검 단일 — 블레이드 스톰
    public class SwordTargetSkill : AttackSkill
    {
        public const int StormHits = 10;

        public SwordTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo storm = hit;
            storm.BaseDamage = hit.BaseDamage / StormHits;

            for (int i = 0; i < StormHits; i++)
            {
                ProjectileHitResolver.HitSingle(target, storm);
            }

            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
