using System.Collections.Generic;

namespace SquareTD
{
    // 빛 광역 — 프리즘 (피해 없이 아군 공격 타워 전체에게 (레벨 × Tier) 동안 공격력·치명타 확률·치명타 피해 +(Power × AbilityValue)%.
    //                   초월 웨이브에는 증가량 2배, AttackCount +1, Mastery +1)
    public class LightSplashSkill : AttackSkill
    {
        public const string Source = "프리즘";
        public const float TranscendMultiplier = 2f;

        public LightSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TowerFinalStats stats = owner.Stats;
            bool transcend = EmblemSharedState.Current.IsTranscendWave;
            float rate = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * SkillFormula.PercentRate;
            if (transcend) rate *= TranscendMultiplier;

            var modifiers = new List<StatModifier>
            {
                StatModifier.Percent(Source, StatType.Power, rate),
                StatModifier.Flat(Source, StatType.CritRate, rate),
                StatModifier.Flat(Source, StatType.CritDamage, rate),
            };
            if (transcend)
            {
                modifiers.Add(StatModifier.Flat(Source, StatType.AttackCount, 1f));
                modifiers.Add(StatModifier.Flat(Source, StatType.Mastery, 1f));
            }

            int duration = EffectDuration(owner);
            foreach (TowerController tower in AllyAttackTowers(owner))
                TimedTowerBuff.Apply(tower.State, Source, duration, modifiers.ToArray());
            return true;
        }

        private static IEnumerable<TowerController> AllyAttackTowers(TowerController owner)
        {
            if (BoardManager.Instance == null)
            {
                yield return owner;
                yield break;
            }
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && tower.IsAttackTower) yield return tower;
            }
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
