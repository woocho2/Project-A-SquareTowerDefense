using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 광역 — 휠윈드 (맵 전체의 적에게 Power × AbilityValue, 타워의 Range 안에 있는 적에게는 반드시 치명타)
    public class AxeSplashSkill : AttackSkill
    {
        public AxeSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.WholeMap(m_grid, m_occupancy);
        }

        protected override bool IsCriticalAgainst(TowerController owner, ITarget target)
        {
            int distance = BoardGrid.GetTileDistance(m_grid.GetSpawnCell(owner.SlotIndex), m_grid.GetPathCell(target.PathIndex));
            return distance <= owner.Stats.Range;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
