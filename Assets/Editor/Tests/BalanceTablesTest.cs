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

        [Test]
        public void 실버는_Action만_마이너스1()
        {
            TierRow row = BalanceTables.GetTierRow(2);
            Assert.AreEqual(-1, row.Action);
            Assert.AreEqual(1f, row.Power);
            Assert.AreEqual(0, row.Mastery);
        }

        [Test]
        public void 골드는_Power_3배_Mastery_플러스1()
        {
            TierRow row = BalanceTables.GetTierRow(3);
            Assert.AreEqual(-1, row.Action);
            Assert.AreEqual(3f, row.Power);
            Assert.AreEqual(1, row.Mastery);
            Assert.AreEqual(0, row.AttackCount);
        }

        [Test]
        public void 미스릴은_AttackCount_플러스1_최대스택_절반_스택값_2배()
        {
            TierRow row = BalanceTables.GetTierRow(4);
            Assert.AreEqual(-2, row.Action);
            Assert.AreEqual(1, row.AttackCount);
            Assert.AreEqual(0.5f, row.MaxStack);
            Assert.AreEqual(2f, row.StackValue);
        }

        [Test]
        public void 다이아는_Power_15배_Action_마이너스5_Mastery_플러스3()
        {
            TierRow row = BalanceTables.GetTierRow(5);
            Assert.AreEqual(-5, row.Action);
            Assert.AreEqual(15f, row.Power);
            Assert.AreEqual(3, row.Mastery);
            Assert.AreEqual(1, row.AttackCount);
            Assert.AreEqual(0.5f, row.MaxStack);
            Assert.AreEqual(2f, row.StackValue);
        }

        [Test]
        public void 표_밖의_티어는_그대로()
        {
            Assert.AreEqual(15f, BalanceTables.GetTierRow(5).Power, "먼저 다이아 줄부터 채우기");
            Assert.AreEqual(1f, BalanceTables.GetTierRow(0).Power);
            Assert.AreEqual(1f, BalanceTables.GetTierRow(6).Power);
            Assert.AreEqual(0, BalanceTables.GetTierRow(6).Action);
        }

        [Test]
        public void Duration은_공격이면_줄고_디버프면_늘고_버프는_그대로()
        {
            Assert.AreEqual(-1, BalanceTables.GetTierDurationDelta(2, TowerColor.Red));
            Assert.AreEqual(-2, BalanceTables.GetTierDurationDelta(5, TowerColor.Blue));
            Assert.AreEqual(1, BalanceTables.GetTierDurationDelta(3, TowerColor.Purple));
            Assert.AreEqual(2, BalanceTables.GetTierDurationDelta(5, TowerColor.Purple));
            Assert.AreEqual(0, BalanceTables.GetTierDurationDelta(5, TowerColor.Yellow));
            Assert.AreEqual(0, BalanceTables.GetTierDurationDelta(1, TowerColor.Red));
        }

        [Test]
        public void 레벨2는_AbilityValue_2배()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(2, TowerColor.Red);
            Assert.AreEqual(2f, row.AbilityValue);
            Assert.AreEqual(0, row.AttackCount);
        }

        [Test]
        public void 레벨3은_광역_AttackCount_플러스1_단일_플러스2()
        {
            Assert.AreEqual(1, BalanceTables.GetColorLevelRow(3, TowerColor.Red).AttackCount);
            Assert.AreEqual(2, BalanceTables.GetColorLevelRow(3, TowerColor.Blue).AttackCount);
        }

        [Test]
        public void 레벨4는_스택값_2배_최대스택_절반()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(4, TowerColor.Red);
            Assert.AreEqual(2f, row.StackValue);
            Assert.AreEqual(0.5f, row.MaxStack);
            Assert.AreEqual(0, row.Mastery);
        }

        [Test]
        public void 레벨5는_AbilityValue_4배_Mastery_플러스1()
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(5, TowerColor.Red);
            Assert.AreEqual(4f, row.AbilityValue);
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
