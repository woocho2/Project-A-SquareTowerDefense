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
    }
}
