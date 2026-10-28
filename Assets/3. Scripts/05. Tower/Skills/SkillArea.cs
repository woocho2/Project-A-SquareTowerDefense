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

        // 연쇄 (전기 과부하, 수업 38): 처음 맞은 적 + 거기서 가까운 적부터 (count − 1)명. 거리가 같으면 tieBreak 값이 큰 적부터 (전하)
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
