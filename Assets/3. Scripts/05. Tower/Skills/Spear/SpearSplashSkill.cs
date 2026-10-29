using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 창 광역 — 용맹한 깃창
    public class SpearSplashSkill : AttackSkill
    {
        public const string Source = "용맹한 깃창";

        private readonly TowerSlots m_slots;

        public SpearSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes, TowerSlots slots)
            : base(grid, occupancy, stackModes)
        {
            m_slots = slots;
        }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);
            TowerFinalStats stats = owner.Stats;

            float percent = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            if (useStack) percent = SkillFormula.WithStack(percent, owner.State.Stack.Max, stats.StackValue);

            foreach (TowerController ally in GetAlliesInRange(owner, stats.Range))
            {
                TimedTowerBuff.Apply(ally.State, Source, stats.ColorLevel * 2,
                    StatModifier.Percent(Source, StatType.Power, percent / 100f),
                    StatModifier.Flat(Source, StatType.CritRate, percent / 100f));
            }
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
