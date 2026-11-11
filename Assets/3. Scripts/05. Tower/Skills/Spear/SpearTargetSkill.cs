using System.Collections.Generic;

namespace SquareTD
{
    // 창 단일 — 헤드헌트 (창 (Tier × 레벨)개를 Range 안에서 체력이 가장 높은 적부터 꽂음, 하나에 Power × AbilityValue × 0.2. 그 적이 죽으면 다음으로 높은 적에게)
    public class SpearTargetSkill : AttackSkill
    {
        public const float SpearRatio = 0.2f;

        public SpearTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        public override bool NeedsLivingTarget => false;

        public static int SpearCount(TowerController owner) => SkillFormula.EffectDuration(owner);

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            ITarget target = HighestInRange(owner);
            if (target == null) return false;

            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * SpearRatio;
            int spears = SpearCount(owner);

            for (int i = 0; i < spears; i++)
            {
                if (!target.IsAlive) target = HighestInRange(owner);
                if (target == null) break;
                ProjectileHitResolver.HitSingle(target, MakeSkillHit(owner, stats, target, damage, false));
            }
            return true;
        }

        private ITarget HighestInRange(TowerController owner) =>
            HighestHp(SkillArea.InRange(m_grid, m_occupancy, m_grid.GetSpawnCell(owner.SlotIndex), owner.Stats.Range));

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
