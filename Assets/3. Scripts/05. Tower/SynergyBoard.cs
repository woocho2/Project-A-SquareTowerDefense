using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 시너지 칸의 시너지 타워와 스킬 (매 적 턴 판 위 티어 합만큼 스택, 테스트 모드·시뮬레이션에서는 알아서 씀)
    public class SynergyBoard
    {
        public class Entry
        {
            public SynergyRecipes.Recipe Recipe;
            public TowerController Tower;
            public SynergySkill Skill;
            public int Uses;
        }

        public static bool TestAutoUse { get; set; }
        public static bool AutoUse => TestAutoUse || SimulationRunner.IsOn;

        private readonly BoardManager m_board;
        private readonly List<Entry> m_entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => m_entries;
        public event Action<Entry> Used;

        public SynergyBoard(BoardManager board)
        {
            m_board = board;
        }

        public void Spawn(TowerFactory factory, ICollection<TowerEmblem> deck)
        {
            TowerSlots slots = m_board.SynergySlots;
            if (factory == null || slots == null || slots.Count == 0) return;

            List<SynergyRecipes.Recipe> picked = SynergyRecipes.Pick(deck, slots.Count);
            for (int i = 0; i < picked.Count; i++)
            {
                TowerController tower = factory.Create(TowerFactory.MakeSynergyDefinition(picked[i].Id), i, TowerPlace.Synergy);
                if (tower == null) continue;
                m_entries.Add(new Entry { Recipe = picked[i], Tower = tower, Skill = SynergyEffects.Create(picked[i].Id) });
            }
            Debug.Log($"[Synergy] 시너지 타워 {m_entries.Count}기: {string.Join(", ", m_entries.ConvertAll(entry => entry.Recipe.Name))}");
        }

        public Entry Find(TowerController tower) => m_entries.Find(entry => entry.Tower == tower);

        public int GetTierSum(Entry entry) => SynergyRecipes.TierSum(entry.Recipe, BoardTiers());

        private Func<TowerColor, TowerEmblem, int> BoardTiers() =>
            SynergyRecipes.HighestTiers(m_board.GetTowers(TowerPlace.Board).ConvertAll(tower => tower.Definition));

        private SynergyContext MakeContext(bool playerTurn) => new SynergyContext
        {
            Enemies = EnemyManager.Instance != null ? new List<EnemyController>(EnemyManager.Instance.Registry.Alive) : new List<EnemyController>(),
            Towers = m_board.GetTowers(TowerPlace.Board),
            Occupancy = m_board.Occupancy,
            Grid = m_board.Grid,
            IsPlayerTurn = playerTurn,
        };

        public void OnEnemyTurnEnded()
        {
            if (m_entries.Count == 0) return;
            Func<TowerColor, TowerEmblem, int> tiers = BoardTiers();
            SynergyContext context = MakeContext(false);
            foreach (Entry entry in m_entries) entry.Skill.Tick(context, SynergyRecipes.TierSum(entry.Recipe, tiers));
            if (AutoUse) AutoUseReady(false);
        }

        public void OnPlayerTurnStarted()
        {
            if (AutoUse) AutoUseReady(true);
        }

        public bool TryUse(Entry entry, int pathA = -1, int pathB = -1)
        {
            if (entry == null) return false;
            bool playerTurn = TurnManager.Instance != null && TurnManager.Instance.Current.CanPlayerAct;
            SynergyContext context = MakeContext(playerTurn);
            context.PathA = pathA;
            context.PathB = pathB;
            if (!entry.Skill.TryUse(context)) return false;

            entry.Uses++;
            Debug.Log($"[Synergy] {entry.Recipe.Name} 스킬 발동 ({entry.Uses}번째)");
            Used?.Invoke(entry);
            return true;
        }

        public bool TryBlockMechanic()
        {
            foreach (Entry entry in m_entries)
            {
                if (!(entry.Skill is BaldrSkill baldr) || !baldr.TryBlockMechanic()) continue;
                Debug.Log("[Synergy] 발드르가 보스 기믹을 한 번 막음");
                return true;
            }
            return false;
        }

        public void FillAll()
        {
            foreach (Entry entry in m_entries) entry.Skill.Fill();
        }

        private void AutoUseReady(bool playerTurn)
        {
            foreach (Entry entry in m_entries)
            {
                if (!entry.Skill.IsReady) continue;
                if (!SynergyAutoUse.ShouldUse(entry.Skill.Id, playerTurn, m_board, out int pathA, out int pathB)) continue;
                TryUse(entry, pathA, pathB);
            }
        }
    }

    // 테스트 모드·시뮬레이션용 시너지 스킬 사용 판단
    public static class SynergyAutoUse
    {
        public static bool ShouldUse(int id, bool playerTurn, BoardManager board, out int pathA, out int pathB)
        {
            pathA = -1;
            pathB = -1;
            var alive = EnemyManager.Instance != null ? new List<EnemyController>(EnemyManager.Instance.Registry.Alive) : new List<EnemyController>();
            alive.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
            int normals = alive.FindAll(enemy => !enemy.IsBoss).Count;
            bool boss = alive.Exists(enemy => enemy.IsBoss);
            int limit = GameSession.Instance != null ? GameSession.Instance.CurrentLimit : int.MaxValue;
            bool crowded = alive.Count >= limit * 0.6f;

            switch (id)
            {
                case SynergyEffects.OdinId:
                    return alive.Exists(enemy => enemy.IsBoss && enemy.CurrentHp <= enemy.MaxHp * OdinSkill.BossHpRate) ||
                           alive.FindAll(enemy => !enemy.IsBoss && enemy.CurrentHp <= enemy.MaxHp * OdinSkill.HpRate).Count >= 3;
                case SynergyEffects.ThorId:
                case SynergyEffects.UllrId:
                    return boss || alive.Count >= 3;
                case SynergyEffects.HelId:
                    return normals >= 5 || normals > 0 && crowded;
                case SynergyEffects.SurtrId:
                    return alive.Exists(enemy => enemy.IsBoss && enemy.CurrentHp <= enemy.MaxHp * 0.7f) || normals >= 5;
                case SynergyEffects.FreyId:
                    return alive.Count > 0;
                case SynergyEffects.SkadiId:
                    return boss || alive.Count >= 5;
                case SynergyEffects.YmirId:
                    return boss || crowded;
                case SynergyEffects.FreyjaId:
                    return playerTurn;
                case SynergyEffects.BaldrId:
                case SynergyEffects.BrynhildId:
                case SynergyEffects.BrokkrId:
                    return true;
                case SynergyEffects.LokiId:
                    if (!playerTurn || alive.Count == 0) return false;
                    pathA = MostCrowdedPath(board);
                    pathB = BestCoveredPath(board, pathA);
                    return pathA >= 0 && pathB >= 0 && pathA != pathB;
                case SynergyEffects.NjordId:
                    if (!playerTurn || alive.Count == 0) return false;
                    pathA = BestCoveredPath(board, -1);
                    return pathA >= 0;
                default:
                    return false;
            }
        }

        private static int MostCrowdedPath(BoardManager board)
        {
            int best = -1, most = 0;
            for (int i = 0; i < board.Grid.PathCount; i++)
            {
                int count = board.Occupancy.GetAt(i).Count;
                if (count > most || count == most && count > 0 && Coverage(board, i) < Coverage(board, best))
                {
                    most = count;
                    best = i;
                }
            }
            return best;
        }

        private static int BestCoveredPath(BoardManager board, int except)
        {
            int best = -1;
            float highest = 0f;
            for (int i = 0; i < board.Grid.PathCount; i++)
            {
                if (i == except) continue;
                float value = Coverage(board, i);
                if (value > highest)
                {
                    highest = value;
                    best = i;
                }
            }
            return best;
        }

        private static float Coverage(BoardManager board, int pathIndex)
        {
            if (pathIndex < 0) return 0f;
            float value = 0f;
            Vector3Int cell = board.Grid.GetPathCell(pathIndex);
            for (int i = 0; i < board.Slots.Count; i++)
            {
                TowerController tower = board.Slots.Get(i);
                if (tower == null || !tower.IsAttackTower) continue;
                if (BoardGrid.GetTileDistance(board.Grid.GetSpawnCell(i), cell) <= tower.Stats.Range) value += tower.Definition.Tier;
            }
            return value;
        }
    }
}
