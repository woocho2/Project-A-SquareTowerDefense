using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 23 4단계: 스킬 2의 틀 (템플릿 메서드) + 피해만 주는 시험 스킬
    public class AttackSkillTest
    {
        private GameObject m_root;
        private BoardGrid m_grid;
        private PathOccupancy m_occupancy;

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("AttackSkillTest");
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_root);
        }

        // 위력 10, 스킬 계수 2 → 기본 스킬 피해 20. 1티어라 티어 표 그대로
        private TowerController MakeTower(int id, float critRate = 0f)
        {
            var go = new GameObject($"Tower {id}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, 10f, 2, 3, 1, critRate, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, null);
            return tower;
        }

        private OccupyingFakeTarget MakeTarget(int pathIndex, float hp = 1000f) =>
            new OccupyingFakeTarget(m_occupancy, pathIndex, hp, m_root.transform);

        private DamageOnlySkill MakeSkill(StackModeSettings modes = null) =>
            new DamageOnlySkill(m_grid, m_occupancy, modes ?? new StackModeSettings());

        // ── 범위 ──

        [Test]
        public void 맵_전체는_모든_칸의_대상()
        {
            MakeTarget(0);
            MakeTarget(2);
            MakeTarget(2);
            MakeTarget(4);
            Assert.AreEqual(4, SkillArea.WholeMap(m_grid, m_occupancy).Count);
        }

        [Test]
        public void 착탄_1단계는_그_칸만()
        {
            MakeTarget(1);
            MakeTarget(0);
            Assert.AreEqual(1, SkillArea.Impact(m_grid, m_occupancy, 1, 1).Count);
        }

        [Test]
        public void 착탄_2단계는_3x3()
        {
            // 1번 칸 (1,0) 둘레 한 칸 안에 길 다섯 칸이 다 들어감
            for (int i = 0; i < 5; i++) MakeTarget(i);
            Assert.AreEqual(5, SkillArea.Impact(m_grid, m_occupancy, 1, 2).Count);
        }

        // ── 시험 스킬의 범위 ──

        [Test]
        public void 광역의_시험_스킬은_맵_전체에()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(2), c = MakeTarget(4);
            MakeSkill().Fire(MakeTower(1101), null);

            foreach (OccupyingFakeTarget target in new[] { a, b, c }) Assert.AreEqual(1, target.Hits.Count, $"{target.PathIndex}번 칸");
        }

        [Test]
        public void 단일의_시험_스킬은_발동시킨_적에게만()
        {
            OccupyingFakeTarget main = MakeTarget(2), other = MakeTarget(2);
            MakeSkill().Fire(MakeTower(1201), main);

            Assert.AreEqual(1, main.Hits.Count);
            Assert.AreEqual(0, other.Hits.Count, "같은 칸이어도 다른 적은 안 맞음");
        }

        // ── 공통 식 ──

        [Test]
        public void 피해는_위력_곱하기_계수이고_스킬로_들어감()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            MakeSkill().Fire(MakeTower(1101), null);

            Assert.AreEqual(20f, target.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(DamageSource.Skill, target.Hits[0].Source, "기본 공격(Basic)과 구별 — 빛 프리즘 등은 기본 공격에만 터짐");
        }

        [Test]
        public void 다_찬_스택은_1점6배로_쓰고_비움()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1101);
            tower.State.Stack.Add(60f);

            MakeSkill().Fire(tower, null);

            Assert.AreEqual(32f, target.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(0f, tower.State.Stack.Current, "쓴 스택은 0으로");
        }

        [Test]
        public void 안_찬_스택은_쓰지_않고_그대로()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1101);
            tower.State.Stack.Add(30f);

            MakeSkill().Fire(tower, null);

            Assert.AreEqual(20f, target.Hits[0].BaseDamage, 0.0001f, "문턱 전의 스택은 아무 영향도 없음");
            Assert.AreEqual(30f, tower.State.Stack.Current, 0.0001f);
        }

        [Test]
        public void 수동인데_누르지_않았으면_기본_피해이고_스택은_다_찬_채로()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1101);
            tower.State.Stack.Add(60f);

            MakeSkill(new StackModeSettings { ManualByDefault = true }).Fire(tower, null);

            Assert.AreEqual(20f, target.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(60f, tower.State.Stack.Current, 0.0001f);
        }

        [Test]
        public void 수동이고_눌렀으면_스택을_씀()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1101);
            tower.State.Stack.Add(60f);
            tower.State.Stack.RequestManualConsume();

            MakeSkill(new StackModeSettings { ManualByDefault = true }).Fire(tower, null);

            Assert.AreEqual(32f, target.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(0f, tower.State.Stack.Current);
        }

        // ── 치명 ──

        [Test]
        public void 치명은_한_번_굴려_모든_대상에_같이()
        {
            var targets = new List<OccupyingFakeTarget> { MakeTarget(0), MakeTarget(2), MakeTarget(4) };
            MakeSkill().Fire(MakeTower(1101, critRate: 1f), null);

            foreach (OccupyingFakeTarget target in targets)
            {
                Assert.IsTrue(target.Hits[0].IsCritical);
                Assert.AreEqual(40f, target.Hits[0].BaseDamage, 0.0001f, "20 × 치명 배율 2");
            }
        }

        // ── 대상 ──

        [Test]
        public void 죽은_대상은_건너뜀()
        {
            OccupyingFakeTarget dead = MakeTarget(0, hp: 0f);
            OccupyingFakeTarget alive = MakeTarget(2);
            MakeSkill().Fire(MakeTower(1101), null);

            Assert.AreEqual(0, dead.Hits.Count);
            Assert.AreEqual(1, alive.Hits.Count);
        }

        [Test]
        public void 대상이_없어도_에러_없이_끝남()
        {
            Assert.DoesNotThrow(() => MakeSkill().Fire(MakeTower(1101), null));
        }
    }
}
