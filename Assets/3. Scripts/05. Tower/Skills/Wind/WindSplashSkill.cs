using System.Collections.Generic;

namespace SquareTD
{
    // 바람 광역 — 윈드 슬래쉬 (착탄 범위, 반드시 치명타, 총 피해를 적 수로 나눔, 가속 상태면 치명타 배율 + 가속 × 0.01)
    public class WindSplashSkill : AttackSkill
    {
        public const float AccelerationRate = 0.01f;

        private int m_areaCount = 1;

        public WindSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            List<ITarget> area = SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
            m_areaCount = 0;
            foreach (ITarget target in area)
            {
                if (target.IsAlive) m_areaCount++;
            }
            if (m_areaCount < 1) m_areaCount = 1;
            return area;
        }

        protected override bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) => true;

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            float multiplier = 1f / m_areaCount;
            float acceleration = Acceleration.Get(owner);
            if (acceleration <= 0f) return multiplier;

            float crit = CriticalRoll.Multiplier(owner.Stats.CriticalDamage);
            return multiplier * (crit + acceleration * AccelerationRate) / crit;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) => ProjectileHitResolver.HitSingle(target, hit);
    }
}
