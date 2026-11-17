using System;
using System.Collections.Generic;
using System.Linq;

namespace SquareTD
{
    // 자동 합성의 묶음 찾기
    public static class TowerProgression
    {
        public const int MergeCount = 3;

        public static bool CanMergeFrom(TowerDefinition definition)
        {
            return definition.Tier >= 1 && definition.Tier < TowerId.MaxTier && !TowerId.IsSynergy(definition.Id);
        }

        public static bool TryFindMergeGroup(TowerSlots slots, Func<TowerController, bool> isBusy,
                                             out TowerController keep, out List<TowerController> materials, int mergeCount = MergeCount)
        {
            var towers = new List<TowerController>();
            for (int i = 0; i < slots.Count; i++) towers.Add(slots.Get(i));
            return TryFindMergeGroup(towers, isBusy, out keep, out materials, mergeCount);
        }

        public static bool TryFindMergeGroup(IEnumerable<TowerController> towers, Func<TowerController, bool> isBusy,
                                             out TowerController keep, out List<TowerController> materials, int mergeCount = MergeCount)
        {
            if (mergeCount < 2) throw new ArgumentOutOfRangeException(nameof(mergeCount));
            keep = null;
            materials = null;

            var groups = new Dictionary<int, List<TowerController>>();
            var order = new List<int>();
            foreach (TowerController tower in towers)
            {
                if (tower == null || !CanMergeFrom(tower.Definition) || tower.State.BossEffects.BlocksProgression) continue;
                if (isBusy != null && isBusy(tower)) continue;

                int id = tower.Definition.Id;
                if (!groups.TryGetValue(id, out List<TowerController> group))
                {
                    groups[id] = group = new List<TowerController>();
                    order.Add(id);
                }
                group.Add(tower);
            }

            foreach (int id in order)
            {
                List<TowerController> group = groups[id];
                if (group.Count < mergeCount) continue;

                List<TowerController> candidates = group.OrderBy(t => t.BuildOrder).Take(mergeCount).ToList();
                List<TowerController> pool = candidates.Where(t => t.IsOnBoard).ToList();
                if (pool.Count == 0) pool = candidates;
                List<TowerController> older = pool.Where(t => candidates.IndexOf(t) < mergeCount - 1).ToList();
                if (older.Count > 0) pool = older;
                keep = pool.OrderBy(t => t.SlotIndex).First();
                TowerController kept = keep;
                materials = candidates.Where(t => t != kept).ToList();
                return true;
            }
            return false;
        }
    }
}
