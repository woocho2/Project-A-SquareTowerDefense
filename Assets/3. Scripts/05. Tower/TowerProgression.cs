using System;
using System.Collections.Generic;
using System.Linq;

namespace SquareTD
{
    // 자동 합성의 묶음 찾기와 티어 강화 비용
    public static class TowerProgression
    {
        public const int MergeCount = 3;
        private static readonly int[] s_tierUpgradeCosts = { 0, 5, 15, 45, 135 };

        public static int GetTierUpgradeCost(int tier) => tier >= 1 && tier <= 4 ? s_tierUpgradeCosts[tier] : 0;

        public static bool CanMergeFrom(TowerDefinition definition)
        {
            return definition.Tier >= 1 && definition.Tier <= 4 && !TowerId.IsSynergy(definition.Id);
        }

        public static bool TryFindMergeGroup(TowerSlots slots, Func<TowerController, bool> isBusy,
                                             out TowerController keep, out List<TowerController> materials, int mergeCount = MergeCount)
        {
            if (mergeCount < 2) throw new ArgumentOutOfRangeException(nameof(mergeCount));
            keep = null;
            materials = null;

            var groups = new Dictionary<int, List<TowerController>>();
            var order = new List<int>();
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null || !CanMergeFrom(tower.Definition)) continue;
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
                keep = candidates.Take(mergeCount - 1).OrderBy(t => t.SlotIndex).First();
                TowerController kept = keep;
                materials = candidates.Where(t => t != kept).ToList();
                return true;
            }
            return false;
        }
    }
}
