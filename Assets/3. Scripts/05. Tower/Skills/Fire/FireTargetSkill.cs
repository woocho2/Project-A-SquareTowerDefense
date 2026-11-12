using System.Collections.Generic;

namespace SquareTD
{
    // 불 단일 — 브레스 ((레벨 × Tier) 턴 동안 매 턴 체력이 가장 많은 적에게 Power × AbilityValue × (1 + 화상 × 0.01), 맞으면 화상 +1. 그동안 기본 공격 없음)
    public class FireTargetSkill : AttackSkill
    {
        public const string TurnsKey = "브레스 남은 턴";
        public const string DamageKey = "브레스 피해";

        public FireTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        public override bool NeedsLivingTarget => false;

        public override bool BlocksBasicAttack(TowerController owner) => owner.State.GetCounter(TurnsKey) > 0f;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            owner.State.Counters[TurnsKey] = EffectDuration(owner);
            owner.State.Counters[DamageKey] = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack));
            return true;
        }

        public override void OnEnemyTurnStart(TowerController owner)
        {
            if (owner.State.GetCounter(TurnsKey) <= 0f) return;
            owner.State.AddCounter(TurnsKey, -1f);

            ITarget target = HighestHp(SkillArea.WholeMap(m_grid, m_occupancy));
            if (target == null) return;

            SkillEffectTable.PlayBreath(owner, target);
            float damage = owner.State.GetCounter(DamageKey) * FireSplashSkill.BurnMultiplier(target);
            ProjectileHitResolver.HitSingle(target, MakeSkillHit(owner, owner.Stats, target, damage, false));
            if (target.IsAlive) BurnEffect.Add(target.Body, 1f);
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
