using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SquareTD
{
    // 밸런스 시뮬레이션의 판 기록 — 타워별 피해·처치, 합성, 티어가 처음 나온 웨이브를 모아 판이 끝나면 CSV 두 개에 한 줄씩 씀
    public static class BalanceRecorder
    {
        public const string GamesHeader =
            "run,seed,mode,emblems,result,reason,wave,enemy_turns,alive,limit,gold,gem,towers,t1,t2,t3,t4,t5,t6," +
            "merge_to2,merge_to3,merge_to4,merge_to5,first_t2,first_t3,first_t4,first_t5,first_t6,blocked_waves,kills,total_damage," +
            "gem_tier_ups,gem_color_ups,gem_spent,rerolls,attack_towers,lineup,variant,merge_count,combat_seed";
        public const string TowersHeader = "run,seed,tower_id,name,basic,skill,ground,other,kills,tower_turns";

        private sealed class TowerRecord
        {
            public float Basic;
            public float Skill;
            public float Ground;
            public float Other;
            public int Kills;
            public int TowerTurns;
        }

        private static readonly Dictionary<int, TowerRecord> s_towers = new Dictionary<int, TowerRecord>();
        private static readonly int[] s_firstWave = new int[7];
        private static readonly int[] s_merges = new int[7];
        private static string s_gamesPath;
        private static string s_towersPath;
        private static bool s_recording;
        private static int s_run;
        private static int s_seed;
        private static string s_mode;
        private static string s_emblems;
        private static int s_enemyTurns;
        private static int s_blockedWaves;
        private static int s_kills;
        private static bool s_gemTierUpPending;
        private static int s_gemTierUps;
        private static int s_gemColorUps;
        private static int s_gemSpent;
        private static int s_rerolls;
        private static string s_variant;
        private static int s_mergeCount;
        private static int s_combatSeed;
        public static int EnemyTurns => s_enemyTurns;
        public static int Kills => s_kills;

        public static string GamesPath(string folder, string name) => Path.Combine(folder, $"{name}_games.csv");
        public static string TowersPath(string folder, string name) => Path.Combine(folder, $"{name}_towers.csv");

        public static void Open(string folder, string name)
        {
            Directory.CreateDirectory(folder);
            s_gamesPath = GamesPath(folder, name);
            s_towersPath = TowersPath(folder, name);
            if (!File.Exists(s_gamesPath)) File.WriteAllText(s_gamesPath, GamesHeader + "\n", Encoding.UTF8);
            if (!File.Exists(s_towersPath)) File.WriteAllText(s_towersPath, TowersHeader + "\n", Encoding.UTF8);
        }

        public static void BeginGame(int run, int seed, string mode, string emblems,
            string variant = "", int mergeCount = TowerProgression.MergeCount, int combatSeed = 0)
        {
            s_towers.Clear();
            System.Array.Clear(s_firstWave, 0, s_firstWave.Length);
            System.Array.Clear(s_merges, 0, s_merges.Length);
            s_run = run;
            s_seed = seed;
            s_mode = mode;
            s_emblems = emblems;
            s_variant = variant;
            s_mergeCount = mergeCount;
            s_combatSeed = combatSeed;
            s_enemyTurns = 0;
            s_blockedWaves = 0;
            s_kills = 0;
            s_gemTierUpPending = false;
            s_gemTierUps = 0;
            s_gemColorUps = 0;
            s_gemSpent = 0;
            s_rerolls = 0;
            s_recording = true;

            if (TowerManager.Instance != null)
            {
                TowerManager.Instance.TowerAdded += OnTowerAdded;
                TowerManager.Instance.TowerReplaced += OnTowerReplaced;
            }
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyDied += OnEnemyDied;
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnStarted += OnEnemyTurnStarted;
        }

        public static void OnDamage(DamageInfo info, float dealt)
        {
            if (!s_recording || dealt <= 0f || (object)info.Attacker == null || info.Attacker.Definition == null) return;

            TowerRecord record = Get(info.Attacker.Definition.Id);
            switch (info.Source)
            {
                case DamageSource.Basic: record.Basic += dealt; break;
                case DamageSource.Skill: record.Skill += dealt; break;
                case DamageSource.Ground: record.Ground += dealt; break;
                default: record.Other += dealt; break;
            }
        }

        public static void CountBlockedWave() => s_blockedWaves++;

        public static void BeginGemTierUp() => s_gemTierUpPending = true;

        public static void EndGemTierUp(int cost)
        {
            s_gemTierUpPending = false;
            if (!s_recording || cost <= 0) return;
            s_gemTierUps++;
            s_gemSpent += cost;
        }

        public static void NoteReroll()
        {
            if (s_recording) s_rerolls++;
        }

        public static void NoteGemColorUp(int cost)
        {
            if (!s_recording || cost <= 0) return;
            s_gemColorUps++;
            s_gemSpent += cost;
        }

        public static void EndGame(string result, string reason)
        {
            if (!s_recording) return;
            s_recording = false;

            if (TowerManager.Instance != null)
            {
                TowerManager.Instance.TowerAdded -= OnTowerAdded;
                TowerManager.Instance.TowerReplaced -= OnTowerReplaced;
            }
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyDied -= OnEnemyDied;
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnStarted -= OnEnemyTurnStarted;

            var tiers = new int[7];
            int towerCount = 0;
            int attackCount = 0;
            var lineup = new List<int>();
            TowerSlots slots = BoardManager.Instance != null ? BoardManager.Instance.Slots : null;
            for (int i = 0; slots != null && i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null) continue;
                towerCount++;
                if (tower.IsAttackTower) attackCount++;
                tiers[Mathf.Clamp(tower.Definition.Tier, 0, 6)]++;
                lineup.Add(tower.Definition.Id);
            }
            lineup.Sort();

            float total = 0f;
            foreach (TowerRecord record in s_towers.Values) total += record.Basic + record.Skill + record.Ground + record.Other;

            var line = new List<string>
            {
                Text(s_run), Text(s_seed), s_mode, s_emblems, result, Clean(reason),
                Text(WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0), Text(s_enemyTurns),
                Text(EnemyManager.Instance != null ? EnemyManager.Instance.Registry.Count : 0),
                Text(GameSession.Instance != null ? GameSession.Instance.CurrentLimit : 0),
                Text(CurrencyManager.Instance != null ? CurrencyManager.Instance.Gold : 0),
                Text(CurrencyManager.Instance != null ? CurrencyManager.Instance.Gem : 0),
                Text(towerCount),
            };
            for (int tier = 1; tier <= 6; tier++) line.Add(Text(tiers[tier]));
            for (int tier = 2; tier <= 5; tier++) line.Add(Text(s_merges[tier]));
            for (int tier = 2; tier <= 6; tier++) line.Add(Text(s_firstWave[tier]));
            line.Add(Text(s_blockedWaves));
            line.Add(Text(s_kills));
            line.Add(Text(total));
            line.Add(Text(s_gemTierUps));
            line.Add(Text(s_gemColorUps));
            line.Add(Text(s_gemSpent));
            line.Add(Text(s_rerolls));
            line.Add(Text(attackCount));
            line.Add(string.Join("|", lineup));
            line.Add(Clean(s_variant));
            line.Add(Text(s_mergeCount));
            line.Add(Text(s_combatSeed));
            Append(s_gamesPath, GamesHeader, string.Join(",", line) + "\n");

            var rows = new StringBuilder();
            foreach (KeyValuePair<int, TowerRecord> pair in s_towers)
            {
                TowerRecord record = pair.Value;
                string name = $"{EmblemNames.GetTierName(Mathf.Clamp(TowerId.GetTier(pair.Key), 1, 5))} {EmblemNames.GetConceptName(pair.Key)}";
                if (TowerId.IsSynergy(pair.Key)) name = $"시너지 {pair.Key}";
                rows.Append(string.Join(",", Text(s_run), Text(s_seed), Text(pair.Key), Clean(name),
                    Text(record.Basic), Text(record.Skill), Text(record.Ground), Text(record.Other), Text(record.Kills), Text(record.TowerTurns))).Append('\n');
            }
            Append(s_towersPath, TowersHeader, rows.ToString());
        }



        public static void NoteSearchAction(string action, int target, int value)
        {
            if (!s_recording || StrategySearch.ActivePolicy == null) return;
            const string header = "run,seed,combat_seed,variant,wave,action,target,value";
            string row = string.Join(",", Text(s_run), Text(s_seed), Text(s_combatSeed), s_variant,
                Text(WaveManager.Instance.CurrentWave), action, Text(target), Text(value));
            Append(s_gamesPath.Replace("_games.csv", "_actions.csv"), header, row + "\n");
        }

        public static void RecordPlacement()
        {
            if (!s_recording || !SimulationRunner.SuppressCosmetics) return;
            BoardManager board = BoardManager.Instance;
            if (board == null) return;
            var spawn = new List<string>();
            var path = new List<string>();
            var towers = new List<string>();
            var buffs = new List<string>();
            var debuffs = new List<string>();
            for (int slot = 0; slot < board.Grid.SpawnCount; slot++)
            {
                Vector3Int cell = board.Grid.GetSpawnCell(slot);
                spawn.Add($"{slot}@{board.Tiles.GetSpawnTile(slot)}@{cell.x}:{cell.y}");
                TowerController tower = board.Slots.Get(slot);
                if (tower != null) towers.Add($"{tower.Definition.Id}@{slot}@{tower.Stats.Range}@{tower.Stats.Action}@{tower.Stats.AttackCount}");
            }
            for (int index = 0; index < board.Grid.PathCount; index++)
            {
                Vector3Int cell = board.Grid.GetPathCell(index);
                path.Add($"{index}@{board.Tiles.GetPathTile(index)}@{cell.x}:{cell.y}");
            }
            foreach (Zone zone in board.Zones.All)
            {
                string value = $"{zone.Owner.Definition.Id}@{zone.CellIndex}@{zone.Stage}@{zone.Stack}";
                (zone.IsBuff ? buffs : debuffs).Add(value);
            }
            const string header = "run,seed,wave,enemy_turns,variant,spawn_tiles,path_tiles,towers,buff_zones,debuff_zones,frame_delta,unscaled_delta";
            string file = s_gamesPath.Replace("_games.csv", "_placements.csv");
            string line = string.Join(",", Text(s_run), Text(s_seed),
                Text(WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0),
                Text(s_enemyTurns), Clean(s_variant), string.Join("|", spawn), string.Join("|", path),
                string.Join("|", towers), string.Join("|", buffs), string.Join("|", debuffs),
                Time.deltaTime.ToString("R", CultureInfo.InvariantCulture), Time.unscaledDeltaTime.ToString("R", CultureInfo.InvariantCulture));
            Append(file, header, line + "\n");
        }

        private static void Append(string path, string header, string text)
        {
            if (!File.Exists(path)) File.WriteAllText(path, header + "\n", Encoding.UTF8);
            File.AppendAllText(path, text, Encoding.UTF8);
        }

        private static void OnTowerAdded(TowerController tower)
        {
            NoteTier(tower);
            NoteSearchAction("spawn", tower.Definition.Id, 0);
        }

        private static void OnTowerReplaced(TowerController old, TowerController created)
        {
            if (created == null) return;
            int tier = created.Definition.Tier;
            if (old != null && tier > old.Definition.Tier && tier < s_merges.Length && !s_gemTierUpPending) s_merges[tier]++;
            NoteTier(created);
        }

        private static void NoteTier(TowerController tower)
        {
            if (tower == null || WaveManager.Instance == null) return;
            int tier = Mathf.Clamp(tower.Definition.Tier, 0, 6);
            if (s_firstWave[tier] == 0) s_firstWave[tier] = WaveManager.Instance.CurrentWave;
        }

        private static void OnEnemyDied(KillInfo kill)
        {
            s_kills++;
            TowerController attacker = kill.Damage.Attacker;
            if ((object)attacker != null && attacker.Definition != null) Get(attacker.Definition.Id).Kills++;
        }

        private static void OnEnemyTurnStarted()
        {
            if (!s_recording) return;
            s_enemyTurns++;
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null) Get(tower.Definition.Id).TowerTurns++;
            }
        }


        private static TowerRecord Get(int id)
        {
            if (!s_towers.TryGetValue(id, out TowerRecord record))
            {
                record = new TowerRecord();
                s_towers[id] = record;
            }
            return record;
        }

        private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Text(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Clean(string text) => string.IsNullOrEmpty(text) ? "" : text.Replace(",", " ").Replace("\n", " ").Replace("\r", " ");
    }
}
