using System.Collections.Generic;

namespace SquareTD
{
    // 방패 광역 — 콘커션 (착탄 범위의 적에게 Power × AbilityValue, 맞은 적은 1행동 기절. 강한 CC라 저항력으로만 막음)
    public class ShieldSplashSkill : AttackSkill
    {
        public const float StunChance = 1f;

        public ShieldSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            return SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo concussion = hit;
            concussion.StunChance = StunChance;
            ProjectileHitResolver.HitSingle(target, concussion);
        }
    }
}
