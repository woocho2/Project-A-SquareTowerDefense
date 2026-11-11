using System.Collections.Generic;

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
            if (!enemy.Movement.ActedThisTurn) return;

            if (m_zones != null && enemy.Movement.ActionStartIndex != enemy.PathIndex) StepZones(enemy);
            if (!enemy.IsAlive) return;

            PathTileEffects.Get(m_layout.GetPathTile(enemy.PathIndex))?.OnStopped(enemy, m_grid, m_layout);
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
    }
}
