using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 자동 진행의 판단 — 상점 카드 사기(집중 문양 우선), 자리가 없으면 집중 밖 1티어 팔기, 대기석 타워 올리기, 경험치·리롤, 존과 타워 자리 고르기
    public static class AutoPlanner
    {
        public const int DefaultFocusCount = 3;
        public static int FocusCount { get; set; } = DefaultFocusCount;
        private static readonly HashSet<TowerEmblem> s_focus = new HashSet<TowerEmblem>();
        private static readonly Dictionary<TowerEmblem, int> s_upgraded = new Dictionary<TowerEmblem, int>();
        private static readonly Dictionary<TowerEmblem, int> s_basic = new Dictionary<TowerEmblem, int>();
        private static readonly Dictionary<TowerEmblem, int> s_firstUpgradeWave = new Dictionary<TowerEmblem, int>();
        private static readonly List<TowerEmblem> s_focusOrder = new List<TowerEmblem>();

        public static IReadOnlyCollection<TowerEmblem> Focus => s_focus;

        public static void ResetFocus() => s_firstUpgradeWave.Clear();

        private static void UpdateFocus(BoardManager board)
        {
            s_focus.Clear();
            if (FocusCount <= 0) return;
            s_upgraded.Clear();
            s_basic.Clear();
            s_focusOrder.Clear();
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
            foreach (TowerController tower in board.GetBoardAndBenchTowers())
            {
                if (TowerId.IsSynergy(tower.Definition.Id)) continue;
                TowerEmblem emblem = tower.Definition.Emblem;
                if (!s_upgraded.ContainsKey(emblem)) { s_upgraded[emblem] = 0; s_basic[emblem] = 0; s_focusOrder.Add(emblem); }
                if (tower.Definition.Tier >= 2)
                {
                    s_upgraded[emblem] += tower.Definition.Tier - 1;
                    if (!s_firstUpgradeWave.ContainsKey(emblem)) s_firstUpgradeWave[emblem] = wave;
                }
                else s_basic[emblem]++;
            }
            if (s_firstUpgradeWave.Count == 0 || s_focusOrder.Count <= FocusCount) return;

            s_focusOrder.Sort(CompareFocus);
            int lead = Mathf.Min(FocusCount - 1, 2);
            for (int i = 0; i < lead; i++) s_focus.Add(s_focusOrder[i]);
            while (s_focus.Count < FocusCount)
            {
                TowerEmblem best = TowerEmblem.None;
                int bestSynergy = -1;
                foreach (TowerEmblem emblem in s_focusOrder)
                {
                    if (s_focus.Contains(emblem)) continue;
                    int synergy = SynergyMatches(emblem);
                    if (synergy > bestSynergy) { best = emblem; bestSynergy = synergy; }
                }
                if (best == TowerEmblem.None) break;
                s_focus.Add(best);
            }
        }

        private static int CompareFocus(TowerEmblem a, TowerEmblem b)
        {
            int upgraded = s_upgraded[b].CompareTo(s_upgraded[a]);
            if (upgraded != 0) return upgraded;
            int first = FirstUpgrade(a).CompareTo(FirstUpgrade(b));
            return first != 0 ? first : s_basic[b].CompareTo(s_basic[a]);
        }

        private static int FirstUpgrade(TowerEmblem emblem) => s_firstUpgradeWave.TryGetValue(emblem, out int wave) ? wave : int.MaxValue;

        private static int SynergyMatches(TowerEmblem candidate)
        {
            SynergyBoard synergy = TowerManager.Instance != null ? TowerManager.Instance.Synergy : null;
            if (synergy == null) return 0;
            int matches = 0;
            foreach (SynergyBoard.Entry entry in synergy.Entries)
            {
                if (System.Array.IndexOf(entry.Recipe.Emblems, candidate) < 0) continue;
                foreach (TowerEmblem emblem in entry.Recipe.Emblems)
                    if (s_focus.Contains(emblem)) matches++;
            }
            return matches;
        }

        private static bool IsOffFocus(TowerController tower) =>
            s_focus.Count > 0 && !TowerId.IsSynergy(tower.Definition.Id) && !s_focus.Contains(tower.Definition.Emblem);

        private const float SupportWeight = 0.7f;

        private const float StayBonus = 1.05f;
        private const float BuffZoneValue = 0.5f;
        private const float DebuffCellBonus = 0.5f;
        private const int DefaultRange = 2;


        internal const int MapBuffDebuffProfile = 1;
        public const int EmblemPlacementProfile = 7;
        public const int CarryPlacementProfile = 8;
        public const int DefaultPlacementProfile = EmblemPlacementProfile;
        private const float PreferredReceiverBonus = 10000f;
        private static bool GroupByEmblem => s_placementProfile == EmblemPlacementProfile;
        private static bool FocusCarry => s_placementProfile == CarryPlacementProfile;
        private static int PriorityProfile => GroupByEmblem || FocusCarry ? MapBuffDebuffProfile : s_placementProfile;
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
            if (profile < 0 || profile > CarryPlacementProfile) throw new System.ArgumentOutOfRangeException(nameof(profile));
            s_placementProfile = profile;
        }

        private static float PriorityWeight(int component)
        {
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            if (policy != null && !policy.Baseline) return policy.Weights[component];
            int[] order = s_priorityOrders[PriorityProfile];
            return component == order[0] ? 4f : component == order[1] ? 2f : 1f;
        }

        private static void PlanPriority(BoardManager board)
        {
            int[] order = s_priorityOrders[PriorityProfile];
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

        private static TowerFinalStats Forecast(BoardManager board, TowerController tower, int slot, bool buffs, Zone extra = null)
        {
            TowerDefinition definition = tower.Definition;
            var state = new TowerState(definition);
            var received = new HashSet<string>();
            foreach (TowerEmblem emblem in tower.State.SupportBuffs.Keys) received.Add(SupportBuffs.SourceOf(emblem));
            foreach (StatModifier modifier in tower.State.Modifiers.All)
                if (!received.Contains(modifier.Source))
                    state.Modifiers.Add(modifier);
            if (buffs && tower.IsAttackTower)
            {
                foreach (Zone zone in board.Zones.GetAtSpawn(slot))
                    foreach (StatModifier modifier in SkillRegistry.GetBuffRule(zone.Emblem).GetModifiers(SupportCast.Skill1(zone.Owner), tower, SupportBuffs.SourceOf(zone.Emblem)))
                        state.Modifiers.Add(modifier);
                if (extra != null)
                    foreach (StatModifier modifier in SkillRegistry.GetBuffRule(extra.Emblem).GetModifiers(SupportCast.Skill1(extra.Owner), tower, SupportBuffs.SourceOf(extra.Emblem)))
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
            IReadOnlyList<int> paths = board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), stats.Range);
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
            IReadOnlyList<int> paths = board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), range);
            if (paths.Count == 0) return 0f;
            float value = 0f;
            foreach (int path in paths)
                foreach (Zone zone in board.Zones.GetAtPath(path))
                    value += Affinity(zone, tower) * 0.5f;
            return value / paths.Count;
        }

        private static float PlacementValue(BoardManager board, TowerController tower, int slot)
        {
            TowerFinalStats plain = Forecast(board, tower, slot, false);
            TowerFinalStats buffed = Forecast(board, tower, slot, true);
            float a = Worth(board, tower, plain, slot);
            float c = Worth(board, tower, buffed, slot);
            if (tower.IsAttackTower && board.Grid.GetPathIndicesInSquare(board.Grid.GetSpawnCell(slot), buffed.Range).Count == 0) return -1000000f;
            float score = a + PriorityWeight(1) * (c - a);
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
            TowerController carry = FocusCarry ? StrongestAttack(board) : null;
            var values = new float[towers.Count, board.Slots.Count];
            for (int row = 0; row < towers.Count; row++)
                for (int slot = 0; slot < board.Slots.Count; slot++)
                {
                    TowerController tower = towers[row];
                    float value = PlacementValue(board, tower, slot);
                    if ((GroupByEmblem || FocusCarry) && value >= 0f && tower.IsAttackTower)
                    {
                        List<Zone> buffs = board.Zones.GetAtSpawn(slot);
                        if (GroupByEmblem && buffs.Exists(zone => zone.Emblem == tower.Definition.Emblem))
                            value += PreferredReceiverBonus;
                        else if (tower == carry)
                            value += PreferredReceiverBonus * buffs.Count;
                    }
                    values[row, slot] = value;
                }
            int[] assignment = FindAssignment(values);
            for (int row = 0; row < towers.Count; row++)
                if (towers[row].SlotIndex != assignment[row])
                    TowerManager.Instance.TryMoveOrSwap(towers[row], assignment[row]);
        }

        private static float UnbuffedWorth(BoardManager board, TowerController tower)
        {
            int slot = tower.SlotIndex;
            return Worth(board, tower, Forecast(board, tower, slot, false), slot);
        }

        private static TowerController StrongestAttack(BoardManager board, TowerEmblem emblem = TowerEmblem.None)
        {
            TowerController selected = null;
            float highest = 0f;
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null || !tower.IsAttackTower ||
                    (emblem != TowerEmblem.None && tower.Definition.Emblem != emblem)) continue;
                float worth = UnbuffedWorth(board, tower);
                if (worth > highest)
                {
                    selected = tower;
                    highest = worth;
                }
            }
            return selected;
        }

        private static void PlacePriorityBuffs(BoardManager board)
        {
            foreach (Zone zone in new List<Zone>(board.Zones.All))
            {
                if (!zone.IsBuff || zone.IsPlaced) continue;
                TowerController receiver = GroupByEmblem ? StrongestAttack(board, zone.Emblem)
                    : FocusCarry ? StrongestAttack(board) : null;
                if (receiver != null && board.Zones.TryPlace(zone, receiver.SlotIndex)) continue;
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
                        float before = Worth(board, tower, Forecast(board, tower, slot, true), slot);
                        TowerFinalStats received = Forecast(board, tower, slot, true, zone);
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
                            (1f + PriorityWeight(1) * BuffBonus(board, slot));
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
                        {
                            float affinity = GroupByEmblem && tower.Definition.Emblem == zone.Emblem
                                ? 8f : Affinity(zone, tower);
                            fire += affinity * tower.Stats.Power * tower.Stats.AttackCount;
                        }
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

            BoardManager board = BoardManager.Instance;
            UpdateFocus(board);
            if (TryBuyCard(towers, board)) return false;
            if (PromoteFromBench(board, towers)) return false;
            if (TrySellForSpace(towers, board)) return false;
            if (TryBuyXp(towers)) return false;
            if (TryShopReroll(towers, board)) return false;

            PlanTurn();
            return !towers.IsMerging;
        }

        public const int XpGoldReserve = 800;
        public const int RerollGoldReserve = 600;
        public const int MaxShopRerollsPerTurn = 4;
        public const int MaxSellsPerTurn = 4;

        private static float CardScore(BoardManager board, TowerShop.Card card)
        {
            int tier = TowerId.GetTier(card.TowerId);
            TowerEmblem emblem = TowerId.GetEmblem(card.TowerId);
            bool boardOpen = board.Slots.TryGetFirstEmpty(out _);
            bool focused = s_focus.Count == 0 || s_focus.Contains(emblem);
            int owned = CountOthers(board, card.TowerId, null);
            if (!focused && !boardOpen) return 0f;
            if (!boardOpen && owned == 0 && tier == 1 && s_focus.Count == 0) return 0f;

            float score = 1f + (boardOpen ? 0f : owned) + (tier - 1) * 1.5f + (focused ? 1f : 0f);
            if (BalanceTables.IsSupport(TowerId.GetColor(card.TowerId)))
            {
                CountRoles(board, out int attack, out int support);
                score *= attack < MinAttackBeforeSupport ? 0f : (float)support / (attack + support) < TargetSupportShare ? 1.1f : 0.7f;
            }
            return score;
        }

        public const int MinAttackBeforeSupport = 2;
        public const float TargetSupportShare = 0.35f;

        private static void CountRoles(BoardManager board, out int attack, out int support)
        {
            attack = 0;
            support = 0;
            foreach (TowerController tower in board.GetBoardAndBenchTowers())
            {
                if (TowerId.IsSynergy(tower.Definition.Id)) continue;
                if (tower.IsAttackTower) attack++;
                else support++;
            }
        }

        private static bool NeedsAttackCard(TowerManager towers, BoardManager board)
        {
            CountRoles(board, out int attack, out _);
            if (attack >= MinAttackBeforeSupport) return false;
            foreach (TowerShop.Card card in towers.Shop.Cards)
                if (card.IsAvailable && !BalanceTables.IsSupport(TowerId.GetColor(card.TowerId))) return false;
            return true;
        }

        private static int BestCard(TowerManager towers, BoardManager board, bool affordableOnly)
        {
            int best = -1;
            float bestScore = 0f;
            for (int i = 0; i < towers.Shop.Cards.Count; i++)
            {
                TowerShop.Card card = towers.Shop.Cards[i];
                if (!card.IsAvailable || affordableOnly && card.Price > CurrencyManager.Instance.Gold) continue;
                float score = CardScore(board, card);
                if (score > bestScore) { best = i; bestScore = score; }
            }
            return best;
        }

        private static bool TryBuyCard(TowerManager towers, BoardManager board)
        {
            if (!TowerManager.TryGetBuildSlot(board, out _, out _)) return false;
            int best = BestCard(towers, board, true);
            return best >= 0 && towers.TryBuyCard(best);
        }

        private static int s_turnWave = -1;
        private static int s_rerollsThisTurn;
        private static int s_sellsThisTurn;

        private static void CountTurn()
        {
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
            if (s_turnWave == wave) return;
            s_turnWave = wave;
            s_rerollsThisTurn = 0;
            s_sellsThisTurn = 0;
        }

        private static bool TryShopReroll(TowerManager towers, BoardManager board)
        {
            CountTurn();
            bool cheap = CurrencyManager.Instance.Gold >= BalanceTables.ShopRerollCost + BalanceTables.GetShopPrice(1);
            bool fill = cheap && (NeedsAttackCard(towers, board) || board.Slots.TryGetFirstEmpty(out _) && BestCard(towers, board, true) < 0);
            if (s_rerollsThisTurn >= MaxShopRerollsPerTurn || !fill && CurrencyManager.Instance.Gold < RerollGoldReserve) return false;
            if (!TowerManager.TryGetBuildSlot(board, out _, out _) && !HasSellable(towers, board)) return false;
            if (!towers.TryRerollShop()) return false;
            s_rerollsThisTurn++;
            BalanceRecorder.NoteReroll();
            return true;
        }

        private static bool HasSellable(TowerManager towers, BoardManager board) => WorstSellable(towers, board) != null;

        private static TowerController WorstSellable(TowerManager towers, BoardManager board)
        {
            List<TowerController> pool = board.GetTowers(TowerPlace.Bench);
            pool.AddRange(board.GetTowers(TowerPlace.Board));
            TowerController worst = null;
            float worstValue = float.MaxValue;
            foreach (TowerController tower in pool)
            {
                if (tower.Definition.Tier != 1 || towers.IsMergingTower(tower) || TowerId.IsSynergy(tower.Definition.Id)) continue;
                if (CountOthers(board, tower.Definition.Id, tower) > 0) continue;
                if (s_focus.Count > 0 && !IsOffFocus(tower)) continue;

                float value = RoleWeight(tower) + (tower.Place == TowerPlace.Bench ? 0f : 0.5f);
                if (value < worstValue || value == worstValue && worst != null && tower.BuildOrder > worst.BuildOrder)
                {
                    worst = tower;
                    worstValue = value;
                }
            }
            return worst;
        }

        private static bool TrySellForSpace(TowerManager towers, BoardManager board)
        {
            CountTurn();
            if (s_sellsThisTurn >= MaxSellsPerTurn || TowerManager.TryGetBuildSlot(board, out _, out _)) return false;
            int wanted = BestCard(towers, board, false);
            if (wanted < 0 || towers.Shop.Cards[wanted].Price > CurrencyManager.Instance.Gold + 200) return false;
            TowerController worst = WorstSellable(towers, board);
            if (worst == null || !towers.TrySell(worst)) return false;
            s_sellsThisTurn++;
            return true;
        }

        private static float BenchValue(TowerController tower) => TierWeight(tower.Definition.Tier) * RoleWeight(tower) * (IsOffFocus(tower) ? 0.8f : 1f);

        public static bool PromoteFromBench(BoardManager board, TowerManager towers)
        {
            if (board.Bench == null || board.Bench.Count == 0) return false;

            TowerController best = null;
            foreach (TowerController tower in board.GetTowers(TowerPlace.Bench))
                if (!towers.IsMergingTower(tower) && (best == null || BenchValue(tower) > BenchValue(best))) best = tower;
            if (best == null) return false;
            if (board.Slots.TryGetFirstEmpty(out int empty)) return towers.TryMoveTo(best, TowerPlace.Board, empty);

            TowerController worst = null;
            foreach (TowerController tower in board.GetTowers(TowerPlace.Board))
            {
                if (towers.IsMergingTower(tower) || tower.State.BossEffects.BlocksProgression) continue;
                if (worst == null || BenchValue(tower) < BenchValue(worst)) worst = tower;
            }
            if (worst == null || BenchValue(best) <= BenchValue(worst) * 1.01f) return false;
            return towers.TryMoveTo(best, TowerPlace.Board, worst.SlotIndex);
        }

        public static bool TryBuyXp(TowerManager towers)
        {
            if (towers.ColorLevels.IsMax) return false;
            if (CurrencyManager.Instance.Gold < XpGoldReserve + BalanceTables.XpPurchaseCost) return false;
            if (!towers.TryBuyXp()) return false;
            BalanceRecorder.NoteXpPurchase(BalanceTables.XpPurchaseCost);
            BalanceRecorder.NoteSearchAction("buy_xp", towers.ColorLevels.Level, BalanceTables.XpPurchaseCost);
            return true;
        }

        private static float TierWeight(int tier) => BalanceTables.GetTierRow(Mathf.Clamp(tier, 1, TowerId.MaxTier)).Power;
        private static float RoleWeight(TowerController tower)
        {
            StrategyPolicy policy = StrategySearch.ActivePolicy;
            return policy != null && !policy.Baseline ? policy.TowerValue(tower) : tower.IsAttackTower ? 1f : SupportWeight;
        }

        private static int CountOthers(BoardManager board, int id, TowerController except)
        {
            int count = 0;
            foreach (TowerController tower in board.GetBoardAndBenchTowers())
                if (tower != except && tower.Definition.Id == id) count++;
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
                    float value = Coverage(board, slot, range) * (1f + BuffBonus(board, slot));
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
                    float value = Coverage(board, slot, tower.Stats.Range) * (1f + BuffBonus(board, slot));
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
                        float value = Coverage(board, slot, generalRange) * (1f + BuffBonus(board, slot));
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
                bonus += BuffZoneValue;
            return bonus;
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
