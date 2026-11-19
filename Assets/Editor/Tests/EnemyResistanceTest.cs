using NUnit.Framework;

namespace SquareTD.Tests
{
    // T6에 더함 — 20번 3장 [저항력]의 예
    public class EnemyResistanceTest
    {
        private const float Pass = 99.9f;   // 주사위가 이 값이면 (저항 100 미만은) 걸림
        private const float Eps = 0.0001f;

        [Test]
        public void 처음엔_지금_저항력이_기본과_같음()
        {
            var resistance = new EnemyResistance(10f);
            Assert.AreEqual(10f, resistance.Base);
            Assert.AreEqual(10f, resistance.Current);
        }

        [Test]
        public void 강한_CC에_걸릴_때마다_기본만큼_오름()
        {
            // 기획 예: 기본 10%가 강한 CC 세 번 → 40%
            var resistance = new EnemyResistance(10f);
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.AreEqual(40f, resistance.Current, Eps);
        }

        [Test]
        public void 막히면_기본만큼_내려가고_기본_아래로는_안_내려감()
        {
            var resistance = new EnemyResistance(10f);
            Assert.IsFalse(resistance.TryStrongCC(5f), "주사위 5 < 저항 10 → 막힘");
            Assert.AreEqual(10f, resistance.Current, Eps);
        }

        [Test]
        public void 걸리면_오르고_막으면_내려감()
        {
            // 기획 예: 기본 10% → 걸림 20% → 걸림 30% → 막음 20% → 걸림 30%
            var resistance = new EnemyResistance(10f);
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.AreEqual(30f, resistance.Current, Eps);
            Assert.IsFalse(resistance.TryStrongCC(25f), "주사위 25 < 저항 30 → 막힘");
            Assert.AreEqual(20f, resistance.Current, Eps);
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.AreEqual(30f, resistance.Current, Eps);
        }

        [Test]
        public void 저항력은_100을_넘지_않고_100이면_언제나_막힘()
        {
            var resistance = new EnemyResistance(60f);
            Assert.IsTrue(resistance.TryStrongCC(Pass));
            Assert.AreEqual(100f, resistance.Current, Eps, "60 + 60이어도 100에서 멈춤");
            Assert.IsFalse(resistance.TryStrongCC(Pass), "100%면 걸리지 않음");
        }

        [Test]
        public void 기본_저항이_0이면_언제나_걸리고_오르지도_않음()
        {
            var resistance = new EnemyResistance(0f);
            Assert.IsTrue(resistance.TryStrongCC(0f));
            Assert.AreEqual(0f, resistance.Current, Eps);
        }

        [Test]
        public void 약한_CC는_기본_저항으로만_줄임()
        {
            // 기획 예: 식의 값 18%에 저항 50% → 9%
            var resistance = new EnemyResistance(50f);
            Assert.AreEqual(0.09f, resistance.ResistWeak(0.18f), Eps);

            resistance.TryStrongCC(Pass);           // 지금 100이 되어도
            Assert.AreEqual(0.09f, resistance.ResistWeak(0.18f), Eps, "약한 CC는 올라간 값이 아니라 기본 50%");
        }

        [Test]
        public void 정화하면_기본으로_돌아감()
        {
            var resistance = new EnemyResistance(10f);
            resistance.TryStrongCC(Pass);
            resistance.TryStrongCC(Pass);
            resistance.Reset();
            Assert.AreEqual(10f, resistance.Current, Eps);
        }

        [Test]
        public void 기본값은_0에서_100_사이로()
        {
            Assert.AreEqual(100f, new EnemyResistance(150f).Base);
            Assert.AreEqual(0f, new EnemyResistance(-5f).Base);
        }
    }
}
