using NUnit.Framework;

namespace SquareTD.Tests
{
    public class CriticalRollTest
    {
        [Test]
        public void 굴린수가_확률보다_작으면_치명()
        {
            Assert.IsTrue(CriticalRoll.Roll(0.3f, 0.29f));
            Assert.IsTrue(CriticalRoll.Roll(0.3f, 0f));
        }

        [Test]
        public void 굴린수가_확률과_같거나_크면_치명_아님()
        {
            Assert.IsFalse(CriticalRoll.Roll(0.3f, 0.3f));
            Assert.IsFalse(CriticalRoll.Roll(0.3f, 0.31f));
        }

        [Test]
        public void 확률0이면_굴린수가_0이어도_치명_아님()
        {
            Assert.IsFalse(CriticalRoll.Roll(0f, 0f));
        }

        [Test]
        public void 확률1이면_굴린수가_1이어도_치명()
        {
            Assert.IsTrue(CriticalRoll.Roll(1f, 1f));
        }

        [Test]
        public void 소수_퍼센트도_정확히()
        {
            Assert.IsTrue(CriticalRoll.Roll(0.055f, 0.054f));
            Assert.IsFalse(CriticalRoll.Roll(0.055f, 0.056f));
        }

        [Test]
        public void 확률0으로_1000번_굴려도_치명_0번()
        {
            int crits = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (CriticalRoll.Roll(0f)) crits++;
            }
            Assert.AreEqual(0, crits);
        }

        [Test]
        public void 확률1로_1000번_굴리면_전부_치명()
        {
            int crits = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (CriticalRoll.Roll(1f)) crits++;
            }
            Assert.AreEqual(1000, crits);
        }

        [Test]
        public void 배율은_2더하기_치명피해()
        {
            Assert.AreEqual(2f, CriticalRoll.Multiplier(0f), 0.0001f);
            Assert.AreEqual(2.7f, CriticalRoll.Multiplier(0.7f), 0.0001f);
        }
    }
}
