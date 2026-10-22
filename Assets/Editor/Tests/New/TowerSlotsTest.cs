using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class TowerSlotsTest
    {
        private readonly List<GameObject> m_created = new List<GameObject>();

        private TowerController MakeTower()
        {
            var go = new GameObject("TestTower");
            m_created.Add(go);
            return go.AddComponent<TowerController>();
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (GameObject go in m_created) Object.DestroyImmediate(go);
            m_created.Clear();
        }

        [Test]
        public void 처음엔_모든_칸이_빔()
        {
            var slots = new TowerSlots(3);
            Assert.AreEqual(3, slots.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(slots.IsEmpty(i));
                Assert.IsNull(slots.Get(i));
            }
        }

        [Test]
        public void 첫_빈칸은_0번부터()
        {
            var slots = new TowerSlots(3);
            Assert.IsTrue(slots.TryGetFirstEmpty(out int index));
            Assert.AreEqual(0, index);
        }

        [Test]
        public void 앞칸이_차면_다음_빈칸()
        {
            var slots = new TowerSlots(3);
            slots.TryPlace(MakeTower(), 0);
            slots.TryPlace(MakeTower(), 1);
            Assert.IsTrue(slots.TryGetFirstEmpty(out int index));
            Assert.AreEqual(2, index);
        }

        [Test]
        public void 중간이_비면_그_칸()
        {
            var slots = new TowerSlots(3);
            slots.TryPlace(MakeTower(), 0);
            slots.TryPlace(MakeTower(), 2);
            Assert.IsTrue(slots.TryGetFirstEmpty(out int index));
            Assert.AreEqual(1, index);
        }

        [Test]
        public void 다_차면_빈칸이_없음()
        {
            var slots = new TowerSlots(2);
            slots.TryPlace(MakeTower(), 0);
            slots.TryPlace(MakeTower(), 1);
            Assert.IsFalse(slots.TryGetFirstEmpty(out _));
        }

        [Test]
        public void 놓으면_그_칸에서_꺼낼_수_있음()
        {
            var slots = new TowerSlots(3);
            TowerController tower = MakeTower();
            Assert.IsTrue(slots.TryPlace(tower, 1));
            Assert.AreSame(tower, slots.Get(1));
            Assert.IsFalse(slots.IsEmpty(1));
        }

        [Test]
        public void 이미_있는_칸에는_놓지_않음()
        {
            var slots = new TowerSlots(3);
            TowerController first = MakeTower();
            slots.TryPlace(first, 1);
            Assert.IsFalse(slots.TryPlace(MakeTower(), 1));
            Assert.AreSame(first, slots.Get(1));
        }

        [Test]
        public void 빼면_다시_빈칸()
        {
            var slots = new TowerSlots(3);
            slots.TryPlace(MakeTower(), 0);
            slots.Remove(0);
            Assert.IsTrue(slots.IsEmpty(0));
            Assert.IsTrue(slots.TryGetFirstEmpty(out int index));
            Assert.AreEqual(0, index);
        }
    }
}
