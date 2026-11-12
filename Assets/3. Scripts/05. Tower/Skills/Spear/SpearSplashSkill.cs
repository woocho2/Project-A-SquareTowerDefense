using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 창 광역 — 랠리 배너 (Range 안의 아군 공격 타워(자기 포함)에게 (레벨 × Tier)번 행동 동안 공격력·치명타 확률 +(Power × AbilityValue)%)
    public class SpearSplashSkill : AttackSkill
    {
        public const string Source = "랠리 배너";

        private readonly TowerSlots m_slots;

        public SpearSplashSkill(BoardGrid grid, PathOccupancy occupancy, TowerSlots slots)
            : base(grid, occupancy)
        {
            m_slots = slots;
        }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);
            TowerFinalStats stats = owner.Stats;

            float rate = SkillFormula.AttackSkill(stats.Power, ChargedAbility(stats, useStack)) * SkillFormula.PercentRate;
            int duration = EffectDuration(owner);

            List<TowerController> receivers = GetAlliesInRange(owner, stats.Range);
            foreach (TowerController ally in receivers)
            {
                TimedTowerBuff.Apply(ally.State, Source, duration,
                    StatModifier.Percent(Source, StatType.Power, rate),
                    StatModifier.Flat(Source, StatType.CritRate, rate));
            }
            SkillEffectTable.PlayRally(owner, receivers);
            return true;
        }

        private List<TowerController> GetAlliesInRange(TowerController owner, int range)
        {
            var allies = new List<TowerController>();
            if (m_slots != null)
            {
                Vector3Int center = m_grid.GetSpawnCell(owner.SlotIndex);
                for (int i = 0; i < m_slots.Count; i++)
                {
                    TowerController tower = m_slots.Get(i);
                    if (tower == null || !tower.IsAttackTower) continue;
                    if (BoardGrid.GetTileDistance(center, m_grid.GetSpawnCell(i)) <= range) allies.Add(tower);
                }
            }
            if (!allies.Contains(owner)) allies.Add(owner);
            return allies;
        }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
