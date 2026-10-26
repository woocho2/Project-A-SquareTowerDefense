using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 23 4b: 스킬 2가 언제 터지나 (광역 = 스킬 주기 턴, 단일 = 같은 적이 맞은 횟수)
    public class SkillTriggerTest
    {
        // 본체(EnemyController)를 가진 가짜 대상 — 단일 타워의 적중 수는 본체가 들고 있음
        private class BodyFakeTarget : ITarget
        {
            public readonly List<DamageInfo> Hits = new List<DamageInfo>();

            public BodyFakeTarget(EnemyController body, int pathIndex, Transform parent)
            {
                Body = body;
                PathIndex = pathIndex;
                CurrentHp = MaxHp = 1000f;
                var go = new GameObject($"BodyFake {pathIndex}");
                go.transform.SetParent(parent);
                Transform = go.transform;
            }

            public Transform Transform { get; }
            public int PathIndex { get; }
            public bool IsAlive => CurrentHp > 0f;
            public bool IsBoss => false;
            public bool IsDummy => false;
            public EnemyController Body { get; }
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
        private EnemyDefinition m_enemyDefinition;

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
            if (m_enemyDefinition != null) Object.DestroyImmediate(m_enemyDefinition);
        }

        // Duration 인자 = 광역은 스킬 주기(턴), 단일은 필요 적중 수
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

        private BodyFakeTarget MakeBodyTarget(int pathIndex = 2)
        {
            if (m_enemyDefinition == null)
            {
                m_enemyDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
                var so = new SerializedObject(m_enemyDefinition);
                so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
                so.FindProperty("<MaxHP>k__BackingField").floatValue = 1000f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var go = new GameObject("Enemy");
            go.transform.SetParent(m_root.transform);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Setup(m_enemyDefinition, null, 1);
            return new BodyFakeTarget(enemy, pathIndex, m_root.transform);
        }

        private static void Turns(TowerController tower, int count)
        {
            for (int i = 0; i < count; i++) tower.OnEnemyTurnStart();
        }

        // ── 광역: 스킬 주기 ──

        [Test]
        public void 광역은_턴마다_카운트가_오름()
        {
            TowerController tower = MakeTower(1101);
            Turns(tower, 3);
            Assert.AreEqual(3, tower.State.SkillTurnCount);
        }

        [Test]
        public void 광역은_Duration_턴이_되면_발동하고_0으로()
        {
            var target = new OccupyingFakeTarget(m_occupancy, 0, 1000f, m_root.transform);
            TowerController tower = MakeTower(1101, duration: 5);

            Turns(tower, 4);
            Assert.IsFalse(tower.TryFireTurnSkill(), "4턴째는 아직");
            Assert.AreEqual(0, target.Hits.Count);

            Turns(tower, 1);
            Assert.IsTrue(tower.TryFireTurnSkill());
            Assert.AreEqual(1, target.Hits.Count);
            Assert.AreEqual(DamageSource.Skill, target.Hits[0].Source);
            Assert.AreEqual(0, tower.State.SkillTurnCount);
        }

        [Test]
        public void 맞을_적이_없으면_카운트를_남기고_적이_오면_발동()
        {
            TowerController tower = MakeTower(1101, duration: 5);
            Turns(tower, 5);
            Assert.IsFalse(tower.TryFireTurnSkill(), "맵에 적이 없으면 발동하지 않음");
            Assert.AreEqual(5, tower.State.SkillTurnCount, "스킬을 버리지 않고 기다림");

            var target = new OccupyingFakeTarget(m_occupancy, 3, 1000f, m_root.transform);
            Assert.IsTrue(tower.TryFireTurnSkill());
            Assert.AreEqual(1, target.Hits.Count);
        }

        [Test]
        public void 적이_없으면_다_찬_스택도_남음()
        {
            TowerController tower = MakeTower(1101, duration: 1);
            tower.State.Stack.Add(60f);
            Turns(tower, 1);

            tower.TryFireTurnSkill();
            Assert.AreEqual(60f, tower.State.Stack.Current, 0.0001f);
        }

        [Test]
        public void 단일은_턴으로_발동하지_않음()
        {
            new OccupyingFakeTarget(m_occupancy, 0, 1000f, m_root.transform);
            TowerController tower = MakeTower(1201, duration: 1);
            Turns(tower, 5);

            Assert.AreEqual(0, tower.State.SkillTurnCount);
            Assert.IsFalse(tower.TryFireTurnSkill());
        }

        // ── 단일: 같은 적이 맞은 횟수 ──

        [Test]
        public void 단일은_적중_수가_다_차면_그_적에게_발동하고_0으로()
        {
            TowerController tower = MakeTower(1201, duration: 5);
            BodyFakeTarget target = MakeBodyTarget();
            TowerEmblem emblem = tower.Definition.Emblem;

            for (int i = 0; i < 4; i++) target.Body.HitCounter.Add(emblem);
            tower.OnBasicHitLanded(target);
            Assert.AreEqual(0, target.Hits.Count, "4회는 아직");

            target.Body.HitCounter.Add(emblem);
            tower.OnBasicHitLanded(target);
            Assert.AreEqual(1, target.Hits.Count);
            Assert.AreEqual(DamageSource.Skill, target.Hits[0].Source);
            Assert.AreEqual(0, target.Body.HitCounter.Get(emblem));
        }

        [Test]
        public void 하위_타워가_쌓은_수를_상위_타워가_터뜨림()
        {
            // 20번 4-1장: 하위(10회)가 7대를 맞힌 뒤 상위(5회)가 1대만 맞혀도 바로 상위의 스킬
            TowerController high = MakeTower(4201, duration: 5);
            BodyFakeTarget target = MakeBodyTarget();
            TowerEmblem emblem = high.Definition.Emblem;

            for (int i = 0; i < 7; i++) target.Body.HitCounter.Add(emblem);
            target.Body.HitCounter.Add(emblem);                     // 상위의 한 대
            high.OnBasicHitLanded(target);

            Assert.AreEqual(1, target.Hits.Count);
            Assert.AreEqual(high, target.Hits[0].Attacker, "맞힌 타워의 스킬이 터짐");
        }

        [Test]
        public void 다른_적은_따로_셈()
        {
            TowerController tower = MakeTower(1201, duration: 2);
            BodyFakeTarget a = MakeBodyTarget(1), b = MakeBodyTarget(3);
            TowerEmblem emblem = tower.Definition.Emblem;

            a.Body.HitCounter.Add(emblem);
            b.Body.HitCounter.Add(emblem);
            tower.OnBasicHitLanded(a);
            tower.OnBasicHitLanded(b);

            Assert.AreEqual(0, a.Hits.Count + b.Hits.Count, "합쳐서 2회여도 어느 쪽에도 발동하지 않음");
        }

        [Test]
        public void 광역은_적중_수로_발동하지_않음()
        {
            TowerController tower = MakeTower(1101, duration: 1);
            BodyFakeTarget target = MakeBodyTarget();
            for (int i = 0; i < 5; i++) target.Body.HitCounter.Add(tower.Definition.Emblem);

            tower.OnBasicHitLanded(target);
            Assert.AreEqual(0, target.Hits.Count);
        }

        // ── 스킬이 없는 타워 ──

        [Test]
        public void 스킬이_없으면_아무_일도_없음()
        {
            TowerController tower = MakeTower(1101, duration: 1, withSkill: false);
            Turns(tower, 3);
            Assert.AreEqual(0, tower.State.SkillTurnCount);
            Assert.IsFalse(tower.TryFireTurnSkill());
            Assert.DoesNotThrow(() => tower.OnBasicHitLanded(MakeBodyTarget()));
        }

        // ── 이어받기 ──

        [Test]
        public void 티어_강화는_스킬_주기_카운트를_이어받음()
        {
            TowerController old = MakeTower(1101);
            Turns(old, 3);
            TowerController created = MakeTower(2101);

            created.State.CopyInheritedFrom(old.State);
            Assert.AreEqual(3, created.State.SkillTurnCount);
        }
    }
}
