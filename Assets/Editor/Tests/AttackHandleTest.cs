using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquareTD.Tests
{
    public class AttackHandleTest
    {
        [Test]
        public void 한_발도_안_쏘면_바로_끝()
        {
            var handle = new AttackHandle();
            Assert.IsTrue(handle.IsDone);
            Assert.AreEqual(0, handle.PendingShots);
        }

        [Test]
        public void 쏘면_끝나지_않음()
        {
            var handle = new AttackHandle();
            handle.AddShot();
            Assert.IsFalse(handle.IsDone);
            Assert.AreEqual(1, handle.PendingShots);
        }

        [Test]
        public void 세_발_쏘고_두_발_닿으면_아직()
        {
            var handle = new AttackHandle();
            handle.AddShot();
            handle.AddShot();
            handle.AddShot();
            handle.CompleteShot();
            handle.CompleteShot();
            Assert.IsFalse(handle.IsDone);
            Assert.AreEqual(1, handle.PendingShots);
        }

        [Test]
        public void 쏜_만큼_다_닿으면_끝()
        {
            var handle = new AttackHandle();
            handle.AddShot();
            handle.AddShot();
            handle.CompleteShot();
            handle.CompleteShot();
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 쏜_것보다_많이_닿아도_0_아래로_안_내려감()
        {
            var handle = new AttackHandle();
            handle.AddShot();
            handle.CompleteShot();

            LogAssert.Expect(LogType.Warning, "[AttackHandle] 쏜 탄보다 도착한 탄이 많음");
            handle.CompleteShot();

            Assert.AreEqual(0, handle.PendingShots);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 두_번_도착한_탄이_있어도_다음_탄으로_끝날_수_있음()
        {
            var handle = new AttackHandle();
            LogAssert.Expect(LogType.Warning, "[AttackHandle] 쏜 탄보다 도착한 탄이 많음");
            handle.CompleteShot();

            handle.AddShot();
            handle.CompleteShot();
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void ForceComplete는_남은_탄을_0으로()
        {
            var handle = new AttackHandle();
            handle.AddShot();
            handle.AddShot();

            LogAssert.Expect(LogType.Warning, "[AttackHandle] 도착하지 못한 탄 2발을 끝난 것으로 침");
            handle.ForceComplete();

            Assert.AreEqual(0, handle.PendingShots);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 다_닿은_뒤의_ForceComplete는_경고_없음()
        {
            var handle = new AttackHandle();
            handle.AddShot();
            handle.CompleteShot();
            handle.ForceComplete();

            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(handle.IsDone);
        }
    }
}
