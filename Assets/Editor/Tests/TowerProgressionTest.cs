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

        [Test]
        public void 완전_일치는_컬러와_문양이_같아야()
        {
            Assert.IsTrue(TowerProgression.Matches(Def(1101), Def(1101), MergeMode.Exact));
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(1102), MergeMode.Exact));
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(1201), MergeMode.Exact));
        }

        [Test]
        public void 컬러_일치는_컬러만_같으면()
        {
            Assert.IsTrue(TowerProgression.Matches(Def(1101), Def(1107), MergeMode.Color));
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(1201), MergeMode.Color));
        }

        [Test]
        public void 문양_일치는_문양만_같으면()
        {
            Assert.IsTrue(TowerProgression.Matches(Def(1101), Def(1401), MergeMode.Emblem));
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(1102), MergeMode.Emblem));
        }

        [Test]
        public void 티어가_다르면_어느_모드든_안_맞음()
        {
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(2101), MergeMode.Exact));
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(2101), MergeMode.Color));
            Assert.IsFalse(TowerProgression.Matches(Def(1101), Def(2101), MergeMode.Emblem));
        }

        [Test]
        public void 재료_두_기를_보드_순서로_찾고_자기는_뺌()
        {
            Place(1101, 0);
            TowerController target = Place(1101, 2);
            TowerController first = Place(1101, 1);
            TowerController second = Place(1101, 4);
            Place(1101, 5);
            var materials = new List<TowerController>();

            Assert.IsTrue(TowerProgression.TryFindMaterials(target, MergeMode.Exact, m_slots, materials));
            Assert.AreEqual(2, materials.Count);
            Assert.IsFalse(materials.Contains(target));
            Assert.AreEqual(0, materials[0].SlotIndex);
            Assert.AreEqual(1, materials[1].SlotIndex);
        }

        [Test]
        public void 재료가_모자라면_false()
        {
            TowerController target = Place(1101, 0);
            Place(1101, 1);
            Place(1102, 2);
            Place(2101, 3);
            var materials = new List<TowerController>();

            Assert.IsFalse(TowerProgression.TryFindMaterials(target, MergeMode.Exact, m_slots, materials));
        }

        [Test]
        public void 컬러_일치_재료는_같은_컬러의_다른_문양도()
        {
            TowerController target = Place(1101, 0);
            Place(1201, 1);
            Place(1102, 2);
            Place(1107, 3);
            var materials = new List<TowerController>();

            Assert.IsTrue(TowerProgression.TryFindMaterials(target, MergeMode.Color, m_slots, materials));
            Assert.AreEqual(2, materials[0].SlotIndex);
            Assert.AreEqual(3, materials[1].SlotIndex);
        }

        [Test]
        public void 다섯_티어는_재료를_찾지_않음()
        {
            TowerController target = Place(5101, 0);
            Place(5101, 1);
            Place(5101, 2);
            var materials = new List<TowerController>();

            Assert.IsFalse(TowerProgression.TryFindMaterials(target, MergeMode.Exact, m_slots, materials));
        }

        [Test]
        public void 완전_일치_결과는_같은_컬러_문양의_다음_티어_하나()
        {
            TowerDatabase database = Database(2101, 2102, 2201);
            List<TowerDefinition> results = TowerProgression.GetResultCandidates(Def(1101), MergeMode.Exact, database, new[] { TowerEmblem.Sword, TowerEmblem.Axe });

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(2101, results[0].Id);
        }

        [Test]
        public void 컬러_일치_결과는_고른_문양_안의_같은_컬러_다음_티어()
        {
            TowerDatabase database = Database(2101, 2102, 2103, 2201);
            List<TowerDefinition> results = TowerProgression.GetResultCandidates(Def(1101), MergeMode.Color, database, new[] { TowerEmblem.Sword, TowerEmblem.Axe });

            CollectionAssert.AreEquivalent(new[] { 2101, 2102 }, results.ConvertAll(d => d.Id));
        }

        [Test]
        public void 문양_일치_결과는_같은_문양의_네_컬러_다음_티어()
        {
            TowerDatabase database = Database(2101, 2201, 2301, 2401, 2102);
            List<TowerDefinition> results = TowerProgression.GetResultCandidates(Def(1101), MergeMode.Emblem, database, new[] { TowerEmblem.Sword });

            CollectionAssert.AreEquivalent(new[] { 2101, 2201, 2301, 2401 }, results.ConvertAll(d => d.Id));
        }

        [Test]
        public void 결과가_CSV에_없으면_빈_목록()
        {
            TowerDatabase database = Database(2102);
            List<TowerDefinition> results = TowerProgression.GetResultCandidates(Def(1101), MergeMode.Exact, database, new[] { TowerEmblem.Sword });

            Assert.AreEqual(0, results.Count);
        }
    }
}
