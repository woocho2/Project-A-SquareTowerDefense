using System.Collections.Generic;

namespace SquareTD
{
    // 해머 단일 — 망치질 (20번 4-8장)
    //   그 적에게 Power × AbilityValue (스택을 쓰면 ×). 50% 확률로 이동력 1 / 1 고정 (SpeedLockEffect), 그 적이 (레벨 × 2)번 행동하는 동안
    //   50%는 저항할 수 있음: 50% × (1 − 저항력%). 100T 해머의 −1과 같이 걸리면 1 / 1이 우선
    //   확률이 실제로 걸린 타격만 "이동력을 바꾼 공격" (젖음·침수가 전부 받음)
    public class HammerTargetSkill : AttackSkill
    {
        public const float LockChance = 0.5f;

        public HammerTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool locks = enemy != null && enemy.IsAlive && Chance.Roll(enemy.Resistance.ResistWeak(LockChance));

            DamageInfo strike = hit;
            strike.ChangesMovement = locks;
            ProjectileHitResolver.HitSingle(target, strike);

            if (locks && enemy.IsAlive) enemy.Status.Add(new SpeedLockEffect(owner.Stats.ColorLevel * 2));
        }
    }
}
