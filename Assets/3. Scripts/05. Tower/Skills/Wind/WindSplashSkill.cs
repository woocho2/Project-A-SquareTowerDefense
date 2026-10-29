using System.Collections.Generic;

namespace SquareTD
{
    // 바람 광역 — 칼바람
    public class WindSplashSkill : AttackSkill
    {
        private int m_areaCount = 1;

        public WindSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

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
            return WindMultiplier(target) / m_areaCount;
        }

        private static float WindMultiplier(ITarget target)
        {
            ZoneStage wind = EmblemDebuffEffect.StageOn(target, TowerEmblem.Wind);
            if (wind == ZoneStage.Skill2) return 3f;
            if (wind == ZoneStage.Skill1) return 2f;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }

        public override void OnBasicHit(TowerController owner, ITarget target)
        {
            ZoneStage wind = EmblemDebuffEffect.StageOn(target, TowerEmblem.Wind);
            if (wind == ZoneStage.None) return;
            owner.AdvanceSkillCount(wind == ZoneStage.Skill2 ? 2 : 1, target);
        }
    }
}
