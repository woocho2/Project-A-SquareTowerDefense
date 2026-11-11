using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 특수 타일을 판에 무작위로 배치
    [Serializable]
    public class SpecialTileCounts
    {
        [Header("길")]
        public bool Bifrost = true;
        public int Purify = 1;
    }

    public class SpecialTileLayout
    {
        private PathTileType[] m_pathTiles = Array.Empty<PathTileType>();

        public event Action LayoutGenerated;

        public PathTileType GetPathTile(int pathIndex) =>
            pathIndex >= 0 && pathIndex < m_pathTiles.Length ? m_pathTiles[pathIndex] : PathTileType.None;

        public void Generate(BoardGrid grid, SpecialTileCounts counts, System.Random random)
        {
            m_pathTiles = new PathTileType[grid.PathCount];

            var pathCells = new List<int>();
            for (int i = 0; i < grid.PathCount; i++) pathCells.Add(i);

            if (counts.Bifrost && grid.PathCount >= 2)
            {
                int first = random.Next(0, grid.PathCount);
                int partner = grid.GetBifrostPartner(first);

                m_pathTiles[first] = PathTileType.Bifrost;
                m_pathTiles[partner] = PathTileType.Bifrost;

                pathCells.Remove(first);
                pathCells.Remove(partner);
            }

            var pathTypes = new List<PathTileType>();
            AddRepeated(pathTypes, PathTileType.Purify, counts.Purify);

            Shuffle(pathCells, random);
            PlaceInOrder(pathCells, pathTypes, m_pathTiles);

            LayoutGenerated?.Invoke();
        }

        public static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);

                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void AddRepeated<T>(List<T> list, T value, int count)
        {
            for (int i = 0; i < count; i++) list.Add(value);
        }

        private static void PlaceInOrder<T>(List<int> cells, List<T> types, T[] board)
        {
            if (types.Count > cells.Count)
                Debug.LogWarning($"[SpecialTileLayout] 특수 타일 {types.Count}개가 빈 칸 {cells.Count}개보다 많아 {cells.Count}개만 깖");

            int placeCount = Mathf.Min(cells.Count, types.Count);
            for (int i = 0; i < placeCount; i++)
            {
                board[cells[i]] = types[i];
            }
        }
    }
}
