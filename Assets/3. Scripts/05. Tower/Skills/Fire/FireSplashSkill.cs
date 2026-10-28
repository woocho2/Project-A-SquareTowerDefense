using System.Collections.Generic;

namespace SquareTD
{
    // 불 광역 — 메테오 (20번 4-9장)
    //   맵 전체의 적에게 Power × AbilityValue (스택을 쓰면 ×). 같은 적은 한 번만
    //   화상인 적에게 +50%, 연소된 적에게 +100% (불 디버프의 단계로 봄. 화염구가 건 화상도 화상)
    //   불씨·불길 존에 서 있으면 스택 하나의 계수 +20% (FireBuffRule이 줌) → 스택을 쓴 메테오가 더 셈
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
            // [B] 피해 주기 — SwordSplashSkill.OnHitTarget과 똑같은 한 줄
        }
    }
}
