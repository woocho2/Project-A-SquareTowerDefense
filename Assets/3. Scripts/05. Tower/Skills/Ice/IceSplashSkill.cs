using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 광역 — 블리자드 (맵 전체, 빙결인 적 최종 피해 +150%, 한기인 적은 맞은 뒤 빙결)
    public class IceSplashSkill : AttackSkill
    {
        public const float FrozenBonus = 1.5f;

        public IceSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => FreezeEffect.IsFrozen(target) ? 1f + FrozenBonus : 1f;

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            bool chilled = ChillEffect.IsChilled(target);
            DamageInfo blizzard = hit;
            blizzard.ChangesMovement = chilled;
            ProjectileHitResolver.HitSingle(target, blizzard);
            if (chilled && target.IsAlive && target.Body != null) target.Body.Status.Add(new FreezeEffect());
        }
    }
}
