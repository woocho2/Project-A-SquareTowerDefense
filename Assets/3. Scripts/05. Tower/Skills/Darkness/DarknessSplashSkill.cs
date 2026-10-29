using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 광역 — 블랙홀
    public class DarknessSplashSkill : AttackSkill
    {
        public const float FeastPerCount = 0.0004f;
        public const float GluttonyPerCount = 0.0008f;

        private readonly GroundEffectRegistry m_grounds;

        public DarknessSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes, GroundEffectRegistry grounds)
            : base(grid, occupancy, stackModes)
        {
            m_grounds = grounds;
        }

        public static float GetCountMultiplier(TowerController owner, float count)
        {
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Darkness);
            if (stage == ZoneStage.Skill2) return 1f + count * GluttonyPerCount;
            if (stage == ZoneStage.Skill1) return 1f + count * FeastPerCount;
            return 1f;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            if (mainTarget == null) return false;
            TowerFinalStats stats = owner.Stats;

            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            if (useStack) damage = SkillFormula.WithStack(damage, owner.State.Stack.Max, stats.StackValue);
            damage *= stats.FinalDamageMultiplier * GetCountMultiplier(owner, EmblemSharedState.Current.DarknessCount);

            m_grounds?.Add(new BlackHoleGround(owner, m_grid, m_occupancy, mainTarget.PathIndex, stats.Mastery, damage, stats.ColorLevel * 2));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
