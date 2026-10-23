using System.Collections.Generic;
using Unity.VisualScripting;

namespace SquareTD
{
    public enum MergeMode
    {
        Exact = 0,
        Color = 1,
        Emblem = 2
    }

    public static class TowerProgression
    {
        public const int MergeCount = 3;
        private static readonly int[] s_tierUpgradeCosts = { 0, 5, 15, 45, 135 };

        public static int GetTierUpgradeCost(int tier) => tier >= 1 && tier <= 4 ? s_tierUpgradeCosts[tier] : 0;

        public static bool CanMergeFrom(TowerDefinition definition)
        {
            return definition.Tier >= 1 && definition.Tier <= 4 && !TowerId.IsSynergy(definition.Id);
        }

        public static bool Matches(TowerDefinition a, TowerDefinition b, MergeMode mode)
        {
            if (a.Tier != b.Tier) return false;

            switch (mode)
            {
                case MergeMode.Exact: return a.Color == b.Color && a.Emblem == b.Emblem;
                case MergeMode.Color: return a.Color == b.Color;
                default: return a.Emblem == b.Emblem;
            }
        }

        public static bool TryFindMaterials(TowerController target, MergeMode mode, TowerSlots slots, List<TowerController> materials)
        {
            materials.Clear();
            if (!CanMergeFrom(target.Definition)) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null || tower == target) continue;
                if (!Matches(target.Definition, tower.Definition, mode)) continue;

                materials.Add(tower);
                if (materials.Count == MergeCount - 1) return true;
            }
            return false;
        }

        public static List<TowerDefinition> GetResultCandidates(TowerDefinition target, MergeMode mode, TowerDatabase database, IReadOnlyList<TowerEmblem> selectedEmblems)
        {
            int nextTier = target.Tier + 1;
            var results = new List<TowerDefinition>();

            switch (mode)
            {
                case MergeMode.Exact:
                    AddIfExists(database, TowerId.Make(nextTier, target.Color, target.Emblem), results);
                    break;

                case MergeMode.Color:
                    foreach (TowerEmblem emblem in selectedEmblems)
                    {
                        AddIfExists(database, TowerId.Make(nextTier, target.Color, emblem), results);
                    }
                    break;

                case MergeMode.Emblem:
                    for (TowerColor color = TowerColor.Red; color <= TowerColor.Purple; color++)
                    {
                        AddIfExists(database, TowerId.Make(nextTier, color, target.Emblem), results);
                    }
                    break;
            }
            return results;
        }

        private static void AddIfExists(TowerDatabase database, int id, List<TowerDefinition> results)
        {
            if (database.TryGet(id, out TowerDefinition definition)) results.Add(definition);
        }
    }
}
