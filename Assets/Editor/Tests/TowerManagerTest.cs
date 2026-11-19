using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerManagerTest
    {
        [TestCase(1, 250)]
        [TestCase(2, 600)]
        [TestCase(3, 1500)]
        [TestCase(4, 4000)]
        public void 상점_가격은_티어마다_250_600_1500_4000(int tier, int price)
        {
            Assert.AreEqual(price, BalanceTables.GetShopPrice(tier));
        }

        [TestCase(250, 200)]
        [TestCase(600, 480)]
        [TestCase(750, 600)]
        [TestCase(4000, 3200)]
        public void 판매는_산_값의_80퍼센트(int paid, int gold)
        {
            Assert.AreEqual(gold, BalanceTables.GetSellGold(paid));
        }

        [Test]
        public void 표_밖의_티어는_0()
        {
            Assert.AreEqual(0, TowerManager.GetSellGold(0));
            Assert.AreEqual(0, TowerManager.GetSellGold(5));
        }

        [Test]
        public void 브론즈_카드_가격은_250()
        {
            Assert.AreEqual(250, TowerManager.FixedBuildCost);
            Assert.AreEqual(50, BalanceTables.ShopRerollCost);
        }

        [Test]
        public void 레벨마다_티어_확률의_합은_1()
        {
            for (int level = 1; level <= BalanceTables.MaxColorLevel; level++)
            {
                float sum = 0f;
                for (int tier = 1; tier <= 4; tier++) sum += BalanceTables.GetTierChance(level, tier);
                Assert.AreEqual(1f, sum, 0.0001f, $"레벨 {level}");
            }
        }

        [TestCase(1, 0.99f, 1)]
        [TestCase(2, 0.94f, 1)]
        [TestCase(2, 0.96f, 2)]
        [TestCase(6, 0.9999f, 4)]
        [TestCase(10, 0.1f, 2)]
        [TestCase(10, 0.5f, 3)]
        [TestCase(10, 0.7f, 4)]
        public void 레벨_확률로_티어를_고름(int level, float roll, int tier)
        {
            Assert.AreEqual(tier, BalanceTables.RollTier(level, roll));
        }

        [Test]
        public void 상점은_카드_5장을_덱_문양으로_채움()
        {
            var deck = new List<TowerEmblem> { TowerEmblem.Sword, TowerEmblem.Fire };
            var shop = new TowerShop(null, () => 1, () => deck, Sequence(0.1f, 0.3f, 0.6f, 0.9f));
            shop.Roll();

            Assert.AreEqual(5, shop.Cards.Count);
            foreach (TowerShop.Card card in shop.Cards)
            {
                Assert.IsTrue(card.IsAvailable);
                Assert.AreEqual(1, TowerId.GetTier(card.TowerId), "레벨 1은 브론즈만");
                Assert.Contains(TowerId.GetEmblem(card.TowerId), deck);
                Assert.AreEqual(250, card.Price);
            }
        }

        [Test]
        public void 잠그면_턴이_시작돼도_카드가_그대로()
        {
            var deck = new List<TowerEmblem> { TowerEmblem.Sword, TowerEmblem.Fire, TowerEmblem.Water };
            var shop = new TowerShop(null, () => 1, () => deck, Sequence(0.05f, 0.45f, 0.85f, 0.25f, 0.65f, 0.15f));
            shop.Roll();
            var before = new List<int>();
            foreach (TowerShop.Card card in shop.Cards) before.Add(card.TowerId);

            shop.SetLocked(true);
            shop.OnPlayerTurnStarted();
            for (int i = 0; i < before.Count; i++) Assert.AreEqual(before[i], shop.Cards[i].TowerId);

            shop.MarkSold(0);
            Assert.IsFalse(shop.Cards[0].IsAvailable, "산 카드는 빈 자리");
        }

        private static System.Func<float> Sequence(params float[] values)
        {
            int index = 0;
            return () => values[index++ % values.Length];
        }
    }
}
