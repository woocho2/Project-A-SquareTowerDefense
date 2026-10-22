using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class TowerControllerTest
    {
        private GameObject m_go;

        private TowerController MakeTower(int id, int action)
        {
            m_go = new GameObject("TestTower");
            var tower = m_go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, action, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0);
            return tower;
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
        }

        [Test]
        public void 처음_남은_행동력은_Action()
        {
            TowerController tower = MakeTower(1101, 3);
            Assert.AreEqual(3, tower.State.RemainingAction);
        }

        [Test]
        public void Action3이면_세_번째_턴마다_행동()
        {
            TowerController tower = MakeTower(1101, 3);
            Assert.IsFalse(tower.TickAction());
            Assert.IsFalse(tower.TickAction());
            Assert.IsTrue(tower.TickAction());
            Assert.IsFalse(tower.TickAction());
            Assert.IsFalse(tower.TickAction());
            Assert.IsTrue(tower.TickAction());
        }

        [Test]
        public void 행동하면_다시_꽉_참()
        {
            TowerController tower = MakeTower(1101, 3);
            tower.TickAction();
            tower.TickAction();
            tower.TickAction();
            Assert.AreEqual(3, tower.State.RemainingAction);
        }

        [Test]
        public void Action1이면_매_턴_행동()
        {
            TowerController tower = MakeTower(1101, 1);
            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(tower.TickAction());
            }
        }

        [Test]
        public void 광역과_단일은_공격_타워()
        {
            Assert.IsTrue(MakeTower(1101, 3).IsAttackTower);
            CleanUp();
            Assert.IsTrue(MakeTower(1201, 3).IsAttackTower);
            CleanUp();
            Assert.IsFalse(MakeTower(1301, 3).IsAttackTower);
            CleanUp();
            Assert.IsFalse(MakeTower(1401, 3).IsAttackTower);
        }

        [Test]
        public void 우선순위는_CSV_값으로_시작()
        {
            TowerController tower = MakeTower(1101, 3);
            Assert.AreEqual(TargetPriority.Closest, tower.State.TargetPriority);
        }
    }
}
