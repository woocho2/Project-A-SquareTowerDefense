using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 단일 — 광폭화 (20번 4-4장)
    //   피해를 주지 않고 자기에게 시간 버프: 이 타워의 행동 (컬러 강화 레벨 × 2)번 동안
    //   치명 확률 100%, 공격력 +(Power × AbilityValue)% (스택을 쓰면 × (1 + Stack × 0.01 × StackValue)), 매 턴 행동 카운트 +1 (행동 간격 절반)
    //   치명 피해는 오르지 않음. 유지 중에 다시 발동하면 겹치지 않고 처음부터
    public class AxeTargetSkill : AttackSkill
    {
        public const string Source = "광폭화";

        public AxeTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;     // 자기 버프라 발동시킨 적이 죽었어도 씀

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);     // 먼저 떼야 지난 광폭화로 오른 공격력이 이번 식에 끼지 않음
            TowerFinalStats stats = owner.Stats;

            float percent = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);     // 브론즈 10 × 2 = 20(%), 다이아 150 × 2 = 300(%)
            if (useStack) percent = SkillFormula.WithStack(percent, owner.State.Stack.Max, stats.StackValue);

            TimedTowerBuff.Apply(owner.State, Source, stats.ColorLevel * 2,
                StatModifier.Flat(Source, StatType.CritRate, 1f),                   // 치명 확률은 0 ~ 1 (1 = 100%)
                StatModifier.Percent(Source, StatType.Power, percent / 100f),       // Percent는 0.2 = +20%
                StatModifier.Flat(Source, StatType.ActionGainPerTurn, 1f));
            return true;
        }

        // 피해를 주지 않는 스킬이라 범위·맞았을 때는 비어 있음 (CastInstead가 true라 Fire가 여기까지 오지 않음)
        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
