using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 광역 — 대회전
    public class AxeSplashSkill : AttackSkill
    {
        private const float EdgeMultiplier = 1.5f;

        public AxeSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.InRange(m_grid, m_occupancy, m_grid.GetSpawnCell(owner.SlotIndex), owner.Stats.Range);
        }

        protected override bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) => true;

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            int distance = BoardGrid.GetTileDistance(m_grid.GetSpawnCell(owner.SlotIndex), m_grid.GetPathCell(target.PathIndex));

            if (distance == owner.Stats.Range) return EdgeMultiplier;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
