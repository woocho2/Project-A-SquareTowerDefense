using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 스킬의 범위 모양 (맵 전체, 착탄, 사거리 안, 하나)
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

        public static List<ITarget> Impact(BoardGrid grid, PathOccupancy occupancy, int centerPathIndex, int mastery) =>
            InRange(grid, occupancy, grid.GetPathCell(centerPathIndex), SplashAction.GetAreaRadius(mastery));

        public static List<ITarget> InRange(BoardGrid grid, PathOccupancy occupancy, Vector3Int center, int range)
        {
            var targets = new List<ITarget>();
            IReadOnlyList<int> indices = grid.GetPathIndicesInSquare(center, range);
            for (int i = 0; i < indices.Count; i++) targets.AddRange(occupancy.GetAt(indices[i]));
            return targets;
        }

        public static List<ITarget> Single(ITarget target)
        {
            var targets = new List<ITarget>();
            if (target != null) targets.Add(target);
            return targets;
        }
    }
}
