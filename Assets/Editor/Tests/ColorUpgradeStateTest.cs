using NUnit.Framework;

namespace SquareTD.Tests
{
    public class ColorUpgradeStateTest
    {
        [Test]
        public void 처음은_레벨1_경험치_0_다음까지_4()
        {
            var state = new ColorUpgradeState();

            Assert.AreEqual(1, state.Level);
            Assert.AreEqual(0, state.Xp);
            Assert.AreEqual(4, state.XpToNext);
        }

        [Test]
        public void 레벨은_네_컬러가_함께_씀()
        {
            var state = new ColorUpgradeState();
            state.AddXp(4);

            Assert.AreEqual(2, state.GetLevel(TowerColor.Red));
            Assert.AreEqual(2, state.GetLevel(TowerColor.Purple));
        }

        [Test]
        public void 경험치를_다_채우면_레벨_10()
        {
            var state = new ColorUpgradeState();
            int[] needs = { 4, 8, 15, 20, 25, 55, 75, 110, 140 };

            for (int i = 0; i < needs.Length; i++)
            {
                Assert.AreEqual(needs[i], state.XpToNext, $"레벨 {i + 1}");
                state.AddXp(needs[i]);
                Assert.AreEqual(i + 2, state.Level);
            }
            Assert.IsTrue(state.IsMax);
            Assert.AreEqual(0, state.XpToNext);
        }

        [Test]
        public void 넘친_경험치는_다음_레벨로_넘어감()
        {
            var state = new ColorUpgradeState();

            int levels = state.AddXp(6);

            Assert.AreEqual(1, levels);
            Assert.AreEqual(2, state.Level);
            Assert.AreEqual(2, state.Xp);
        }

        [Test]
        public void 한_번에_여러_레벨이_오를_수_있음()
        {
            var state = new ColorUpgradeState();

            Assert.AreEqual(2, state.AddXp(12));
            Assert.AreEqual(3, state.Level);
            Assert.AreEqual(0, state.Xp);
        }

        [Test]
        public void 최대_레벨에서는_더_쌓이지_않음()
        {
            var state = new ColorUpgradeState();
            state.AddXp(1000);

            Assert.AreEqual(BalanceTables.MaxColorLevel, state.Level);
            Assert.AreEqual(0, state.Xp);
            Assert.AreEqual(0, state.AddXp(10));
        }

        [Test]
        public void 레벨이_오를_때만_LevelChanged()
        {
            var state = new ColorUpgradeState();
            int count = 0, last = 0;
            state.LevelChanged += level => { count++; last = level; };

            state.AddXp(3);
            Assert.AreEqual(0, count);
            state.AddXp(1);
            Assert.AreEqual(1, count);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void 경험치는_한_번에_1_골드_100()
        {
            Assert.AreEqual(1, BalanceTables.XpPerPurchase);
            Assert.AreEqual(100, BalanceTables.XpPurchaseCost);
        }
    }
}
