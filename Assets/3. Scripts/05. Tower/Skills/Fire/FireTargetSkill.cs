using System.Collections.Generic;

namespace SquareTD
{
    // 불 단일 — 화염구
    public class FireTargetSkill : AttackSkill
    {
        public const float BurnChance = 0.6f;
        public const float BurnBonus = 0.1f;
        public const float BlazeBonus = 0.2f;

        public FireTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target) => BurnMultiplier(target);
        public override float ModifyBasicDamage(TowerController owner, ITarget target) => BurnMultiplier(target);

        private static float BurnMultiplier(ITarget target)
        {
            ZoneStage burn = EmblemDebuffEffect.StageOn(target, TowerEmblem.Fire);
            if (burn == ZoneStage.Skill2) return 1f + BlazeBonus;
            if (burn == ZoneStage.Skill1) return 1f + BurnBonus;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);

            EnemyController enemy = target.Body;
            if (enemy == null || !enemy.IsAlive) return;
            if (Chance.Roll(enemy.Resistance.ResistWeak(BurnChance))) enemy.Status.Add(FireDebuffEffect.Fireball(owner));
        }
    }
}
