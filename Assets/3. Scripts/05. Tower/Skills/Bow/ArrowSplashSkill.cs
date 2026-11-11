using System.Collections.Generic;

namespace SquareTD
{
    // 활 광역 — 애로우 레인 ((레벨 × Tier) 턴 동안 ⑤마다 맵 전체에 Power × AbilityValue × 0.2. 여러 개가 따로 깔림)
    public class ArrowSplashSkill : AttackSkill
    {
        private readonly GroundEffectRegistry m_grounds;

        public ArrowSplashSkill(BoardGrid grid, PathOccupancy occupancy, GroundEffectRegistry grounds)
            : base(grid, occupancy)
        {
            m_grounds = grounds;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * ArrowRainGround.TurnRatio * stats.FinalDamageMultiplier;

            m_grounds?.Add(new ArrowRainGround(owner, m_grid, m_occupancy, damage, EffectDuration(owner)));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
