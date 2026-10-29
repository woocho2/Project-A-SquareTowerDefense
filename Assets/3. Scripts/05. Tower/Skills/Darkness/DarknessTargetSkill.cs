using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 단일 — 심연
    public class DarknessTargetSkill : AttackSkill
    {
        public DarknessTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        public static float GetAbyssMultiplier(TowerController owner, float count)
        {
            float rate = 1f;
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Darkness);
            if (stage == ZoneStage.Skill2) rate = 2f;
            else if (stage == ZoneStage.Skill1) rate = 1.5f;
            return 1f + count * rate * 0.01f;
        }

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            return GetAbyssMultiplier(owner, EmblemSharedState.Current.DarknessCount);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
