using System.Collections.Generic;

namespace SquareTD
{
    // 도끼 단일 — 블러드 레이지 (자기에게 (레벨 × Tier)번 행동 동안 치명타 확률 100%, 공격력 +(Power × AbilityValue)%, 매 턴 행동 카운트 +1)
    public class AxeTargetSkill : AttackSkill
    {
        public const string Source = "블러드 레이지";

        public AxeTargetSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);
            TowerFinalStats stats = owner.Stats;

            float percent = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack));

            TimedTowerBuff.Apply(owner.State, Source, EffectDuration(owner),
                StatModifier.Flat(Source, StatType.CritRate, 1f),
                StatModifier.Percent(Source, StatType.Power, percent * SkillFormula.PercentRate),
                StatModifier.Flat(Source, StatType.ActionGainPerTurn, 1f));
            return true;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
