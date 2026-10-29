using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class TowerProgressionTest
    {
        private const string Header =
            "ID,Name,Power,Range,Action,AttackCount,CriticalRate,CriticalDamage,Mastery,Stack,Duration,AbilityValue,ProjectileSpeed,TargetPriority";

        private readonly List<GameObject> m_created = new List<GameObject>();
        private TowerSlots m_slots;

        [SetUp]
        public void SetUp()
        {
            m_slots = new TowerSlots(6);
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (GameObject go in m_created) Object.DestroyImmediate(go);
            m_created.Clear();
        }

        private static TowerDefinition Def(int id) =>
            new TowerDefinition(id, 10f, 2, 3, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);

        private TowerController Place(int id, int slot)
        {
            var go = new GameObject($"Tower {id}");
            m_created.Add(go);
            var tower = go.AddComponent<TowerController>();
            tower.Init(Def(id), slot, null);
            m_slots.TryPlace(tower, slot);
            return tower;
        }

        private static TowerDatabase Database(params int[] ids)
        {
            var lines = new List<string> { Header };
            foreach (int id in ids) lines.Add($"{id},t,10,2,3,1,0,0,1,60,5,2,5,Closest");
            var database = new TowerDatabase();
            database.Load(string.Join("\n", lines));
            return database;
        }

        [Test]
        public void 티어_강화_비용은_5_15_45_135()
        {
            Assert.AreEqual(5, TowerProgression.GetTierUpgradeCost(1));
            Assert.AreEqual(15, TowerProgression.GetTierUpgradeCost(2));
            Assert.AreEqual(45, TowerProgression.GetTierUpgradeCost(3));
            Assert.AreEqual(135, TowerProgression.GetTierUpgradeCost(4));
        }

        [Test]
        public void 다섯_티어와_시너지는_티어_강화_비용_0()
        {
            Assert.AreEqual(0, TowerProgression.GetTierUpgradeCost(5));
            Assert.AreEqual(0, TowerProgression.GetTierUpgradeCost(6));
        }

        [Test]
        public void 일_사_티어만_합성_가능()
        {
            Assert.IsTrue(TowerProgression.CanMergeFrom(Def(1101)));
            Assert.IsTrue(TowerProgression.CanMergeFrom(Def(4214)));
            Assert.IsFalse(TowerProgression.CanMergeFrom(Def(5101)));
            Assert.IsFalse(TowerProgression.CanMergeFrom(Def(6001)));
        }

        private bool Find(out TowerController keep, out List<TowerController> materials, System.Func<TowerController, bool> isBusy = null) =>
            TowerProgression.TryFindMergeGroup(m_slots, isBusy, out keep, out materials);

        [Test]
        public void 같은_타워_셋이면_원래_있던_두_기_중_앞_칸이_남음()
        {
            TowerController first = Place(1101, 3);
            TowerController second = Place(1101, 1);
            TowerController newest = Place(1101, 5);

            Assert.IsTrue(Find(out TowerController keep, out List<TowerController> materials));
            Assert.AreEqual(second, keep, "먼저 있던 3번·1번 칸 중 1번");
            CollectionAssert.AreEquivalent(new[] { first, newest }, materials);
        }

        [Test]
        public void 방금_생긴_타워가_앞_칸이어도_재료가_됨()
        {
            TowerController first = Place(1101, 4);
            TowerController second = Place(1101, 5);
            TowerController newest = Place(1101, 0);

            Assert.IsTrue(Find(out TowerController keep, out List<TowerController> materials));
            Assert.AreEqual(first, keep);
            CollectionAssert.AreEquivalent(new[] { second, newest }, materials);
        }

        [Test]
        public void 둘뿐이면_합성하지_않음()
        {
            Place(1101, 0);
            Place(1101, 1);
            Assert.IsFalse(Find(out _, out _));
        }

        [Test]
        public void 컬러나_문양만_같으면_합성하지_않음()
        {
            Place(1101, 0);
            Place(1102, 1);
            Place(1201, 2);
            Place(2101, 3);
            Assert.IsFalse(Find(out _, out _));
        }

        [Test]
        public void 다섯_티어와_시너지는_합성하지_않음()
        {
            Place(5101, 0);
            Place(5101, 1);
            Place(5101, 2);
            Place(6001, 3);
            Place(6001, 4);
            Place(6001, 5);
            Assert.IsFalse(Find(out _, out _));
        }

        [Test]
        public void 넷이면_먼저_세운_셋으로()
        {
            TowerController a = Place(1101, 0);
            TowerController b = Place(1101, 4);
            TowerController c = Place(1101, 2);
            TowerController d = Place(1101, 1);

            Assert.IsTrue(Find(out TowerController keep, out List<TowerController> materials));
            Assert.AreEqual(a, keep);
            CollectionAssert.AreEquivalent(new[] { b, c }, materials);
            CollectionAssert.DoesNotContain(materials, d);
        }

        [Test]
        public void 합성_중인_타워는_빼고_찾음()
        {
            TowerController busy = Place(1101, 0);
            Place(1101, 1);
            Place(1101, 2);
            Assert.IsFalse(Find(out _, out _, tower => tower == busy));
        }
    }
}
