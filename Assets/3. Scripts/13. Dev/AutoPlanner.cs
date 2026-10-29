using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 자동 진행의 판단 — 생성, 젬 쓰기(티어·컬러 강화), 칸이 꽉 차면 쓸모없는 1티어를 팔고 다시 뽑기, 버프존·디버프존 자리와 타워 자리 고르기
    public static class AutoPlanner
    {
        private const float SupportWeight = 0.7f;
        private const float ColorLevelValue = 0.25f;
        private const float BreakPairPenalty = 0.5f;
        private static readonly TowerColor[] s_colors = { TowerColor.Red, TowerColor.Blue, TowerColor.Yellow, TowerColor.Purple };

        private const float StayBonus = 1.05f;
        private const float BuffZoneValue = 0.5f;
        private const float BuffZoneSkill2Value = 0.8f;
        private const float DebuffCellBonus = 0.5f;
        private const float GenericActionTileValue = 0.6f;
        private const float GenericAttackTileValue = 0.5f;
        private const int DefaultRange = 2;

        public static bool PlayStep()
        {
            TowerManager towers = TowerManager.Instance;
            if (towers == null || towers.IsMerging) return false;

            while (CurrencyManager.Instance.Gold >= towers.BuildCost
                   && BoardManager.Instance.Slots.TryGetFirstEmpty(out _)
                   && towers.TryBuild()) { }
            if (towers.IsMerging) return false;
            if (TrySpendGems(towers)) return false;
            if (TryReroll(towers)) return false;

            PlanTurn();
            return !towers.IsMerging;
        }

        public static bool TryReroll(TowerManager towers)
        {
            BoardManager board = BoardManager.Instance;
            if (CurrencyManager.Instance.Gold < towers.BuildCost || board.Slots.TryGetFirstEmpty(out _)) return false;

            TowerController worst = null;
            float worstValue = float.MaxValue;
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null || tower.Definition.Tier != 1 || towers.IsMergingTower(tower)) continue;
                if (CountOthers(board, tower.Definition.Id, tower) > 0) continue;

                float value = RoleWeight(tower);
                if (!tower.IsAttackTower)
                {
                    foreach (Zone zone in board.Zones.GetByOwner(tower))
                        if (zone.Stage == ZoneStage.Skill2) value += 1f - SupportWeight;
                }
                if (value < worstValue || value == worstValue && worst != null && tower.BuildOrder > worst.BuildOrder)
                {
                    worst = tower;
                    worstValue = value;
                }
            }
            if (worst == null || !towers.TrySell(worst)) return false;

            BalanceRecorder.NoteReroll();
            towers.TryBuild();
            return true;
        }

        public static bool TrySpendGems(TowerManager towers)
        {
            BoardManager board = BoardManager.Instance;
            int gem = CurrencyManager.Instance.Gem;
            float bestRatio = 0f;
            TowerController bestTower = null;
            TowerColor bestColor = TowerColor.None;

            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null || towers.IsMergingTower(tower) || TowerId.IsSynergy(tower.Definition.Id)) continue;

                int tier = tower.Definition.Tier;
                int cost = TowerProgression.GetTierUpgradeCost(tier);
                int nextId = TowerId.GetNextTier(tower.Definition.Id);
                if (cost <= 0 || cost > gem || !towers.Database.TryGet(nextId, out _)) continue;

                float value = (TierWeight(tier + 1) - TierWeight(tier)) * RoleWeight(tower);
                if (CountOthers(board, nextId, tower) >= 2) value += TierWeight(tier + 1) * RoleWeight(tower);
                if (CountOthers(board, tower.Definition.Id, tower) == 1) value -= TierWeight(tier) * BreakPairPenalty * RoleWeight(tower);

                float ratio = value / cost;
                if (ratio > bestRatio)
                {
                    bestRatio = ratio;
                    bestTower = tower;
                    bestColor = TowerColor.None;
                }
            }

            foreach (TowerColor color in s_colors)
            {
                ColorUpgradeState levels = towers.ColorLevels;
                int cost = levels.CanUpgrade(color) ? levels.GetCost(color) : 0;
                if (cost <= 0 || cost > gem) continue;

                float value = 0f;
                for (int i = 0; i < board.Slots.Count; i++)
                {
                    TowerController tower = board.Slots.Get(i);
                    if (tower != null && tower.Definition.Color == color) value += TierWeight(tower.Definition.Tier) * RoleWeight(tower);
                }

                float ratio = value * ColorLevelValue / cost;
                if (ratio > bestRatio)
                {
                    bestRatio = ratio;
                    bestTower = null;
                    bestColor = color;
                }
            }

            if (bestTower != null)
            {
                int cost = TowerProgression.GetTierUpgradeCost(bestTower.Definition.Tier);
                BalanceRecorder.BeginGemTierUp();
                bool upgraded = towers.TryTierUpgrade(bestTower);
                BalanceRecorder.EndGemTierUp(upgraded ? cost : 0);
                return upgraded;
            }
            if (bestColor != TowerColor.None)
            {
                int cost = towers.ColorLevels.GetCost(bestColor);
                bool upgraded = towers.TryUpgradeColor(bestColor);
                if (upgraded) BalanceRecorder.NoteGemColorUp(cost);
                return upgraded;
            }
            return false;
        }

        private static float TierWeight(int tier) => Mathf.Pow(2f, Mathf.Clamp(tier, 1, 6) - 1);
        private static float RoleWeight(TowerController tower) => tower.IsAttackTower ? 1f : SupportWeight;

        private static int CountOthers(BoardManager board, int id, TowerController except)
        {
            int count = 0;
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower != null && tower != except && tower.Definition.Id == id) count++;
            }
            return count;
        }

        public static void PlanTurn()
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Grid == null || board.Zones == null) return;

            PlaceBuffZones(board);
            ArrangeTowers(board);
            PlaceDebuffZones(board);
        }

        private static void PlaceBuffZones(BoardManager board)
        {
            int range = ReferenceRange(board);
            foreach (Zone zone in new List<Zone>(board.Zones.All))
            {
                if (!zone.IsBuff || zone.IsPlaced) continue;

                int best = -1;
                float bestValue = float.MinValue;
                for (int slot = 0; slot < board.Grid.SpawnCount; slot++)
                {
                    if (!board.Zones.CanPlace(zone, slot, out _)) continue;
                    float value = Coverage(board, slot, range) * (1f + TileBonus(board, slot, null) + BuffBonus(board, slot));
                    if (value > bestValue)
                    {
                        best = slot;
                        bestValue = value;
                    }
                }
                if (best >= 0) board.Zones.TryPlace(zone, best);
            }
        }

        private static void ArrangeTowers(BoardManager board)
        {
            TowerSlots slots = board.Slots;
            var attackers = new List<TowerController>();
            var supporters = new List<TowerController>();
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null) continue;
                if (tower.IsAttackTower) attackers.Add(tower);
                else supporters.Add(tower);
            }
            attackers.Sort((a, b) => a.Definition.Tier != b.Definition.Tier
                ? b.Definition.Tier.CompareTo(a.Definition.Tier)
                : b.Stats.Power.CompareTo(a.Stats.Power));

            var taken = new bool[slots.Count];
            var plan = new List<(TowerController Tower, int Slot)>();
            foreach (TowerController tower in attackers)
            {
                int best = -1;
                float bestValue = float.MinValue;
                for (int slot = 0; slot < slots.Count; slot++)
                {
                    if (taken[slot]) continue;
                    float value = Coverage(board, slot, tower.Stats.Range) * (1f + TileBonus(board, slot, tower) + BuffBonus(board, slot));
                    if (slot == tower.SlotIndex) value *= StayBonus;
                    if (value > bestValue)
                    {
                        best = slot;
                        bestValue = value;
                    }
                }
                taken[best] = true;
                plan.Add((tower, best));
            }

            int generalRange = ReferenceRange(board);
            foreach (TowerController tower in supporters)
            {
                int best = tower.SlotIndex;
                if (taken[best])
                {
                    float lowest = float.MaxValue;
                    for (int slot = 0; slot < slots.Count; slot++)
                    {
                        if (taken[slot]) continue;
                        float value = Coverage(board, slot, generalRange) * (1f + TileBonus(board, slot, null) + BuffBonus(board, slot));
                        if (value < lowest)
                        {
                            best = slot;
                            lowest = value;
                        }
                    }
                }
                taken[best] = true;
                plan.Add((tower, best));
            }

            foreach ((TowerController tower, int slot) in plan)
            {
                if (tower.SlotIndex != slot) TowerManager.Instance.TryMoveOrSwap(tower, slot);
            }
        }

        private static void PlaceDebuffZones(BoardManager board)
        {
            BoardGrid grid = board.Grid;
            foreach (Zone zone in new List<Zone>(board.Zones.All))
            {
                if (zone.IsBuff || zone.IsPlaced) continue;

                int best = -1;
                float bestValue = float.MinValue;
                for (int cell = 0; cell < grid.PathCount; cell++)
                {
                    if (!board.Zones.CanPlace(zone, cell, out _)) continue;
                    if (board.Tiles != null && board.Tiles.GetPathTile(cell) == PathTileType.Purify) continue;

                    float value = -0.01f * board.Zones.GetAtPath(cell).Count;
                    Vector3Int pathCell = grid.GetPathCell(cell);
                    for (int slot = 0; slot < board.Slots.Count; slot++)
                    {
                        TowerController tower = board.Slots.Get(slot);
                        if (tower == null || !tower.IsAttackTower) continue;
                        if (BoardGrid.GetTileDistance(grid.GetSpawnCell(slot), pathCell) <= tower.Stats.Range) value += tower.Definition.Tier;
                    }
                    if (value > bestValue)
                    {
                        best = cell;
                        bestValue = value;
                    }
                }
                if (best >= 0) board.Zones.TryPlace(zone, best);
            }
        }

        private static float Coverage(BoardManager board, int slot, int range)
        {
            float coverage = 0f;
            foreach (int pathIndex in board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), range))
            {
                coverage += 1f;
                if (board.Zones.GetAtPath(pathIndex).Count > 0) coverage += DebuffCellBonus;
            }
            return coverage;
        }

        private static float BuffBonus(BoardManager board, int slot)
        {
            float bonus = 0f;
            foreach (Zone zone in board.Zones.GetAtSpawn(slot))
                bonus += zone.Stage == ZoneStage.Skill2 ? BuffZoneSkill2Value : BuffZoneValue;
            return bonus;
        }

        private static float TileBonus(BoardManager board, int slot, TowerController tower)
        {
            SpawnTileType tile = board.Tiles != null ? board.Tiles.GetSpawnTile(slot) : SpawnTileType.None;
            switch (tile)
            {
                case SpawnTileType.Power:
                    return 0.3f;
                case SpawnTileType.AttackCount:
                    return tower != null ? 1f / Mathf.Max(1, tower.Stats.AttackCount) : GenericAttackTileValue;
                case SpawnTileType.ActionCount:
                    if (tower == null) return GenericActionTileValue;
                    int action = Mathf.Max(1, tower.Stats.Action);
                    return action / (float)Mathf.CeilToInt(action / 2f) - 1f;
                case SpawnTileType.Mastery:
                    return 0.4f;
                default:
                    return 0f;
            }
        }

        private static int ReferenceRange(BoardManager board)
        {
            int range = 0;
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower != null && tower.IsAttackTower) range = Mathf.Max(range, tower.Stats.Range);
            }
            return range > 0 ? range : DefaultRange;
        }
    }
}
