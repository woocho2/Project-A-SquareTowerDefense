using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD
{
    public class ZoneRegistry
    {
        public const int MaxEmblemsPerCell = 3;

        private readonly BoardGrid m_grid;
        private readonly SpecialTileLayout m_layout;
        private readonly List<Zone> m_zones = new List<Zone>();

        public IReadOnlyList<Zone> All => m_zones;
        public event Action<Zone> ZoneChanged;

        public ZoneRegistry(BoardGrid grid, SpecialTileLayout layout)
        {
            m_grid = grid;
            m_layout = layout;
        }

        public void EnsureZones(TowerController owner, int count, bool isBuff)
        {
            int have = GetByOwner(owner).Count;
            for (int i = have; i < count; i++)
            {
                var zone = new Zone(owner, isBuff);
                m_zones.Add(zone);
                ZoneChanged?.Invoke(zone);
            }
        }

        public void EnsureZonesFor(TowerController tower)
        {
            TowerColor color = tower.Definition.Color;
            if (color != TowerColor.Yellow && color != TowerColor.Purple) return;
            EnsureZones(tower, tower.Stats.Mastery, color == TowerColor.Yellow);
        }

        public int RemoveByOwner(TowerController owner)
        {
            List<Zone> owned = GetByOwner(owner);
            foreach (Zone zone in owned)
            {
                m_zones.Remove(zone);
                ZoneChanged?.Invoke(zone);
            }
            return owned.Count;
        }

        public List<Zone> GetByOwner(TowerController owner) => m_zones.FindAll(zone => zone.Owner == owner);

        public List<Zone> GetAtSpawn(int spawnIndex) => m_zones.FindAll(zone => zone.IsBuff && zone.CellIndex == spawnIndex);
        public List<Zone> GetAtPath(int pathIndex) => m_zones.FindAll(zone => !zone.IsBuff && zone.CellIndex == pathIndex);

        public bool CanPlace(Zone zone, int cellIndex, out string reason)
        {
            reason = null;
            if (zone.IsPlaced) { reason = "이미 놓은 존 (옮길 수 없음)"; return false; }

            if (zone.IsBuff)
            {
                if (cellIndex < 0 || cellIndex >= m_grid.SpawnCount) { reason = "버프존은 타워 스폰 칸에만"; return false; }
            }
            else
            {
                if (cellIndex < 0 || cellIndex >= m_grid.PathCount) { reason = "디버프존은 길 칸에만"; return false; }
                if (m_layout != null && m_layout.GetPathTile(cellIndex) == PathTileType.Bifrost) { reason = "비프로스트 칸에는 놓을 수 없음"; return false; }
            }

            List<Zone> here = zone.IsBuff ? GetAtSpawn(cellIndex) : GetAtPath(cellIndex);
            var emblems = new HashSet<TowerEmblem>();
            foreach (Zone other in here)
            {
                if (other.Emblem == zone.Emblem) { reason = "같은 문양의 존이 이미 있음"; return false; }
                emblems.Add(other.Emblem);
            }
            if (emblems.Count >= MaxEmblemsPerCell) { reason = $"한 칸에 서로 다른 문양은 {MaxEmblemsPerCell}개까지"; return false; }

            return true;
        }

        public bool TryPlace(Zone zone, int cellIndex)
        {
            if (!CanPlace(zone, cellIndex, out _)) return false;
            zone.PlaceAt(cellIndex);
            ZoneChanged?.Invoke(zone);
            return true;
        }

        public bool HasPlaceableCell(Zone zone)
        {
            int cellCount = zone.IsBuff ? m_grid.SpawnCount : m_grid.PathCount;
            for (int i = 0; i < cellCount; i++)
            {
                if (CanPlace(zone, i, out _)) return true;
            }
            return false;
        }

        public bool HasUnplaced()
        {
            foreach (Zone zone in m_zones)
            {
                if (!zone.IsPlaced && HasPlaceableCell(zone)) return true;
            }
            return false;
        }

        public void NotifyChanged(Zone zone) => ZoneChanged?.Invoke(zone);
    }
}
