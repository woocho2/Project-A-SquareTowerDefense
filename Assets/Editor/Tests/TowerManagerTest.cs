using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerManagerTest
    {
        [TestCase(1, 200)]
        [TestCase(2, 600)]
        [TestCase(3, 1800)]
        [TestCase(4, 5400)]
        public void 판매_골드는_티어마다_200_600_1800_5400(int tier, int gold)
        {
            Assert.AreEqual(gold, TowerManager.GetSellGold(tier));
        }

        [Test]
        public void 표_밖의_티어는_0()
        {
            Assert.AreEqual(0, TowerManager.GetSellGold(0));
            Assert.AreEqual(0, TowerManager.GetSellGold(5));
        }

        [Test]
        public void 생성_비용은_250_고정()
        {
            Assert.AreEqual(250, TowerManager.FixedBuildCost);
        }

        [Test]
        public void 첫_생성은_광역_두번째는_단일_그다음은_무작위()
        {
            Assert.AreEqual(TowerColor.Red, TowerManager.GetGuaranteedColor(0));
            Assert.AreEqual(TowerColor.Blue, TowerManager.GetGuaranteedColor(1));
            Assert.AreEqual(TowerColor.None, TowerManager.GetGuaranteedColor(2));
            Assert.AreEqual(TowerColor.None, TowerManager.GetGuaranteedColor(3));
        }
    }
}
