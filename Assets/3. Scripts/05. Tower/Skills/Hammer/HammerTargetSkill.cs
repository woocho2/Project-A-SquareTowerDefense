using System.Collections.Generic;

namespace SquareTD
{
    // 해머 단일 — 아머 크러시 (8번에 나눠 Power × AbilityValue, 마지막 공격에 맞은 적은 1번 행동하는 동안 방어력 0)
    public class HammerTargetSkill : AttackSkill
    {
        public const int Strikes = 8;

        public HammerTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo strike = hit;
            strike.BaseDamage = hit.BaseDamage / Strikes;

            for (int i = 0; i < Strikes && target.IsAlive; i++)
            {
                ProjectileHitResolver.HitSingle(target, strike);
            }

            EnemyController enemy = target.Body;
            if (enemy == null || !enemy.IsAlive) return;
            enemy.Status.Add(new ArmorCrushEffect(enemy.Resistance.ResistWeak(1f)));
        }
    }

    public class ArmorCrushEffect : StatusEffect
    {
        public const string CrushKey = "아머 크러시";

        private readonly float m_rate;

        public ArmorCrushEffect(float rate) : base(1)
        {
            m_rate = rate;
        }

        public override string Key => CrushKey;
        public override float Strength => m_rate;

        public override float ModifyDefense(float defense) => defense > 0f ? defense * (1f - m_rate) : defense;

        public override string Describe() => m_rate >= 1f
            ? "아머 크러시: 방어력 0 (1번 행동 동안)"
            : $"아머 크러시: 방어력 −{m_rate * 100f:0.#}% (저항, 1번 행동 동안)";
    }
}
