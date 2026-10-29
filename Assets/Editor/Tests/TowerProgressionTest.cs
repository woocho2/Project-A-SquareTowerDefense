using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class TowerProgressionTest
    {
        private const string Header =
            "ID,Name,Power,Range,Action,AttackCount,CriticalRate,CriticalDamage,Mastery,Stack,Duration,AbilityValue,ProjectileSpeed,TargetPriority";

        private readonly List<GameObject> m_created = new List<GameObject>();
        private TowerSlots m_slots;

        [SetUp]
        public void SetUp()
        {
            m_slots = new TowerSlots(6);
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (GameObject go in m_created) Object.DestroyImmediate(go);
            m_created.Clear();
        }

        private static TowerDefinition Def(int id) =>
            new TowerDefinition(id, 10f, 2, 3, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);

        private TowerController Place(int id, int slot)
        {
            var go = new GameObject($"Tower {id}");
            m_created.Add(go);
            var tower = go.AddComponent<TowerController>();
            tower.Init(Def(id), slot, null);
            m_slots.TryPlace(tower, slot);
            return tower;
        }

        private static TowerDatabase Database(params int[] ids)
        {
            var lines = new List<string> { Header };
            foreach (int id in ids) lines.Add($"{id},t,10,2,3,1,0,0,1,60,5,2,5,Closest");
            var database = new TowerDatabase();
            database.Load(string.Join("\n", lines));
            return database;
        }

        [Test]
        public void 티어_강화_비용은_5_15_45_135()
        {
            Assert.AreEqual(5, TowerProgression.GetTierUpgradeCost(1));
            Assert.AreEqual(15, TowerProgression.GetTierUpgradeCost(2));
            Assert.AreEqual(45, TowerProgression.GetTierUpgradeCost(3));
            Assert.AreEqual(135, TowerProgression.GetTierUpgradeCost(4));
        }

        [Test]
        public void 다섯_티어와_시너지는_티어_강화_비용_0()
        {
            Assert.AreEqual(0, TowerProgression.GetTierUpgradeCost(5));
            Assert.AreEqual(0, TowerProgression.GetTierUpgradeCost(6));
        }

        [Test]
        public void 일_사_티어만_합성_가능()
        {
            Assert.IsTrue(TowerProgression.CanMergeFrom(Def(1101)));
            Assert.IsTrue(TowerProgression.CanMergeFrom(Def(4214)));
            Assert.IsFalse(TowerProgression.CanMergeFrom(Def(5101)));
            Assert.IsFalse(TowerProgression.CanMergeFrom(Def(6001)));
        }

        private bool Find(out TowerController keep, out List<TowerController> materials, System.Func<TowerController, bool> isBusy = null) =>
            TowerProgression.TryFindMergeGroup(m_slots, isBusy, out keep, out materials);

        [Test]
        public void 같은_타워_셋이면_원래_있던_두_기_중_앞_칸이_남음()
        {
            TowerController first = Place(1101, 3);
            TowerController second = Place(1101, 1);
            TowerController newest = Place(1101, 5);

            Assert.IsTrue(Find(out TowerController keep, out List<TowerController> materials));
            Assert.AreEqual(second, keep, "먼저 있던 3번·1번 칸 중 1번");
            CollectionAssert.AreEquivalent(new[] { first, newest }, materials);
        }

        [Test]
        public void 방금_생긴_타워가_앞_칸이어도_재료가_됨()
        {
            TowerController first = Place(1101, 4);
            TowerController second = Place(1101, 5);
            TowerController newest = Place(1101, 0);

            Assert.IsTrue(Find(out TowerController keep, out List<TowerController> materials));
            Assert.AreEqual(first, keep);
            CollectionAssert.AreEquivalent(new[] { second, newest }, materials);
        }

        [Test]
        public void 둘뿐이면_합성하지_않음()
        {
            Place(1101, 0);
            Place(1101, 1);
            Assert.IsFalse(Find(out _, out _));
        }

        [Test]
        public void 컬러나_문양만_같으면_합성하지_않음()
        {
            Place(1101, 0);
            Place(1102, 1);
            Place(1201, 2);
            Place(2101, 3);
            Assert.IsFalse(Find(out _, out _));
        }

        [Test]
        public void 다섯_티어와_시너지는_합성하지_않음()
        {
            Place(5101, 0);
            Place(5101, 1);
            Place(5101, 2);
            Place(6001, 3);
            Place(6001, 4);
            Place(6001, 5);
            Assert.IsFalse(Find(out _, out _));
        }

        [Test]
        public void 넷이면_먼저_세운_셋으로()
        {
            TowerController a = Place(1101, 0);
            TowerController b = Place(1101, 4);
            TowerController c = Place(1101, 2);
            TowerController d = Place(1101, 1);

            Assert.IsTrue(Find(out TowerController keep, out List<TowerController> materials));
            Assert.AreEqual(a, keep);
            CollectionAssert.AreEquivalent(new[] { b, c }, materials);
            CollectionAssert.DoesNotContain(materials, d);
        }

        [Test]
        public void 합성_중인_타워는_빼고_찾음()
        {
            TowerController busy = Place(1101, 0);
            Place(1101, 1);
            Place(1101, 2);
            Assert.IsFalse(Find(out _, out _, tower => tower == busy));
        }

        [Test]
        public void 두_기_실험은_먼저_세운_타워를_남기고_한_기만_소모()
        {
            TowerController first = Place(1101, 4);
            TowerController second = Place(1101, 1);
            Assert.IsTrue(TowerProgression.TryFindMergeGroup(m_slots, null, out TowerController keep, out List<TowerController> materials, 2));
            Assert.AreEqual(first, keep);
            CollectionAssert.AreEquivalent(new[] { second }, materials);
            Assert.IsFalse(Find(out _, out _), "실험 호출이 기본 3기 규칙을 바꾸면 안 됨");
        }

        [Test]
        public void 두_기_실험도_나중_타워와_다른_ID는_섞지_않음()
        {
            TowerController first = Place(1101, 4);
            TowerController second = Place(1101, 3);
            TowerController newest = Place(1101, 0);
            Place(1201, 1);
            Assert.IsTrue(TowerProgression.TryFindMergeGroup(m_slots, null, out TowerController keep, out List<TowerController> materials, 2));
            Assert.AreEqual(first, keep);
            CollectionAssert.AreEquivalent(new[] { second }, materials);
            CollectionAssert.DoesNotContain(materials, newest);
        }

        [Test]
        public void 합성_필요_수가_한_기면_거부()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                TowerProgression.TryFindMergeGroup(m_slots, null, out _, out _, 1));
        }

        [Test]
        public void AB는_같은_시드로_A_B를_교차_실행()
        {
            var settings = new SimulationSettings { CompareMergeCounts = true, Runs = 2, FirstSeed = 40, ResultName = "example" };
            Assert.AreEqual(4, settings.TotalRuns);
            CollectionAssert.AreEqual(new[] { 40, 40, 41, 41 }, new[] { settings.GetSeed(0), settings.GetSeed(1), settings.GetSeed(2), settings.GetSeed(3) });
            Assert.AreEqual("A", settings.GetVariant(0));
            Assert.AreEqual("B", settings.GetVariant(1));
            Assert.AreEqual(3, settings.GetMergeCount(0));
            Assert.AreEqual(2, settings.GetMergeCount(1));
            Assert.AreEqual(1, settings.GetPairIndex(2));
            Assert.AreEqual(settings.GetResultName(0), settings.GetResultName(2));
            Assert.AreNotEqual(settings.GetResultName(0), settings.GetResultName(1));
        }

        [Test]
        public void 단일_실행은_시드가_증가하고_합성은_기본_세_기()
        {
            var settings = new SimulationSettings { CompareMergeCounts = false, Runs = 2, FirstSeed = 7, MergeCountA = 2, ResultName = "single" };
            Assert.AreEqual(2, settings.TotalRuns);
            Assert.AreEqual(7, settings.GetSeed(0));
            Assert.AreEqual(8, settings.GetSeed(1));
            Assert.AreEqual(3, settings.GetMergeCount(0));
            Assert.AreEqual(3, settings.GetMergeCount(1));
            Assert.AreEqual("", settings.GetVariant(0));
            Assert.AreEqual("single", settings.GetResultName(0));
        }

        [Test]
        public void AB_요약은_시간초과_조건불일치_외짝을_평균에서_제외()
        {
            string name = "ab_test_" + System.Guid.NewGuid().ToString("N");
            string folder = SimulationSettings.Folder;
            System.IO.Directory.CreateDirectory(folder);
            string a = BalanceRecorder.GamesPath(folder, name + "_A");
            string b = BalanceRecorder.GamesPath(folder, name + "_B");
            string summary = System.IO.Path.Combine(folder, name + "_comparison.txt");
            string paired = System.IO.Path.Combine(folder, name + "_paired.csv");
            try
            {
                string header = "seed,mode,emblems,result,merge_count,wave,gold,gem\n";
                System.IO.File.WriteAllText(a, header +
                    "1,Normal,Sword|Axe,GameOver,3,10,100,5\n2,Normal,Sword|Axe,Timeout,3,999,99999,99999\n" +
                    "3,Normal,Sword|Axe,Clear,3,40,10,10\n4,Normal,Sword|Axe,GameOver,3,8,8,8\n");
                System.IO.File.WriteAllText(b, header +
                    "1,Normal,Sword|Axe,Clear,2,40,50,8\n2,Normal,Sword|Axe,Clear,2,40,99999,99999\n" +
                    "4,Hard,Sword|Axe,Clear,2,40,8,8\n");
                var settings = new SimulationSettings { CompareMergeCounts = true, Runs = 4, ResultName = name };
                var method = typeof(BalanceSimulationWindow).GetMethod("SummarizeComparison",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                method.Invoke(null, new object[] { settings });
                string text = System.IO.File.ReadAllText(summary);
                StringAssert.Contains("유효 완료 짝 1 / 한쪽 기록만 1, 조건 불일치 1, 중지·시간 초과·멈춤 1", text);
                StringAssert.Contains("도달 웨이브: A 10 / B 40 / B-A +30", text);
                StringAssert.Contains("종료 시 골드: A 100 / B 50 / B-A -50", text);
                Assert.AreEqual(5, System.IO.File.ReadAllLines(paired).Length);
            }
            finally
            {
                foreach (string file in new[] { a, b, summary, paired })
                    if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            }
        }

        [Test]
        public void 덱_비교는_동일시드_노말넷_하드여섯_합성세기()
        {
            var settings = new SimulationSettings { CompareDeckModes = true, Runs = 100, FirstSeed = 1, ResultName = "deck" };
            Assert.AreEqual(200, settings.TotalRuns);
            Assert.AreEqual(1, settings.GetSeed(0));
            Assert.AreEqual(1, settings.GetSeed(1));
            Assert.AreEqual(100, settings.GetSeed(199));
            Assert.AreEqual((int)GameMode.Normal, settings.GetMode(0));
            Assert.AreEqual((int)GameMode.Hard, settings.GetMode(1));
            Assert.AreEqual(4, settings.GetDeckCount(0));
            Assert.AreEqual(6, settings.GetDeckCount(1));
            Assert.AreEqual("deck_Normal", settings.GetResultName(0));
            Assert.AreEqual("deck_Hard", settings.GetResultName(1));
            Assert.AreEqual(3, settings.GetMergeCount(0));
            Assert.AreEqual(3, settings.GetMergeCount(1));
        }



        [Test]
        public void 전략_탐색은_같은초기시드와_열번의_전투반복을_구분한다()
        {
            var settings = new SimulationSettings { SearchStrategies = true, FirstSeed = 85 };
            Assert.AreEqual(5000, settings.TotalRuns);
            Assert.AreEqual(85, settings.GetSeed(0));
            Assert.AreEqual(85, settings.GetSeed(4999));
            Assert.AreEqual((int)GameMode.Normal, settings.GetMode(0));
            Assert.AreEqual(4, settings.GetDeckCount(0));
            settings.SearchCandidate = 3;
            settings.SearchTrial = 7;
            Assert.AreEqual("Strategy0003", settings.GetVariant(0));
            Assert.AreEqual(7, settings.GetPairIndex(0));
            Assert.AreEqual(1, settings.GetPlacementProfile(0));
        }

        [Test]
        public void 전략_다섯판의_낮은후보만_경험적기준으로_탈락시킨다()
        {
            var leader = new StrategyCase { Id = 0 };
            for (int i = 0; i < 10; i++) leader.Trials.Add(new StrategyTrial { Wave = 10 });
            foreach (int mean in new[] { 3, 8, 15 })
            {
                var candidate = new StrategyCase { Id = mean };
                for (int i = 0; i < 4; i++) candidate.Trials.Add(new StrategyTrial { Wave = mean });
                Assert.IsFalse(StrategySearch.ShouldPrune(candidate, leader, 5, 0.8f));
                candidate.Trials.Add(new StrategyTrial { Wave = mean });
                Assert.AreEqual(mean == 3, StrategySearch.ShouldPrune(candidate, leader, 5, 0.8f));
            }
        }

        [Test]
        public void 전략_변이는_재현되고_부모의_선택규칙을_바꾸지않는다()
        {
            var parent = new StrategyPolicy();
            string before = JsonUtility.ToJson(parent);
            StrategyPolicy a = StrategyPolicy.Next(parent, 1, 85, false);
            StrategyPolicy b = StrategyPolicy.Next(parent, 1, 85, false);
            Assert.AreEqual(JsonUtility.ToJson(a), JsonUtility.ToJson(b));
            Assert.AreEqual(before, JsonUtility.ToJson(parent));
            Assert.IsFalse(a.Baseline);
            Assert.AreEqual(0, a.Parent);
            Assert.IsTrue(StrategyPolicy.Next(parent, 1, 85, true).Baseline);
        }

        [Test]
        public void 전략_구매난수는_전투시드와_분리하고_전투난수를_소비하지않는다()
        {
            UnityEngine.Random.State original = UnityEngine.Random.state;
            try
            {
                var a = new StrategyRandomStreams(85, 1);
                float drawA = 0f;
                a.RunPlanner(() => { drawA = UnityEngine.Random.value; return true; });
                float combatA = UnityEngine.Random.value;
                var b = new StrategyRandomStreams(85, 1);
                b.RunPlanner(() => { for (int i = 0; i < 100; i++) { float ignored = UnityEngine.Random.value; } return true; });
                Assert.AreEqual(combatA, UnityEngine.Random.value);
                var c = new StrategyRandomStreams(85, 2);
                float drawC = 0f;
                c.RunPlanner(() => { drawC = UnityEngine.Random.value; return true; });
                Assert.AreEqual(drawA, drawC);
                Assert.AreNotEqual(combatA, UnityEngine.Random.value);
            }
            finally { UnityEngine.Random.state = original; }
        }

        [Test]
        public void 배치_기본정책은_맵버프디버프이며_실험종료후_복원한다()
        {
            Assert.AreEqual(1, AutoPlanner.DefaultPlacementProfile);
            var settings = new SimulationSettings();
            Assert.AreEqual(1, settings.GetPlacementProfile(0));
            settings.CompareMergeCounts = true;
            Assert.AreEqual(1, settings.GetPlacementProfile(1));
            settings.CompareDeckModes = true;
            Assert.AreEqual(1, settings.GetPlacementProfile(1));
            settings.PlacementProfiles = new[] { 0, 1 };
            Assert.AreEqual(0, settings.GetPlacementProfile(0));
            Assert.AreEqual(1, settings.GetPlacementProfile(1));
            settings.PlacementProfiles = new int[0];
            settings.PlacementProfile = 0;
            Assert.AreEqual(0, settings.GetPlacementProfile(0));
            try
            {
                AutoPlanner.ConfigurePlacement(6);
                AutoPlanner.ResetPlacement();
                Assert.AreEqual(1, AutoPlanner.PlacementProfile);
            }
            finally { AutoPlanner.ResetPlacement(); }
        }

        [Test]
        public void 배치_실험은_활창얼음불_일곱정책_동일시드로_진행한다()
        {
            var settings = new SimulationSettings
            {
                Runs = 100, FirstSeed = 1, CompareMergeCounts = true,
                PlacementProfiles = new[] { 0, 1, 2, 3, 4, 5, 6 },
            };
            Assert.AreEqual(700, settings.TotalRuns);
            CollectionAssert.AreEqual(new[] { 3, 4, 11, 7 }, settings.FixedDeck);
            for (int run = 0; run < settings.TotalRuns; run++)
            {
                Assert.AreEqual(run / 7 + 1, settings.GetSeed(run));
                Assert.AreEqual(run % 7, settings.GetPlacementProfile(run));
                Assert.AreEqual((int)GameMode.Normal, settings.GetMode(run));
                Assert.AreEqual(3, settings.GetMergeCount(run));
                Assert.AreEqual(4, settings.GetDeckCount(run));
            }
            settings.PlacementProfiles = new[] { 0, 0, 1, 1 };
            Assert.AreNotEqual(settings.GetResultName(0), settings.GetResultName(1));
            Assert.AreEqual(settings.GetSeed(0), settings.GetSeed(3));
        }

        [Test]
        public void 배치_자리배정은_탐욕선택의_손해를_피하고_동점에서_안정적이다()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 },
                AutoPlanner.FindAssignment(new float[,] { { 10, 9 }, { 9, 0 } }));
            CollectionAssert.AreEqual(new[] { 1, 0 },
                AutoPlanner.FindAssignment(new float[,] { { -4, -1, -3 }, { -1, -4, -2 } }));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, AutoPlanner.FindAssignment(new float[3, 4]));
            Assert.IsEmpty(AutoPlanner.FindAssignment(new float[0, 3]));
        }

        [Test]
        public void 배치_자리배정은_작은행렬의_전수정답과_일치한다()
        {
            var random = new System.Random(84721);
            for (int rows = 1; rows <= 4; rows++)
                for (int sample = 0; sample < 30; sample++)
                {
                    var values = new float[rows, rows + 1];
                    for (int row = 0; row < rows; row++)
                        for (int slot = 0; slot <= rows; slot++) values[row, slot] = random.Next(-20, 21);
                    int[] assignment = AutoPlanner.FindAssignment(values);
                    var used = new bool[rows + 1];
                    float sum = 0f;
                    for (int row = 0; row < rows; row++)
                    {
                        Assert.IsFalse(used[assignment[row]]);
                        used[assignment[row]] = true;
                        sum += values[row, assignment[row]];
                    }
                    Assert.AreEqual(PlacementBruteForce(values, 0, new bool[rows + 1]), sum);
                }
        }

        [Test]
        public void 배치_자리배정은_잘못된입력을_거부한다()
        {
            Assert.Throws<System.ArgumentNullException>(() => AutoPlanner.FindAssignment(null));
            Assert.Throws<System.ArgumentException>(() => AutoPlanner.FindAssignment(new float[3, 2]));
            Assert.Throws<System.ArgumentException>(() => AutoPlanner.FindAssignment(new float[,] { { float.NaN } }));
            Assert.Throws<System.ArgumentException>(() => AutoPlanner.FindAssignment(new float[,] { { float.PositiveInfinity } }));
        }

        private static float PlacementBruteForce(float[,] values, int row, bool[] used)
        {
            if (row == values.GetLength(0)) return 0f;
            float best = float.NegativeInfinity;
            for (int slot = 0; slot < used.Length; slot++)
            {
                if (used[slot]) continue;
                used[slot] = true;
                best = System.Math.Max(best, values[row, slot] + PlacementBruteForce(values, row + 1, used));
                used[slot] = false;
            }
            return best;
        }
    }
}
