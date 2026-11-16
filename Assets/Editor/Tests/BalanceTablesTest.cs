using NUnit.Framework;

namespace SquareTD.Tests
{
    public class BalanceTablesTest
    {
        [Test]
        public void 브론즈는_그대로()
        {
            TierRow row = BalanceTables.GetTierRow(1);
            Assert.AreEqual(0, row.Action);
            Assert.AreEqual(1f, row.Power);
            Assert.AreEqual(0, row.Mastery);
            Assert.AreEqual(0, row.AttackCount);
        }

        [TestCase(2, 0, 1.5f, 0, 0, 1f, 1f, 0.95f, 1.25f)]
        [TestCase(3, 0, 2.45f, 1, 0, 1f, 1f, 0.9f, 2.5f)]
        [TestCase(4, -1, 6.15f, 1, 1, 0.75f, 2f, 0.8f, 6.25f)]
        public void 공격_티어_표(int tier, int action, float power, int attackCount, int mastery, float maxStack, float stackValue, float skillCount, float abilityValue)
        {
            AssertTierRow(BalanceTables.GetTierRow(tier, TowerColor.Red), action, power, attackCount, mastery, maxStack, stackValue, skillCount, abilityValue);
        }

        [TestCase(2, 0, 1.5f, 0, 0, 1f, 1f, 0.95f, 1.25f)]
        [TestCase(3, 0, 2f, 1, 0, 1f, 1f, 0.9f, 2.5f)]
        [TestCase(4, -1, 4f, 1, 1, 0.75f, 2f, 0.8f, 6.25f)]
        public void 지원_티어_표(int tier, int action, float power, int attackCount, int mastery, float maxStack, float stackValue, float skillCount, float abilityValue)
        {
            AssertTierRow(BalanceTables.GetTierRow(tier, TowerColor.Yellow), action, power, attackCount, mastery, maxStack, stackValue, skillCount, abilityValue);
            AssertTierRow(BalanceTables.GetTierRow(tier, TowerColor.Purple), action, power, attackCount, mastery, maxStack, stackValue, skillCount, abilityValue);
        }

        private static void AssertTierRow(TierRow row, int action, float power, int attackCount, int mastery, float maxStack, float stackValue, float skillCount, float abilityValue)
        {
            Assert.AreEqual(action, row.Action);
            Assert.AreEqual(power, row.Power, 0.0001f);
            Assert.AreEqual(attackCount, row.AttackCount);
            Assert.AreEqual(mastery, row.Mastery);
            Assert.AreEqual(maxStack, row.MaxStack, 0.0001f);
            Assert.AreEqual(stackValue, row.StackValue, 0.0001f);
            Assert.AreEqual(skillCount, row.SkillCount, 0.0001f);
            Assert.AreEqual(abilityValue, row.AbilityValue, 0.0001f);
        }

        [Test]
        public void 공격_타워는_광역과_단일의_티어_표가_같음()
        {
            Assert.AreEqual(BalanceTables.GetTierRow(4, TowerColor.Red).Power, BalanceTables.GetTierRow(4, TowerColor.Blue).Power, 0.0001f);
            Assert.AreEqual(BalanceTables.GetTierRow(4, TowerColor.Red).AbilityValue, BalanceTables.GetTierRow(4, TowerColor.Blue).AbilityValue, 0.0001f);
        }

        [Test]
        public void 표_밖의_티어는_그대로()
        {
            Assert.AreEqual(1f, BalanceTables.GetTierRow(0).Power);
            Assert.AreEqual(1f, BalanceTables.GetTierRow(6).Power);
            Assert.AreEqual(0, BalanceTables.GetTierRow(6).Action);
            Assert.AreEqual(1f, BalanceTables.GetTierRow(6).SkillCount);
            Assert.AreEqual(1f, BalanceTables.GetTierRow(6).AbilityValue);
        }

        // ── 컬러 강화 ──

        [TestCase(1, 1f, 1f, 1f, 0)]
        [TestCase(2, 1.25f, 1.1f, 1f, 0)]
        [TestCase(3, 1.5f, 1.2f, 1f, 0)]
        [TestCase(4, 2f, 1.5f, 0.5f, 0)]
        [TestCase(5, 3f, 2f, 0.5f, 1)]
        public void 공격_컬러_표(int level, float abilityValue, float stackValue, float maxStack, int mastery)
        {
            foreach (TowerColor color in new[] { TowerColor.Red, TowerColor.Blue })
                AssertColorRow(BalanceTables.GetColorLevelRow(level, color), abilityValue, stackValue, maxStack, mastery);
        }

        [TestCase(1, 1f, 1f, 1f, 0)]
        [TestCase(2, 1.25f, 1.1f, 1f, 0)]
        [TestCase(3, 1.5f, 1.2f, 1f, 0)]
        [TestCase(4, 2f, 1.5f, 0.8f, 0)]
        [TestCase(5, 3f, 2f, 0.7f, 1)]
        public void 지원_컬러_표(int level, float abilityValue, float stackValue, float maxStack, int mastery)
        {
            foreach (TowerColor color in new[] { TowerColor.Yellow, TowerColor.Purple })
                AssertColorRow(BalanceTables.GetColorLevelRow(level, color), abilityValue, stackValue, maxStack, mastery);
        }

        private static void AssertColorRow(ColorLevelRow row, float abilityValue, float stackValue, float maxStack, int mastery)
        {
            Assert.AreEqual(abilityValue, row.AbilityValue, 0.0001f);
            Assert.AreEqual(stackValue, row.StackValue, 0.0001f);
            Assert.AreEqual(maxStack, row.MaxStack, 0.0001f);
            Assert.AreEqual(mastery, row.Mastery);
        }

        [Test]
        public void 레벨_밖은_가까운_끝으로()
        {
            Assert.AreEqual(1f, BalanceTables.GetColorLevelRow(0, TowerColor.Red).AbilityValue);
            Assert.AreEqual(3f, BalanceTables.GetColorLevelRow(9, TowerColor.Red).AbilityValue, 0.0001f);
        }

        [Test]
        public void 다이아_5레벨_스킬은_브론즈_1레벨의_115배()
        {
            float bronze = BalanceTables.GetTierRow(1).Power * BalanceTables.GetTierRow(1).AbilityValue * BalanceTables.GetColorLevelRow(1, TowerColor.Red).AbilityValue;
            float diamond = BalanceTables.GetTierRow(4).Power * BalanceTables.GetTierRow(4).AbilityValue * BalanceTables.GetColorLevelRow(5, TowerColor.Red).AbilityValue;
            Assert.AreEqual(115.3125f, diamond / bronze, 0.01f, "6.15 × 6.25 × 3");
        }

        [TestCase(1, 1.2f)]
        [TestCase(2, 1.5f)]
        [TestCase(3, 3f)]
        [TestCase(4, 7.5f)]
        public void 공격_타워_스킬은_그_티어_기본_공격의_1점2_1점5_3_7점5배(int tier, float ratio)
        {
            Assert.AreEqual(ratio, 1.2f * BalanceTables.GetTierRow(tier, TowerColor.Red).AbilityValue, 0.0001f, "브론즈 CSV AbilityValue 1.2 × 티어 배율");
        }

        [Test]
        public void 컬러_강화_비용은_10_20_40_80()
        {
            Assert.AreEqual(10, BalanceTables.GetColorUpgradeCost(1));
            Assert.AreEqual(20, BalanceTables.GetColorUpgradeCost(2));
            Assert.AreEqual(40, BalanceTables.GetColorUpgradeCost(3));
            Assert.AreEqual(80, BalanceTables.GetColorUpgradeCost(4));
        }

        [Test]
        public void 레벨5_이상은_비용_0()
        {
            Assert.AreEqual(0, BalanceTables.GetColorUpgradeCost(5));
            Assert.AreEqual(0, BalanceTables.GetColorUpgradeCost(6));
            Assert.AreEqual(0, BalanceTables.GetColorUpgradeCost(0));
        }
    }
}
