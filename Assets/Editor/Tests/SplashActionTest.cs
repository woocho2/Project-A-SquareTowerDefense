using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class SplashActionTest
    {
        // 판은 TestBoards.MakeGrid. 타워는 1번 스폰 칸 (3,0), 사거리 1 → 2번 (2,1)과 4번 (2,-1)이 사거리 안
        // 2번 칸 둘레 3×3(반지름 1)에는 1번 (1,0), 2번, 3번 (1,1)이 들어가고 0번·4번은 밖
        private BoardGrid m_grid;
        private PathOccupancy m_occupancy;
        private GameObject m_root;

        [SetUp]
        public void SetUp()
        {
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
            m_root = new GameObject("SplashActionTest");
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_root != null) Object.DestroyImmediate(m_root);
        }

        private TowerController MakeTower(int attackCount, int mastery, float critRate = 0f, float critDamage = 0f)
        {
            var go = new GameObject("TestTower");
            go.transform.SetParent(m_root.transform);
            go.transform.position = new Vector3(3f, 0f, 0f);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(1101, 10f, 1, 1, attackCount, critRate, critDamage, mastery, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 1, null);
            return tower;
        }

        private OccupyingFakeTarget MakeTarget(int pathIndex, float hp) => new OccupyingFakeTarget(m_occupancy, pathIndex, hp, m_root.transform);

        private SplashAction MakeAction(IProjectileLauncher launcher = null)
        {
            return new SplashAction(m_grid, m_occupancy, launcher ?? new InstantLauncher());
        }

        private static void Act(SplashAction action, TowerController tower, AttackHandle handle = null)
        {
            IEnumerator routine = action.Act(tower, tower.Stats, handle ?? new AttackHandle());
            while (routine.MoveNext()) { }
        }

        [Test]
        public void 범위의_한_변은_1_3_5()
        {
            Assert.AreEqual(1, SplashAction.GetAreaSide(1));
            Assert.AreEqual(3, SplashAction.GetAreaSide(2));
            Assert.AreEqual(5, SplashAction.GetAreaSide(3));
        }

        [Test]
        public void 사거리에_대상이_없으면_CanAct는_false()
        {
            TowerController tower = MakeTower(1, 1);
            MakeTarget(0, 100f);
            Assert.IsFalse(MakeAction().CanAct(tower, tower.Stats));
        }

        [Test]
        public void 사거리에_대상이_있으면_CanAct는_true()
        {
            TowerController tower = MakeTower(1, 1);
            MakeTarget(2, 100f);
            Assert.IsTrue(MakeAction().CanAct(tower, tower.Stats));
        }

        [Test]
        public void Mastery1이면_착탄_칸의_적만_맞음()
        {
            TowerController tower = MakeTower(1, 1);
            OccupyingFakeTarget aimedA = MakeTarget(2, 1000f);
            OccupyingFakeTarget aimedB = MakeTarget(2, 1000f);
            OccupyingFakeTarget beside = MakeTarget(3, 1000f);

            Act(MakeAction(), tower);

            Assert.AreEqual(1, aimedA.Hits.Count);
            Assert.AreEqual(1, aimedB.Hits.Count);
            Assert.AreEqual(0, beside.Hits.Count);
        }

        [Test]
        public void Mastery2이면_착탄_칸_둘레_3x3의_적이_다_맞음()
        {
            TowerController tower = MakeTower(1, 2);
            OccupyingFakeTarget aimed = MakeTarget(2, 1000f);
            OccupyingFakeTarget left = MakeTarget(1, 1000f);       // 사거리 밖이어도 범위 안이면 맞음
            OccupyingFakeTarget up = MakeTarget(3, 1000f);
            OccupyingFakeTarget farBelow = MakeTarget(4, 1000f);   // 사거리 안이지만 범위 밖

            Act(MakeAction(), tower);

            Assert.AreEqual(1, aimed.Hits.Count);
            Assert.AreEqual(1, left.Hits.Count);
            Assert.AreEqual(1, up.Hits.Count);
            Assert.AreEqual(0, farBelow.Hits.Count);
        }

        [Test]
        public void 공격_횟수만큼_투사체()
        {
            TowerController tower = MakeTower(3, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();
            var handle = new AttackHandle();

            Act(MakeAction(launcher), tower, handle);
            Assert.AreEqual(3, handle.PendingShots);

            launcher.ArriveAll();
            Assert.AreEqual(3, target.Hits.Count);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 날아가는_동안은_피해가_없음()
        {
            TowerController tower = MakeTower(1, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            Assert.AreEqual(0, target.Hits.Count);
        }

        [Test]
        public void 착탄_칸은_우선순위로_고른_적의_칸()
        {
            TowerController tower = MakeTower(1, 1);
            tower.State.TargetPriority = TargetPriority.LeastHP;
            OccupyingFakeTarget strong = MakeTarget(2, 1000f);
            OccupyingFakeTarget weak = MakeTarget(4, 10f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            Assert.AreEqual(weak.Transform.position, launcher.LastTo);
            launcher.ArriveAll();
            Assert.AreEqual(0, strong.Hits.Count);
            Assert.AreEqual(1, weak.Hits.Count);
        }

        [Test]
        public void 공격_칸을_고르면_그_칸이_착탄_칸()
        {
            TowerController tower = MakeTower(1, 1);                 // 가까운 적 우선이면 2번 칸이 먼저
            tower.State.AttackCellIndex = 4;
            OccupyingFakeTarget first = MakeTarget(2, 1000f);
            OccupyingFakeTarget onCell = MakeTarget(4, 1000f);

            Act(MakeAction(), tower);

            Assert.AreEqual(0, first.Hits.Count);
            Assert.AreEqual(1, onCell.Hits.Count);
        }

        [Test]
        public void 치명이면_범위의_모두가_치명()
        {
            TowerController tower = MakeTower(1, 2, 1f, 0.5f);
            OccupyingFakeTarget aimed = MakeTarget(2, 1000f);
            OccupyingFakeTarget beside = MakeTarget(3, 1000f);

            Act(MakeAction(), tower);

            Assert.IsTrue(aimed.Hits[0].IsCritical);
            Assert.IsTrue(beside.Hits[0].IsCritical);
            Assert.AreEqual(25f, beside.Hits[0].BaseDamage, 0.0001f);   // 10 × (2 + 0.5)
        }

        [Test]
        public void 명중_이펙트_크기는_범위의_한_변()
        {
            TowerController tower = MakeTower(1, 2);
            MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            Assert.AreEqual(3f, launcher.LastEffectScale, 0.0001f);
        }

        [Test]
        public void 위성은_한_자리에_한_발()
        {
            TowerController tower = MakeTower(2, 1);
            MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            Assert.AreEqual(2, launcher.Froms.Count);
            Vector3 expected12 = tower.transform.position + SatelliteLayout.GetOffset(0, 2, tower.Satellite.Radius);
            Vector3 expected6 = tower.transform.position + SatelliteLayout.GetOffset(1, 2, tower.Satellite.Radius);
            Assert.AreEqual(expected12.y, launcher.Froms[0].y, 0.001f);
            Assert.AreEqual(expected6.y, launcher.Froms[1].y, 0.001f);
        }
    }
}
