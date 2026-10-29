using System.Collections.Generic;

namespace SquareTD
{
    // 대지 단일 — 씨뿌리기
    public class EarthTargetSkill : AttackSkill
    {
        public const string GrowthSource = "씨뿌리기 성장";
        public const string BuryKey = "파묻힘";

        public EarthTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            OnBasicHit(owner, target);
        }

        public override void OnBasicHit(TowerController owner, ITarget target) => Bury(owner, target);

        public static void Bury(TowerController owner, ITarget target)
        {
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive) return;

            ZoneStage mud = EmblemDebuffEffect.StageOn(target, TowerEmblem.Earth);
            if (mud == ZoneStage.None) return;

            int amount = mud == ZoneStage.Skill2 ? 2 : 1;
            enemy.Status.Add(new TimedModifierEffect(BuryKey, owner.Stats.ColorLevel * 2, amount, $"최소 이동력 −{amount}",
                StatModifier.Flat(BuryKey, StatType.SpeedMin, -amount)));
        }

        protected override void OnFired(TowerController owner, int hitCount)
        {
            Grow(owner, GrowthSource, StatType.Power, EarthSplashSkill.GrowthPerUse(owner));
        }
    }
}
