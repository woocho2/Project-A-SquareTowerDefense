using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SquareTD.Tests
{
    public class TowerTurnRunnerTest
    {
        // 행동하면 탄 1발을 쏜 것으로 치고, 그 영수증을 모아 두는 가짜 공격 방식 (테스트가 직접 "닿음"을 부름)
        private class FakeAction : TowerAction
        {
            public readonly List<AttackHandle> Handles = new List<AttackHandle>();
            public readonly List<TowerController> ActOrder;

            public FakeAction(List<TowerController> actOrder) : base(null, null, null)
            {
                ActOrder = actOrder;
            }

            public override bool CanAct(TowerController tower, TowerFinalStats stats) => true;

            public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
            {
                ActOrder.Add(tower);
                handle.AddShot();
                Handles.Add(handle);
                yield break;
            }
        }

        private GameObject m_root;
        private TowerSlots m_slots;
        private List<TowerController> m_actOrder;

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("TowerTurnRunnerTest");
            m_slots = new TowerSlots(3);
            m_actOrder = new List<TowerController>();
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_root != null) Object.DestroyImmediate(m_root);
        }

        private FakeAction PlaceTower(int slot, int id)
        {
            var go = new GameObject($"Tower {slot}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var action = new FakeAction(m_actOrder);
            var definition = new TowerDefinition(id, 10f, 1, 1, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, slot, action);
            m_slots.TryPlace(tower, slot);
            return action;
        }

        // Unity처럼 yield return된 IEnumerator 안으로 들어가며 한 걸음씩 진행 (끝나면 false)
        private static bool Step(Stack<IEnumerator> stack)
        {
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                if (top.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }

                return true;
            }
            return false;
        }

        private static void StepMany(Stack<IEnumerator> stack, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!Step(stack)) return;
            }
        }

        private static Stack<IEnumerator> Start(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            return stack;
        }

        [Test]
        public void 앞_타워의_탄이_닿기_전에는_다음_타워가_쏘지_않음()
        {
            PlaceTower(0, 1201);
            PlaceTower(1, 1202);
            var runner = new TowerTurnRunner(m_slots, 10f);

            Stack<IEnumerator> routine = Start(runner.RunAttackTowers());
            StepMany(routine, 20);

            Assert.AreEqual(1, m_actOrder.Count);
            Assert.AreEqual(0, m_actOrder[0].SlotIndex);
        }

        [Test]
        public void 앞_타워의_탄이_다_닿으면_다음_타워가_쏨()
        {
            FakeAction first = PlaceTower(0, 1201);
            PlaceTower(1, 1202);
            var runner = new TowerTurnRunner(m_slots, 10f);

            Stack<IEnumerator> routine = Start(runner.RunAttackTowers());
            StepMany(routine, 5);
            first.Handles[0].CompleteShot();
            StepMany(routine, 5);

            Assert.AreEqual(2, m_actOrder.Count);
            Assert.AreEqual(0, m_actOrder[0].SlotIndex);
            Assert.AreEqual(1, m_actOrder[1].SlotIndex);
        }

        [Test]
        public void 보드_순서대로_쏨()
        {
            FakeAction second = PlaceTower(2, 1202);
            FakeAction first = PlaceTower(0, 1201);
            var runner = new TowerTurnRunner(m_slots, 10f);

            Stack<IEnumerator> routine = Start(runner.RunAttackTowers());
            StepMany(routine, 5);
            first.Handles[0].CompleteShot();
            StepMany(routine, 5);
            second.Handles[0].CompleteShot();
            while (Step(routine)) { }

            Assert.AreEqual(0, m_actOrder[0].SlotIndex);
            Assert.AreEqual(2, m_actOrder[1].SlotIndex);
        }

        [Test]
        public void 버프_디버프_타워는_공격_단계에서_건너뜀()
        {
            PlaceTower(0, 1301);
            PlaceTower(1, 1401);
            var runner = new TowerTurnRunner(m_slots, 10f);

            Stack<IEnumerator> routine = Start(runner.RunAttackTowers());
            while (Step(routine)) { }

            Assert.AreEqual(0, m_actOrder.Count);
        }

        [Test]
        public void 안전_시간이_지나면_남은_탄을_끝난_것으로_치고_다음_타워로()
        {
            FakeAction first = PlaceTower(0, 1201);
            PlaceTower(1, 1202);
            var runner = new TowerTurnRunner(m_slots, 0f);              // 안전 시간 0 → 기다리지 않음

            LogAssert.Expect(LogType.Warning, new Regex(@"\[TowerTurnRunner\]"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[AttackHandle\]"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[TowerTurnRunner\]"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[AttackHandle\]"));

            Stack<IEnumerator> routine = Start(runner.RunAttackTowers());
            while (Step(routine)) { }

            Assert.AreEqual(2, m_actOrder.Count);
            Assert.IsTrue(first.Handles[0].IsDone);
        }
    }
}
