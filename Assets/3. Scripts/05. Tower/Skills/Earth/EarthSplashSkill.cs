using System.Collections.Generic;

namespace SquareTD
{
    // 대지 광역 — 어스퀘이크 (맵 전체, 맞은 적은 다음 행동 한 번 행동 불가, 쓸 때마다 AbilityValue +(성장 스택)%)
    public class EarthSplashSkill : AttackSkill
    {
        public const string GrowthSource = "어스퀘이크 성장";

        public EarthSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            if (target.IsAlive && target.Body != null && !target.IsDummy) target.Body.Status.Add(new ImmobilizeEffect("어스퀘이크"));
        }

        protected override void OnFired(TowerController owner, int hitCount)
        {
            float growth = Growth.Get(owner);
            if (growth > 0f) Grow(owner, GrowthSource, StatType.AbilityValue, growth * SkillFormula.PercentRate);
        }
    }
}
