using System.Collections.Generic;

namespace SquareTD
{
    // 활 광역 — 화살비 (20번 4-5장, 2026-10-28 기획 변경)
    //   발동할 때는 피해 없이 하늘로 화살을 쏘아 올림 (연출만). 화살은 ⑤마다 맵 전체에 떨어짐 (ArrowRainGround)
    //   매 턴 Power × AbilityValue × 0.4, (컬러 강화 레벨 × 2)턴. 스택을 쓰면 × (1 + Stack × 0.01 × StackValue)
    //   남아 있는 동안 다시 발동하면 여러 개가 따로 겹쳐 깔림
    public class ArrowSplashSkill : AttackSkill
    {
        private readonly GroundEffectRegistry m_grounds;

        public ArrowSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes, GroundEffectRegistry grounds)
            : base(grid, occupancy, stackModes)
        {
            m_grounds = grounds;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue) * ArrowRainGround.TurnRatio;
            if (useStack) damage = SkillFormula.WithStack(damage, owner.State.Stack.Max, stats.StackValue);
            damage *= stats.FinalDamageMultiplier;          // 쏜 때의 최종 피해 버프(글로리 등)까지 담아 둠

            m_grounds?.Add(new ArrowRainGround(owner, m_grid, m_occupancy, damage, stats.ColorLevel * 2));
            return true;
        }

        // 피해는 장판이 줌. 발동 순간에는 맞는 적이 없음 (CastInstead가 true라 Fire가 여기까지 오지 않음)
        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
