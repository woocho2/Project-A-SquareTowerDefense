using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 타워 행동의 베이스 (사거리 안 대상 모으기, 기본 공격 피해 만들기)
    public abstract class TowerAction
    {
        protected readonly BoardGrid m_grid;
        protected readonly PathOccupancy m_occupancy;
        protected readonly IProjectileLauncher m_launcher;
        public virtual void OnMoved(TowerController tower) { }
        public virtual void OnRemoved(TowerController tower) { }

        protected TowerAction(BoardGrid grid, PathOccupancy occupancy, IProjectileLauncher launcher)
        {
            m_grid = grid;
            m_occupancy = occupancy;
            m_launcher = launcher;
        }

        public abstract bool CanAct(TowerController tower, TowerFinalStats stats);
        public abstract IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle);

        protected List<ITarget> CollectTargetsInRange(TowerController tower, int range)
        {
            return CollectTargetsInSquare(m_grid.GetSpawnCell(tower.SlotIndex), range);
        }

        protected List<ITarget> CollectTargetsInSquare(Vector3Int center, int radius)
        {
            var targets = new List<ITarget>();
            foreach (int index in m_grid.GetPathIndicesInSquare(center, radius))
            {
                targets.AddRange(m_occupancy.GetAt(index));
            }
            return targets;
        }

        protected DamageInfo MakeBasicHit(TowerController tower, TowerFinalStats stats, ITarget target, bool isCritical)
        {
            float critMultiplier = isCritical ? CriticalRoll.Multiplier(stats.CriticalDamage) : 1f;
            Vector3Int from = m_grid.GetSpawnCell(tower.SlotIndex);
            Vector3Int to = m_grid.GetPathCell(target.PathIndex);
            int distance = BoardGrid.GetTileDistance(from, to);
            float skillMultiplier = tower.Skill != null ? tower.Skill.ModifyBasicDamage(tower, target) : 1f;

            return new DamageInfo
            {
                Attacker = tower,
                Source = DamageSource.Basic,
                Emblem = tower.Definition.Emblem,
                Attribute = tower.Definition.Attribute,
                BaseDamage = DamageCalculator.GetOutgoing(stats.Power, stats.GetDistanceMultiplier(distance), critMultiplier) * stats.FinalDamageMultiplier * skillMultiplier,
                IsCritical = isCritical,
                CritMultiplier = critMultiplier,
                ArmorPenetration = stats.ArmorPenetration,
                Distance = distance,
                ForceFixed = stats.ForcesFixedDamage,
            };
        }
    }
}
