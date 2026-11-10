using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 단일 — 아이시클 레인 (한기인 적에게 반드시 치명타, 빙결인 적에게는 반드시 치명타 + 치명타 피해 +300%)
    public class IceTargetSkill : AttackSkill
    {
        public const float FrozenCritDamage = 3f;

        public IceTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) =>
            ChillEffect.IsChilled(mainTarget) || FreezeEffect.IsFrozen(mainTarget);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            if (!FreezeEffect.IsFrozen(target)) return 1f;
            float crit = CriticalRoll.Multiplier(owner.Stats.CriticalDamage);
            return (crit + FrozenCritDamage) / crit;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) => ProjectileHitResolver.HitSingle(target, hit);
    }
}
