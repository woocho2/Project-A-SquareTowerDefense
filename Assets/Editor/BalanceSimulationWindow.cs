using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 밸런스 시뮬레이션 창 (Tools > 밸런스 시뮬레이션) — 설정을 정해 플레이 모드로 여러 판을 돌리고, 끝나면 요약을 만듦
    [InitializeOnLoad]
    public sealed class BalanceSimulationWindow : EditorWindow
    {
        private static readonly string[] s_modeNames = { "씬 그대로", "노말", "하드" };
        private static readonly int[] s_modeValues = { 0, (int)GameMode.Normal, (int)GameMode.Hard };
        private static double s_nextCheck;
        private static int s_progress;
        private static int s_total;

        private int m_runs;
        private int m_firstSeed;
        private int m_modeIndex;
        private bool m_randomEmblems;
        private float m_timeScale;
        private float m_timeout;

        static BalanceSimulationWindow()
        {
            EditorApplication.update += Watch;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/밸런스 시뮬레이션")]
        private static void Open() => GetWindow<BalanceSimulationWindow>("밸런스 시뮬레이션");

        private void OnEnable()
        {
            m_runs = EditorPrefs.GetInt("SquareTD.Sim.Runs", 20);
            m_firstSeed = EditorPrefs.GetInt("SquareTD.Sim.Seed", 1);
            m_modeIndex = EditorPrefs.GetInt("SquareTD.Sim.Mode", 0);
            m_randomEmblems = EditorPrefs.GetBool("SquareTD.Sim.RandomEmblems", false);
            m_timeScale = EditorPrefs.GetFloat("SquareTD.Sim.TimeScale", 100f);
            m_timeout = EditorPrefs.GetFloat("SquareTD.Sim.Timeout", 900f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Stage 씬을 연 상태에서 실행. 돌리는 동안 화면·로그는 꺼짐 (경고·오류만 나옴)", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                m_runs = Mathf.Max(1, EditorGUILayout.IntField("판 수", m_runs));
                m_firstSeed = EditorGUILayout.IntField("시작 시드", m_firstSeed);
                m_modeIndex = EditorGUILayout.Popup("난이도", m_modeIndex, s_modeNames);
                m_randomEmblems = EditorGUILayout.Toggle(new GUIContent("문양 무작위", "끄면 씬의 TowerManager에 고른 문양 그대로. 켜면 판마다 시드로 노말 6개 / 하드 8개를 뽑음"), m_randomEmblems);
                m_timeScale = Mathf.Clamp(EditorGUILayout.FloatField("배속", m_timeScale), 1f, 100f);
                m_timeout = Mathf.Max(30f, EditorGUILayout.FloatField("한 판 제한 (초)", m_timeout));

                if (GUILayout.Button("실행", GUILayout.Height(28))) Run();
            }

            if (EditorApplication.isPlaying && s_total > 0)
            {
                EditorGUILayout.HelpBox($"진행 중: {s_progress} / {s_total}판", MessageType.Info);
                if (GUILayout.Button("멈춤 (지금까지 결과로 요약)")) Stop();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("결과 폴더 열기"))
            {
                Directory.CreateDirectory(SimulationSettings.Folder);
                EditorUtility.RevealInFinder(SimulationSettings.Folder);
            }
            if (GUILayout.Button("마지막 결과로 요약 다시 만들기"))
            {
                SimulationSettings last = SimulationSettings.Load();
                if (last != null && !string.IsNullOrEmpty(last.ResultName)) Summarize(last.ResultName);
            }
        }

        private void Run()
        {
            if (!SceneManager.GetActiveScene().name.Contains("Stage"))
            {
                EditorUtility.DisplayDialog("밸런스 시뮬레이션", "Stage 씬을 연 상태에서 실행해 주세요.", "확인");
                return;
            }

            EditorPrefs.SetInt("SquareTD.Sim.Runs", m_runs);
            EditorPrefs.SetInt("SquareTD.Sim.Seed", m_firstSeed);
            EditorPrefs.SetInt("SquareTD.Sim.Mode", m_modeIndex);
            EditorPrefs.SetBool("SquareTD.Sim.RandomEmblems", m_randomEmblems);
            EditorPrefs.SetFloat("SquareTD.Sim.TimeScale", m_timeScale);
            EditorPrefs.SetFloat("SquareTD.Sim.Timeout", m_timeout);

            var settings = new SimulationSettings
            {
                Active = true,
                Done = false,
                Runs = m_runs,
                FirstSeed = m_firstSeed,
                Mode = s_modeValues[m_modeIndex],
                RandomEmblems = m_randomEmblems,
                TimeScale = m_timeScale,
                GameTimeoutSeconds = m_timeout,
                ResultName = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                Finished = 0,
            };
            settings.Save();
            s_progress = 0;
            s_total = m_runs;
            EditorApplication.isPlaying = true;
        }

        private static void Stop()
        {
            SimulationSettings settings = SimulationSettings.Load();
            if (settings != null)
            {
                settings.Done = true;
                settings.Save();
            }
            EditorApplication.isPlaying = false;
        }

        private static void Watch()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < s_nextCheck) return;
            s_nextCheck = EditorApplication.timeSinceStartup + 0.5;

            SimulationSettings settings = SimulationSettings.Load();
            if (settings == null || !settings.Active) return;

            s_progress = settings.Finished;
            s_total = settings.Runs;
            foreach (BalanceSimulationWindow window in Resources.FindObjectsOfTypeAll<BalanceSimulationWindow>()) window.Repaint();
            if (settings.Done) EditorApplication.isPlaying = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;

            SimulationSettings settings = SimulationSettings.Load();
            if (settings == null || !settings.Active) return;

            settings.Active = false;
            settings.Save();
            Time.timeScale = 1f;
            s_total = 0;
            if (settings.Finished > 0) Summarize(settings.ResultName);
        }

        private static void Summarize(string name)
        {
            string folder = SimulationSettings.Folder;
            string gamesPath = BalanceRecorder.GamesPath(folder, name);
            string towersPath = BalanceRecorder.TowersPath(folder, name);
            if (!File.Exists(gamesPath))
            {
                Debug.LogWarning($"[Simulation] 결과 파일이 없음: {gamesPath}");
                return;
            }

            List<Dictionary<string, string>> games = ReadCsv(gamesPath);
            List<Dictionary<string, string>> towers = File.Exists(towersPath) ? ReadCsv(towersPath) : new List<Dictionary<string, string>>();
            if (games.Count == 0) return;
            if (!games[0].ContainsKey("result") || towers.Count > 0 && !towers[0].ContainsKey("tower_id"))
            {
                Debug.LogError($"[Simulation] 결과 파일 맨 위에 헤더 줄이 없어 요약을 못 만듦 (중간에 파일이 지워졌는지 확인): {gamesPath}");
                return;
            }

            var text = new StringBuilder();
            int count = games.Count;
            int clears = 0, overs = 0, stuck = 0;
            int minWave = int.MaxValue, maxWave = 0;
            float waveSum = 0f, blockedSum = 0f;
            var defeatBuckets = new SortedDictionary<int, int>();
            var reasons = new Dictionary<string, int>();
            var emblemGames = new Dictionary<string, int>();
            var emblemClears = new Dictionary<string, int>();
            var firstSums = new float[7];
            var firstCounts = new int[7];
            var mergeSums = new float[6];

            foreach (Dictionary<string, string> game in games)
            {
                string result = game["result"];
                int wave = Int(game["wave"]);
                waveSum += wave;
                minWave = Mathf.Min(minWave, wave);
                maxWave = Mathf.Max(maxWave, wave);
                blockedSum += Int(game["blocked_waves"]);

                if (result == "Clear") clears++;
                else if (result == "GameOver")
                {
                    overs++;
                    int bucket = (wave - 1) / 5 * 5 + 1;
                    defeatBuckets[bucket] = defeatBuckets.TryGetValue(bucket, out int n) ? n + 1 : 1;
                    string reason = string.IsNullOrEmpty(game["reason"]) ? "(이유 없음)" : game["reason"];
                    reason = reason.Length > 20 ? reason.Substring(0, 20) : reason;
                    reasons[reason] = reasons.TryGetValue(reason, out int r) ? r + 1 : 1;
                }
                else stuck++;

                foreach (string emblem in game["emblems"].Split('|'))
                {
                    if (emblem.Length == 0) continue;
                    emblemGames[emblem] = emblemGames.TryGetValue(emblem, out int g) ? g + 1 : 1;
                    if (result == "Clear") emblemClears[emblem] = emblemClears.TryGetValue(emblem, out int c) ? c + 1 : 1;
                }

                for (int tier = 2; tier <= 6; tier++)
                {
                    int first = Int(game[$"first_t{tier}"]);
                    if (first <= 0) continue;
                    firstSums[tier] += first;
                    firstCounts[tier]++;
                }
                for (int tier = 2; tier <= 5; tier++) mergeSums[tier] += Int(game[$"merge_to{tier}"]);
            }

            text.AppendLine($"밸런스 시뮬레이션 요약 — {name}");
            text.AppendLine($"판 {count} (클리어 {clears}, 게임 오버 {overs}, 멈춤·시간 초과 {stuck}) / 시드 {games[0]["seed"]} ~ {games[count - 1]["seed"]} / 난이도 {games[0]["mode"]}");
            text.AppendLine();
            text.AppendLine("[결과]");
            text.AppendLine($"  승률 {Percent(clears, count)}");
            text.AppendLine($"  도달 웨이브 평균 {waveSum / count:0.0}, 최저 {minWave}, 최고 {maxWave}");
            if (defeatBuckets.Count > 0)
            {
                var buckets = new List<string>();
                foreach (KeyValuePair<int, int> pair in defeatBuckets) buckets.Add($"{pair.Key}~{pair.Key + 4}: {pair.Value}");
                text.AppendLine($"  패배 웨이브 {string.Join(", ", buckets)}");
            }
            foreach (KeyValuePair<string, int> pair in reasons) text.AppendLine($"  게임 오버 이유 \"{pair.Key}\" {pair.Value}판");
            text.AppendLine();

            text.AppendLine("[합성]");
            for (int tier = 2; tier <= 6; tier++)
            {
                string label = tier == 6 ? "시너지" : $"{tier}티어";
                string average = firstCounts[tier] > 0 ? $"{firstSums[tier] / firstCounts[tier]:0.0}웨이브" : "-";
                text.AppendLine($"  {label} 처음 나온 웨이브 평균 {average} (나온 판 {firstCounts[tier]}/{count})");
            }
            text.AppendLine($"  판당 합성 →2 {mergeSums[2] / count:0.0}, →3 {mergeSums[3] / count:0.0}, →4 {mergeSums[4] / count:0.0}, →5 {mergeSums[5] / count:0.0}");
            text.AppendLine($"  칸이 꽉 차 생성을 못 한 웨이브 판당 {blockedSum / count:0.0}");
            text.AppendLine();

            float tierUps = 0f, colorUps = 0f, gemSpent = 0f, gemLeft = 0f, goldLeft = 0f, rerolls = 0f;
            var silent = new List<string>();
            foreach (Dictionary<string, string> game in games)
            {
                tierUps += Int(Cell(game, "gem_tier_ups"));
                colorUps += Int(Cell(game, "gem_color_ups"));
                gemSpent += Int(Cell(game, "gem_spent"));
                rerolls += Int(Cell(game, "rerolls"));
                gemLeft += Int(game["gem"]);
                goldLeft += Int(game["gold"]);
                if (Float(game["total_damage"]) <= 0f)
                    silent.Add($"  시드 {game["seed"]}: {game["wave"]}웨이브, 타워 {game["towers"]}기 중 공격 {Cell(game, "attack_towers")}기 [{Cell(game, "lineup")}]");
            }
            text.AppendLine("[젬·골드]");
            text.AppendLine($"  판당 티어 강화 {tierUps / count:0.0}회, 컬러 강화 {colorUps / count:0.0}회, 쓴 젬 {gemSpent / count:0}");
            text.AppendLine($"  칸이 꽉 차 1티어를 팔고 다시 뽑은 횟수 판당 {rerolls / count:0.0}");
            text.AppendLine($"  판이 끝났을 때 남은 젬 평균 {gemLeft / count:0}, 남은 골드 평균 {goldLeft / count:0}");
            if (silent.Count > 0)
            {
                text.AppendLine($"  피해가 0인 판 {silent.Count}개 (확인 필요)");
                foreach (string line in silent) text.AppendLine(line);
            }
            text.AppendLine();

            AppendTowerShares(text, towers, count);

            if (HasVariedEmblems(games))
            {
                text.AppendLine();
                text.AppendLine("[문양별 승률] (그 문양을 고른 판 중)");
                var emblemKeys = new List<string>(emblemGames.Keys);
                emblemKeys.Sort();
                foreach (string emblem in emblemKeys)
                {
                    int played = emblemGames[emblem];
                    int won = emblemClears.TryGetValue(emblem, out int w) ? w : 0;
                    text.AppendLine($"  {emblem,-10} {Percent(won, played)} ({played}판)");
                }
            }

            string summaryPath = Path.Combine(folder, $"{name}_summary.txt");
            File.WriteAllText(summaryPath, text.ToString(), new UTF8Encoding(false));
            Debug.Log($"[Simulation] 요약: {summaryPath}\n{text}");
        }

        private static void AppendTowerShares(StringBuilder text, List<Dictionary<string, string>> towers, int gameCount)
        {
            var damage = new Dictionary<int, float[]>();
            var games = new Dictionary<int, HashSet<string>>();
            var kills = new Dictionary<int, int>();
            float total = 0f;
            int totalKills = 0;

            foreach (Dictionary<string, string> row in towers)
            {
                int id = Int(row["tower_id"]);
                int kind = TowerId.IsSynergy(id) ? id : id % 1000;
                if (!damage.TryGetValue(kind, out float[] parts)) damage[kind] = parts = new float[4];
                parts[0] += Float(row["basic"]);
                parts[1] += Float(row["skill"]);
                parts[2] += Float(row["ground"]);
                parts[3] += Float(row["other"]);
                total += Float(row["basic"]) + Float(row["skill"]) + Float(row["ground"]) + Float(row["other"]);

                if (!games.TryGetValue(kind, out HashSet<string> runs)) games[kind] = runs = new HashSet<string>();
                runs.Add(row["run"]);
                int k = Int(row["kills"]);
                kills[kind] = kills.TryGetValue(kind, out int before) ? before + k : k;
                totalKills += k;
            }
            if (damage.Count == 0 || total <= 0f) return;

            var kinds = new List<int>(damage.Keys);
            float averagePerGame = 0f;
            foreach (int kind in kinds) averagePerGame += Sum(damage[kind]) / games[kind].Count;
            averagePerGame /= kinds.Count;
            kinds.Sort((a, b) => Sum(damage[b]).CompareTo(Sum(damage[a])));

            text.AppendLine("[타워 종류별 피해 몫] (종류 = 컬러 + 문양, 티어 합침)");
            text.AppendLine("  표시: ▲ 나온 판 기준 판당 피해가 평균의 2배 이상, ▼ 절반 이하");
            text.AppendLine("  순위 이름              몫     판당 피해  기본/스킬/장판/기타      처치 몫  나온 판");
            for (int i = 0; i < kinds.Count; i++)
            {
                int kind = kinds[i];
                float[] parts = damage[kind];
                float sum = Sum(parts);
                int played = games[kind].Count;
                float perGame = sum / played;
                string mark = perGame >= averagePerGame * 2f ? "▲" : perGame <= averagePerGame * 0.5f ? "▼" : " ";
                string name = TowerId.IsSynergy(kind) ? $"시너지 {kind}" : EmblemNames.GetConceptName(kind);
                string split = $"{parts[0] / sum * 100f:0}/{parts[1] / sum * 100f:0}/{parts[2] / sum * 100f:0}/{parts[3] / sum * 100f:0}";
                string killShare = totalKills > 0 ? $"{(kills.TryGetValue(kind, out int k) ? k : 0) * 100f / totalKills:0.0}%" : "-";
                text.AppendLine($"  {i + 1,3} {mark}{name,-14} {sum / total * 100f,5:0.0}%  {perGame,10:0}  {split,-20}  {killShare,7}  {played}/{gameCount}");
            }
        }

        private static bool HasVariedEmblems(List<Dictionary<string, string>> games)
        {
            for (int i = 1; i < games.Count; i++) if (games[i]["emblems"] != games[0]["emblems"]) return true;
            return false;
        }

        private static List<Dictionary<string, string>> ReadCsv(string path)
        {
            var rows = new List<Dictionary<string, string>>();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return rows;

            string[] header = lines[0].Split(',');
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cells = lines[i].Split(',');
                var row = new Dictionary<string, string>();
                for (int c = 0; c < header.Length; c++) row[header[c]] = c < cells.Length ? cells[c] : "";
                rows.Add(row);
            }
            return rows;
        }

        private static string Cell(Dictionary<string, string> row, string key) => row.TryGetValue(key, out string value) ? value : "";
        private static float Sum(float[] parts) => parts[0] + parts[1] + parts[2] + parts[3];
        private static int Int(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
        private static float Float(string text) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        private static string Percent(int part, int whole) => whole > 0 ? $"{part * 100f / whole:0.0}% ({part}/{whole})" : "-";
    }
}
