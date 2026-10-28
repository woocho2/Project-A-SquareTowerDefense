using System.Collections.Generic;

namespace SquareTD
{
    // 전기 광역 — 라이트닝 볼텍스 (20번 4-14장)
    //   맵 전체의 적에게 Power × AbilityValue (스택을 쓰면 ×), 맞은 모든 적에게 전하 (10 × 컬러 강화 레벨)
    //   전기 광역의 기본 공격도 맞은 적에게 전하 1
    //   전기 타워끼리: 충전 존에 서 있으면 볼텍스의 전하 2배, 과충전 존에서는 4배 (레벨 5에 과충전이면 200)
    public class ElectricSplashSkill : AttackSkill
    {
        public const int ChargePerLevel = 10;
        public const int BasicCharge = 1;

        public ElectricSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        public static float GetVortexCharge(TowerController owner)
        {
            float charge = ChargePerLevel * owner.Stats.ColorLevel;
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Electricity);
            if (stage == ZoneStage.Skill2) return charge * 4f;
            if (stage == ZoneStage.Skill1) return charge * 2f;
            return charge;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            ChargeEffect.Add(target.Body, GetVortexCharge(owner));
        }

        public override void OnBasicHit(TowerController owner, ITarget target)
        {
            // [B] 기본 공격은 전하 1 — 바로 위 OnHitTarget의 ChargeEffect.Add 줄과 같은 모양 (적은 target?.Body, 양은 BasicCharge)
        }
    }
}
