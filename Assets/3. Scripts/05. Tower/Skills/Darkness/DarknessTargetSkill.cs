using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 단일 — 심연 (20번 4-16장)
    //   그 적에게 Power × AbilityValue × (1 + 카운트 × 0.01) (스택을 쓰면 ×). 카운트 100이면 2배, 1000이면 11배
    //   카운트를 쓰기만 하고 줄이지 않음
    //   어둠 타워끼리: 포식 존에 서 있으면 카운트를 1.5배로, 폭식 존에서는 2배로 셈 (1000이면 16배 / 21배)
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
            // [B] 피해 주기 — SwordSplashSkill.OnHitTarget과 똑같은 한 줄
        }
    }
}
