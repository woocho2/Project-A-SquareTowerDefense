using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 시너지 타워 14종의 재료 문양과, 덱으로 고르는 순서·판의 티어 합
    public static class SynergyRecipes
    {
        public readonly struct Recipe
        {
            public readonly int Id;
            public readonly string Name;
            public readonly TowerEmblem[] Emblems;

            public Recipe(int id, string name, TowerEmblem a, TowerEmblem b, TowerEmblem c)
            {
                Id = id;
                Name = name;
                Emblems = new[] { a, b, c };
            }
        }

        private static readonly Recipe[] s_recipes =
        {
            new Recipe(6001, "오딘", TowerEmblem.Spear, TowerEmblem.Electricity, TowerEmblem.Darkness),
            new Recipe(6002, "토르", TowerEmblem.Hammer, TowerEmblem.Electricity, TowerEmblem.Wind),
            new Recipe(6003, "로키", TowerEmblem.Bow, TowerEmblem.Fire, TowerEmblem.Darkness),
            new Recipe(6004, "헬", TowerEmblem.Axe, TowerEmblem.Ice, TowerEmblem.Darkness),
            new Recipe(6005, "수르트", TowerEmblem.Sword, TowerEmblem.Fire, TowerEmblem.Earth),
            new Recipe(6006, "프레이", TowerEmblem.Sword, TowerEmblem.Light, TowerEmblem.Wind),
            new Recipe(6007, "발드르", TowerEmblem.Water, TowerEmblem.Fire, TowerEmblem.Light),
            new Recipe(6008, "스카디", TowerEmblem.Bow, TowerEmblem.Ice, TowerEmblem.Water),
            new Recipe(6009, "프레이야", TowerEmblem.Spear, TowerEmblem.Light, TowerEmblem.Earth),
            new Recipe(6010, "이미르", TowerEmblem.Hammer, TowerEmblem.Ice, TowerEmblem.Earth),
            new Recipe(6011, "울르", TowerEmblem.Axe, TowerEmblem.Bow, TowerEmblem.Shield),
            new Recipe(6012, "브륀힐드", TowerEmblem.Sword, TowerEmblem.Shield, TowerEmblem.Electricity),
            new Recipe(6013, "브록 & 에이트리", TowerEmblem.Axe, TowerEmblem.Spear, TowerEmblem.Hammer),
            new Recipe(6014, "뇨르드", TowerEmblem.Water, TowerEmblem.Wind, TowerEmblem.Shield),
        };

        public static IReadOnlyList<Recipe> All => s_recipes;

        public static bool TryGet(int id, out Recipe recipe)
        {
            foreach (Recipe candidate in s_recipes)
            {
                if (candidate.Id != id) continue;
                recipe = candidate;
                return true;
            }
            recipe = default;
            return false;
        }

        public static int Overlap(Recipe recipe, ICollection<TowerEmblem> deck)
        {
            int count = 0;
            foreach (TowerEmblem emblem in recipe.Emblems) if (deck.Contains(emblem)) count++;
            return count;
        }

        public static List<Recipe> GetRelated(ICollection<TowerEmblem> deck)
        {
            var picked = new List<Recipe>();
            foreach (Recipe recipe in s_recipes) if (Overlap(recipe, deck) > 0) picked.Add(recipe);
            picked.Sort((a, b) =>
            {
                int byOverlap = Overlap(b, deck).CompareTo(Overlap(a, deck));
                return byOverlap != 0 ? byOverlap : a.Id.CompareTo(b.Id);
            });
            return picked;
        }

        public static List<Recipe> Pick(ICollection<TowerEmblem> deck, int slotCount)
        {
            var picked = GetRelated(deck);
            picked.RemoveAll(recipe => Overlap(recipe, deck) != recipe.Emblems.Length);
            if (picked.Count > slotCount) picked.RemoveRange(slotCount, picked.Count - slotCount);
            return picked;
        }

        public static readonly TowerColor[] Colors = { TowerColor.Red, TowerColor.Blue, TowerColor.Yellow, TowerColor.Purple };

        public static int MaxTierSum => 3 * Colors.Length * TowerId.MaxTier;

        public static int TierSum(Recipe recipe, Func<TowerColor, TowerEmblem, int> highestTier)
        {
            int sum = 0;
            foreach (TowerEmblem emblem in recipe.Emblems)
                foreach (TowerColor color in Colors)
                    sum += Mathf.Clamp(highestTier(color, emblem), 0, TowerId.MaxTier);
            return sum;
        }

        public static Func<TowerColor, TowerEmblem, int> HighestTiers(IEnumerable<TowerDefinition> towers)
        {
            var highest = new Dictionary<int, int>();
            foreach (TowerDefinition tower in towers)
            {
                if (tower == null || TowerId.IsSynergy(tower.Id)) continue;
                int key = TowerId.Make(1, tower.Color, tower.Emblem);
                if (!highest.TryGetValue(key, out int tier) || tower.Tier > tier) highest[key] = tower.Tier;
            }
            return (color, emblem) => highest.TryGetValue(TowerId.Make(1, color, emblem), out int tier) ? tier : 0;
        }
    }
}
