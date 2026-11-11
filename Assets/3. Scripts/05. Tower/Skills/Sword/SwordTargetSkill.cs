using System.Collections.Generic;

namespace SquareTD
{
    // 검 단일 — 블레이드 스톰 (검기 10번에 나눠 Power × AbilityValue, 마지막 폭발은 그 적이 잃은 체력의 (Power × AbilityValue)%)
    public class SwordTargetSkill : AttackSkill
    {
        public const int StormHits = 10;

        public SwordTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo storm = hit;
            storm.BaseDamage = hit.BaseDamage / StormHits;

            for (int i = 0; i < StormHits && target.IsAlive; i++)
            {
                ProjectileHitResolver.HitSingle(target, storm);
            }
            if (!target.IsAlive) return;

            DamageInfo burst = hit;
            burst.BaseDamage = (target.MaxHp - target.CurrentHp) * hit.BaseDamage * SkillFormula.PercentRate;
            ProjectileHitResolver.HitSingle(target, burst);
        }
    }
}
