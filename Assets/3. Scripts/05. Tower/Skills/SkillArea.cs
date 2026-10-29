using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 스킬 2의 범위 모양 (맵 전체, 착탄, 사거리 안, 하나)
    public static class SkillArea
    {
        public static List<ITarget> WholeMap(BoardGrid grid, PathOccupancy occupancy)
        {
            var targets = new List<ITarget>();

            for (int i = 0; i < grid.PathCount; i++)
            {
                targets.AddRange(occupancy.GetAt(i));
            }
            return targets;
        }

        public static List<ITarget> Impact(BoardGrid grid, PathOccupancy occupancy, int centerPathIndex, int mastery)
        {
            var targets = new List<ITarget>();
            int radius = SplashAction.GetAreaRadius(mastery);
            foreach (int index in grid.GetPathIndicesInSquare(grid.GetPathCell(centerPathIndex), radius))
            {
                targets.AddRange(occupancy.GetAt(index));
            }
            return targets;
        }

        public static List<ITarget> InRange(BoardGrid grid, PathOccupancy occupancy, Vector3Int towerCell, int range)
        {
            var targets = new List<ITarget>();
            foreach (int index in grid.GetPathIndicesInSquare(towerCell, range))
            {
                targets.AddRange(occupancy.GetAt(index));
            }
            return targets;
        }

        public static List<ITarget> Chain(BoardGrid grid, PathOccupancy occupancy, ITarget first, int count, System.Func<ITarget, float> tieBreak)
        {
            var chain = new List<ITarget>();
            if (first == null || count <= 0) return chain;
            chain.Add(first);

            Vector3Int origin = grid.GetPathCell(first.PathIndex);
            var others = new List<ITarget>();
            foreach (ITarget target in WholeMap(grid, occupancy))
            {
                if (target != first && target.IsAlive) others.Add(target);
            }

            others.Sort((a, b) =>
            {
                int byDistance = BoardGrid.GetTileDistance(origin, grid.GetPathCell(a.PathIndex))
                    .CompareTo(BoardGrid.GetTileDistance(origin, grid.GetPathCell(b.PathIndex)));
                return byDistance != 0 ? byDistance : tieBreak(b).CompareTo(tieBreak(a));
            });

            for (int i = 0; i < others.Count && chain.Count < count; i++) chain.Add(others[i]);
            return chain;
        }

        public static List<ITarget> Single(ITarget target)
        {
            var targets = new List<ITarget>();
            if (target != null) targets.Add(target);
            return targets;
        }
    }
}
