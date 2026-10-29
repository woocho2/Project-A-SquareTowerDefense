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


        public const int DefaultPlacementProfile = 1;
        private static int s_placementProfile = DefaultPlacementProfile;
        public static int PlacementProfile => s_placementProfile;

        public static void ResetPlacement() => ConfigurePlacement(DefaultPlacementProfile);
        private static readonly int[][] s_priorityOrders =
        {
            new[] { 0, 1, 2 }, new[] { 0, 1, 2 }, new[] { 0, 2, 1 },
            new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 },
        };

        public static void ConfigurePlacement(int profile)
        {
            if (profile < 0 || profile > 6) throw new System.ArgumentOutOfRangeException(nameof(profile));
            s_placementProfile = profile;
        }

        private static float PriorityWeight(int component)
        {
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            if (policy != null && !policy.Baseline) return policy.Weights[component];
            int[] order = s_priorityOrders[s_placementProfile];
            return component == order[0] ? 4f : component == order[1] ? 2f : 1f;
        }

        private static void PlanPriority(BoardManager board)
        {
            int[] order = s_priorityOrders[s_placementProfile];
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            if (policy != null && !policy.Baseline)
            {
                order = (int[])order.Clone();
                System.Array.Sort(order, (a, b) => policy.Weights[b].CompareTo(policy.Weights[a]));
            }
            foreach (int component in order)
            {
                if (component == 0) AssignPrioritySlots(board);
                else if (component == 1) PlacePriorityBuffs(board);
                else PlacePriorityDebuffs(board);
            }
            AssignPrioritySlots(board);
        }

        private static TowerFinalStats Forecast(BoardManager board, TowerController tower, int slot, bool map, bool buffs, Zone extra = null)
        {
            TowerDefinition definition = tower.Definition;
            var state = new TowerState(definition);
            var received = new HashSet<string>(tower.State.ReceivedZoneBuffs.Values);
            foreach (StatModifier modifier in tower.State.Modifiers.All)
                if (modifier.Source != SpawnTileEffects.Source && !received.Contains(modifier.Source))
                    state.Modifiers.Add(modifier);
            if (map)
                foreach (StatModifier modifier in SpawnTileEffects.GetModifiers(board.Tiles.GetSpawnTile(slot)))
                    state.Modifiers.Add(modifier);
            if (buffs && tower.IsAttackTower)
            {
                foreach (Zone zone in board.Zones.GetAtSpawn(slot))
                    foreach (StatModifier modifier in SkillRegistry.GetBuffRule(zone.Emblem).GetModifiers(zone, tower, BuffZoneAction.SourceOf(zone)))
                        state.Modifiers.Add(modifier);
                if (extra != null)
                    foreach (StatModifier modifier in SkillRegistry.GetBuffRule(extra.Emblem).GetModifiers(extra, tower, BuffZoneAction.SourceOf(extra)))
                        state.Modifiers.Add(modifier);
            }
            TowerManager.Instance.Database.TryGet(TowerId.Make(1, definition.Color, definition.Emblem), out TowerDefinition tierOne);
            return TowerStatCalculator.Calculate(definition, tierOne, state, TowerManager.Instance.ColorLevels.GetLevel(definition.Color));
        }

        private static float Worth(BoardManager board, TowerController tower, TowerFinalStats stats, int slot)
        {
            float gain = Mathf.Max(1f, 1f + Mathf.Round(stats.Get(StatType.ActionGainPerTurn)));
            float period = Mathf.Max(1f, Mathf.Ceil(stats.Action / gain));
            if (!tower.IsAttackTower)
                return RoleWeight(tower) * stats.Power * stats.AbilityValue * stats.AttackCount / period *
                    Mathf.Max(1, board.Zones.GetByOwner(tower).Count);
            List<int> paths = board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), stats.Range);
            if (paths.Count == 0) return 0f;
            float distance = 0f;
            foreach (int cell in paths)
                distance += stats.GetDistanceMultiplier(BoardGrid.GetTileDistance(board.Grid.GetSpawnCell(slot), board.Grid.GetPathCell(cell)));
            distance /= paths.Count;
            float hits = tower.IsTarget ? 2f * stats.Mastery : 1f + (stats.Mastery * 2f - 2f) * 0.5f;
            float crit = 1f + stats.CriticalRate * (1f + stats.CriticalDamage);
            return RoleWeight(tower) * stats.Power * stats.AttackCount * hits * crit * distance / period *
                paths.Count / board.Grid.PathCount * (1f + 0.25f * stats.ArmorPenetration);
        }

        private static float Affinity(Zone zone, TowerController tower)
        {
            AttackAttribute attribute = zone.Owner.Definition.Attribute;
            bool match = attribute == AttackAttribute.None
                ? tower.Definition.Emblem == zone.Emblem
                : tower.Definition.Attribute == attribute;
            return match ? 1.5f : 1f;
        }

        private static float DebuffCoverage(BoardManager board, TowerController tower, int slot, int range)
        {
            List<int> paths = board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), range);
            if (paths.Count == 0) return 0f;
            float value = 0f;
            foreach (int path in paths)
                foreach (Zone zone in board.Zones.GetAtPath(path))
                    value += Affinity(zone, tower) * (zone.Stage == ZoneStage.Skill2 ? 1f : 0.5f);
            return value / paths.Count;
        }

        private static float PlacementValue(BoardManager board, TowerController tower, int slot)
        {
            TowerFinalStats plain = Forecast(board, tower, slot, false, false);
            TowerFinalStats mapped = Forecast(board, tower, slot, true, false);
            TowerFinalStats buffed = Forecast(board, tower, slot, true, true);
            float a = Worth(board, tower, plain, slot);
            float b = Worth(board, tower, mapped, slot);
            float c = Worth(board, tower, buffed, slot);
            if (tower.IsAttackTower && board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), buffed.Range).Count == 0) return -1000000f;
            float score = a + PriorityWeight(0) * (b - a) + PriorityWeight(1) * (c - b);
            if (tower.IsAttackTower)
                score += PriorityWeight(2) * Mathf.Max(0f, c) * DebuffCoverage(board, tower, slot, buffed.Range);
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            if (policy != null && !policy.Baseline)
                score += Mathf.Max(0.01f, a) * policy.SlotScale * policy.CellValue(tower.Definition.Emblem, slot, false);
            return score;
        }

        private static void AssignPrioritySlots(BoardManager board)
        {
            var towers = new List<TowerController>();
            for (int slot = 0; slot < board.Slots.Count; slot++)
                if (board.Slots.Get(slot) != null) towers.Add(board.Slots.Get(slot));
            if (towers.Count == 0) return;
            towers.Sort((a, b) => a.BuildOrder.CompareTo(b.BuildOrder));
            var values = new float[towers.Count, board.Slots.Count];
            for (int row = 0; row < towers.Count; row++)
                for (int slot = 0; slot < board.Slots.Count; slot++)
                    values[row, slot] = PlacementValue(board, towers[row], slot);
            int[] assignment = FindAssignment(values);
            for (int row = 0; row < towers.Count; row++)
                if (towers[row].SlotIndex != assignment[row])
                    TowerManager.Instance.TryMoveOrSwap(towers[row], assignment[row]);
        }

        private static void PlacePriorityBuffs(BoardManager board)
        {
            foreach (Zone zone in new List<Zone>(board.Zones.All))
            {
                if (!zone.IsBuff || zone.IsPlaced) continue;
                int best = -1;
                float highest = float.NegativeInfinity;
                for (int slot = 0; slot < board.Grid.SpawnCount; slot++)
                {
                    if (!board.Zones.CanPlace(zone, slot, out _)) continue;
                    float bestReceiver = float.NegativeInfinity;
                    for (int i = 0; i < board.Slots.Count; i++)
                    {
                        TowerController tower = board.Slots.Get(i);
                        if (tower == null || !tower.IsAttackTower) continue;
                        float before = Worth(board, tower, Forecast(board, tower, slot, true, true), slot);
                        TowerFinalStats received = Forecast(board, tower, slot, true, true, zone);
                        float after = Worth(board, tower, received, slot);
                        float value = PriorityWeight(1) * (after - before) +
                            0.1f * PriorityWeight(0) * before +
                            0.1f * PriorityWeight(2) * before * DebuffCoverage(board, tower, slot, received.Range);
                        StrategyPolicy policy = StrategySearch.ActivePolicy;
                        if (policy != null && !policy.Baseline)
                            value += before * 0.5f * (policy.ReceiverValue(zone.Emblem, tower.Definition.Emblem) - 1f);
                        if (value > bestReceiver) bestReceiver = value;
                    }
                    if (float.IsNegativeInfinity(bestReceiver))
                        bestReceiver = Coverage(board, slot, ReferenceRange(board)) *
                            (1f + PriorityWeight(0) * TileBonus(board, slot, null) + PriorityWeight(1) * BuffBonus(board, slot));
                    if (bestReceiver > highest) { highest = bestReceiver; best = slot; }
                }
                if (best >= 0) board.Zones.TryPlace(zone, best);
            }
        }

        private static void PlacePriorityDebuffs(BoardManager board)
        {
            foreach (Zone zone in new List<Zone>(board.Zones.All))
            {
                if (zone.IsBuff || zone.IsPlaced) continue;
                int best = -1;
                float highest = float.NegativeInfinity;
                for (int cell = 0; cell < board.Grid.PathCount; cell++)
                {
                    if (!board.Zones.CanPlace(zone, cell, out _) || board.Tiles.GetPathTile(cell) == PathTileType.Purify) continue;
                    float pressure = 1f;
                    foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
                    {
                        if (enemy.PathIndex == cell) pressure += 1f;
                        MoveRange move = enemy.Stats.GetMoveRange();
                        int count = move.Max - move.Min + 1;
                        if (count <= 0 || count > 128) continue;
                        for (int roll = move.Min; roll <= move.Max; roll++)
                            if ((enemy.PathIndex + roll + board.Grid.PathCount * 4) % board.Grid.PathCount == cell)
                                pressure += 0.25f / count;
                    }
                    float fire = 0f;
                    for (int i = 0; i < board.Slots.Count; i++)
                    {
                        TowerController tower = board.Slots.Get(i);
                        if (tower == null || !tower.IsAttackTower) continue;
                        if (BoardGrid.GetTileDistance(board.Grid.GetSpawnCell(i), board.Grid.GetPathCell(cell)) <= tower.Stats.Range)
                            fire += Affinity(zone, tower) * tower.Stats.Power * tower.Stats.AttackCount;
                    }
                    float score = pressure * (0.2f + fire) / (1f + 0.1f * board.Zones.GetAtPath(cell).Count);
                    StrategyPolicy policy = StrategySearch.ActivePolicy;
                    if (policy != null && !policy.Baseline)
                        score += pressure * (0.2f + fire) * policy.PathScale * policy.CellValue(zone.Emblem, cell, true);
                    if (score > highest) { highest = score; best = cell; }
                }
                if (best >= 0) board.Zones.TryPlace(zone, best);
            }
        }

        public static int[] FindAssignment(float[,] values)
        {
            if (values == null) throw new System.ArgumentNullException(nameof(values));
            int n = values.GetLength(0), m = values.GetLength(1);
            if (n > m) throw new System.ArgumentException("타워보다 자리가 적음");
            var u = new double[n + 1]; var v = new double[m + 1];
            var p = new int[m + 1]; var way = new int[m + 1];
            for (int i = 1; i <= n; i++)
            {
                p[0] = i;
                int j0 = 0;
                var min = new double[m + 1];
                var used = new bool[m + 1];
                for (int j = 1; j <= m; j++) min[j] = double.PositiveInfinity;
                do
                {
                    used[j0] = true;
                    int i0 = p[j0], j1 = 0;
                    double delta = double.PositiveInfinity;
                    for (int j = 1; j <= m; j++)
                    {
                        if (used[j]) continue;
                        float score = values[i0 - 1, j - 1];
                        if (float.IsNaN(score) || float.IsInfinity(score)) throw new System.ArgumentException("유효하지 않은 배치 점수");
                        double cur = -score - u[i0] - v[j];
                        if (cur < min[j]) { min[j] = cur; way[j] = j0; }
                        if (min[j] < delta) { delta = min[j]; j1 = j; }
                    }
                    for (int j = 0; j <= m; j++)
                        if (used[j]) { u[p[j]] += delta; v[j] -= delta; }
                        else min[j] -= delta;
                    j0 = j1;
                } while (p[j0] != 0);
                do { int j1 = way[j0]; p[j0] = p[j1]; j0 = j1; } while (j0 != 0);
            }
            var assignment = new int[n];
            for (int j = 1; j <= m; j++) if (p[j] > 0) assignment[p[j] - 1] = j - 1;
            return assignment;
        }

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
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            bool searching = policy != null && !policy.Baseline;
            int gem = Mathf.Max(0, CurrencyManager.Instance.Gem - (searching ? policy.GemReserve : 0));
            float bestRatio = 0f;
            TowerController bestTower = null;
            TowerColor bestColor = TowerColor.None;

            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null || towers.IsMergingTower(tower) || TowerId.IsSynergy(tower.Definition.Id) || searching && policy.UpgradeMode == 2) continue;

                int tier = tower.Definition.Tier;
                int cost = TowerProgression.GetTierUpgradeCost(tier);
                int nextId = TowerId.GetNextTier(tower.Definition.Id);
                if (cost <= 0 || cost > gem || !towers.Database.TryGet(nextId, out _)) continue;

                float value = (TierWeight(tier + 1) - TierWeight(tier)) * RoleWeight(tower);
                if (CountOthers(board, nextId, tower) >= 2) value += TierWeight(tier + 1) * RoleWeight(tower);
                if (CountOthers(board, tower.Definition.Id, tower) == 1) value -= TierWeight(tier) * BreakPairPenalty * RoleWeight(tower);

                float ratio = value * (searching ? policy.TierScale : 1f) / cost;
                if (ratio > bestRatio)
                {
                    bestRatio = ratio;
                    bestTower = tower;
                    bestColor = TowerColor.None;
                }
            }

            foreach (TowerColor color in s_colors)
            {
                if (searching && policy.UpgradeMode == 1) continue;
                ColorUpgradeState levels = towers.ColorLevels;
                int cost = levels.CanUpgrade(color) ? levels.GetCost(color) : 0;
                if (cost <= 0 || cost > gem) continue;

                float value = 0f;
                for (int i = 0; i < board.Slots.Count; i++)
                {
                    TowerController tower = board.Slots.Get(i);
                    if (tower != null && tower.Definition.Color == color) value += TierWeight(tower.Definition.Tier) * RoleWeight(tower);
                }

                float ratio = value * ColorLevelValue * (searching ? policy.ColorScale : 1f) / cost;
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
                int target = bestTower.Definition.Id;
                bool upgraded = towers.TryTierUpgrade(bestTower);
                BalanceRecorder.EndGemTierUp(upgraded ? cost : 0);
                if (upgraded) BalanceRecorder.NoteSearchAction("tier_upgrade", target, cost);
                return upgraded;
            }
            if (bestColor != TowerColor.None)
            {
                int cost = towers.ColorLevels.GetCost(bestColor);
                bool upgraded = towers.TryUpgradeColor(bestColor);
                if (upgraded)
                {
                    BalanceRecorder.NoteGemColorUp(cost);
                    BalanceRecorder.NoteSearchAction("color_upgrade", (int)bestColor, cost);
                }
                return upgraded;
            }
            return false;
        }

        private static float TierWeight(int tier) => Mathf.Pow(2f, Mathf.Clamp(tier, 1, 6) - 1);
        private static float RoleWeight(TowerController tower)
        {
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            return policy != null && !policy.Baseline ? policy.TowerValue(tower) : tower.IsAttackTower ? 1f : SupportWeight;
        }

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

            if (s_placementProfile > 0)
            {
                PlanPriority(board);
                BalanceRecorder.RecordPlacement();
                return;
            }
            PlaceBuffZones(board);
            ArrangeTowers(board);
            PlaceDebuffZones(board);
            BalanceRecorder.RecordPlacement();
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
