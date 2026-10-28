using NUnit.Framework;

namespace SquareTD.Tests
{
    public class DamageCalculatorTest
    {
        private const float Tolerance = 0.001f;

        private static DamageInfo Hit(float baseDamage, float penetration = 0f, bool forceFixed = false)
        {
            return new DamageInfo { BaseDamage = baseDamage, ArmorPenetration = penetration, ForceFixed = forceFixed };
        }

        [Test]
        public void 쏘는쪽_거리보너스와_치명을_둘다_곱함()
        {
            Assert.AreEqual(22f, DamageCalculator.GetOutgoing(10f, 1.1f, 2f), Tolerance);
        }

        [Test]
        public void 쏘는쪽_치명이_아니면_배율1()
        {
            Assert.AreEqual(11f, DamageCalculator.GetOutgoing(10f, 1.1f, 1f), Tolerance);
        }

        [Test]
        public void 방어계수_0이면_1()
        {
            Assert.AreEqual(1f, DamageCalculator.GetDefenseCoefficient(0f), Tolerance);
        }

        [Test]
        public void 방어계수_100이면_절반()
        {
            Assert.AreEqual(0.5f, DamageCalculator.GetDefenseCoefficient(100f), Tolerance);
        }

        [Test]
        public void 방어계수_음수100이면_2배()
        {
            Assert.AreEqual(2f, DamageCalculator.GetDefenseCoefficient(-100f), Tolerance);
        }

        [Test]
        public void 방어계수_음수300이면_4배()
        {
            Assert.AreEqual(4f, DamageCalculator.GetDefenseCoefficient(-300f), Tolerance);
        }

        [Test]
        public void 관통20퍼센트_방어100은_80()
        {
            Assert.AreEqual(80f, DamageCalculator.ApplyPenetration(100f, 0.2f), Tolerance);
        }

        [Test]
        public void 관통20퍼센트_방어음수100은_음수120()
        {
            Assert.AreEqual(-120f, DamageCalculator.ApplyPenetration(-100f, 0.2f), Tolerance);
        }

        [Test]
        public void 최종_방어100이면_절반()
        {
            Assert.AreEqual(50f, DamageCalculator.Calculate(Hit(100f), 100f, 1f, 0f), Tolerance);
        }

        [Test]
        public void 최종_방어음수100이면_2배()
        {
            Assert.AreEqual(200f, DamageCalculator.Calculate(Hit(100f), -100f, 1f, 0f), Tolerance);
        }

        [Test]
        public void 최종_관통이_방어보다_먼저()
        {
            Assert.AreEqual(100f / 1.8f, DamageCalculator.Calculate(Hit(100f, 0.2f), 100f, 1f, 0f), Tolerance);
        }

        [Test]
        public void 최종_받는피해배율을_곱함()
        {
            Assert.AreEqual(150f, DamageCalculator.Calculate(Hit(100f), 0f, 1.5f, 0f), Tolerance);
        }

        [Test]
        public void 최종_고정피해는_방어를_무시()
        {
            Assert.AreEqual(75f, DamageCalculator.Calculate(Hit(100f), 100f, 1f, 0.5f), Tolerance);
        }

        [Test]
        public void 최종_고정피해는_받는피해배율도_무시()
        {
            Assert.AreEqual(150f, DamageCalculator.Calculate(Hit(100f), 0f, 2f, 0.5f), Tolerance);
        }

        [Test]
        public void 최종_ForceFixed면_전부_고정()
        {
            Assert.AreEqual(100f, DamageCalculator.Calculate(Hit(100f, 0f, true), 300f, 3f, 0f), Tolerance);
        }

        [Test]
        public void 최종_상한이_없음()
        {
            Assert.AreEqual(1000000f, DamageCalculator.Calculate(Hit(1000000f), 0f, 1f, 0f), 1f);
        }
    }
}
