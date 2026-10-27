using System.Collections.Generic;

namespace SquareTD
{
    // 스킬 2의 범위 모양 (23번 #54). 문양 스킬은 GetArea에서 이 중 하나를 고름
    //   지금 있는 것: 맵 전체, 착탄(Mastery 범위), 대상 하나
    //   수업 30(창): 가로·세로줄 — 대상이 선 줄 중 적이 더 많은 쪽, 같으면 가로
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

        public static List<ITarget> Single(ITarget target)
        {
            var targets = new List<ITarget>();
            if (target != null) targets.Add(target);
            return targets;
        }
    }
}
