using NUnit.Framework;

namespace SquareTD.Tests
{
    public class ColorUpgradeStateTest
    {
        [Test]
        public void 처음은_모든_컬러가_레벨1_비용4()
        {
            var state = new ColorUpgradeState();

            Assert.AreEqual(1, state.GetLevel(TowerColor.Red));
            Assert.AreEqual(1, state.GetLevel(TowerColor.Purple));
            Assert.AreEqual(4, state.GetCost(TowerColor.Red));
        }

        [Test]
        public void 강화하면_그_컬러만_오름()
        {
            var state = new ColorUpgradeState();

            Assert.IsTrue(state.TryUpgrade(TowerColor.Red));

            Assert.AreEqual(2, state.GetLevel(TowerColor.Red));
            Assert.AreEqual(1, state.GetLevel(TowerColor.Blue));
        }

        [Test]
        public void 비용은_지금_레벨을_따름_4_12_48_240()
        {
            var state = new ColorUpgradeState();
            int[] expected = { 4, 12, 48, 240 };

            foreach (int cost in expected)
            {
                Assert.AreEqual(cost, state.GetCost(TowerColor.Blue));
                state.TryUpgrade(TowerColor.Blue);
            }
        }

        [Test]
        public void 레벨5에서는_더_못_올리고_비용0()
        {
            var state = new ColorUpgradeState();
            for (int i = 0; i < 4; i++) state.TryUpgrade(TowerColor.Yellow);

            Assert.AreEqual(5, state.GetLevel(TowerColor.Yellow));
            Assert.IsFalse(state.CanUpgrade(TowerColor.Yellow));
            Assert.IsFalse(state.TryUpgrade(TowerColor.Yellow));
            Assert.AreEqual(5, state.GetLevel(TowerColor.Yellow));
            Assert.AreEqual(0, state.GetCost(TowerColor.Yellow));
        }

        [Test]
        public void 강화하면_LevelChanged에_컬러와_새_레벨()
        {
            var state = new ColorUpgradeState();
            TowerColor changedColor = TowerColor.None;
            int changedLevel = 0;
            state.LevelChanged += (color, level) => { changedColor = color; changedLevel = level; };

            state.TryUpgrade(TowerColor.Purple);

            Assert.AreEqual(TowerColor.Purple, changedColor);
            Assert.AreEqual(2, changedLevel);
        }

        [Test]
        public void 못_올리면_LevelChanged_없음()
        {
            var state = new ColorUpgradeState();
            for (int i = 0; i < 4; i++) state.TryUpgrade(TowerColor.Red);
            int count = 0;
            state.LevelChanged += (color, level) => count++;

            state.TryUpgrade(TowerColor.Red);

            Assert.AreEqual(0, count);
        }
    }
}
