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

        public EnemyTileEffectApplier(BoardGrid grid, SpecialTileLayout layout, ZoneRegistry zones = null)
        {
            m_grid = grid;
            m_layout = layout;
            m_zones = zones;
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

            if (m_zones != null && enemy.Movement.ActedThisTurn)
            {
                foreach (Zone zone in m_zones.GetAtPath(enemy.PathIndex))
                    DebuffZoneAction.Step(zone, enemy, m_zones);
            }

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

        private static StatModifier Scale(StatModifier modifier, float rate) => modifier.IsPercent
            ? StatModifier.Percent(modifier.Source, modifier.Stat, modifier.Value * rate)
            : StatModifier.Flat(modifier.Source, modifier.Stat, modifier.Value * rate);
    }
}
