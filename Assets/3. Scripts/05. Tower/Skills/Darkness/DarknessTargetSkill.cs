using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 단일 — 심연 (Power × AbilityValue × (1 + 침식 카운트 × 0.01))
    public class DarknessTargetSkill : AttackSkill
    {
        public DarknessTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        public static float GetAbyssMultiplier(float count) => 1f + count * 0.01f;

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => GetAbyssMultiplier(EmblemSharedState.Current.DarknessCount);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) => ProjectileHitResolver.HitSingle(target, hit);
    }
}
