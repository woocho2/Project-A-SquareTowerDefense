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
        private bool m_compareMergeCounts;
        private int m_mergeCountA;
        private int m_mergeCountB;
        private string m_resultText = "";
        private string m_resultPath = "";
        private Vector2 m_resultScroll;

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
            m_compareMergeCounts = EditorPrefs.GetBool("SquareTD.Sim.CompareMergeCounts", true);
            m_mergeCountA = EditorPrefs.GetInt("SquareTD.Sim.MergeCountA", 3);
            m_mergeCountB = EditorPrefs.GetInt("SquareTD.Sim.MergeCountB", 2);
            minSize = new Vector2(400f, 600f);
            RefreshResults();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Stage 씬을 연 상태에서 실행. 돌리는 동안 화면·로그는 꺼짐 (경고·오류만 나옴)", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                m_compareMergeCounts = EditorGUILayout.Toggle("합성 A/B 비교", m_compareMergeCounts);
                m_runs = Mathf.Clamp(EditorGUILayout.IntField(m_compareMergeCounts ? "조건당 판 수" : "판 수", m_runs), 1, 10000);
                if (m_compareMergeCounts)
                {
                    m_mergeCountA = EditorGUILayout.IntSlider("A 합성 필요 수", m_mergeCountA, 2, 5);
                    m_mergeCountB = EditorGUILayout.IntSlider("B 합성 필요 수", m_mergeCountB, 2, 5);
                    EditorGUILayout.HelpBox($"같은 시드로 A → B를 반복. 각 {m_runs}판, 총 {m_runs * 2}판. 일반 플레이의 기본 합성은 3기 유지.", MessageType.Info);
                }
                m_firstSeed = EditorGUILayout.IntField("시작 시드", m_firstSeed);
                m_modeIndex = EditorGUILayout.Popup("난이도", m_modeIndex, s_modeNames);
                m_randomEmblems = EditorGUILayout.Toggle(new GUIContent("문양 무작위", "끄면 씬의 TowerManager에 고른 문양 그대로. 켜면 판마다 시드로 노말 6개 / 하드 8개를 뽑음"), m_randomEmblems);
                EditorGUILayout.LabelField("기본 배치", "맵 → 버프 → 디버프");
                m_timeScale = Mathf.Clamp(EditorGUILayout.FloatField("배속", m_timeScale), 1f, 100f);
                m_timeout = Mathf.Max(30f, EditorGUILayout.FloatField("한 판 제한 (초)", m_timeout));

                if (GUILayout.Button(m_compareMergeCounts ? "A/B 실행" : "실행", GUILayout.Height(28))) Run();
                if (GUILayout.Button("게임 전략 탐색 (500후보 · 최대10회)", GUILayout.Height(26))) Run(true);
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
            if (GUILayout.Button("현재/마지막 결과 보기"))
            {
                SimulationSettings last = SimulationSettings.Load();
                if (last != null && last.Finished > 0 && !string.IsNullOrEmpty(last.ResultName)) CreateSummaries(last);
                RefreshResults();
            }
            DrawResults();
        }


        private void RefreshResults()
        {
            m_resultText = "";
            m_resultPath = "";
            SimulationSettings last = SimulationSettings.Load();
            if (last != null && !string.IsNullOrEmpty(last.ResultName))
            {
                string suffix = last.SearchStrategies ? "_strategy_summary.md" : last.IsPlacementStudy ? "_placement_summary.md" : last.CompareDeckModes ? "_dpt_summary.txt" : last.CompareMergeCounts ? "_comparison.txt" : "_summary.txt";
                m_resultPath = Path.Combine(SimulationSettings.Folder, last.ResultName + suffix);
            }
            else if (Directory.Exists(SimulationSettings.Folder))
            {
                foreach (string path in Directory.GetFiles(SimulationSettings.Folder, "*.txt", SearchOption.TopDirectoryOnly))
                {
                    if (!path.EndsWith("_comparison.txt", StringComparison.Ordinal) &&
                        !path.EndsWith("_summary.txt", StringComparison.Ordinal)) continue;
                    if (string.IsNullOrEmpty(m_resultPath) ||
                        File.GetLastWriteTimeUtc(path) > File.GetLastWriteTimeUtc(m_resultPath))
                        m_resultPath = path;
                }
            }
            if (!string.IsNullOrEmpty(m_resultPath) && File.Exists(m_resultPath))
                m_resultText = File.ReadAllText(m_resultPath, Encoding.UTF8);
        }

        private void DrawResults()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("시뮬레이션 결과", EditorStyles.boldLabel);
            if (string.IsNullOrEmpty(m_resultText))
            {
                EditorGUILayout.HelpBox("아직 표시할 결과가 없습니다. 실행 후 '현재/마지막 결과 보기'를 누르면 완료된 판의 결과를 볼 수 있습니다. 실행이 끝나면 자동으로 표시됩니다.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField(Path.GetFileName(m_resultPath), EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("결과 복사")) EditorGUIUtility.systemCopyBuffer = m_resultText;
            m_resultScroll = EditorGUILayout.BeginScrollView(m_resultScroll, GUILayout.MinHeight(150f), GUILayout.ExpandHeight(true));
            float height = Mathf.Max(150f, EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(m_resultText), Mathf.Max(100f, position.width - 45f)));
            EditorGUILayout.SelectableLabel(m_resultText, EditorStyles.wordWrappedLabel, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();
        }

        public static void RunBatch()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("RunBatch는 -batchmode로 실행해 주세요.");

            string[] args = Environment.GetCommandLineArgs();
            bool searchSmoke = Array.IndexOf(args, "-simStrategySmoke") >= 0;
            bool search = searchSmoke || Array.IndexOf(args, "-simStrategySearch") >= 0;
            bool compareDecks = Array.IndexOf(args, "-simDeckRanking") >= 0;
            bool placement = Array.IndexOf(args, "-simPlacementRanking") >= 0;
            bool smoke = Array.IndexOf(args, "-simPlacementSmoke") >= 0;
            if (search && (placement || smoke || compareDecks)) throw new ArgumentException("전략 탐색은 다른 비교 모드와 따로 실행해 주세요.");
            if ((placement || smoke) && compareDecks) throw new ArgumentException("배치 비교와 덱 비교는 따로 실행해 주세요.");
            var settings = new SimulationSettings
            {
                Active = true,
                Runs = BatchInt(args, "-simRuns", 20),
                FirstSeed = BatchInt(args, "-simSeed", 1),
                CompareMergeCounts = !compareDecks && !placement && !smoke && !search,
                SearchStrategies = search,
                SearchSmoke = searchSmoke,
                SearchCandidates = BatchInt(args, "-simCandidates", searchSmoke ? 2 : 500),
                SearchRepeats = BatchInt(args, "-simRepeats", 10),
                SearchMinRepeats = BatchInt(args, "-simMinRepeats", 5),
                CompareDeckModes = compareDecks,
                PlacementProfiles = smoke ? new[] { 0, 0, 1, 1 } : placement ? new[] { 0, 1, 2, 3, 4, 5, 6 } : new int[0],
                PlacementProfile = BatchInt(args, "-simPlacementProfile", AutoPlanner.DefaultPlacementProfile),
                NormalDeckCount = 4,
                HardDeckCount = 6,
                MergeCountA = BatchInt(args, "-simMergeA", 3),
                MergeCountB = BatchInt(args, "-simMergeB", 2),
                Mode = s_modeValues[Mathf.Clamp(EditorPrefs.GetInt("SquareTD.Sim.Mode", 0), 0, s_modeValues.Length - 1)],
                RandomEmblems = compareDecks || EditorPrefs.GetBool("SquareTD.Sim.RandomEmblems", false),
                TimeScale = Mathf.Clamp(EditorPrefs.GetFloat("SquareTD.Sim.TimeScale", 100f), 1f, 100f),
                GameTimeoutSeconds = Mathf.Max(30f, EditorPrefs.GetFloat("SquareTD.Sim.Timeout", 900f)),
                ResultName = (searchSmoke ? "strategy_smoke_" : search ? "strategy_" : placement ? "placement_" : smoke ? "placement_smoke_" : compareDecks ? "deck_" : "batch_") + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),
            };
            if (settings.SearchCandidates < 1 || settings.SearchCandidates > 10000 || settings.SearchRepeats < 1 ||
                settings.SearchRepeats > 100 || settings.SearchMinRepeats < 1 || settings.SearchMinRepeats > settings.SearchRepeats)
                throw new ArgumentOutOfRangeException("전략 탐색", "후보1~10000개, 반복1~100회, 최소 반복은 최대 반복 이하로 입력해 주세요.");
            if (settings.PlacementProfile < 0 || settings.PlacementProfile > 6)
                throw new ArgumentOutOfRangeException("-simPlacementProfile", "배치 정책은 0~6이어야 합니다.");
            if (settings.Runs < 1 || settings.Runs > 10000)
                throw new ArgumentOutOfRangeException("-simRuns", "조건당 판 수는 1~10000이어야 합니다.");
            if (settings.MergeCountA < 2 || settings.MergeCountA > 5 ||
                settings.MergeCountB < 2 || settings.MergeCountB > 5)
                throw new ArgumentOutOfRangeException("-simMergeA/-simMergeB", "합성 필요 수는 2~5여야 합니다.");

            using (var sha = System.Security.Cryptography.SHA256.Create())
                settings.CsvSha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources", "TowerData.csv")))).Replace("-", "").ToLowerInvariant();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity");
            settings.Save();
            File.WriteAllText(Path.Combine(SimulationSettings.Folder, settings.ResultName + "_settings.json"), JsonUtility.ToJson(settings, true));
            s_progress = 0;
            s_total = settings.TotalRuns;
            Debug.Log(settings.SearchStrategies
                ? $"[Simulation] 게임 전략 탐색: 초기 시드 {settings.FirstSeed}, 후보 {settings.SearchCandidates}개 × 최대 {settings.SearchRepeats}회."
                : settings.IsPlacementStudy
                ? $"[Simulation] 활·창·얼음·불 배치 비교: 전략 {settings.ScenarioCount}개 × {settings.Runs}판. 결과: {SimulationSettings.Folder}"
                : compareDecks
                ? $"[Simulation] 덱 화력 테스트: 노말4/하드6, 각 {settings.Runs}판. 결과: {SimulationSettings.Folder}"
                : $"[Simulation] 배치 시작: A {settings.MergeCountA}기 / B {settings.MergeCountB}기, 각 {settings.Runs}판. 결과: {SimulationSettings.Folder}");
            EditorApplication.isPlaying = true;
        }

        private static int BatchInt(string[] args, string option, int fallback)
        {
            int index = Array.IndexOf(args, option);
            if (index < 0) return fallback;
            if (index + 1 >= args.Length ||
                !int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new ArgumentException(option + " 뒤에 정수를 입력해 주세요.");
            return value;
        }

        private void Run(bool search = false)
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
            EditorPrefs.SetBool("SquareTD.Sim.CompareMergeCounts", m_compareMergeCounts);
            EditorPrefs.SetInt("SquareTD.Sim.MergeCountA", m_mergeCountA);
            EditorPrefs.SetInt("SquareTD.Sim.MergeCountB", m_mergeCountB);

            var settings = new SimulationSettings
            {
                Active = true,
                Done = false,
                Runs = m_runs,
                CompareMergeCounts = !search && m_compareMergeCounts,
                SearchStrategies = search,
                MergeCountA = m_mergeCountA,
                MergeCountB = m_mergeCountB,
                FirstSeed = m_firstSeed,
                Mode = s_modeValues[m_modeIndex],
                RandomEmblems = m_randomEmblems,
                TimeScale = m_timeScale,
                GameTimeoutSeconds = m_timeout,
                ResultName = (search ? "strategy_" : "") + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"),
                Finished = 0,
            };
            settings.Save();
            File.WriteAllText(Path.Combine(SimulationSettings.Folder, settings.ResultName + "_settings.json"), JsonUtility.ToJson(settings, true));
            s_progress = 0;
            s_total = settings.TotalRuns;
            RefreshResults();
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
            s_total = settings.TotalRuns;
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
            try
            {
                if (settings.Finished > 0) CreateSummaries(settings);
                foreach (BalanceSimulationWindow window in Resources.FindObjectsOfTypeAll<BalanceSimulationWindow>())
                {
                    window.RefreshResults();
                    window.Repaint();
                }
            }
            catch (Exception exception)
            {
                if (!Application.isBatchMode) throw;
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }
            if (Application.isBatchMode)
                EditorApplication.Exit(settings.Done && (settings.SearchStrategies ? settings.SearchComplete && !settings.SearchFailed : settings.Finished >= settings.TotalRuns) ? 0 : 2);
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

        private sealed class DptRecord
        {
            public int Id;
            public int Turns;
            public readonly float[] Damage = new float[4];
            public readonly HashSet<string> Games = new HashSet<string>();
            public float Total => Sum(Damage);
            public float Dpt => Turns > 0 ? Total / Turns : 0f;
        }

        private static void SummarizeDeckRanking(SimulationSettings settings)
        {
            string folder = SimulationSettings.Folder;
            var text = new StringBuilder();
            var csv = new StringBuilder("mode,tier,rank,tower_id,name,games,tower_turns,basic,skill,ground,other,total,dpt,basic_dpt,skill_dpt,ground_dpt,other_dpt\n");
            text.AppendLine("공격 타워 실전 화력 순위 — " + settings.ResultName);
            text.AppendLine($"노말 {settings.NormalDeckCount}문양 / 하드 {settings.HardDeckCount}문양, 모드당 {settings.Runs}판, 합성 {TowerProgression.MergeCount}기");
            text.AppendLine("단위: 타워 1기당·적 턴당 피해(DPT). 연출·배속에 좌우되는 초당 피해가 아님.");
            text.AppendLine("기본·스킬·장판·기타 피해의 합 / 타워가 배치된 적 턴 수. 공격 대기와 사거리 밖 대기도 포함.");
            text.AppendLine("Clear/GameOver 판만 사용. 등장하지 않은 타워는 순위에서 제외. 서로 다른 티어·덱·배치가 섞인 실전 표본.");
            text.AppendLine("기본 공격 횟수·스킬 계수·보조 타워의 영향이 포함됨. Power·AbilityValue는 현재 임시값.");
            text.AppendLine("같은 시드는 시작 상태를 맞추며, 연출 난수와 프레임 차이까지 고정하는 재현성은 아직 미확보.");
            text.AppendLine("동일 티어 비교는 CSV의 tier=1~5, 전체 티어 합산은 tier=0. 즉사량은 피해로 환산하지 않음.");
            text.AppendLine("TowerData SHA256: " + settings.CsvSha256);
            foreach (string mode in new[] { "Normal", "Hard" })
            {
                string resultName = settings.ResultName + "_" + mode;
                string gamesPath = BalanceRecorder.GamesPath(folder, resultName);
                string towersPath = BalanceRecorder.TowersPath(folder, resultName);
                var eligible = new HashSet<string>();
                int games = 0, completed = 0, clear = 0;
                float waves = 0f;
                if (File.Exists(gamesPath))
                    foreach (var row in ReadCsv(gamesPath))
                    {
                        games++;
                        if (!Completed(row)) continue;
                        if (Cell(row, "mode") != mode) throw new InvalidDataException("난이도가 다른 화력 기록: " + gamesPath);
                        int deckCount = Cell(row, "emblems").Split('|').Length;
                        if (deckCount != (mode == "Normal" ? settings.NormalDeckCount : settings.HardDeckCount))
                            throw new InvalidDataException("문양 수가 다른 화력 기록: " + gamesPath);
                        string key = Cell(row, "run") + "|" + Cell(row, "seed");
                        if (!eligible.Add(key)) throw new InvalidDataException("중복된 판의 화력 기록: " + gamesPath);
                        completed++;
                        if (Cell(row, "result") == "Clear") clear++;
                        waves += Float(Cell(row, "wave"));
                    }
                var all = new Dictionary<int, DptRecord>();
                var byTier = new Dictionary<int, DptRecord>();
                if (File.Exists(towersPath))
                    foreach (var row in ReadCsv(towersPath))
                    {
                        string key = Cell(row, "run") + "|" + Cell(row, "seed");
                        if (!eligible.Contains(key)) continue;
                        int id = Int(Cell(row, "tower_id"));
                        if (TowerId.IsSynergy(id)) continue;
                        TowerColor color = TowerId.GetColor(id);
                        if (color != TowerColor.Red && color != TowerColor.Blue) continue;
                        if (!row.TryGetValue("tower_turns", out string turnsText) ||
                            !int.TryParse(turnsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int turns) || turns < 0)
                            throw new InvalidDataException("배치 기간이 없는 화력 기록: " + towersPath);
                        AddDpt(all, 1000 + id % 1000, key, turns, row);
                        AddDpt(byTier, id, key, turns, row);
                    }
                text.AppendLine();
                text.AppendLine($"[{mode}] 완료 {completed}/{settings.Runs}판, 제외 {games - completed}판, 클리어 {Percent(clear, completed)}, 평균 웨이브 {(completed > 0 ? (waves / completed).ToString("0.##", CultureInfo.InvariantCulture) : "-")}");
                text.AppendLine("순위 이름 / 방식 / 턴당 합계 / 기본 / 스킬 / 장판 / 기타 / 출전 판 / 기수×적턴");
                AppendDptRanking(text, csv, mode, 0, all.Values);
                for (int tier = 1; tier <= 5; tier++)
                {
                    var rows = new List<DptRecord>();
                    foreach (DptRecord row in byTier.Values) if (TowerId.GetTier(row.Id) == tier) rows.Add(row);
                    AppendDptRanking(null, csv, mode, tier, rows);
                }
            }
            File.WriteAllText(Path.Combine(folder, settings.ResultName + "_dpt_ranking.csv"), csv.ToString(), new UTF8Encoding(false));
            string summaryPath = Path.Combine(folder, settings.ResultName + "_dpt_summary.txt");
            File.WriteAllText(summaryPath, text.ToString(), new UTF8Encoding(false));
            Debug.Log("[Simulation] 턴당 화력 순위: " + summaryPath);
        }

        private static void AddDpt(Dictionary<int, DptRecord> records, int id, string game, int turns, Dictionary<string, string> row)
        {
            if (!records.TryGetValue(id, out DptRecord record))
            {
                record = new DptRecord { Id = id };
                records.Add(id, record);
            }
            record.Turns += turns;
            record.Games.Add(game);
            record.Damage[0] += Float(Cell(row, "basic"));
            record.Damage[1] += Float(Cell(row, "skill"));
            record.Damage[2] += Float(Cell(row, "ground"));
            record.Damage[3] += Float(Cell(row, "other"));
        }

        private static void AppendDptRanking(StringBuilder text, StringBuilder csv, string mode, int tier, IEnumerable<DptRecord> source)
        {
            var rows = new List<DptRecord>();
            foreach (DptRecord row in source) if (row.Turns > 0) rows.Add(row);
            rows.Sort((a, b) => a.Dpt != b.Dpt ? b.Dpt.CompareTo(a.Dpt) : a.Id.CompareTo(b.Id));
            for (int i = 0; i < rows.Count; i++)
            {
                DptRecord row = rows[i];
                string name = EmblemNames.GetConceptName(row.Id);
                string type = TowerId.GetColor(row.Id) == TowerColor.Red ? "광역" : "타겟";
                if (text != null)
                    text.AppendLine($"{i + 1,2}. {name} / {type} / {DptNumber(row.Dpt)} / {DptNumber(row.Damage[0] / row.Turns)} / {DptNumber(row.Damage[1] / row.Turns)} / {DptNumber(row.Damage[2] / row.Turns)} / {DptNumber(row.Damage[3] / row.Turns)} / {row.Games.Count} / {row.Turns}");
                var cells = new List<string> { mode, tier.ToString(CultureInfo.InvariantCulture),
                    (i + 1).ToString(CultureInfo.InvariantCulture), row.Id.ToString(CultureInfo.InvariantCulture),
                    name, row.Games.Count.ToString(CultureInfo.InvariantCulture), row.Turns.ToString(CultureInfo.InvariantCulture) };
                foreach (float part in row.Damage) cells.Add(DptNumber(part));
                cells.Add(DptNumber(row.Total));
                cells.Add(DptNumber(row.Dpt));
                foreach (float part in row.Damage) cells.Add(DptNumber(part / row.Turns));
                csv.AppendLine(string.Join(",", cells));
            }
        }

        private static string DptNumber(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static void CreateSummaries(SimulationSettings settings)
        {
            if (settings.SearchStrategies)
            {
                string progressPath = Path.Combine(SimulationSettings.Folder, settings.ResultName + "_strategy_search.json");
                if (File.Exists(progressPath))
                    StrategySearch.WriteSummary(settings, JsonUtility.FromJson<StrategyProgress>(File.ReadAllText(progressPath)));
                return;
            }
            if (settings.IsPlacementStudy)
            {
                for (int profile = 0; profile < settings.ScenarioCount; profile++)
                    if (File.Exists(BalanceRecorder.GamesPath(SimulationSettings.Folder, settings.GetResultName(profile))))
                        Summarize(settings.GetResultName(profile));
                SummarizePlacement(settings);
                return;
            }
            if (settings.CompareDeckModes)
            {
                foreach (string suffix in new[] { "_Normal", "_Hard" })
                    if (File.Exists(BalanceRecorder.GamesPath(SimulationSettings.Folder, settings.ResultName + suffix)))
                        Summarize(settings.ResultName + suffix);
                SummarizeDeckRanking(settings);
                return;
            }
            if (!settings.CompareMergeCounts)
            {
                Summarize(settings.ResultName);
                return;
            }
            foreach (string suffix in new[] { "_A", "_B" })
                if (File.Exists(BalanceRecorder.GamesPath(SimulationSettings.Folder, settings.ResultName + suffix)))
                    Summarize(settings.ResultName + suffix);
            SummarizeComparison(settings);
        }


        private static void SummarizePlacement(SimulationSettings settings)
        {
            string folder = SimulationSettings.Folder;
            int count = settings.ScenarioCount;
            var games = new Dictionary<int, Dictionary<string, string>>[count];
            for (int profile = 0; profile < count; profile++)
                games[profile] = BySeed(BalanceRecorder.GamesPath(folder, settings.GetResultName(profile)));
            var seeds = new List<int>();
            string deck = string.Join("|", Array.ConvertAll(settings.FixedDeck, id => ((TowerEmblem)id).ToString()));
            for (int run = 0; run < settings.Runs; run++)
            {
                int seed = unchecked(settings.FirstSeed + run);
                bool valid = true;
                for (int profile = 0; profile < count; profile++)
                    if (!games[profile].TryGetValue(seed, out Dictionary<string, string> row) ||
                        !Completed(row) || Cell(row, "mode") != "Normal" || Cell(row, "emblems") != deck ||
                        Int(Cell(row, "merge_count")) != TowerProgression.MergeCount)
                        valid = false;
                if (valid) seeds.Add(seed);
            }
            var totals = new double[count, 5];
            var paired = new StringBuilder("seed,variant,enemy_turns,wave,kills,total_damage,turn_delta_from_baseline,wave_delta_from_baseline,kills_delta_from_baseline\n");
            foreach (int seed in seeds)
            {
                Dictionary<string, string> baseline = games[0][seed];
                for (int profile = 0; profile < count; profile++)
                {
                    Dictionary<string, string> row = games[profile][seed];
                    totals[profile, 0] += Int(Cell(row, "enemy_turns"));
                    totals[profile, 1] += Int(Cell(row, "wave"));
                    totals[profile, 2] += Int(Cell(row, "kills"));
                    totals[profile, 3] += Float(Cell(row, "total_damage"));
                    if (Cell(row, "result") == "Clear") totals[profile, 4]++;
                    paired.Append(seed).Append(',').Append(settings.GetVariant(profile)).Append(',')
                        .Append(Cell(row, "enemy_turns")).Append(',').Append(Cell(row, "wave")).Append(',')
                        .Append(Cell(row, "kills")).Append(',').Append(Cell(row, "total_damage")).Append(',')
                        .Append(Int(Cell(row, "enemy_turns")) - Int(Cell(baseline, "enemy_turns"))).Append(',')
                        .Append(Int(Cell(row, "wave")) - Int(Cell(baseline, "wave"))).Append(',')
                        .Append(Int(Cell(row, "kills")) - Int(Cell(baseline, "kills"))).Append('\n');
                }
            }
            var order = new List<int>();
            for (int i = 0; i < count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                for (int metric = 0; metric < 3; metric++)
                {
                    int comparison = totals[b, metric].CompareTo(totals[a, metric]);
                    if (comparison != 0) return comparison;
                }
                return a.CompareTo(b);
            });
            var text = new StringBuilder();
            text.AppendLine("# 활·창·얼음·불 배치 우선순위 비교");
            text.AppendLine();
            text.AppendLine($"노말 / 합성 {TowerProgression.MergeCount}기 / 전략당 요청 {settings.Runs}판 / 공통 완료 시드 {seeds.Count}개");
            text.AppendLine("정렬: 평균 생존 적 턴 → 평균 도달 웨이브 → 평균 처치 수. 1·2·3순위 가중치 4:2:1.");
            text.AppendLine("스탯·재화·합성·강화·판매 정책은 고정. 맵 시드·시간 간격·장식 난수 통제는 이번 배치 실험에만 적용.");
            text.AppendLine("맵별 휴리스틱 배치 전략 비교이며 전체 가능한 배치의 전역 최적해를 뜻하지 않음.");
            text.AppendLine($"고정 게임 시간 간격 {settings.FixedGameStep:0.######}초. 같은 전략 반복은 재현성 확인, 전략이 바뀌면 이후 난수 소비 순서는 달라질 수 있음.");
            text.AppendLine($"CSV SHA256: {settings.CsvSha256}");
            text.AppendLine();
            text.AppendLine("| 순위 | 전략 | 평균 생존 턴 | 평균 웨이브 | 평균 처치 | 평균 피해 | 클리어 | 기존 대비 생존 승/무/패 |");
            text.AppendLine("|---:|---|---:|---:|---:|---:|---:|---|");
            var csv = new StringBuilder("rank,variant,profile,valid_seeds,mean_enemy_turns,mean_wave,mean_kills,mean_damage,clears,paired_better,paired_equal,paired_worse\n");
            int rank = 0;
            foreach (int profile in order)
            {
                rank++;
                int better = 0, equal = 0, worse = 0;
                foreach (int seed in seeds)
                {
                    int delta = Int(Cell(games[profile][seed], "enemy_turns")) - Int(Cell(games[0][seed], "enemy_turns"));
                    if (delta > 0) better++; else if (delta < 0) worse++; else equal++;
                }
                string[] means = new string[4];
                for (int metric = 0; metric < 4; metric++)
                    means[metric] = seeds.Count == 0 ? "-" : (totals[profile, metric] / seeds.Count).ToString("0.###", CultureInfo.InvariantCulture);
                string name = settings.GetVariant(profile);
                text.AppendLine($"| {rank} | {name} | {means[0]} | {means[1]} | {means[2]} | {means[3]} | {totals[profile, 4]}/{seeds.Count} | {better}/{equal}/{worse} |");
                csv.AppendLine($"{rank},{name},{settings.GetPlacementProfile(profile)},{seeds.Count},{string.Join(",", means)},{totals[profile, 4]},{better},{equal},{worse}");
            }
            string prefix = Path.Combine(folder, settings.ResultName);
            File.WriteAllText(prefix + "_placement_summary.md", text.ToString(), Encoding.UTF8);
            File.WriteAllText(prefix + "_placement_ranking.csv", csv.ToString(), Encoding.UTF8);
            File.WriteAllText(prefix + "_placement_pairs.csv", paired.ToString(), Encoding.UTF8);
            Debug.Log("[Simulation] 배치 비교 요약: " + prefix + "_placement_summary.md");
        }

        private static Dictionary<int, Dictionary<string, string>> BySeed(string path)
        {
            var result = new Dictionary<int, Dictionary<string, string>>();
            if (!File.Exists(path)) return result;
            foreach (Dictionary<string, string> row in ReadCsv(path))
            {
                if (!row.TryGetValue("seed", out string seedText) ||
                    !int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
                    throw new InvalidDataException("시드가 없는 비교 결과: " + path);
                if (result.ContainsKey(seed)) throw new InvalidDataException("같은 시드가 중복된 비교 결과: " + path);
                result.Add(seed, row);
            }
            return result;
        }

        private static bool Completed(Dictionary<string, string> row) =>
            row != null && (Cell(row, "result") == "Clear" || Cell(row, "result") == "GameOver");

        private static void SummarizeComparison(SimulationSettings settings)
        {
            string folder = SimulationSettings.Folder;
            var a = BySeed(BalanceRecorder.GamesPath(folder, settings.ResultName + "_A"));
            var b = BySeed(BalanceRecorder.GamesPath(folder, settings.ResultName + "_B"));
            var seeds = new SortedSet<int>(a.Keys);
            seeds.UnionWith(b.Keys);
            var pairedA = new List<Dictionary<string, string>>();
            var pairedB = new List<Dictionary<string, string>>();
            var csv = new StringBuilder("seed,valid_pair,a_result,b_result,mode,emblems,a_merge_count,b_merge_count,a_wave,b_wave,a_gold,b_gold,a_gem,b_gem,a_gem_spent,b_gem_spent,a_merge_to2,b_merge_to2,a_merge_to3,b_merge_to3,a_merge_to4,b_merge_to4,a_merge_to5,b_merge_to5,a_first_t2,b_first_t2,a_first_t3,b_first_t3,a_first_t4,b_first_t4,a_first_t5,b_first_t5\n");
            int incomplete = 0, mismatched = 0, unfinished = 0, paired = 0, clearA = 0, clearB = 0;
            foreach (int seed in seeds)
            {
                a.TryGetValue(seed, out Dictionary<string, string> rowA);
                b.TryGetValue(seed, out Dictionary<string, string> rowB);
                bool both = rowA != null && rowB != null;
                bool same = both && Cell(rowA, "mode") == Cell(rowB, "mode") &&
                    Cell(rowA, "emblems") == Cell(rowB, "emblems") &&
                    Int(Cell(rowA, "merge_count")) == settings.GetMergeCount(0) &&
                    Int(Cell(rowB, "merge_count")) == settings.GetMergeCount(1);
                bool valid = same && Completed(rowA) && Completed(rowB);
                if (!both) incomplete++;
                else
                {
                    paired++;
                    if (!same) mismatched++;
                    else if (!valid) unfinished++;
                }
                if (valid)
                {
                    pairedA.Add(rowA);
                    pairedB.Add(rowB);
                    if (Cell(rowA, "result") == "Clear") clearA++;
                    if (Cell(rowB, "result") == "Clear") clearB++;
                }
                var cells = new List<string> { seed.ToString(CultureInfo.InvariantCulture), valid ? "1" : "0",
                    Value(rowA, "result"), Value(rowB, "result"), Value(rowA ?? rowB, "mode"),
                    Value(rowA ?? rowB, "emblems"), Value(rowA, "merge_count"), Value(rowB, "merge_count") };
                foreach (string field in new[] { "wave", "gold", "gem", "gem_spent", "merge_to2", "merge_to3", "merge_to4", "merge_to5", "first_t2", "first_t3", "first_t4", "first_t5" })
                {
                    cells.Add(Value(rowA, field));
                    cells.Add(Value(rowB, field));
                }
                csv.AppendLine(string.Join(",", cells));
            }
            var text = new StringBuilder();
            text.AppendLine($"합성 A/B 비교 — {settings.ResultName}");
            text.AppendLine($"A: 같은 타워 {settings.GetMergeCount(0)}기 / B: 같은 타워 {settings.GetMergeCount(1)}기");
            text.AppendLine($"설정당 {settings.Runs}판 / 실제 기록 A {a.Count}, B {b.Count} / 양쪽 기록 {paired}짝");
            text.AppendLine($"유효 완료 짝 {pairedA.Count} / 한쪽 기록만 {incomplete}, 조건 불일치 {mismatched}, 중지·시간 초과·멈춤 {unfinished}");
            text.AppendLine("아래 평균은 시드·난이도·문양·합성 설정이 맞고 양쪽 모두 클리어/게임 오버로 끝난 짝만 사용.");
            text.AppendLine("같은 초기 시드라도 행동과 합성이 달라지면 이후 난수 소비 순서는 달라질 수 있음.");
            text.AppendLine();
            text.AppendLine($"클리어: A {Percent(clearA, pairedA.Count)} / B {Percent(clearB, pairedB.Count)}");
            foreach (var metric in new[]
            {
                new[] { "wave", "도달 웨이브" }, new[] { "gold", "종료 시 골드" }, new[] { "gem", "종료 시 젬" },
                new[] { "gem_spent", "강화에 쓴 젬" }, new[] { "gem_tier_ups", "젬 티어 강화 횟수" },
                new[] { "gem_color_ups", "컬러 강화 횟수" }, new[] { "rerolls", "판매 후 재소환 횟수" },
                new[] { "blocked_waves", "생성 공간 부족 웨이브" }, new[] { "merge_to2", "2티어 합성 횟수" },
                new[] { "merge_to3", "3티어 합성 횟수" }, new[] { "merge_to4", "4티어 합성 횟수" },
                new[] { "merge_to5", "5티어 합성 횟수" }
            })
                AppendComparisonMetric(text, metric[1], pairedA, pairedB, metric[0], false);
            text.AppendLine();
            text.AppendLine("첫 등장 웨이브: 해당 티어를 얻은 판만 평균, 괄호는 도달 판 수/유효 완료 짝.");
            for (int tier = 2; tier <= 6; tier++)
                AppendComparisonMetric(text, tier == 6 ? "시너지 첫 등장" : $"{tier}티어 첫 등장", pairedA, pairedB, $"first_t{tier}", true);
            File.WriteAllText(Path.Combine(folder, settings.ResultName + "_paired.csv"), csv.ToString(), new UTF8Encoding(false));
            string summaryPath = Path.Combine(folder, settings.ResultName + "_comparison.txt");
            File.WriteAllText(summaryPath, text.ToString(), new UTF8Encoding(false));
            Debug.Log($"[Simulation] A/B 비교: {summaryPath}\n{text}");
        }

        private static string Value(Dictionary<string, string> row, string field) => row != null ? Cell(row, field) : "";

        private static void AppendComparisonMetric(StringBuilder text, string title,
            List<Dictionary<string, string>> a, List<Dictionary<string, string>> b, string field, bool reachedOnly)
        {
            float sumA = 0f, sumB = 0f;
            int countA = 0, countB = 0;
            foreach (var row in a)
            {
                float value = Float(Cell(row, field));
                if (reachedOnly && value <= 0f) continue;
                sumA += value;
                countA++;
            }
            foreach (var row in b)
            {
                float value = Float(Cell(row, field));
                if (reachedOnly && value <= 0f) continue;
                sumB += value;
                countB++;
            }
            string meanA = countA > 0 ? (sumA / countA).ToString("0.##", CultureInfo.InvariantCulture) : "-";
            string meanB = countB > 0 ? (sumB / countB).ToString("0.##", CultureInfo.InvariantCulture) : "-";
            string delta = countA > 0 && countB > 0
                ? (sumB / countB - sumA / countA).ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture) : "-";
            if (reachedOnly)
            {
                meanA += $" ({countA}/{a.Count})";
                meanB += $" ({countB}/{b.Count})";
            }
            text.AppendLine($"  {title}: A {meanA} / B {meanB} / B-A {delta}");
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
