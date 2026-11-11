using System.Collections.Generic;

namespace SquareTD
{
    // 물 단일 — 아쿠아 프리즌 ((Tier × 레벨) 턴 동안 가둠(0 / 0), 매 턴 Power × AbilityValue × (1 + 젖음 × 0.01). 저항하면 갇히지 않고 피해만 저항만큼 줄어 들어감)
    public class WaterTargetSkill : AttackSkill
    {
        public WaterTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            EnemyController enemy = mainTarget?.Body;
            if (enemy == null || !enemy.IsAlive) return false;

            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * stats.FinalDamageMultiplier;
            bool locked = Chance.Roll(enemy.Resistance.ResistWeak(1f));
            if (!locked) damage = enemy.Resistance.ResistWeak(damage);
            enemy.Status.Add(new AquaPrisonEffect(owner, EffectDuration(owner), damage, locked));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }

    public class AquaPrisonEffect : StatusEffect
    {
        public const string PrisonKey = "아쿠아 프리즌";

        private readonly TowerController m_owner;
        private readonly float m_damage;
        private readonly bool m_locked;
        private int m_turnsLeft;

        public AquaPrisonEffect(TowerController owner, int turns, float damage, bool locked) : base(1)
        {
            m_owner = owner;
            m_turnsLeft = turns;
            m_damage = damage;
            m_locked = locked;
        }

        public override string Key => PrisonKey;
        public override bool IsPermanent => m_turnsLeft > 0;
        public override float Strength => m_damage;

        public override MoveRange OnDecideMove(EnemyController enemy, MoveRange range) => m_locked ? new MoveRange(0, 0, range.Action) : range;

        public override void OnTurn(EnemyController enemy)
        {
            if (m_turnsLeft <= 0) return;
            m_turnsLeft--;
            enemy.TakeDamage(new DamageInfo
            {
                Attacker = m_owner,
                Source = DamageSource.Skill,
                Emblem = TowerEmblem.Water,
                BaseDamage = m_damage * WaterSplashSkill.WetMultiplier(enemy),
            });
            if (m_turnsLeft <= 0 && enemy.IsAlive) enemy.Status.Remove(PrisonKey);
        }

        public override string Describe() => $"아쿠아 프리즌: {(m_locked ? "갇힘 (0 / 0), " : "")}매 턴 {m_damage:0.#} 피해, {m_turnsLeft}턴 남음";
    }
}
