using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerManagerTest
    {
        [Test]
        public void 판매_젬은_티어마다_1_3_9_27_81()
        {
            Assert.AreEqual(1, TowerManager.GetSellGem(1));
            Assert.AreEqual(3, TowerManager.GetSellGem(2));
            Assert.AreEqual(9, TowerManager.GetSellGem(3));
            Assert.AreEqual(27, TowerManager.GetSellGem(4));
            Assert.AreEqual(81, TowerManager.GetSellGem(5));
        }

        [Test]
        public void 표_밖의_티어는_0()
        {
            Assert.AreEqual(0, TowerManager.GetSellGem(0));
            Assert.AreEqual(0, TowerManager.GetSellGem(6));
        }
    }
}
