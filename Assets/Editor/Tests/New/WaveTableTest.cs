using NUnit.Framework;
using Unity.VisualScripting;

namespace SquareTD.Tests
{
    public class WaveTableTest
    {
        [Test]
        public void GetCycle에_1을_넣으면_1()
        {
            Assert.AreEqual(1, WaveTable.GetCycle(1));
        }

        [Test]
        public void GetCycle에_10을_넣으면_1()
        {
            Assert.AreEqual(1, WaveTable.GetCycle(10));
        }

        [Test]
        public void GetCycle에_11을_넣으면_2()
        {
            Assert.AreEqual(2, WaveTable.GetCycle(11));
        }

        [Test]
        public void GetCycle에_40을_넣으면_4()
        {
            Assert.AreEqual(4, WaveTable.GetCycle(40));
        }

        [Test]
        public void GetOrder에_1을_넣으면_1()
        {
            Assert.AreEqual(1, WaveTable.GetOrder(1));
        }

        [Test]
        public void GetOrder에_10을_넣으면_10()
        {
            Assert.AreEqual(10, WaveTable.GetOrder(10));
        }

        [Test]
        public void GetOrder에_35을_넣으면_5()
        {
            Assert.AreEqual(5, WaveTable.GetOrder(35));
        }

        [Test]
        public void GetNormalCount에_3을_넣으면_8()
        {
            Assert.AreEqual(8, WaveTable.GetNormalCount(3));
        }

        [Test]
        public void GetNormalCount에_7을_넣으면_10()
        {
            Assert.AreEqual(10, WaveTable.GetNormalCount(7));
        }

        [Test]
        public void GetNormalCount에_5을_넣으면_0()
        {
            Assert.AreEqual(0, WaveTable.GetNormalCount(5));
        }


        [Test]
        public void GetNormalCount에_22을_넣으면_24()
        {
            Assert.AreEqual(24, WaveTable.GetNormalCount(22));
        }

        [Test]
        public void GetNormalCount에_38을_넣으면_40()
        {
            Assert.AreEqual(40, WaveTable.GetNormalCount(38));
        }

        [Test]
        public void GetNormalCount에_40을_넣으면_0()
        {
            Assert.AreEqual(0, WaveTable.GetNormalCount(40));
        }

        [Test]
        public void HasMiddleBoss에_5를_넣으면_True()
        {
            Assert.IsTrue(WaveTable.HasMiddleBoss(5));
        }


        [Test]
        public void HasMiddleBoss에_8을_넣으면_False()
        {
            Assert.IsFalse(WaveTable.HasMiddleBoss(8));
        }

        [Test]
        public void HasMiddleBoss에_25를_넣으면_True()
        {
            Assert.IsTrue(WaveTable.HasMiddleBoss(25));
        }

        [Test]
        public void HasFinalBoss에_10을_넣으면_True()
        {
            Assert.IsTrue(WaveTable.HasFinalBoss(10));
        }


        [Test]
        public void HasFinalBoss에_16을_넣으면_False()
        {
            Assert.IsFalse(WaveTable.HasFinalBoss(16));
        }

        [Test]
        public void HasFinalBoss에_30를_넣으면_True()
        {
            Assert.IsTrue(WaveTable.HasFinalBoss(30));
        }

        [Test]
        public void 웨이브_Enemy_전부_더하면_720()
        {
            int total = 0;

            for (int i = 1; i <= 40; i++)
            {
                total += WaveTable.GetNormalCount(i);
            }

            Assert.AreEqual(720, total);
        }

        [Test]
        public void 웨이브_8에_노말이면_Hp배율_1점7()
        {
            Assert.AreEqual(1.7f, WaveTable.GetHpMultiplier(8, EnemyKind.Normal), 0.0001f);
        }

        [Test]
        public void 웨이브_32에_노말이면_Hp배율_8점8()
        {
            Assert.AreEqual(8.8f, WaveTable.GetHpMultiplier(32, EnemyKind.Normal), 0.0001f);
        }

        [Test]
        public void 웨이브_25에_중간보스이면_Hp배율_4()
        {
            Assert.AreEqual(4, WaveTable.GetHpMultiplier(25, EnemyKind.MiddleBoss), 0.0001f);
        }

        [Test]
        public void 웨이브_40에_최종보스이면_Hp배율_8()
        {
            Assert.AreEqual(8, WaveTable.GetHpMultiplier(40, EnemyKind.FinalBoss), 0.0001f);
        }

        [Test]
        public void 웨이브_11에_노말이면_Defense배율_2()
        {
            Assert.AreEqual(2f, WaveTable.GetDefenseMultiplier(11, EnemyKind.Normal), 0.0001f);
        }

        [Test]
        public void 웨이브_33에_노말이면_Defense배율_9점6()
        {
            Assert.AreEqual(9.6f, WaveTable.GetDefenseMultiplier(33, EnemyKind.Normal), 0.0001f);
        }

        [Test]
        public void 웨이브_35에_중간보스이면_Defense배율_8()
        {
            Assert.AreEqual(8, WaveTable.GetDefenseMultiplier(35, EnemyKind.MiddleBoss), 0.0001f);
        }

        [Test]
        public void 웨이브_10에_최종보스이면_Defense배율_1()
        {
            Assert.AreEqual(1, WaveTable.GetDefenseMultiplier(10, EnemyKind.FinalBoss), 0.0001f);
        }

        [Test]
        public void story모드이면0점75배율()
        {
            Assert.AreEqual(0.75, WaveTable.GetModeStatMultiplier(GameMode.Story));
        }

        [Test]
        public void normal모드이면1배율()
        {
            Assert.AreEqual(1, WaveTable.GetModeStatMultiplier(GameMode.Normal));
        }

        [Test]
        public void hard모드이면1점5배율()
        {
            Assert.AreEqual(1.5, WaveTable.GetModeStatMultiplier(GameMode.Hard));
        }

        [Test]
        public void story모드에_22웨이브이면_한도수75()
        {
            Assert.AreEqual(75, WaveTable.GetEnemyLimit(GameMode.Story, WaveTable.GetCycle(22)), 0.0001f);
        }

        [Test]
        public void normal모드에_18웨이브이면_한도수40()
        {
            Assert.AreEqual(40, WaveTable.GetEnemyLimit(GameMode.Normal, WaveTable.GetCycle(18)), 0.0001f);
        }

        [Test]
        public void Hard모드에_37웨이브이면_한도수60()
        {
            Assert.AreEqual(60, WaveTable.GetEnemyLimit(GameMode.Hard, WaveTable.GetCycle(37)), 0.0001f);
        }

        [Test]
        public void normal_웨이브27_gold_30_gem_0()
        {
            WaveTable.GetKillReward(EnemyKind.Normal, 27, out int gold, out int gem);
            Assert.AreEqual(30, gold);
            Assert.AreEqual(0, gem);
        }

        [Test]
        public void middleBoss_웨이브15_gold_2000_gem_20()
        {
            WaveTable.GetKillReward(EnemyKind.MiddleBoss, 15, out int gold, out int gem);
            Assert.AreEqual(2000, gold);
            Assert.AreEqual(40, gem);
        }

        [Test]
        public void middleBoss_웨이브35_gold_8000_gem_80()
        {
            WaveTable.GetKillReward(EnemyKind.MiddleBoss, 35, out int gold, out int gem);
            Assert.AreEqual(8000, gold);
            Assert.AreEqual(160, gem);
        }

        [Test]
        public void finalBoss_웨이브10_gold_1000_gem_10()
        {
            WaveTable.GetKillReward(EnemyKind.FinalBoss, 10, out int gold, out int gem);
            Assert.AreEqual(1000, gold);
            Assert.AreEqual(20, gem);
        }

        [Test]
        public void finalBoss_웨이브40_gold_16000_gem_160()
        {
            WaveTable.GetKillReward(EnemyKind.FinalBoss, 40, out int gold, out int gem);
            Assert.AreEqual(16000, gold);
            Assert.AreEqual(320, gem);
        }
    }
}