using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerStackTest
    {
        [Test]
        public void 처음엔_비어_있음()
        {
            var stack = new TowerStack(60);
            Assert.AreEqual(0f, stack.Current);
            Assert.AreEqual(60, stack.Max);
            Assert.IsFalse(stack.IsFull);
        }

        [Test]
        public void 최대는_1_이상()
        {
            Assert.AreEqual(1, new TowerStack(0).Max);
            Assert.AreEqual(1, new TowerStack(-5).Max);
        }

        // ── 쌓기 ──

        [Test]
        public void 행동마다_더한_만큼_쌓임()
        {
            var stack = new TowerStack(60);
            stack.Add(3f);
            stack.Add(3f);
            Assert.AreEqual(6f, stack.Current, 0.0001f);
        }

        [Test]
        public void 소수도_쌓임()
        {
            var stack = new TowerStack(60);
            stack.Add(0.5f);
            stack.Add(0.75f);
            Assert.AreEqual(1.25f, stack.Current, 0.0001f, "물결: 쌓이는 양이 % 로 늘면 소수가 됨");
        }

        [Test]
        public void 최대를_넘지_않음()
        {
            var stack = new TowerStack(60);
            stack.Add(58f);
            stack.Add(10f);
            Assert.AreEqual(60f, stack.Current, 0.0001f);
            Assert.IsTrue(stack.IsFull);
        }

        [Test]
        public void 다_찬_뒤의_공격은_버려짐()
        {
            var stack = new TowerStack(10);
            stack.Add(10f);
            stack.Add(5f);
            Assert.AreEqual(10f, stack.Current, 0.0001f);
        }

        [Test]
        public void 영_이하는_쌓지_않음()
        {
            var stack = new TowerStack(10);
            stack.Add(3f);
            stack.Add(0f);
            stack.Add(-2f);
            Assert.AreEqual(3f, stack.Current, 0.0001f);
        }

        // ── Full 이벤트 ──

        [Test]
        public void 막_다_찼을_때_Full_한_번()
        {
            var stack = new TowerStack(10);
            int count = 0;
            stack.Full += () => count++;

            stack.Add(6f);
            Assert.AreEqual(0, count);
            stack.Add(6f);
            Assert.AreEqual(1, count);
            stack.Add(6f);
            Assert.AreEqual(1, count, "이미 다 찬 뒤에는 다시 알리지 않음");
        }

        [Test]
        public void 쓰고_다시_차면_Full_다시()
        {
            var stack = new TowerStack(10);
            int count = 0;
            stack.Full += () => count++;

            stack.Add(10f);
            stack.Consume();
            stack.Add(10f);
            Assert.AreEqual(2, count);
        }

        // ── 쓰기 ──

        [Test]
        public void 쓰면_0으로()
        {
            var stack = new TowerStack(60);
            stack.Add(60f);
            stack.Consume();
            Assert.AreEqual(0f, stack.Current);
            Assert.IsFalse(stack.IsFull);
        }

        [Test]
        public void 해류면_최대의_절반에서_다시()
        {
            var stack = new TowerStack(60);
            stack.Add(60f);
            stack.Consume(keepHalf: true);
            Assert.AreEqual(30f, stack.Current, 0.0001f);
        }

        // ── 최대가 바뀔 때 ──

        [Test]
        public void 최대가_줄면_넘치는_만큼_버림()
        {
            // 20번 4-1장 예: 100 / 120 → 60 / 60으로 바로 다 참
            var stack = new TowerStack(120);
            stack.Add(100f);
            int count = 0;
            stack.Full += () => count++;

            stack.SetMax(60);
            Assert.AreEqual(60, stack.Max);
            Assert.AreEqual(60f, stack.Current, 0.0001f);
            Assert.IsTrue(stack.IsFull);
            Assert.AreEqual(1, count, "줄어서 다 차도 Full");
        }

        [Test]
        public void 최대가_줄어도_적게_쌓였으면_그대로()
        {
            var stack = new TowerStack(120);
            stack.Add(40f);
            stack.SetMax(60);
            Assert.AreEqual(40f, stack.Current, 0.0001f);
            Assert.IsFalse(stack.IsFull);
        }

        [Test]
        public void 최대가_늘어도_개수는_그대로()
        {
            // 만개 존을 벗어나 레벨 5로 돌아가 최대 스택이 다시 늘어도 개수는 그대로
            var stack = new TowerStack(30);
            stack.Add(30f);
            stack.SetMax(60);
            Assert.AreEqual(30f, stack.Current, 0.0001f);
            Assert.IsFalse(stack.IsFull);
        }

        // ── 수동 소모 ──

        [Test]
        public void 다_찼을_때만_수동_요청을_받음()
        {
            var stack = new TowerStack(10);
            stack.Add(5f);
            stack.RequestManualConsume();
            Assert.IsFalse(stack.ManualRequested, "안 찼으면 무시");

            stack.Add(5f);
            stack.RequestManualConsume();
            Assert.IsTrue(stack.ManualRequested);
        }

        [Test]
        public void 쓰면_수동_요청도_꺼짐()
        {
            var stack = new TowerStack(10);
            stack.Add(10f);
            stack.RequestManualConsume();
            stack.Consume();
            Assert.IsFalse(stack.ManualRequested);
        }

        // ── 이어받기 ──

        [Test]
        public void 티어_강화는_스택을_이어받음()
        {
            var old = new TowerStack(60);
            old.Add(25f);
            var created = new TowerStack(60);
            created.CopyFrom(old);
            Assert.AreEqual(25f, created.Current, 0.0001f);
        }

        [Test]
        public void 미스릴이_되면_새_최대를_넘는_만큼_버림()
        {
            var old = new TowerStack(60);
            old.Add(50f);
            var mithril = new TowerStack(30);
            mithril.CopyFrom(old);
            Assert.AreEqual(30f, mithril.Current, 0.0001f);
            Assert.IsTrue(mithril.IsFull);
        }

        [Test]
        public void 수동_요청은_이어받지_않음()
        {
            var old = new TowerStack(10);
            old.Add(10f);
            old.RequestManualConsume();
            var created = new TowerStack(10);
            created.CopyFrom(old);
            Assert.IsFalse(created.ManualRequested);
        }
    }
}
