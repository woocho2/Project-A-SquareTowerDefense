using System.Collections.Generic;

namespace SquareTD
{
    // 전기 광역 — 라이트닝 볼텍스
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
            ChargeEffect.Add(target?.Body, BasicCharge);
        }
    }
}
