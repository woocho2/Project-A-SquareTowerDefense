using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 적 턴 ③: 멈춘 칸의 효과를 적용
    public class EnemyTileEffectApplier
    {
        private readonly BoardGrid m_grid;
        private readonly SpecialTileLayout m_layout;
        private readonly ZoneRegistry m_zones;

        public static EnemyTileEffectApplier Current { get; private set; }

        public EnemyTileEffectApplier(BoardGrid grid, SpecialTileLayout layout, ZoneRegistry zones = null)
        {
            m_grid = grid;
            m_layout = layout;
            m_zones = zones;
            Current = this;
        }

        public static void ApplyForcedStop(EnemyController enemy)
        {
            if (Current == null || enemy == null || !enemy.IsAlive) return;
            Current.StepZones(enemy);
            if (!enemy.IsAlive) return;

            PathTileEffect effect = PathTileEffects.Get(Current.m_layout.GetPathTile(enemy.PathIndex));
            effect?.OnStopped(enemy, Current.m_grid, Current.m_layout);
        }

        public void ApplyAll(IEnumerable<EnemyController> enemies)
        {
            var snapshot = new List<EnemyController>(enemies);
            foreach (EnemyController enemy in snapshot)
            {
                if (enemy.IsAlive) Apply(enemy);
            }
        }

        public void Apply(EnemyController enemy)
        {
            enemy.Stats.Modifiers.RemoveBySource(PathTileEffect.Source);

            if (m_zones != null && enemy.Movement.ActedThisTurn && enemy.Movement.ActionStartIndex != enemy.PathIndex)
                StepZones(enemy);

            PathTileType tile = m_layout.GetPathTile(enemy.PathIndex);
            PathTileEffect effect = PathTileEffects.Get(tile);
            if (effect == null) return;

            float rate = enemy.Stats.Get(StatType.MapBuffRate);
            foreach (StatModifier modifier in effect.GetStandingModifiers())
            {
                enemy.Stats.Modifiers.Add(Mathf.Approximately(rate, 1f) ? modifier : Scale(modifier, rate));
            }

            if (enemy.Movement.ActedThisTurn) effect.OnStopped(enemy, m_grid, m_layout);

            effect.OnTurnStanding(enemy);
        }

        public void StepZones(EnemyController enemy)
        {
            if (m_zones == null) return;
            foreach (Zone zone in m_zones.GetAtPath(enemy.PathIndex))
            {
                if (!enemy.IsAlive) return;
                DebuffZoneAction.Step(zone, enemy);
            }
        }

        private static StatModifier Scale(StatModifier modifier, float rate) => modifier.IsPercent
            ? StatModifier.Percent(modifier.Source, modifier.Stat, modifier.Value * rate)
            : StatModifier.Flat(modifier.Source, modifier.Stat, modifier.Value * rate);
    }
}
