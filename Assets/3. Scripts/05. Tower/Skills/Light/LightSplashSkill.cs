using System.Collections.Generic;

namespace SquareTD
{
    // 빛 광역 — 프리즘 (피해 없이 아군 공격 타워 전체에게 (레벨 × Tier) 동안 공격력·치명타 확률·치명타 피해 +(Power × AbilityValue)%.
    //                   초월 웨이브인 동안은 걸려 있는 프리즘도 증가량 2배, AttackCount +1, Mastery +1. 끝나면 원래대로)
    public class LightSplashSkill : AttackSkill
    {
        public const string Source = "프리즘";
        public const string TranscendSource = "프리즘 초월";
        public const string RateKey = "프리즘 증가량";
        public const float TranscendMultiplier = 2f;

        public LightSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);
            TimedTowerBuff.Remove(owner.State, TranscendSource);
            TowerFinalStats stats = owner.Stats;
            float rate = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * SkillFormula.PercentRate;
            int duration = EffectDuration(owner);

            var receivers = new List<TowerController>();
            foreach (TowerController tower in AllyAttackTowers(owner))
            {
                TimedTowerBuff.Apply(tower.State, Source, duration, RateModifiers(Source, rate));
                tower.State.Counters[RateKey] = rate;
                RefreshTranscend(tower);
                receivers.Add(tower);
            }
            SkillEffectTable.PlayPrism(owner, receivers);
            return true;
        }

        public static void RefreshTranscend(TowerController tower)
        {
            TowerState state = tower.State;
            int left = TimedTowerBuff.GetRemaining(state, Source);
            if (left <= 0 || !EmblemSharedState.Current.IsTranscendWave)
            {
                TimedTowerBuff.Remove(state, TranscendSource);
                return;
            }

            var modifiers = new List<StatModifier>(RateModifiers(TranscendSource, state.GetCounter(RateKey) * (TranscendMultiplier - 1f)))
            {
                StatModifier.Flat(TranscendSource, StatType.AttackCount, 1f),
                StatModifier.Flat(TranscendSource, StatType.Mastery, 1f),
            };
            TimedTowerBuff.Apply(state, TranscendSource, left, modifiers.ToArray());
        }

        public static void RefreshTranscendAll()
        {
            if (BoardManager.Instance == null) return;
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && tower.IsAttackTower) RefreshTranscend(tower);
            }
        }

        private static StatModifier[] RateModifiers(string source, float rate) => new[]
        {
            StatModifier.Percent(source, StatType.Power, rate),
            StatModifier.Flat(source, StatType.CritRate, rate),
            StatModifier.Flat(source, StatType.CritDamage, rate),
        };

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
