using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 얼음 단일 — 스노우볼
    public class IceTargetSkill : AttackSkill
    {
        public const float PerSpeed = 0.25f;

        public IceTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            if (target.Stats == null) return 1f;
            return 1f + ReducedSpeed(target.Stats) * PerSpeed;
        }

        public static int ReducedSpeed(EnemyStats stats)
        {
            MoveRange now = stats.GetMoveRange();
            int baseMin = Mathf.RoundToInt(stats.GetBase(StatType.SpeedMin));
            int baseMax = Mathf.RoundToInt(stats.GetBase(StatType.SpeedMax));
            return Mathf.Max(0, baseMin - now.Min) + Mathf.Max(0, baseMax - now.Max);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
