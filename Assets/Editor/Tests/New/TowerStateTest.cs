using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerStateTest
    {
        private static TowerDefinition Def(int id, int action, TargetPriority priority) =>
            new TowerDefinition(id, 10f, 2, action, 1, 0f, 0f, 1, 60, 5, 2f, 5f, priority);

        [Test]
        public void 티어_강화는_우선순위와_공격_칸을_이어받음()
        {
            var old = new TowerState(Def(1201, 3, TargetPriority.Closest))
            {
                TargetPriority = TargetPriority.Boss,
                AttackCellIndex = 7
            };
            var created = new TowerState(Def(2201, 2, TargetPriority.Closest));

            created.CopyInheritedFrom(old);

            Assert.AreEqual(TargetPriority.Boss, created.TargetPriority);
            Assert.AreEqual(7, created.AttackCellIndex);
        }

        [Test]
        public void 남은_행동력은_새_타워_것()
        {
            var old = new TowerState(Def(1201, 3, TargetPriority.Closest)) { RemainingAction = 1 };
            var created = new TowerState(Def(2201, 2, TargetPriority.Closest));

            created.CopyInheritedFrom(old);

            Assert.AreEqual(2, created.RemainingAction);
        }

        [Test]
        public void 처음_스택은_비어_있고_최대는_CSV_값()
        {
            var state = new TowerState(Def(1201, 3, TargetPriority.Closest));

            Assert.IsNotNull(state.Stack, "생성자에서 스택을 만들어야 함");
            Assert.AreEqual(0f, state.Stack.Current);
            Assert.AreEqual(60, state.Stack.Max);
        }

        [Test]
        public void 티어_강화는_스택을_이어받음()
        {
            var old = new TowerState(Def(1201, 3, TargetPriority.Closest));
            old.Stack.Add(25f);
            var created = new TowerState(Def(2201, 2, TargetPriority.Closest));

            created.CopyInheritedFrom(old);

            Assert.AreEqual(25f, created.Stack.Current, 0.0001f);
            Assert.AreNotSame(old.Stack, created.Stack, "같은 TowerStack을 같이 쓰는 게 아니라 값만 옮김");
        }
    }
}
