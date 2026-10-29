using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SquareTD
{
    // 고정 초기 조건에서 구매·판매·강화·배치 전략을 반복 평가하고 낮은 후보의 반복을 조기 중단
    [Serializable]
    public class StrategyPolicy
    {
        public int Id;
        public int Parent = -1;
        public bool Baseline = true;
        public int ShopOrder;
        public int UpgradeMode;
        public int MaxTowers = 28;
        public int GemReserve;
        public int SaleLimit;
        public int SaleTier = 1;
        public int MinAttackers = 1;
        public float GoldReserve;
        public float TierScale = 1f;
        public float ColorScale = 1f;
        public float SlotScale;
        public float PathScale;
        public float[] RoleValues = { 1f, 1f, 0.7f, 0.7f };
        public float[] EmblemValues = new float[14];
        public float[] Weights = { 4f, 2f, 1f };
        public float[] BuffPreference = new float[196];
        public float[] SlotPreference = new float[392];
        public float[] PathPreference = new float[448];

        public StrategyPolicy()
        {
            for (int i = 0; i < EmblemValues.Length; i++) EmblemValues[i] = 1f;
            for (int i = 0; i < BuffPreference.Length; i++) BuffPreference[i] = 1f;
        }

        public float TowerValue(TowerController tower)
        {
            int color = (int)tower.Definition.Color - 1;
            int emblem = (int)tower.Definition.Emblem - 1;
            return color >= 0 && color < RoleValues.Length && emblem >= 0 && emblem < EmblemValues.Length
                ? RoleValues[color] * EmblemValues[emblem] : 1f;
        }

        public float ReceiverValue(TowerEmblem owner, TowerEmblem receiver)
        {
            int index = ((int)owner - 1) * 14 + (int)receiver - 1;
            return index >= 0 && index < BuffPreference.Length ? BuffPreference[index] : 1f;
        }

        public float CellValue(TowerEmblem emblem, int cell, bool path)
        {
            float[] values = path ? PathPreference : SlotPreference;
            int width = path ? 32 : 28;
            int index = ((int)emblem - 1) * width + cell;
            return cell >= 0 && cell < width && index >= 0 && index < values.Length ? values[index] : 0f;
        }

        public static StrategyPolicy Next(StrategyPolicy parent, int id, int seed, bool repeatBaseline)
        {
            var result = JsonUtility.FromJson<StrategyPolicy>(JsonUtility.ToJson(parent));
            result.Id = id;
            result.Parent = parent.Id;
            if (repeatBaseline) return result;
            result.Baseline = false;
            var random = new System.Random(unchecked(seed + 104729 * id));
            bool fresh = id % 8 == 0;
            int[] emblems = { 2, 3, 6, 10 };
            for (int change = 0; change < (fresh ? 21 : 4); change++)
            {
                int gene = fresh ? change : random.Next(21);
                float unit = (float)random.NextDouble();
                switch (gene)
                {
                    case 0: result.ShopOrder = random.Next(4); break;
                    case 1: result.MaxTowers = new[] { 4, 6, 8, 12, 18, 28 }[random.Next(6)]; break;
                    case 2: result.GemReserve = random.Next(3); break;
                    case 3: result.SaleLimit = random.Next(5); break;
                    case 4: result.SaleTier = random.Next(1, 6); break;
                    case 5: result.MinAttackers = random.Next(1, 4); break;
                    case 6: result.GoldReserve = unit * 0.5f; break;
                    case 7: result.TierScale = 0.25f + unit * 3.75f; break;
                    case 8: result.ColorScale = 0.25f + unit * 3.75f; break;
                    case 9: result.RoleValues[random.Next(4)] = 0.1f + unit * 1.9f; break;
                    case 10: result.EmblemValues[emblems[random.Next(4)]] = 0.25f + unit * 2.75f; break;
                    case 11: result.Weights[random.Next(3)] = 0.25f + unit * 7.75f; break;
                    case 12:
                    case 13:
                        for (int i = 0; i < 14; i++) result.BuffPreference[emblems[random.Next(4)] * 14 + emblems[random.Next(4)]] = 0.1f + (float)random.NextDouble() * 3.9f;
                        break;
                    case 14: result.SlotScale = unit * 8f; break;
                    case 15:
                    case 16:
                        for (int i = 0; i < 28; i++) result.SlotPreference[emblems[random.Next(4)] * 28 + random.Next(28)] = (float)random.NextDouble() * 2f - 1f;
                        break;
                    case 17: result.PathScale = unit * 4f; break;
                    case 18:
                    case 19:
                        for (int i = 0; i < 32; i++) result.PathPreference[emblems[random.Next(4)] * 32 + random.Next(32)] = (float)random.NextDouble() * 2f - 1f;
                        break;
                    case 20: result.UpgradeMode = random.Next(3); break;
                }
            }
            return result;
        }
    }

    [Serializable]
    public class StrategyTrial
    {
        public int Trial;
        public int CombatSeed;
        public string Result;
        public int Wave;
        public int Turns;
        public int Kills;
    }

    [Serializable]
    public class StrategyCase
    {
        public int Id;
        public int Parent;
        public string Status = "Running";
        public List<StrategyTrial> Trials = new List<StrategyTrial>();
        public float MeanWave => Mean(Trials.Count);
        public float Mean(int count)
        {
            if (count < 1 || count > Trials.Count) return 0f;
            float sum = 0f;
            for (int i = 0; i < count; i++) sum += Trials[i].Wave;
            return sum / count;
        }
    }

    [Serializable]
    public class StrategyProgress
    {
        public int BaseSeed;
        public int Leader;
        public int CandidateLimit;
        public int Repeats;
        public int MinRepeats;
        public float RetainRatio;
        public bool Complete;
        public List<StrategyCase> Cases = new List<StrategyCase>();
    }


    public sealed class StrategyRandomStreams
    {
        private UnityEngine.Random.State m_economy;
        public StrategyRandomStreams(int baseSeed, int combatSeed)
        {
            UnityEngine.Random.InitState(unchecked(baseSeed ^ 0x45d9f3b));
            m_economy = UnityEngine.Random.state;
            UnityEngine.Random.InitState(combatSeed);
        }

        public bool RunPlanner(Func<bool> action)
        {
            UnityEngine.Random.State combat = UnityEngine.Random.state;
            UnityEngine.Random.state = m_economy;
            try { return action(); }
            finally
            {
                m_economy = UnityEngine.Random.state;
                UnityEngine.Random.state = combat;
            }
        }
    }

    public sealed class StrategySearch
    {
        public static StrategyPolicy ActivePolicy { get; private set; }
        public StrategyProgress Progress { get; }
        public int Candidate => Progress.Cases.Count - 1;
        public int Trial => Progress.Cases[Candidate].Trials.Count;
        public StrategyPolicy Policy { get; private set; }
        private readonly SimulationSettings m_settings;
        private readonly List<StrategyPolicy> m_policies = new List<StrategyPolicy>();
        private int m_wave = -1;
        private int m_actions;
        private int m_sales;
        public static int CurrentCombatSeed { get; private set; }

        public StrategySearch(SimulationSettings settings)
        {
            m_settings = settings;
            Progress = new StrategyProgress
            {
                BaseSeed = settings.FirstSeed, CandidateLimit = settings.SearchCandidates,
                Repeats = settings.SearchRepeats, MinRepeats = settings.SearchMinRepeats,
                RetainRatio = settings.SearchRetainRatio,
            };
            AddPolicy(new StrategyPolicy());
        }

        private string Prefix => Path.Combine(SimulationSettings.Folder, m_settings.ResultName);
        public int CombatSeed => unchecked(m_settings.FirstSeed + 104729 * (Trial + 1));

        private void AddPolicy(StrategyPolicy policy)
        {
            Policy = policy;
            m_policies.Add(policy);
            Progress.Cases.Add(new StrategyCase { Id = policy.Id, Parent = policy.Parent });
            File.WriteAllText(Prefix + "_strategy_" + policy.Id.ToString("D4") + ".json", JsonUtility.ToJson(policy, true));
            Save();
        }

        public void BeginGame()
        {
            ActivePolicy = Policy;
            CurrentCombatSeed = CombatSeed;
            m_wave = -1;
            m_actions = m_sales = 0;
            m_settings.SearchCandidate = Candidate;
            m_settings.SearchTrial = Trial;
        }

        public static void Reset() { ActivePolicy = null; CurrentCombatSeed = 0; }

        public static bool ShouldPrune(StrategyCase candidate, StrategyCase leader, int minRepeats, float retainRatio)
        {
            int count = candidate.Trials.Count;
            return candidate.Id != leader.Id && count >= minRepeats && count < leader.Trials.Count &&
                candidate.Mean(count) < leader.Mean(count) * retainRatio - 0.0001f;
        }

        public bool FinishGame(string result, int wave, int turns, int kills)
        {
            StrategyCase current = Progress.Cases[Candidate];
            current.Trials.Add(new StrategyTrial { Trial = Trial, CombatSeed = CombatSeed, Result = result, Wave = wave, Turns = turns, Kills = kills });
            bool invalid = result != "Clear" && result != "GameOver";
            if (invalid)
            {
                current.Status = "Invalid";
                m_settings.SearchFailed = true;
                Progress.Complete = true;
                Save();
                return false;
            }
            StrategyCase leader = Progress.Cases[Progress.Leader];
            bool full = current.Trials.Count >= Progress.Repeats;
            bool pruned = !full && ShouldPrune(current, leader, Progress.MinRepeats, Progress.RetainRatio);
            if (!full && !pruned) { Save(); return true; }
            current.Status = full ? "Completed" : "PrunedHeuristic";
            if (full && (current.MeanWave > leader.MeanWave ||
                Mathf.Approximately(current.MeanWave, leader.MeanWave) && SumKills(current) > SumKills(leader)))
                Progress.Leader = current.Id;
            if (Progress.Cases.Count >= Progress.CandidateLimit)
            {
                Progress.Complete = true;
                m_settings.SearchComplete = true;
                Save();
                return false;
            }
            AddPolicy(StrategyPolicy.Next(m_policies[Progress.Leader], Progress.Cases.Count, Progress.BaseSeed, m_settings.SearchSmoke));
            return true;
        }

        private static int SumKills(StrategyCase item)
        {
            int sum = 0;
            foreach (StrategyTrial trial in item.Trials) sum += trial.Kills;
            return sum;
        }

        private void Save()
        {
            m_settings.SearchCandidate = Candidate;
            m_settings.SearchTrial = Trial;
            File.WriteAllText(Prefix + "_strategy_search.json", JsonUtility.ToJson(Progress, true));
            WriteSummary(m_settings, Progress);
        }

        public static void WriteSummary(SimulationSettings settings, StrategyProgress progress)
        {
            var ranked = progress.Cases.FindAll(item => item.Status == "Completed");
            ranked.Sort((a, b) =>
            {
                int comparison = b.MeanWave.CompareTo(a.MeanWave);
                return comparison != 0 ? comparison : SumKills(b).CompareTo(SumKills(a));
            });
            var text = new StringBuilder();
            text.AppendLine("# 게임 전략 탐색");
            text.AppendLine();
            text.AppendLine($"초기 시드 {progress.BaseSeed} / 노말 활·창·얼음·불 / 합성3기 / 후보 {progress.Cases.Count}/{progress.CandidateLimit} / 최대 {progress.Repeats}회");
            text.AppendLine($"후보당 {progress.MinRepeats}회부터 같은 전투 시나리오의 현재 1위 평균 ×{progress.RetainRatio:0.##} 미만이면 반복 조기 중단.");
            text.AppendLine("구매·판매·젬 티어 강화·컬러 강화·유지 기수·타워 자리·버프 대상·디버프 위치의 선택 규칙을 함께 탐색.");
            text.AppendLine("유한한 전략 후보 탐색이며 모든 합법 행동 순서의 열거 또는 전역 최적성 증명은 아님. 조기 탈락은 경험적 기준.");
            text.AppendLine("맵·시작 상태·구매 난수는 고정하고 전투 난수만 반복마다 변경. 모든 후보는 같은 반복 번호의 전투 시드를 공유.");
            text.AppendLine($"{progress.Repeats}회 완료 후보만 최종 순위에 포함. 중도 탈락 후보의 잠정 평균은 별도 JSON 기록. Power·AV는 수정하지 않음.");
            text.AppendLine();
            text.AppendLine("| 순위 | 후보 | 부모 | 반복 | 평균 도달 웨이브 | 최고 웨이브 | 클리어 |");
            text.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");
            int rank = 0;
            foreach (StrategyCase item in ranked)
            {
                int maximum = 0, clears = 0;
                foreach (StrategyTrial trial in item.Trials) { maximum = Math.Max(maximum, trial.Wave); if (trial.Result == "Clear") clears++; }
                text.AppendLine($"| {++rank} | {item.Id} | {item.Parent} | {item.Trials.Count} | {item.MeanWave.ToString("0.00", CultureInfo.InvariantCulture)} | {maximum} | {clears} |");
            }
            text.AppendLine();
            text.AppendLine($"진행 완료: {progress.Complete}. 현재 완료 후보 1위: {progress.Leader}.");
            File.WriteAllText(Path.Combine(SimulationSettings.Folder, settings.ResultName + "_strategy_summary.md"), text.ToString(), Encoding.UTF8);
        }

        public bool PlayStep()
        {
            TowerManager manager = TowerManager.Instance;
            if (manager == null || manager.IsMerging) return false;
            int wave = WaveManager.Instance.CurrentWave;
            if (wave != m_wave) { m_wave = wave; m_actions = m_sales = 0; }
            if (Policy.Baseline) return AutoPlanner.PlayStep();
            if (m_actions < 48)
            {
                bool upgradeFirst = Policy.ShopOrder == 1 || Policy.ShopOrder == 3;
                bool sellFirst = Policy.ShopOrder >= 2;
                if (upgradeFirst && AutoPlanner.TrySpendGems(manager)) { m_actions++; return false; }
                if (sellFirst && TrySell(manager)) { m_actions++; return false; }
                int count = 0;
                BoardManager board = BoardManager.Instance;
                for (int i = 0; i < board.Slots.Count; i++) if (board.Slots.Get(i) != null) count++;
                int gold = CurrencyManager.Instance.Gold;
                if (count < Policy.MaxTowers && gold - Mathf.CeilToInt(gold * Policy.GoldReserve) >= manager.BuildCost &&
                    board.Slots.TryGetFirstEmpty(out _) && manager.TryBuild())
                { m_actions++; return false; }
                if (!sellFirst && TrySell(manager)) { m_actions++; return false; }
                if (!upgradeFirst && AutoPlanner.TrySpendGems(manager)) { m_actions++; return false; }
            }
            AutoPlanner.PlanTurn();
            return true;
        }

        private bool TrySell(TowerManager manager)
        {
            if (m_sales >= Policy.SaleLimit) return false;
            BoardManager board = BoardManager.Instance;
            int attackers = 0, desired = 0;
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null) continue;
                if (tower.IsAttackTower)
                {
                    attackers++;
                    int cost = Policy.UpgradeMode == 2 && manager.ColorLevels.CanUpgrade(tower.Definition.Color)
                        ? manager.ColorLevels.GetCost(tower.Definition.Color) : TowerProgression.GetTierUpgradeCost(tower.Definition.Tier);
                    desired = Math.Max(desired, cost + Policy.GemReserve);
                }
            }
            if (attackers == 0 || CurrencyManager.Instance.Gem >= desired) return false;
            TowerController worst = null;
            float worstValue = float.PositiveInfinity;
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null || manager.IsMergingTower(tower) || TowerId.IsSynergy(tower.Definition.Id) ||
                    tower.Definition.Tier > Policy.SaleTier || tower.IsAttackTower && attackers <= Policy.MinAttackers) continue;
                float value = Policy.TowerValue(tower) * Mathf.Pow(2f, tower.Definition.Tier - 1);
                if (value < worstValue) { worstValue = value; worst = tower; }
            }
            if (worst == null) return false;
            int target = worst.Definition.Id;
            int gem = TowerManager.GetSellGem(worst.Definition.Tier);
            if (!manager.TrySell(worst)) return false;
            BalanceRecorder.NoteSearchAction("sell", target, gem);
            m_sales++;
            return true;
        }
    }
}
