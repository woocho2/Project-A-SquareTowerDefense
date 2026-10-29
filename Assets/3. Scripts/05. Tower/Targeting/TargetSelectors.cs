using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 타겟 우선순위별로 공격할 적을 고르는 방법들
    public abstract class TargetSelector
    {
        private static readonly TargetSelector s_closest = new ClosestSelector();
        private static readonly TargetSelector s_farthest = new FarthestSelector();
        private static readonly TargetSelector s_mostHp = new MostHpSelector();
        private static readonly TargetSelector s_leastHp = new LeastHpSelector();
        private static readonly TargetSelector s_mostCrowded = new MostCrowdedSelector();
        private static readonly TargetSelector s_boss = new BossSelector();

        public static TargetSelector For(TargetPriority priority)
        {
            switch (priority)
            {
                case TargetPriority.Farthest: return s_farthest;
                case TargetPriority.MostHP: return s_mostHp;
                case TargetPriority.LeastHP: return s_leastHp;
                case TargetPriority.MostCrowded: return s_mostCrowded;
                case TargetPriority.Boss: return s_boss;
                default: return s_closest;
            }
        }

        public abstract ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid);

        public static IReadOnlyList<ITarget> PreferAttackCell(IReadOnlyList<ITarget> candidates, int attackCellIndex)
        {
            if (attackCellIndex < 0) return candidates;

            var onCell = new List<ITarget>();
            foreach (ITarget target in candidates)
            {
                if (target.PathIndex == attackCellIndex) onCell.Add(target);
            }
            return onCell.Count > 0 ? onCell : candidates;
        }

        protected static int DistanceTo(ITarget target, Vector3Int towerCell, BoardGrid grid)
        {
            return BoardGrid.GetTileDistance(towerCell, grid.GetPathCell(target.PathIndex));
        }
    }

    public class ClosestSelector : TargetSelector
    {
        public override ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid)
        {
            ITarget best = null;
            int bestDistance = int.MaxValue;
            foreach (ITarget target in candidates)
            {
                int distance = DistanceTo(target, towerCell, grid);
                if (distance < bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }

    public class FarthestSelector : TargetSelector
    {
        public override ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid)
        {
            ITarget best = null;
            int bestDistance = int.MinValue;
            foreach (ITarget target in candidates)
            {
                int distance = DistanceTo(target, towerCell, grid);
                if (distance > bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }

    public class MostHpSelector : TargetSelector
    {
        public override ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid)
        {
            ITarget best = null;
            float bestHp = float.MinValue;
            foreach (ITarget target in candidates)
            {
                float hp = target.CurrentHp;
                if (hp > bestHp)
                {
                    best = target;
                    bestHp = target.CurrentHp;
                }
            }
            return best;
        }
    }

    public class LeastHpSelector : TargetSelector
    {
        public override ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid)
        {
            ITarget best = null;
            float bestHp = float.MaxValue;
            foreach (ITarget target in candidates)
            {
                float hp = target.CurrentHp;
                if (hp < bestHp)
                {
                    best = target;
                    bestHp = target.CurrentHp;
                }
            }
            return best;
        }
    }

    public class MostCrowdedSelector : TargetSelector
    {
        public override ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid)
        {
            var counts = new Dictionary<int, int>();
            foreach (ITarget target in candidates)
            {
                counts.TryGetValue(target.PathIndex, out int count);
                counts[target.PathIndex] = count + 1;
            }

            ITarget best = null;
            int bestCount = 0;
            foreach (ITarget target in candidates)
            {
                int count = counts[target.PathIndex];
                if (count > bestCount)
                {
                    best = target;
                    bestCount = count;
                }
            }
            return best;
        }
    }

    public class BossSelector : TargetSelector
    {
        public override ITarget Select(IReadOnlyList<ITarget> candidates, Vector3Int towerCell, BoardGrid grid)
        {
            ITarget best = null;
            float bestHp = float.MinValue;
            foreach (ITarget target in candidates)
            {
                if (!target.IsBoss) continue;
                if (target.CurrentHp > bestHp)
                {
                    best = target;
                    bestHp = target.CurrentHp;
                }
            }

            if (best != null) return best;
            return For(TargetPriority.MostHP).Select(candidates, towerCell, grid);
        }
    }
}
