using System.Collections.Generic;

namespace SquareTD
{
    // 문양 없이 피해만 주는 시험 스킬
    public class DamageOnlySkill : AttackSkill
    {
        public DamageOnlySkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (owner.Definition.Color == TowerColor.Red)
                return SkillArea.WholeMap(m_grid, m_occupancy);

            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
