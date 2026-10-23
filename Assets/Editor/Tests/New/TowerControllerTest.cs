using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class TowerControllerTest
    {
        // 판 없이 "쏠 대상이 있나"만 정해 주는 가짜 공격 방식
        private class FakeAction : TowerAction
        {
            public bool HasTarget = true;

            public FakeAction() : base(null, null, null) { }

            public override bool CanAct(TowerController tower, TowerFinalStats stats) => HasTarget;

            public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
            {
                yield break;
            }
        }

        private GameObject m_go;

        private TowerController MakeTower(int id, int action, TowerAction towerAction = null)
        {
            m_go = new GameObject("TestTower");
            var tower = m_go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, action, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, towerAction);
            return tower;
        }

        [Test]
        public void 대상이_없으면_0에서_기다림()
        {
            var action = new FakeAction { HasTarget = false };
            TowerController tower = MakeTower(1201, 2, action);

            Assert.IsFalse(tower.TickAction());
            Assert.AreEqual(1, tower.State.RemainingAction);
            Assert.IsFalse(tower.TickAction());
            Assert.AreEqual(0, tower.State.RemainingAction);
            Assert.IsFalse(tower.TickAction());
            Assert.AreEqual(0, tower.State.RemainingAction);
        }

        [Test]
        public void 기다리다_대상이_들어온_턴에_바로_행동()
        {
            var action = new FakeAction { HasTarget = false };
            TowerController tower = MakeTower(1201, 2, action);
            tower.TickAction();
            tower.TickAction();
            tower.TickAction();

            action.HasTarget = true;
            Assert.IsTrue(tower.TickAction());
            Assert.AreEqual(2, tower.State.RemainingAction);
        }

        [Test]
        public void 대상이_있으면_기다리지_않고_Action마다_행동()
        {
            TowerController tower = MakeTower(1201, 2, new FakeAction());
            Assert.IsFalse(tower.TickAction());
            Assert.IsTrue(tower.TickAction());
            Assert.IsFalse(tower.TickAction());
            Assert.IsTrue(tower.TickAction());
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
        public void 공격_칸은_고르지_않은_채로_시작()
        {
            TowerController tower = MakeTower(1201, 3);
            Assert.AreEqual(-1, tower.State.AttackCellIndex);
        }

        [Test]
        public void 옮기면_고른_공격_칸이_풀림()
        {
            TowerController tower = MakeTower(1201, 3);
            tower.State.AttackCellIndex = 5;
            tower.SetSlot(2, Vector3.zero);
            Assert.AreEqual(-1, tower.State.AttackCellIndex);
        }

        [Test]
        public void 우선순위는_CSV_값으로_시작()
        {
            TowerController tower = MakeTower(1101, 3);
            Assert.AreEqual(TargetPriority.Closest, tower.State.TargetPriority);
        }
    }
}
