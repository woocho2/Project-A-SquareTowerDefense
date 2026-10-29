using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 길 칸·스폰 칸의 좌표와 거리 계산
    public class BoardGrid
    {
        private readonly List<Vector3Int> m_pathCells = new List<Vector3Int>();
        private readonly Dictionary<Vector3Int, int> m_pathIndexByCell = new Dictionary<Vector3Int, int>();
        private readonly List<Vector3Int> m_spawnCells = new List<Vector3Int>();
        private readonly Dictionary<Vector3Int, int> m_spawnIndexByCell = new Dictionary<Vector3Int, int>();

        public BoardGrid(IReadOnlyList<Vector3Int> pathCells, IReadOnlyList<Vector3Int> spawnCells)
        {
            for (int i = 0; i < pathCells.Count; i++)
            {
                Vector3Int cell = pathCells[i];
                m_pathCells.Add(cell);

                if (!m_pathIndexByCell.ContainsKey(cell))
                {
                    m_pathIndexByCell.Add(cell, i);
                }
            }

            for (int j = 0; j < spawnCells.Count; j++)
            {
                Vector3Int cell = spawnCells[j];
                m_spawnCells.Add(cell);

                if (!m_spawnIndexByCell.ContainsKey(cell))
                {
                    m_spawnIndexByCell.Add(cell, j);
                }
            }
        }

        public int PathCount => m_pathCells.Count;
        public int SpawnCount => m_spawnCells.Count;

        public int WrapPathIndex(int index)
        {
            return ((index % PathCount) + PathCount) % PathCount;
        }

        public Vector3Int GetPathCell(int index)
        {
            return m_pathCells[WrapPathIndex(index)];
        }

        public Vector3Int GetSpawnCell(int index)
        {
            return m_spawnCells[index];
        }

        public bool TryGetPathIndex(Vector3Int cell, out int index)
        {
            return m_pathIndexByCell.TryGetValue(cell, out index);
        }

        public bool TryGetSpawnIndex(Vector3Int cell, out int index)
        {
            return m_spawnIndexByCell.TryGetValue(cell, out index);
        }

        public static int GetTileDistance(Vector3Int a, Vector3Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy);
        }

        public List<int> GetPathIndicesInSquare(Vector3Int center, int radius)
        {
            var result = new List<int>();

            for (int i = 0; i < PathCount; i++)
            {
                if (GetTileDistance(m_pathCells[i], center) <= radius)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public int GetBifrostPartner(int index)
        {
            return WrapPathIndex(index + (PathCount/2));
        }
    }
}
