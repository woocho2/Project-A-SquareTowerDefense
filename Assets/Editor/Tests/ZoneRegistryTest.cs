using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 26 1단계: 존과 존 목록 (스택·단계, 놓을 수 있는 칸, 턴 종료 검사)
    public class ZoneRegistryTest
    {
        private GameObject m_root;
        private BoardGrid m_grid;               // 길 5칸, 스폰 3칸 (TestBoards)

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("ZoneRegistryTest");
            m_grid = TestBoards.MakeGrid();
        }

        [TearDown]
        public void CleanUp() => Object.DestroyImmediate(m_root);

        // 버프 타워 = 옐로우(3), 디버프 타워 = 퍼플(4)
        private TowerController MakeTower(TowerColor color, TowerEmblem emblem)
        {
            var go = new GameObject($"{color} {emblem}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(TowerId.Make(1, color, emblem), 10f, 2, 3, 1, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, null);
            return tower;
        }

        private ZoneRegistry MakeRegistry(SpecialTileLayout layout = null) => new ZoneRegistry(m_grid, layout);

        private Zone NewBuffZone(ZoneRegistry registry, TowerEmblem emblem)
        {
            TowerController tower = MakeTower(TowerColor.Yellow, emblem);
            registry.EnsureZones(tower, 1, true);
            return registry.GetByOwner(tower)[0];
        }

        // ── 존 하나: 스택과 단계 ──

        [Test]
        public void 처음엔_0스택_스킬1_안_놓음()
        {
            var zone = new Zone(MakeTower(TowerColor.Yellow, TowerEmblem.Sword), true);
            Assert.AreEqual(0f, zone.Stack);
            Assert.AreEqual(ZoneStage.Skill1, zone.Stage);
            Assert.IsFalse(zone.IsPlaced);
        }

        [Test]
        public void 스택이_문턱에_닿으면_스킬2()
        {
            var zone = new Zone(MakeTower(TowerColor.Yellow, TowerEmblem.Sword), true);
            zone.AddStack(30f, 60);
            Assert.AreEqual(ZoneStage.Skill1, zone.Stage);

            zone.AddStack(30f, 60);
            Assert.AreEqual(ZoneStage.Skill2, zone.Stage);
            Assert.AreEqual(60f, zone.Stack, 0.0001f);
        }

        [Test]
        public void 스킬2는_더_쌓지_않고_0으로도_안_돌아감()
        {
            var zone = new Zone(MakeTower(TowerColor.Yellow, TowerEmblem.Sword), true);
            zone.AddStack(60f, 60);
            zone.AddStack(10f, 120);                // 문턱이 늘어도
            Assert.AreEqual(60f, zone.Stack, 0.0001f);

            zone.ResetIfSkill1();                   // 컬러 레벨 4가 되어도
            Assert.AreEqual(ZoneStage.Skill2, zone.Stage);
            Assert.AreEqual(60f, zone.Stack, 0.0001f);
        }

        [Test]
        public void 컬러_레벨_4가_되면_스킬1_존은_0부터()
        {
            var zone = new Zone(MakeTower(TowerColor.Yellow, TowerEmblem.Sword), true);
            zone.AddStack(40f, 60);
            zone.ResetIfSkill1();
            Assert.AreEqual(0f, zone.Stack);
        }

        // ── 만들기·지우기 ──

        [Test]
        public void Mastery만큼_존이_생기고_늘면_새_존만_더함()
        {
            ZoneRegistry registry = MakeRegistry();
            TowerController tower = MakeTower(TowerColor.Yellow, TowerEmblem.Sword);

            registry.EnsureZones(tower, 1, true);
            Zone first = registry.GetByOwner(tower)[0];
            first.AddStack(20f, 60);
            registry.TryPlace(first, 0);

            registry.EnsureZones(tower, 2, true);
            Assert.AreEqual(2, registry.GetByOwner(tower).Count);
            Assert.IsTrue(first.IsPlaced, "기존 존은 그대로");
            Assert.AreEqual(20f, first.Stack, 0.0001f);
            Assert.IsFalse(registry.GetByOwner(tower)[1].IsPlaced, "새 존만 자리를 고름");
        }

        [Test]
        public void 타워가_사라지면_그_타워의_존만_사라짐()
        {
            ZoneRegistry registry = MakeRegistry();
            Zone a = NewBuffZone(registry, TowerEmblem.Sword);
            Zone b = NewBuffZone(registry, TowerEmblem.Axe);

            Assert.AreEqual(1, registry.RemoveByOwner(a.Owner));
            Assert.AreEqual(1, registry.All.Count);
            Assert.AreSame(b, registry.All[0]);
        }

        // ── 놓을 수 있는 칸 ──

        [Test]
        public void 버프존은_스폰_칸에_디버프존은_길_칸에()
        {
            ZoneRegistry registry = MakeRegistry();
            Zone buff = NewBuffZone(registry, TowerEmblem.Sword);
            TowerController debuffTower = MakeTower(TowerColor.Purple, TowerEmblem.Sword);
            registry.EnsureZones(debuffTower, 1, false);
            Zone debuff = registry.GetByOwner(debuffTower)[0];

            Assert.IsFalse(registry.CanPlace(buff, 3, out _), "스폰 칸은 0 ~ 2");
            Assert.IsTrue(registry.CanPlace(buff, 2, out _));
            Assert.IsTrue(registry.CanPlace(debuff, 4, out _), "길 칸은 0 ~ 4");
            Assert.IsFalse(registry.CanPlace(debuff, 5, out _));
        }

        [Test]
        public void 놓으면_그_칸에서_찾을_수_있고_다시_못_옮김()
        {
            ZoneRegistry registry = MakeRegistry();
            Zone zone = NewBuffZone(registry, TowerEmblem.Sword);

            Assert.IsTrue(registry.TryPlace(zone, 1));
            Assert.AreEqual(1, registry.GetAtSpawn(1).Count);
            Assert.IsFalse(registry.TryPlace(zone, 2), "확정한 뒤에는 옮길 수 없음");
            Assert.AreEqual(1, zone.CellIndex);
        }

        [Test]
        public void 한_칸에_같은_문양은_하나만()
        {
            ZoneRegistry registry = MakeRegistry();
            Zone a = NewBuffZone(registry, TowerEmblem.Sword);
            Zone b = NewBuffZone(registry, TowerEmblem.Sword);      // 다른 검 버프 타워

            registry.TryPlace(a, 0);
            Assert.IsFalse(registry.CanPlace(b, 0, out string reason));
            StringAssert.Contains("같은 문양", reason);
            Assert.IsTrue(registry.CanPlace(b, 1, out _));
        }

        [Test]
        public void 한_칸에_다른_문양은_3개까지()
        {
            ZoneRegistry registry = MakeRegistry();
            registry.TryPlace(NewBuffZone(registry, TowerEmblem.Sword), 0);
            registry.TryPlace(NewBuffZone(registry, TowerEmblem.Axe), 0);
            Zone third = NewBuffZone(registry, TowerEmblem.Bow);
            Assert.IsTrue(registry.TryPlace(third, 0));

            Zone fourth = NewBuffZone(registry, TowerEmblem.Spear);
            Assert.IsFalse(registry.CanPlace(fourth, 0, out string reason));
            StringAssert.Contains("3개", reason);
        }

        [Test]
        public void 디버프존은_비프로스트_칸에_못_놓음()
        {
            var layout = new SpecialTileLayout();
            layout.Generate(m_grid, new SpecialTileCounts
            {
                Defense = 0, Speed = 0, PathActionCount = 0, Heal = 0, Purify = 0,
                Power = 0, SpawnActionCount = 0, AttackCount = 0, Mastery = 0,
            }, new System.Random(1));
            ZoneRegistry registry = MakeRegistry(layout);
            TowerController tower = MakeTower(TowerColor.Purple, TowerEmblem.Sword);
            registry.EnsureZones(tower, 1, false);
            Zone zone = registry.GetByOwner(tower)[0];

            for (int i = 0; i < m_grid.PathCount; i++)
            {
                bool bifrost = layout.GetPathTile(i) == PathTileType.Bifrost;
                Assert.AreEqual(!bifrost, registry.CanPlace(zone, i, out _), $"{i}번 칸");
            }
        }

        // ── 턴 종료 검사 ──

        [Test]
        public void 안_놓은_존이_있으면_턴을_못_넘김()
        {
            ZoneRegistry registry = MakeRegistry();
            Zone zone = NewBuffZone(registry, TowerEmblem.Sword);
            Assert.IsTrue(registry.HasUnplaced());

            registry.TryPlace(zone, 0);
            Assert.IsFalse(registry.HasUnplaced());
        }

        [Test]
        public void 놓을_칸이_하나도_없는_존은_빼고_셈()
        {
            // 스폰 칸 3개에 검 버프존이 이미 다 있으면, 새 검 버프존은 놓을 곳이 없음 → 턴을 막지 않음
            ZoneRegistry registry = MakeRegistry();
            for (int cell = 0; cell < m_grid.SpawnCount; cell++) registry.TryPlace(NewBuffZone(registry, TowerEmblem.Sword), cell);
            Zone stuck = NewBuffZone(registry, TowerEmblem.Sword);

            Assert.IsFalse(registry.HasPlaceableCell(stuck));
            Assert.IsFalse(registry.HasUnplaced());
        }

        [Test]
        public void 놓거나_지우면_ZoneChanged()
        {
            ZoneRegistry registry = MakeRegistry();
            int count = 0;
            registry.ZoneChanged += _ => count++;

            Zone zone = NewBuffZone(registry, TowerEmblem.Sword);       // 만듦 1
            registry.TryPlace(zone, 0);                                  // 놓음 2
            registry.RemoveByOwner(zone.Owner);                          // 지움 3
            Assert.AreEqual(3, count);
        }
    }
}
