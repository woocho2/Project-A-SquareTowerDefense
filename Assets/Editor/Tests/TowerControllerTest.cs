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

        // ── 수업 20: 스탯 연결 ──

        private TowerController MakeTower(TowerDefinition definition, TowerDefinition tier1, ColorUpgradeState colorLevels)
        {
            m_go = new GameObject("TestTower");
            var tower = m_go.AddComponent<TowerController>();
            tower.Init(definition, 0, null, tier1, colorLevels);
            return tower;
        }

        private static TowerDefinition Def(int id, int action = 3) =>
            new TowerDefinition(id, 10f, 2, action, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);

        [Test]
        public void 처음_남은_행동력은_CSV가_아니라_계산된_Action()
        {
            TowerController tower = MakeTower(Def(5101), Def(1101), null);

            Assert.AreEqual(1, tower.Stats.Action, "1티어 3 − 5 → 최소 1");
            Assert.AreEqual(1, tower.State.RemainingAction);
        }

        [Test]
        public void 강화_상태의_레벨로_계산함()
        {
            var colorLevels = new ColorUpgradeState();
            for (int i = 0; i < 4; i++) colorLevels.TryUpgrade(TowerColor.Red);

            TowerController tower = MakeTower(Def(1101), null, colorLevels);

            Assert.AreEqual(5, tower.Stats.ColorLevel);
            Assert.AreEqual(8f, tower.Stats.AbilityValue, 0.0001f);
        }

        [Test]
        public void 수정자가_더해지면_바로_다시_계산()
        {
            TowerController tower = MakeTower(Def(1101), null, null);

            tower.State.Modifiers.Add(StatModifier.Percent("버프", StatType.Power, 0.5f));

            Assert.AreEqual(15f, tower.Stats.Power, 0.0001f);
        }

        [Test]
        public void Action이_줄면_남은_행동력도_그만큼_줆()
        {
            TowerController tower = MakeTower(Def(1101), null, null);
            tower.TickAction();                                                         // 남은 3 → 2

            tower.State.Modifiers.Add(StatModifier.Flat("버프", StatType.MaxAction, -1f)); // Action 3 → 2

            Assert.AreEqual(2, tower.Stats.Action);
            Assert.AreEqual(1, tower.State.RemainingAction, "2 + (−1)");
        }

        [Test]
        public void 버프가_풀리면_남은_행동력도_돌아옴()
        {
            TowerController tower = MakeTower(Def(1101), null, null);
            tower.TickAction();                                                         // 남은 2
            tower.State.Modifiers.Add(StatModifier.Flat("버프", StatType.MaxAction, -1f)); // 남은 1

            tower.State.Modifiers.RemoveBySource("버프");

            Assert.AreEqual(3, tower.Stats.Action);
            Assert.AreEqual(2, tower.State.RemainingAction, "1 + 1");
        }

        [Test]
        public void 남은_행동력은_0_아래로_안_내려감()
        {
            TowerController tower = MakeTower(Def(1101), null, null);
            tower.TickAction();
            tower.TickAction();                                                         // 남은 1
            tower.State.Modifiers.Add(StatModifier.Flat("버프", StatType.MaxAction, -2f)); // Action 3 → 1, 차이 −2

            Assert.AreEqual(0, tower.State.RemainingAction, "1 + (−2) = −1 → 0");
        }
    }
}
