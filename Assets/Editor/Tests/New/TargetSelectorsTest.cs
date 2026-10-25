using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class TargetSelectorsTest
    {
        // 칸 번호·체력·보스 여부만 있는 가짜 대상 (점유·피해는 쓰지 않음)
        private class FakeTarget : ITarget
        {
            public FakeTarget(int pathIndex, float hp, bool isBoss = false)
            {
                PathIndex = pathIndex;
                CurrentHp = hp;
                MaxHp = hp;
                IsBoss = isBoss;
            }

            public Transform Transform => null;
            public int PathIndex { get; }
            public bool IsAlive => CurrentHp > 0f;
            public bool IsBoss { get; }
            public bool IsDummy => false;
            public EnemyController Body => null;
            public float CurrentHp { get; }
            public float MaxHp { get; }
            public EnemyStats Stats => null;
            public float PreviewDamage(DamageInfo info) => info.BaseDamage;
            public void TakeDamage(DamageInfo info) { }
            public void SetSortingOrder(int order) { }
        }

        // BoardGridTest와 같은 판. 타워는 (3,0)
        // 칸까지의 타일 거리: 0번 3, 1번 2, 2번 1, 3번 2, 4번 1
        private static readonly Vector3Int TowerCell = new Vector3Int(3, 0, 0);
        private BoardGrid m_grid;

        [SetUp]
        public void SetUp()
        {
            var path = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, -1, 0),
            };
            var spawn = new List<Vector3Int> { TowerCell };
            m_grid = new BoardGrid(path, spawn);
        }

        private ITarget Select(TargetPriority priority, params ITarget[] candidates)
        {
            return TargetSelector.For(priority).Select(candidates, TowerCell, m_grid);
        }

        [Test]
        public void 우선순위마다_맞는_선택자()
        {
            Assert.IsInstanceOf<ClosestSelector>(TargetSelector.For(TargetPriority.Closest));
            Assert.IsInstanceOf<FarthestSelector>(TargetSelector.For(TargetPriority.Farthest));
            Assert.IsInstanceOf<MostHpSelector>(TargetSelector.For(TargetPriority.MostHP));
            Assert.IsInstanceOf<LeastHpSelector>(TargetSelector.For(TargetPriority.LeastHP));
            Assert.IsInstanceOf<MostCrowdedSelector>(TargetSelector.For(TargetPriority.MostCrowded));
            Assert.IsInstanceOf<BossSelector>(TargetSelector.For(TargetPriority.Boss));
        }

        [Test]
        public void None이면_가까운_적()
        {
            Assert.IsInstanceOf<ClosestSelector>(TargetSelector.For(TargetPriority.None));
        }

        [Test]
        public void 후보가_없으면_모두_null()
        {
            foreach (TargetPriority priority in new[] { TargetPriority.Closest, TargetPriority.Farthest, TargetPriority.MostHP,
                                                       TargetPriority.LeastHP, TargetPriority.MostCrowded, TargetPriority.Boss })
            {
                Assert.IsNull(Select(priority), priority.ToString());
            }
        }

        [Test]
        public void 가까운_적은_타일_거리가_가장_짧은_적()
        {
            var far = new FakeTarget(0, 100f);
            var middle = new FakeTarget(1, 100f);
            var near = new FakeTarget(2, 100f);
            Assert.AreSame(near, Select(TargetPriority.Closest, far, middle, near));
        }

        [Test]
        public void 거리가_같으면_목록의_앞쪽()
        {
            var first = new FakeTarget(4, 100f);
            var second = new FakeTarget(2, 100f);
            Assert.AreSame(first, Select(TargetPriority.Closest, first, second));
        }

        [Test]
        public void 먼_적은_타일_거리가_가장_긴_적()
        {
            var far = new FakeTarget(0, 100f);
            var middle = new FakeTarget(1, 100f);
            var near = new FakeTarget(2, 100f);
            Assert.AreSame(far, Select(TargetPriority.Farthest, near, middle, far));
        }

        [Test]
        public void 체력_많은_적()
        {
            var a = new FakeTarget(0, 50f);
            var b = new FakeTarget(1, 80f);
            var c = new FakeTarget(2, 30f);
            Assert.AreSame(b, Select(TargetPriority.MostHP, a, b, c));
        }

        [Test]
        public void 체력_많은_적은_보스도_예외_없음()
        {
            var boss = new FakeTarget(0, 50f, isBoss: true);
            var normal = new FakeTarget(1, 80f);
            Assert.AreSame(normal, Select(TargetPriority.MostHP, boss, normal));
        }

        [Test]
        public void 체력_적은_적()
        {
            var a = new FakeTarget(0, 50f);
            var b = new FakeTarget(1, 80f);
            var c = new FakeTarget(2, 30f);
            Assert.AreSame(c, Select(TargetPriority.LeastHP, a, b, c));
        }

        [Test]
        public void 적이_가장_많은_곳은_그_칸의_첫_번째_적()
        {
            var alone = new FakeTarget(1, 100f);
            var crowdFirst = new FakeTarget(3, 100f);
            var crowdSecond = new FakeTarget(3, 100f);
            var other = new FakeTarget(2, 100f);
            Assert.AreSame(crowdFirst, Select(TargetPriority.MostCrowded, alone, crowdFirst, other, crowdSecond));
        }

        [Test]
        public void 보스가_있으면_보스()
        {
            var normal = new FakeTarget(0, 100f);
            var boss = new FakeTarget(1, 50f, isBoss: true);
            Assert.AreSame(boss, Select(TargetPriority.Boss, normal, boss));
        }

        [Test]
        public void 보스가_여럿이면_체력_많은_보스()
        {
            var weakBoss = new FakeTarget(0, 50f, isBoss: true);
            var strongBoss = new FakeTarget(1, 70f, isBoss: true);
            var normal = new FakeTarget(2, 999f);
            Assert.AreSame(strongBoss, Select(TargetPriority.Boss, weakBoss, normal, strongBoss));
        }

        [Test]
        public void 공격_칸이_없으면_후보_그대로()
        {
            var a = new FakeTarget(1, 100f);
            var b = new FakeTarget(2, 100f);
            var candidates = new List<ITarget> { a, b };
            CollectionAssert.AreEqual(candidates, TargetSelector.PreferAttackCell(candidates, -1));
        }

        [Test]
        public void 공격_칸에_대상이_있으면_그_칸의_대상만()
        {
            var a = new FakeTarget(1, 100f);
            var b = new FakeTarget(3, 100f);
            var c = new FakeTarget(3, 50f);
            var candidates = new List<ITarget> { a, b, c };
            CollectionAssert.AreEqual(new List<ITarget> { b, c }, TargetSelector.PreferAttackCell(candidates, 3));
        }

        [Test]
        public void 공격_칸이_비었으면_후보_그대로()
        {
            var a = new FakeTarget(1, 100f);
            var b = new FakeTarget(2, 100f);
            var candidates = new List<ITarget> { a, b };
            CollectionAssert.AreEqual(candidates, TargetSelector.PreferAttackCell(candidates, 4));
        }

        [Test]
        public void 보스가_없으면_체력_많은_적()
        {
            var a = new FakeTarget(0, 50f);
            var b = new FakeTarget(1, 80f);
            Assert.AreSame(b, Select(TargetPriority.Boss, a, b));
        }
    }
}
