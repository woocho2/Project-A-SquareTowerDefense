using System.Collections.Generic;

namespace SquareTD
{
    // 방패 단일 — 아머 브레이크
    public class ShieldTargetSkill : AttackSkill
    {
        public const string Key = "아머 브레이크";
        public const float DefenseCut = 0.2f;

        public ShieldTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            if (target.Body != null) ApplyArmorBreak(owner, target.Body);
        }

        public static void ApplyArmorBreak(TowerController owner, EnemyController enemy)
        {
            if (!enemy.IsAlive) return;

            float cut = enemy.Resistance.ResistWeak(DefenseCut);
            enemy.Status.Add(new TimedModifierEffect(Key, owner.Stats.ColorLevel * 2, cut, $"방어력 −{cut * 100f:0.#}%",
            StatModifier.Percent(Key, StatType.Defense, -cut)));
        }
    }
}
