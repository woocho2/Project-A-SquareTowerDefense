using System.Collections.Generic;

namespace SquareTD
{
    // 방패 단일 — 아머 브레이크 (20번 4-7장)
    //   그 적에게 Power × AbilityValue (스택을 쓰면 ×), 그리고 방어력 −20% — 그 적이 (컬러 강화 레벨 × 2)번 행동하는 동안
    //   겹치지 않음 (다시 맞으면 유지만 처음부터). 방어력 감소는 저항력만큼 줄어듦: 20% × (1 − 저항력%)
    //   다른 방어력 감소(서프레스·락다운)와는 더함
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
            if (target.Body != null) ApplyArmorBreak(owner, target.Body);       // 가짜 대상(테스트)은 본체가 없음
        }

        // 방어력 −20% (저항만큼 덜), 레벨 × 2 행동. 같은 키라 다시 걸리면 유지만 처음부터
        public static void ApplyArmorBreak(TowerController owner, EnemyController enemy)
        {
            if (!enemy.IsAlive) return;                                         // 이 일격에 죽은 적

            float cut = enemy.Resistance.ResistWeak(DefenseCut);
            enemy.Status.Add(new TimedModifierEffect(Key, owner.Stats.ColorLevel * 2, cut, $"방어력 −{cut * 100f:0.#}%",
                StatModifier.Percent(Key, StatType.Defense, -cut)));
        }
    }
}
