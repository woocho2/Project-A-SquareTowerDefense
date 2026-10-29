using System.Collections.Generic;

namespace SquareTD
{
    // 해머 단일 — 망치질
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
