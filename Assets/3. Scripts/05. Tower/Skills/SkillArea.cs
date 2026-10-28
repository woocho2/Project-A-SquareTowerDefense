using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 스킬 2의 범위 모양 (21번 #54). 문양 스킬은 GetArea에서 이 중 하나를 고름
    //   지금 있는 것: 맵 전체, 착탄(Mastery 범위), 사거리 안(수업 28 대회전), 대상 하나
    //   수업 38(전기): 연쇄 — 첫 대상에서 가까운 순으로 n명 (전하 높은 순은 그때)
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

        // 타워 칸에서 타일 거리 range 이내 (기본 공격의 사거리와 같은 모양)
        public static List<ITarget> InRange(BoardGrid grid, PathOccupancy occupancy, Vector3Int towerCell, int range)
        {
            var targets = new List<ITarget>();
            foreach (int index in grid.GetPathIndicesInSquare(towerCell, range))
            {
                targets.AddRange(occupancy.GetAt(index));
            }
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
