using System.Collections.Generic;

namespace SquareTD
{
    // 활 단일 — 헤비 볼트 (그 적의 최대 체력의 (Power × AbilityValue)% 피해, 저항력과 무관)
    public class ArrowTargetSkill : AttackSkill
    {
        public ArrowTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo bolt = hit;
            bolt.BaseDamage = target.MaxHp * hit.BaseDamage * SkillFormula.PercentRate;
            ProjectileHitResolver.HitSingle(target, bolt);
        }
    }
}
