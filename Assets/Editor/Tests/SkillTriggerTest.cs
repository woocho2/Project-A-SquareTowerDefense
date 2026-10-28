using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 스킬 2가 언제 터지나 — 광역·단일 모두 "타워의 적중 수" (수업 28, 20번 4-1장 [컬러별 틀])
    //   2026-10-28 사용자: 착탄 수(광역)·같은 적 n회(단일) → 둘 다 타워가 세는 적중 수
    public class SkillTriggerTest
    {
        // 맵(PathOccupancy)에 올라가지 않는 가짜 대상 — 광역 스킬의 맵 전체에는 잡히지 않음
        private class LooseFakeTarget : ITarget
        {
            public readonly List<DamageInfo> Hits = new List<DamageInfo>();

            public LooseFakeTarget(int pathIndex, Transform parent)
            {
                PathIndex = pathIndex;
                CurrentHp = MaxHp = 1000f;
                var go = new GameObject($"LooseFake {pathIndex}");
                go.transform.SetParent(parent);
                Transform = go.transform;
            }

            public Transform Transform { get; }
            public int PathIndex { get; }
            public bool IsAlive => CurrentHp > 0f;
            public bool IsBoss => false;
            public bool IsDummy => false;
            public EnemyController Body => null;
            public float CurrentHp { get; private set; }
            public float MaxHp { get; }
            public EnemyStats Stats => null;
            public EnemyStatusEffects Status => null;
            public float PreviewDamage(DamageInfo info) => info.BaseDamage;
            public void TakeDamage(DamageInfo info) { Hits.Add(info); CurrentHp -= info.BaseDamage; }
            public void SetSortingOrder(int order) { }
        }

        private GameObject m_root;
        private BoardGrid m_grid;
        private PathOccupancy m_occupancy;

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("SkillTriggerTest");
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_root);
        }

        // Duration 인자 = 스킬 2까지 필요한 적중 수
        private TowerController MakeTower(int id, int duration = 5, bool withSkill = true)
        {
            var go = new GameObject($"Tower {id}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, 3, 1, 0f, 0f, 1, 60, duration, 2f, 5f, TargetPriority.Closest);
            AttackSkill skill = withSkill ? new DamageOnlySkill(m_grid, m_occupancy, new StackModeSettings()) : null;
            tower.Init(definition, 0, null, null, null, skill);
            return tower;
        }

        private OccupyingFakeTarget OnMap(int pathIndex = 0) => new OccupyingFakeTarget(m_occupancy, pathIndex, 1000f, m_root.transform);
        private LooseFakeTarget OffMap(int pathIndex = 2) => new LooseFakeTarget(pathIndex, m_root.transform);

        private static void Turns(TowerController tower, int count)
        {
            for (int i = 0; i < count; i++) tower.OnEnemyTurnStart();
        }

        // 기본 공격이 target을 count번 맞힌 뒤 행동이 끝남 (TowerTurnRunner처럼 줄 선 스킬 2를 다 쏨). 스킬 2가 터졌는지 돌려줌
        private static bool Hits(TowerController tower, ITarget target, int count)
        {
            for (int i = 0; i < count; i++) tower.CountSkillHit(target);
            return FireAll(tower) > 0;
        }

        // 줄 선 스킬 2를 다 쏨. 몇 번 쐈는지 돌려줌
        private static int FireAll(TowerController tower)
        {
            int fired = 0;
            while (tower.FireNextPendingSkill()) fired++;
            return fired;
        }

        // ── 공통: 세는 법 ──

        [TestCase(1101)]
        [TestCase(1201)]
        public void 적중마다_카운트가_오름(int id)
        {
            TowerController tower = MakeTower(id);
            Hits(tower, OffMap(), 3);
            Assert.AreEqual(3, tower.State.SkillHitCount);
        }

        [TestCase(1101)]
        [TestCase(1201)]
        public void 적_턴으로는_세지_않음(int id)
        {
            TowerController tower = MakeTower(id);
            Turns(tower, 5);
            Assert.AreEqual(0, tower.State.SkillHitCount);
        }

        [TestCase(1101)]
        [TestCase(1201)]
        public void 이미_죽은_적은_세지_않음(int id)
        {
            TowerController tower = MakeTower(id, duration: 1);
            LooseFakeTarget dead = OffMap();
            dead.TakeDamage(new DamageInfo { BaseDamage = 5000f });

            Assert.IsFalse(tower.CountSkillHit(dead));
            Assert.AreEqual(0, tower.State.SkillHitCount);
            Assert.AreEqual(0, FireAll(tower));
        }

        [TestCase(1101)]
        [TestCase(1201)]
        public void 적중_때는_줄만_세우고_행동이_끝난_뒤에_쏨(int id)
        {
            OccupyingFakeTarget target = OnMap();
            TowerController tower = MakeTower(id, duration: 2);

            Assert.IsFalse(tower.CountSkillHit(target));
            Assert.IsTrue(tower.CountSkillHit(target), "2번째 적중에 찼음");
            Assert.AreEqual(0, target.Hits.Count, "아직 안 쏨 — 탄이 날아가는 중에 스킬 연출이 겹치지 않게");

            Assert.IsTrue(tower.FireNextPendingSkill());
            Assert.AreEqual(1, target.Hits.Count);
            Assert.AreEqual(DamageSource.Skill, target.Hits[0].Source);
            Assert.AreEqual(0, tower.State.SkillHitCount);
            Assert.IsFalse(tower.FireNextPendingSkill(), "줄이 비었음");
        }

        [TestCase(1101)]
        [TestCase(1201)]
        public void 한_행동에_두_번_차면_두_번_차례로(int id)
        {
            OccupyingFakeTarget target = OnMap();
            TowerController tower = MakeTower(id, duration: 2);

            for (int i = 0; i < 5; i++) tower.CountSkillHit(target);
            Assert.AreEqual(1, tower.State.SkillHitCount, "4번으로 두 번 차고 1번 남음");

            Assert.IsTrue(tower.FireNextPendingSkill());
            Assert.AreEqual(1, target.Hits.Count, "하나씩: 첫 스킬만");
            Assert.IsTrue(tower.FireNextPendingSkill());
            Assert.AreEqual(2, target.Hits.Count);
            Assert.IsFalse(tower.FireNextPendingSkill());
        }

        // ── 광역 ──

        [Test]
        public void 광역은_Duration_번_적중하면_맵_전체에_발동()
        {
            OccupyingFakeTarget a = OnMap(0), b = OnMap(3);
            TowerController tower = MakeTower(1101, duration: 5);

            Assert.IsFalse(Hits(tower, a, 4), "4번째는 아직");
            Assert.AreEqual(0, a.Hits.Count + b.Hits.Count);

            Assert.IsTrue(Hits(tower, a, 1));
            Assert.AreEqual(1, a.Hits.Count);
            Assert.AreEqual(1, b.Hits.Count, "맞힌 적만이 아니라 맵 전체");
        }

        [Test]
        public void 광역은_맞을_적이_없으면_카운트를_남기고_다음_적중에_발동()
        {
            TowerController tower = MakeTower(1101, duration: 5);
            Assert.IsFalse(Hits(tower, OffMap(), 5), "맵에 적이 없으면 발동하지 않음");
            Assert.AreEqual(5, tower.State.SkillHitCount, "스킬을 버리지 않고 기다림");

            OccupyingFakeTarget target = OnMap(3);
            Assert.IsTrue(Hits(tower, target, 1));
            Assert.AreEqual(1, target.Hits.Count);
            Assert.AreEqual(0, tower.State.SkillHitCount);
        }

        [Test]
        public void 광역은_적이_없으면_다_찬_스택도_남음()
        {
            TowerController tower = MakeTower(1101, duration: 1);
            tower.State.Stack.Add(60f);

            Hits(tower, OffMap(), 1);
            Assert.AreEqual(60f, tower.State.Stack.Current, 0.0001f);
        }

        // ── 단일 ──

        [Test]
        public void 단일은_다른_적을_맞힌_것도_합쳐_세고_그_순간_맞은_적에게_발동()
        {
            TowerController tower = MakeTower(1201, duration: 5);
            LooseFakeTarget a = OffMap(1), b = OffMap(3);

            Assert.IsFalse(Hits(tower, a, 4));
            Assert.IsTrue(Hits(tower, b, 1), "a 4번 + b 1번 = 5번");

            Assert.AreEqual(0, a.Hits.Count);
            Assert.AreEqual(1, b.Hits.Count, "5번째에 맞은 b에게");
            Assert.AreEqual(tower, b.Hits[0].Attacker);
            Assert.AreEqual(0, tower.State.SkillHitCount);
        }

        [Test]
        public void 단일은_줄_선_사이에_그_적이_죽으면_넘어감()
        {
            TowerController tower = MakeTower(1201, duration: 1);
            LooseFakeTarget target = OffMap();
            tower.CountSkillHit(target);

            target.TakeDamage(new DamageInfo { BaseDamage = 5000f });   // 같은 행동의 다른 탄에 죽음
            int before = target.Hits.Count;

            Assert.AreEqual(0, FireAll(tower), "그 적에게만 쏘는 스킬이라 쏠 곳이 없음");
            Assert.AreEqual(before, target.Hits.Count);
        }

        // ── 세지 않는 타워 ──

        [Test]
        public void 스킬이_없으면_아무_일도_없음()
        {
            TowerController tower = MakeTower(1101, duration: 1, withSkill: false);
            Assert.IsFalse(Hits(tower, OnMap(), 3));
            Assert.AreEqual(0, tower.State.SkillHitCount);
            Assert.DoesNotThrow(() => tower.OnBasicHitLanded(OffMap()));
            Assert.IsFalse(tower.FireNextPendingSkill());
        }

        // ── 이어받기 ──

        [Test]
        public void 티어_강화는_적중_카운트를_이어받음()
        {
            TowerController old = MakeTower(1101);
            Hits(old, OffMap(), 3);
            TowerController created = MakeTower(2101);

            created.State.CopyInheritedFrom(old.State);
            Assert.AreEqual(3, created.State.SkillHitCount);
        }
    }
}
