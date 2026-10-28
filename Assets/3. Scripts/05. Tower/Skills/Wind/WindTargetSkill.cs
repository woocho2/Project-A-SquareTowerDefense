using System.Collections.Generic;

namespace SquareTD
{
    // 바람 단일 — 폭풍 (20번 4-12장)
    //   피해를 주지 않고 자기 강화만. (Power × AbilityValue)% 확률로 터짐 (스택을 쓰면 확률 × (1 + Stack × 0.01 × StackValue))
    //   터지면 이 타워가 (레벨 × 2)번 행동하는 동안 매 턴 행동 카운트 +2, 공격 횟수 +1. 겹치지 않음 (또 터지면 처음부터)
    //   예: 브론즈는 20%, 스택을 쓰면 32%
    public class WindTargetSkill : AttackSkill
    {
        public const string Source = "폭풍";

        public WindTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;    // 자기 강화라 발동시킨 적이 죽었어도 굴림

        public static float GetChance(TowerController owner, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            float percent = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);     // 브론즈 10 × 2 = 20(%)
            if (useStack) percent = SkillFormula.WithStack(percent, owner.State.Stack.Max, stats.StackValue);
            return percent / 100f;
        }

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            if (!Chance.Roll(GetChance(owner, useStack))) return true;     // 안 터져도 쓴 것으로 침 (스택도 씀)

            TimedTowerBuff.Apply(owner.State, Source, owner.Stats.ColorLevel * 2,
                StatModifier.Flat(Source, StatType.ActionGainPerTurn, 2f),
                StatModifier.Flat(Source, StatType.AttackCount, 1f));
            return true;
        }

        // 피해가 없는 스킬 (CastInstead가 늘 true라 Fire가 여기까지 오지 않음)
        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
