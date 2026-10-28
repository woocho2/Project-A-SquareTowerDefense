using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 광역 — 블랙홀 (20번 4-16장)
    //   발동하면 우선순위로 정한 적의 칸 위에 블랙홀을 깖 (BlackHoleGround). 그 자리에서는 피해가 없음 (화살비처럼 ⑤마다)
    //   매 턴 피해 = Power × AbilityValue (스택을 쓰면 ×) — 소환한 때의 값으로 고정 (중간에 카운트·스탯이 바뀌어도 그대로)
    //   어둠 타워끼리: 포식 존에 서 있으면 카운트 1당 +0.04%, 폭식 존에서는 +0.08% (카운트 1000이면 +40% / +80%)
    //   남아 있는 동안 다시 발동하면 여러 개가 따로 겹쳐 깔림
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

        // 피해는 장판이 줌 (CastInstead가 true라 Fire가 여기까지 오지 않음)
        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
