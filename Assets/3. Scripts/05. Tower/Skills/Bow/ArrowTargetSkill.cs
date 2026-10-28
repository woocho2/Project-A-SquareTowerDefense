using System.Collections.Generic;

namespace SquareTD
{
    // 활 단일 — 은화살 (20번 4-5장)
    //   발동시킨 그 적에게 최대 체력의 (Power × AbilityValue)% 피해. 스택을 쓰면 × (1 + Stack × 0.01 × StackValue)
    //   공통 식 그대로이고 기준만 최대 체력 → hit.BaseDamage(치명·최종 피해·거리까지 곱해진 값)를 "%"로 읽음
    //   디버프가 아니라 스킬의 직접 피해라 저항으로 줄지 않음. 보스에게도 그대로
    public class ArrowTargetSkill : AttackSkill
    {
        public ArrowTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {            
            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo silver = hit;
            silver.BaseDamage = target.MaxHp * hit.BaseDamage / 100f;     // 브론즈 10 × 2 = 20 → 최대 체력의 20%
            ProjectileHitResolver.HitSingle(target, silver);
        }
    }
}
