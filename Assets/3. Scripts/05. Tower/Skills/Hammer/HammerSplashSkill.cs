using System.Collections.Generic;

namespace SquareTD
{
    // 해머 광역 — 100T 해머
    public class HammerSplashSkill : AttackSkill
    {
        public const string Key = "100T 해머";

        public HammerSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            return SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo smash = hit;
            smash.ChangesMovement = true;
            ProjectileHitResolver.HitSingle(target, smash);
            if (target.Body != null) ApplySlow(owner, target.Body);
        }

        public static void ApplySlow(TowerController owner, EnemyController enemy)
        {
            if (!enemy.IsAlive) return;
            enemy.Status.Add(new TimedModifierEffect(Key, owner.Stats.ColorLevel * 2, 1f, "최대 이동력 −1",
                StatModifier.Flat(Key, StatType.SpeedMax, -1f)));
        }
    }
}
