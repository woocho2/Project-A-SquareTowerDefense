using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerManagerTest
    {
        [Test]
        public void 판매_젬은_골드_1_다이아_5()
        {
            Assert.AreEqual(0, TowerManager.GetSellGem(1));
            Assert.AreEqual(0, TowerManager.GetSellGem(2));
            Assert.AreEqual(1, TowerManager.GetSellGem(3));
            Assert.AreEqual(5, TowerManager.GetSellGem(4));
        }

        [Test]
        public void 표_밖의_티어는_0()
        {
            Assert.AreEqual(0, TowerManager.GetSellGem(0));
            Assert.AreEqual(0, TowerManager.GetSellGem(5));
        }

        [TestCase(1, 50)]
        [TestCase(2, 55)]
        [TestCase(11, 100)]
        [TestCase(40, 245)]
        public void 판매_골드는_50에_웨이브마다_5씩(int wave, int gold)
        {
            Assert.AreEqual(gold, TowerManager.GetSellGold(wave));
        }

        [Test]
        public void 첫_생성은_광역_두번째는_단일_그다음은_무작위()
        {
            Assert.AreEqual(TowerColor.Red, TowerManager.GetGuaranteedColor(0));
            Assert.AreEqual(TowerColor.Blue, TowerManager.GetGuaranteedColor(1));
            Assert.AreEqual(TowerColor.None, TowerManager.GetGuaranteedColor(2));
            Assert.AreEqual(TowerColor.None, TowerManager.GetGuaranteedColor(3));
        }

        [Test]
        public void 웨이브_0은_1웨이브와_같음()
        {
            Assert.AreEqual(50, TowerManager.GetSellGold(0));
        }
    }
}
