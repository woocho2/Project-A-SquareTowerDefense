using System.Collections.Generic;

namespace SquareTD
{
    // 빛 광역 — 프리즘 (20번 4-15장)
    //   맵 전체의 적에게 Power × AbilityValue (스택을 쓰면 ×)를 주고 프리즘을 남김 (기본 2스택. 남아 있으면 더함)
    //   프리즘은 그 다음 기본 공격에 터짐 (PrismEffect). 더미에 남은 프리즘은 본체 최대 체력의 (10 × 스택)% (DummyTarget)
    //   빛 타워끼리: 광채 존에 서 있으면 3스택, 광휘 존에서는 4스택 (버프가 터졌을 때가 아니라 존에 서 있는 동안 항상)
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
