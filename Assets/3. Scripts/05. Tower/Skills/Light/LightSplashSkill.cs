using System.Collections.Generic;

namespace SquareTD
{
    // 빛 광역 — 프리즘
    public class LightSplashSkill : AttackSkill
    {
        public const int BaseStacks = 2;

        public LightSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        public static int GetPrismStacks(TowerController owner)
        {
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Light);
            if (stage == ZoneStage.Skill2) return BaseStacks + 2;
            if (stage == ZoneStage.Skill1) return BaseStacks + 1;
            return BaseStacks;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            if (!target.IsAlive) return;

            int stacks = GetPrismStacks(owner);
            if (target is DummyTarget dummy) dummy.AddPrism(stacks);
            else PrismEffect.Apply(target.Body, stacks);
        }
    }
}
