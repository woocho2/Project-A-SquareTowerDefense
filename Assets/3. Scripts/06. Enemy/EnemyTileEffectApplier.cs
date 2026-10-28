using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 적 턴 ③ "멈춘 칸의 효과" (21번 4-1). 부르는 곳은 TurnManager.TileEffectsPhase 하나뿐
    // (B12: 옛 코드는 두 곳에서 불러 회복이 두 번 들어감)
    public class EnemyTileEffectApplier
    {
        private readonly BoardGrid m_grid;
        private readonly SpecialTileLayout m_layout;
        private readonly ZoneRegistry m_zones;              // 디버프존 (수업 26). 없으면 밟기를 건너뜀

        public EnemyTileEffectApplier(BoardGrid grid, SpecialTileLayout layout, ZoneRegistry zones = null)
        {
            m_grid = grid;
            m_layout = layout;
            m_zones = zones;
        }

        public void ApplyAll(IEnumerable<EnemyController> enemies)
        {
            var snapshot = new List<EnemyController>(enemies);      // 도는 중에 목록이 바뀌어도 안전하게
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

            float rate = enemy.Stats.Get(StatType.MapBuffRate);        // 침수면 맵 버프를 절반만 (수업 34)
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
