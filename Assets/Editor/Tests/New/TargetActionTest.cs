using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SquareTD.Tests
{
    public class TargetActionTest
    {
        // 판은 TestBoards.MakeGrid (BoardGridTest와 같음). 타워는 1번 스폰 칸 (3,0)
        // 사거리 1이면 2번 (2,1)과 4번 (2,-1)만 들어오고, 0번 (0,0)은 거리 3이라 밖
        private BoardGrid m_grid;
        private PathOccupancy m_occupancy;
        private GameObject m_root;

        [SetUp]
        public void SetUp()
        {
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
            m_root = new GameObject("TargetActionTest");
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
            var definition = new TowerDefinition(1201, 10f, 1, 1, attackCount, critRate, critDamage, mastery, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 1, null);
            return tower;
        }

        private OccupyingFakeTarget MakeTarget(int pathIndex, float hp) => new OccupyingFakeTarget(m_occupancy, pathIndex, hp, m_root.transform);

        private TargetAction MakeAction(IProjectileLauncher launcher = null)
        {
            return new TargetAction(m_grid, m_occupancy, launcher ?? new InstantLauncher());
        }

        private static void Run(IEnumerator routine)
        {
            while (routine.MoveNext()) { }
        }

        private static void Act(TargetAction action, TowerController tower, AttackHandle handle = null)
        {
            Run(action.Act(tower, tower.Stats, handle ?? new AttackHandle()));
        }

        [Test]
        public void 발_수는_Mastery_곱하기_2()
        {
            Assert.AreEqual(2, TargetAction.GetShotsPerAttack(1));
            Assert.AreEqual(4, TargetAction.GetShotsPerAttack(2));
            Assert.AreEqual(6, TargetAction.GetShotsPerAttack(3));
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
        public void 공격_횟수3_Mastery1이면_6발()
        {
            TowerController tower = MakeTower(3, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            Act(MakeAction(), tower);
            Assert.AreEqual(6, target.Hits.Count);
        }

        [Test]
        public void 공격_횟수1_Mastery2이면_4발()
        {
            TowerController tower = MakeTower(1, 2);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            Act(MakeAction(), tower);
            Assert.AreEqual(4, target.Hits.Count);
        }

        [Test]
        public void 사거리_밖의_대상은_맞지_않음()
        {
            TowerController tower = MakeTower(3, 1);
            OccupyingFakeTarget inside = MakeTarget(2, 1000f);
            OccupyingFakeTarget outside = MakeTarget(0, 1000f);
            Act(MakeAction(), tower);
            Assert.AreEqual(6, inside.Hits.Count);
            Assert.AreEqual(0, outside.Hits.Count);
        }

        [Test]
        public void 바로_닿는_탄이면_쏘다가_죽은_뒤의_탄은_다음_대상에게()
        {
            TowerController tower = MakeTower(3, 1);                 // 6발, 한 발 10
            OccupyingFakeTarget first = MakeTarget(2, 25f);                    // 3발에 죽음
            OccupyingFakeTarget second = MakeTarget(4, 1000f);
            Act(MakeAction(), tower);
            Assert.AreEqual(3, first.Hits.Count);
            Assert.AreEqual(3, second.Hits.Count);
        }

        [Test]
        public void 사거리의_대상이_다_죽으면_남은_탄은_쏘지_않고_끝남()
        {
            TowerController tower = MakeTower(3, 1);
            OccupyingFakeTarget target = MakeTarget(2, 10f);                   // 1발에 죽음
            var handle = new AttackHandle();
            Assert.DoesNotThrow(() => Act(MakeAction(), tower, handle));
            Assert.AreEqual(1, target.Hits.Count);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 기본_공격의_DamageInfo()
        {
            TowerController tower = MakeTower(1, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            Act(MakeAction(), tower);

            DamageInfo hit = target.Hits[0];
            Assert.AreSame(tower, hit.Attacker);
            Assert.AreEqual(DamageSource.Basic, hit.Source);
            Assert.AreEqual(TowerEmblem.Sword, hit.Emblem);
            Assert.AreEqual(AttackAttribute.Slash, hit.Attribute);
            Assert.AreEqual(10f, hit.BaseDamage, 0.0001f);
            Assert.IsFalse(hit.IsCritical);
            Assert.AreEqual(1, hit.Distance);
        }

        [Test]
        public void 치명_확률1이면_모든_탄이_치명이고_피해는_2_더하기_치명피해_배()
        {
            TowerController tower = MakeTower(1, 1, 1f, 0.5f);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            Act(MakeAction(), tower);

            Assert.AreEqual(2, target.Hits.Count);
            foreach (DamageInfo hit in target.Hits)
            {
                Assert.IsTrue(hit.IsCritical);
                Assert.AreEqual(25f, hit.BaseDamage, 0.0001f);       // 10 × (2 + 0.5)
            }
        }

        [Test]
        public void 탄이_날아가는_동안은_피해가_없고_AttackHandle도_끝나지_않음()
        {
            TowerController tower = MakeTower(3, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();
            var handle = new AttackHandle();

            Act(MakeAction(launcher), tower, handle);

            Assert.AreEqual(0, target.Hits.Count);
            Assert.AreEqual(6, handle.PendingShots);
            Assert.IsFalse(handle.IsDone);
        }

        [Test]
        public void 탄이_다_닿으면_피해가_들어가고_AttackHandle이_끝남()
        {
            TowerController tower = MakeTower(3, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();
            var handle = new AttackHandle();

            Act(MakeAction(launcher), tower, handle);
            launcher.ArriveAll();

            Assert.AreEqual(6, target.Hits.Count);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 미리_나눔_4발에_죽는_적에게_6발이면_4발과_다음_적_2발()
        {
            TowerController tower = MakeTower(3, 1);                 // 6발, 한 발 10
            OccupyingFakeTarget first = MakeTarget(2, 40f);                    // 4발에 죽음
            OccupyingFakeTarget second = MakeTarget(4, 1000f);
            var launcher = new DelayedLauncher();
            var handle = new AttackHandle();

            Act(MakeAction(launcher), tower, handle);
            launcher.ArriveAll();

            Assert.AreEqual(4, first.Hits.Count);
            Assert.AreEqual(2, second.Hits.Count);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 미리_나눔_치명이면_예상_피해가_커서_덜_쏨()
        {
            TowerController tower = MakeTower(3, 1, 1f, 0.5f);       // 6발, 한 발 25 (치명)
            OccupyingFakeTarget first = MakeTarget(2, 40f);                    // 2발에 죽음
            OccupyingFakeTarget second = MakeTarget(4, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);
            launcher.ArriveAll();

            Assert.AreEqual(2, first.Hits.Count);
            Assert.AreEqual(4, second.Hits.Count);
        }

        [Test]
        public void 미리_나눔_다_죽을_예정이면_남은_탄은_원래_우선순위_대상에게()
        {
            TowerController tower = MakeTower(3, 1);                 // 6발, 한 발 10, 가까운 적
            OccupyingFakeTarget first = MakeTarget(2, 10f);                    // 둘 다 거리 1, 목록의 앞쪽이 2번 칸
            OccupyingFakeTarget second = MakeTarget(4, 10f);
            var launcher = new DelayedLauncher();
            var handle = new AttackHandle();

            Act(MakeAction(launcher), tower, handle);

            Assert.AreEqual(6, handle.PendingShots);                  // 다 죽을 예정이어도 6발을 다 쏨
            launcher.ArriveAll();
            Assert.AreEqual(1, first.Hits.Count);                     // 닿을 때는 이미 죽어서 남은 탄은 피해 없음
            Assert.AreEqual(1, second.Hits.Count);
            Assert.IsTrue(handle.IsDone);
        }

        [Test]
        public void 우선순위_체력_적은_적이면_체력이_적은_쪽을_쏨()
        {
            TowerController tower = MakeTower(1, 1);
            tower.State.TargetPriority = TargetPriority.LeastHP;
            OccupyingFakeTarget strong = MakeTarget(2, 1000f);
            OccupyingFakeTarget weak = MakeTarget(4, 500f);

            Act(MakeAction(), tower);

            Assert.AreEqual(0, strong.Hits.Count);
            Assert.AreEqual(2, weak.Hits.Count);
        }

        [Test]
        public void 우선순위를_바꾸면_다음_행동부터_노리는_적이_바뀜()
        {
            TowerController tower = MakeTower(1, 1);
            OccupyingFakeTarget strong = MakeTarget(2, 1000f);
            OccupyingFakeTarget weak = MakeTarget(4, 500f);
            TargetAction action = MakeAction();

            tower.State.TargetPriority = TargetPriority.MostHP;
            Act(action, tower);
            tower.State.TargetPriority = TargetPriority.LeastHP;
            Act(action, tower);

            Assert.AreEqual(2, strong.Hits.Count);
            Assert.AreEqual(2, weak.Hits.Count);
        }

        [Test]
        public void 공격_칸을_고르면_우선순위보다_그_칸의_적_먼저()
        {
            TowerController tower = MakeTower(1, 1);                 // 가까운 적 우선이면 2번 칸이 먼저
            tower.State.AttackCellIndex = 4;
            OccupyingFakeTarget first = MakeTarget(2, 1000f);
            OccupyingFakeTarget onCell = MakeTarget(4, 1000f);

            Act(MakeAction(), tower);

            Assert.AreEqual(0, first.Hits.Count);
            Assert.AreEqual(2, onCell.Hits.Count);
        }

        [Test]
        public void 공격_칸의_적이_죽을_예정이면_남은_탄은_우선순위대로()
        {
            TowerController tower = MakeTower(3, 1);                 // 6발, 한 발 10
            tower.State.AttackCellIndex = 4;
            OccupyingFakeTarget other = MakeTarget(2, 1000f);
            OccupyingFakeTarget onCell = MakeTarget(4, 20f);          // 2발에 죽음
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);
            launcher.ArriveAll();

            Assert.AreEqual(2, onCell.Hits.Count);
            Assert.AreEqual(4, other.Hits.Count);
        }

        [Test]
        public void 탄은_대상_자리로_타워의_모양과_속력으로()
        {
            TowerController tower = MakeTower(1, 1);
            OccupyingFakeTarget target = MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            Assert.AreEqual(201, launcher.LastLookId);                // 1201 → 201
            Assert.AreEqual(target.Transform.position, launcher.LastTo);
            Assert.AreEqual(5f, launcher.LastSpeed, 0.0001f);         // TowerDefinition의 ProjectileSpeed
            Assert.AreEqual(1f, launcher.LastEffectScale, 0.0001f);   // 단일은 명중 이펙트 크기를 표 그대로
        }

        [Test]
        public void 위성은_AttackCount개이고_자리마다_발_수만큼_벌려서_뜸()
        {
            TowerController tower = MakeTower(2, 1);                  // 위성 2개(12시·6시) × 2발
            MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            SatelliteSettings satellite = tower.Satellite;
            Vector3 center = tower.transform.position;
            Assert.AreEqual(4, launcher.Froms.Count);
            for (int s = 0; s < 2; s++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Vector3 expected = center
                        + SatelliteLayout.GetOffset(s, 2, satellite.Radius)
                        + SatelliteLayout.GetSpread(s, 2, j, 2, satellite.GroupSpacing);
                    Vector3 actual = launcher.Froms[s * 2 + j];
                    Assert.AreEqual(expected.x, actual.x, 0.001f, $"위성 {s}, {j}번째 탄의 x");
                    Assert.AreEqual(expected.y, actual.y, 0.001f, $"위성 {s}, {j}번째 탄의 y");
                }
            }
        }

        [Test]
        public void 모든_탄은_같은_시간을_기다렸다_날아감()
        {
            TowerController tower = MakeTower(3, 1);                  // 위성 3개
            MakeTarget(2, 1000f);
            var launcher = new DelayedLauncher();

            Act(MakeAction(launcher), tower);

            SatelliteSettings satellite = tower.Satellite;
            float expected = (3 - 1) * satellite.Interval + satellite.LaunchDelay;
            Assert.AreEqual(6, launcher.Holds.Count);
            foreach (float hold in launcher.Holds)
            {
                Assert.AreEqual(expected, hold, 0.0001f);
            }
        }
    }
}
