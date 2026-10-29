using System.Collections.Generic;

namespace SquareTD
{
    // 불 광역 — 메테오
    public class FireSplashSkill : AttackSkill
    {
        public const float BurnBonus = 0.5f;
        public const float BlazeBonus = 1f;

        public FireSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            ZoneStage burn = EmblemDebuffEffect.StageOn(target, TowerEmblem.Fire);
            if (burn == ZoneStage.Skill2) return 1f + BlazeBonus;
            if (burn == ZoneStage.Skill1) return 1f + BurnBonus;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
