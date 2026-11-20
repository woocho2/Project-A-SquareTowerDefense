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

        [TestCase(2, 0, 1.5f, 0, 0, 0.95f, 1f)]
        [TestCase(3, -2, 4f, 1, 0, 0.9f, 1f)]
        [TestCase(4, -3, 8f, 1, 0, 0.8f, 1.5f)]
        [TestCase(5, -4, 16f, 2, 1, 0.5f, 3f)]
        public void 공격_티어_표(int tier, int action, float power, int attackCount, int mastery, float skillCount, float abilityValue)
        {
            AssertTierRow(BalanceTables.GetTierRow(tier, TowerColor.Red), action, power, attackCount, mastery, skillCount, abilityValue);
        }

        [TestCase(2, -1, 1.5f, 0, 0, 0.95f, 1f)]
        [TestCase(3, -2, 2f, 1, 0, 0.9f, 1f)]
        [TestCase(4, -3, 3f, 1, 0, 0.8f, 1.5f)]
        [TestCase(5, -4, 4.5f, 2, 1, 0.5f, 3f)]
        public void 지원_티어_표(int tier, int action, float power, int attackCount, int mastery, float skillCount, float abilityValue)
        {
            AssertTierRow(BalanceTables.GetTierRow(tier, TowerColor.Yellow), action, power, attackCount, mastery, skillCount, abilityValue);
            AssertTierRow(BalanceTables.GetTierRow(tier, TowerColor.Purple), action, power, attackCount, mastery, skillCount, abilityValue);
        }

        private static void AssertTierRow(TierRow row, int action, float power, int attackCount, int mastery, float skillCount, float abilityValue)
        {
            Assert.AreEqual(action, row.Action);
            Assert.AreEqual(power, row.Power, 0.0001f);
            Assert.AreEqual(attackCount, row.AttackCount);
            Assert.AreEqual(mastery, row.Mastery);
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

        [TestCase(1, 1f, 0)]
        [TestCase(2, 1.1f, 0)]
        [TestCase(4, 1.35f, 0)]
        [TestCase(5, 1.5f, 1)]
        [TestCase(7, 2f, 1)]
        [TestCase(9, 3f, 1)]
        [TestCase(10, 4f, 2)]
        public void 레벨_표는_네_컬러_공통(int level, float abilityValue, int mastery)
        {
            foreach (TowerColor color in new[] { TowerColor.Red, TowerColor.Blue, TowerColor.Yellow, TowerColor.Purple })
            {
                ColorLevelRow row = BalanceTables.GetColorLevelRow(level, color);
                Assert.AreEqual(abilityValue, row.AbilityValue, 0.0001f);
                Assert.AreEqual(mastery, row.Mastery);
            }
        }

        [Test]
        public void 레벨_밖은_가까운_끝으로()
        {
            Assert.AreEqual(1f, BalanceTables.GetColorLevelRow(0, TowerColor.Red).AbilityValue);
            Assert.AreEqual(4f, BalanceTables.GetColorLevelRow(11, TowerColor.Red).AbilityValue, 0.0001f);
        }

        [Test]
        public void 다이아_10레벨_스킬은_브론즈_1레벨의_192배()
        {
            float bronze = BalanceTables.GetTierRow(1).Power * BalanceTables.GetTierRow(1).AbilityValue * BalanceTables.GetColorLevelRow(1, TowerColor.Red).AbilityValue;
            float diamond = BalanceTables.GetTierRow(5).Power * BalanceTables.GetTierRow(5).AbilityValue * BalanceTables.GetColorLevelRow(10, TowerColor.Red).AbilityValue;
            Assert.AreEqual(192f, diamond / bronze, 0.01f, "16 × 3 × 4");
        }

        [TestCase(1, 1.2f)]
        [TestCase(2, 1.2f)]
        [TestCase(3, 1.2f)]
        [TestCase(4, 1.8f)]
        [TestCase(5, 3.6f)]
        public void 공격_타워_스킬은_그_티어_기본_공격의_티어_AV_배율을_따름(int tier, float ratio)
        {
            Assert.AreEqual(ratio, 1.2f * BalanceTables.GetTierRow(tier, TowerColor.Red).AbilityValue, 0.0001f, "브론즈 CSV AbilityValue 1.2 × 티어 배율");
        }

        [Test]
        public void 레벨업_경험치_합은_452()
        {
            int[] needs = { 4, 8, 15, 20, 25, 55, 75, 110, 140 };
            for (int level = 1; level <= needs.Length; level++) Assert.AreEqual(needs[level - 1], BalanceTables.GetXpToNext(level), $"레벨 {level}");
        }

        [Test]
        public void 레벨10_이상은_경험치_0()
        {
            Assert.AreEqual(0, BalanceTables.GetXpToNext(10));
            Assert.AreEqual(0, BalanceTables.GetXpToNext(11));
            Assert.AreEqual(0, BalanceTables.GetXpToNext(0));
        }
    }
}
