using System.Collections.Generic;

namespace SquareTD
{
    // 전기 단일 — 오버로드 (맵 전체, × (1 + 그 적의 전하 × 0.01), 맞은 적의 전하를 터뜨려 0)
    public class ElectricTargetSkill : AttackSkill
    {
        public ElectricTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => 1f + ChargeEffect.Get(target) * 0.01f;

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            ChargeEffect.Clear(target);
        }
    }
}
