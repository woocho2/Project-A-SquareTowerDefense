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

        [TestCase(2, -1, 2f, 0, 0, 0.95f, 1f, 0.95f)]
        [TestCase(3, -2, 4f, 1, 0, 0.9f, 1f, 0.9f)]
        [TestCase(4, -3, 8f, 1, 0, 0.8f, 1.5f, 0.8f)]
        [TestCase(5, -4, 16f, 2, 1, 0.5f, 2.5f, 0.5f)]
        public void 티어_표(int tier, int action, float power, int attackCount, int mastery, float maxStack, float stackValue, float skillCount)
        {
            TierRow row = BalanceTables.GetTierRow(tier);
            Assert.AreEqual(action, row.Action);
            Assert.AreEqual(power, row.Power, 0.0001f);
            Assert.AreEqual(attackCount, row.AttackCount);
            Assert.AreEqual(mastery, row.Mastery);
            Assert.AreEqual(maxStack, row.MaxStack, 0.0001f);
            Assert.AreEqual(stackValue, row.StackValue, 0.0001f);
            Assert.AreEqual(skillCount, row.SkillCount, 0.0001f);
        }

        [Test]
        public void 표_밖의_티어는_그대로()
        {
            Assert.AreEqual(1f, BalanceTables.GetTierRow(0).Power);
            Assert.AreEqual(1f, BalanceTables.GetTierRow(6).Power);
            Assert.AreEqual(0, BalanceTables.GetTierRow(6).Action);
            Assert.AreEqual(1f, BalanceTables.GetTierRow(6).SkillCount);
        }

        [Test]
        public void 레벨2는_AbilityValue_2배()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(2, TowerColor.Red);
            Assert.AreEqual(2f, row.AbilityValue);
            Assert.AreEqual(1.5f, row.StackValue);
            Assert.AreEqual(0, row.AttackCount);
        }

        [Test]
        public void 레벨3은_광역_AttackCount_플러스1_단일_플러스2()
        {
            Assert.AreEqual(1, BalanceTables.GetColorLevelRow(3, TowerColor.Red).AttackCount);
            Assert.AreEqual(2, BalanceTables.GetColorLevelRow(3, TowerColor.Blue).AttackCount);
        }

        [Test]
        public void 레벨4는_스택값_2점5배_최대스택_절반()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(4, TowerColor.Red);
            Assert.AreEqual(2.5f, row.StackValue);
            Assert.AreEqual(0.5f, row.MaxStack);
            Assert.AreEqual(0, row.Mastery);
        }

        [Test]
        public void 레벨5는_AbilityValue_4배_Mastery_플러스1()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(5, TowerColor.Red);
            Assert.AreEqual(4f, row.AbilityValue);
            Assert.AreEqual(4f, row.StackValue);
            Assert.AreEqual(2, row.AttackCount);
            Assert.AreEqual(1, row.Mastery);
            Assert.AreEqual(4, BalanceTables.GetColorLevelRow(5, TowerColor.Blue).AttackCount);
        }

        [Test]
        public void 레벨6은_AbilityValue_6배_Mastery_플러스2()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(6, TowerColor.Red);
            Assert.AreEqual(6f, row.AbilityValue);
            Assert.AreEqual(2, row.AttackCount);
            Assert.AreEqual(2, row.Mastery);
            Assert.AreEqual(4, BalanceTables.GetColorLevelRow(6, TowerColor.Blue).AttackCount);
        }

        [Test]
        public void 레벨_밖은_가까운_끝으로()
        {
            Assert.AreEqual(1f, BalanceTables.GetColorLevelRow(0, TowerColor.Red).AbilityValue);
            Assert.AreEqual(6f, BalanceTables.GetColorLevelRow(9, TowerColor.Red).AbilityValue);
        }

        [Test]
        public void 단일만_AttackCount가_두배이고_다른_값은_같음()
        {
            ColorLevelRow red = BalanceTables.GetColorLevelRow(5, TowerColor.Red);
            ColorLevelRow blue = BalanceTables.GetColorLevelRow(5, TowerColor.Blue);
            Assert.AreEqual(red.AbilityValue, blue.AbilityValue);
            Assert.AreEqual(red.StackValue, blue.StackValue);
            Assert.AreEqual(red.MaxStack, blue.MaxStack);
            Assert.AreEqual(red.Mastery, blue.Mastery);
        }

        [Test]
        public void 컬러_강화_비용은_4_12_48_240()
        {
            Assert.AreEqual(4, BalanceTables.GetColorUpgradeCost(1));
            Assert.AreEqual(12, BalanceTables.GetColorUpgradeCost(2));
            Assert.AreEqual(48, BalanceTables.GetColorUpgradeCost(3));
            Assert.AreEqual(240, BalanceTables.GetColorUpgradeCost(4));
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
