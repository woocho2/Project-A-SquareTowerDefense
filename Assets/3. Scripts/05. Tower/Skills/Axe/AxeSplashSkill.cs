using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 광역 — 대회전 (20번 4-4장)
    //   도끼를 크게 한 바퀴: 타워 사거리 안의 모든 적에게 Power × AbilityValue, 반드시 치명 (치명 피해가 높은 도끼의 값을 그대로 살림)
    //   사거리의 끝 칸(타워와의 타일 거리 == Range)은 × 1.5 — 도끼날이 가장 빠르게 지나가는 자리
    public class AxeSplashSkill : AttackSkill
    {
        private const float EdgeMultiplier = 1.5f;

        public AxeSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.InRange(m_grid, m_occupancy, m_grid.GetSpawnCell(owner.SlotIndex), owner.Stats.Range);
        }

        protected override bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) => true;     // 반드시 치명

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
