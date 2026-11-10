using System.Collections.Generic;

namespace SquareTD
{
    // 전기 광역 — 라이트닝 볼텍스 (맵 전체, 맞은 적에게 전하 +(Tier × 레벨 × 최대 Charge). 하이퍼 펄스를 받는 동안 2배)
    public class ElectricSplashSkill : AttackSkill
    {
        public ElectricSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        public static float GetVortexCharge(TowerController owner) =>
            owner.Definition.Tier * owner.Stats.ColorLevel * owner.Stats.Stack * ElectricBuffRule.ChargeMultiplier(owner);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            if (target.IsAlive && !target.IsDummy) ChargeEffect.Add(target.Body, GetVortexCharge(owner));
        }
    }
}
