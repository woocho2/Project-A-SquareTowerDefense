using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD.Tests
{
    public class BossMechanicIntegrationTest
    {
        private GameObject m_root;
        private BoardManager m_board;
        private TowerManager m_towers;
        private EnemyManager m_enemies;
        private BossMechanicService m_service;
        private readonly List<Object> m_assets = new List<Object>();
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name).SetValue(owner, value);
        private static void Field(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static void Bind<T>(T instance) where T : Singleton<T> => typeof(Singleton<T>).GetProperty("Instance").SetValue(null, instance);
        private GameObject Child(string name, bool active = false)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            go.transform.SetParent(m_root.transform);
            return go;
        }

        [SetUp]
        public void Setup()
        {
            m_root = new GameObject("BossMechanicIntegration");
            m_board = Child("Board").AddComponent<BoardManager>();
            m_towers = Child("Towers").AddComponent<TowerManager>();
            m_enemies = Child("Enemies").AddComponent<EnemyManager>();
            Bind(m_board);
            Bind(m_towers);
            Bind(m_enemies);
            BoardGrid grid = TestBoards.MakeGrid();
            Property(m_board, "Grid", grid);
            Property(m_board, "Slots", new TowerSlots(grid.SpawnCount));
            Property(m_board, "Occupancy", new PathOccupancy());
            Property(m_board, "Zones", new ZoneRegistry(grid, new SpecialTileLayout()));
            var gridObject = Child("Grid");
            gridObject.AddComponent<Grid>();
            var tileObject = Child("Tiles");
            tileObject.transform.SetParent(gridObject.transform);
            Field(m_board, "m_pathTilemap", tileObject.AddComponent<Tilemap>());
            Property(m_enemies, "Registry", new EnemyRegistry());
            Property(m_enemies, "BossTimer", new BossTimer());
            var database = new TowerDatabase();
            database.Load("ID,Power,Range,Action,AttackCount,CriticalRate,CriticalDamage,Mastery,Duration,AbilityValue,ProjectileSpeed,TargetPriority\n" +
                          "1101,100,2,4,2,0,0,1,5,2,5,Closest\n1201,100,2,4,2,0,0,1,5,2,5,Closest");
            Property(m_towers, "Database", database);
            Property(m_towers, "ColorLevels", new ColorUpgradeState());
            Field(m_towers, "m_selectedEmblems", new List<TowerEmblem> { TowerEmblem.Sword });
            Field(m_towers, "m_factory", new TowerFactory(null, m_root.transform, m_board, database, m_towers.ColorLevels,
                go => Object.DestroyImmediate(go)));
            m_service = new BossMechanicService();
            m_service.BeginTurn();
        }

        [TearDown]
        public void Cleanup()
        {
            Bind<BoardManager>(null);
            Bind<TowerManager>(null);
            Bind<EnemyManager>(null);
            Object.DestroyImmediate(m_root);
            foreach (Object asset in m_assets) if (asset != null) Object.DestroyImmediate(asset);
            m_assets.Clear();
        }

        private TowerController Tower(int id = 2101, int slot = 0)
        {
            var tower = Child("Tower").AddComponent<TowerController>();
            tower.Init(m_towers.Database.Get(id), slot, null, m_towers.Database.Get(id % 1000 + 1000), m_towers.ColorLevels);
            m_board.Slots.TryPlace(tower, slot);
            return tower;
        }

        private EnemyDefinition Definition(EnemyKind kind)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            m_assets.Add(definition);
            Property(definition, "Kind", kind);
            Property(definition, "MaxHP", 1000f);
            Property(definition, "Defense", 100f);
            Property(definition, "Action", 3);
            Property(definition, "SpeedMin", 1);
            Property(definition, "SpeedMax", 3);
            return definition;
        }

        private EnemyController Boss(BossMechanicKind kind)
        {
            var boss = Child("Boss").AddComponent<EnemyController>();
            boss.Setup(Definition(EnemyKind.FinalBoss), null, 10, "Boss", kind);
            m_enemies.Registry.Register(boss);
            return boss;
        }

        private void EndNextTurn() { m_service.BeginTurn(); m_service.EndTurn(); }

        [Test]
        public void 로키는_선택한_문양의_다른_역할로_바꾸고_동일_객체와_상태로_복구함()
        {
            TowerController tower = Tower();
            TowerState state = tower.State;
            state.SkillCount = 2;
            int order = tower.BuildOrder;
            EnemyController boss = Boss(BossMechanicKind.Loki);
            m_service.Trigger(boss, 2, 70);
            Assert.AreEqual(2201, tower.Definition.Id);
            Assert.IsTrue(tower.State.BossEffects.BlocksProgression);
            Assert.AreSame(tower, m_board.Slots.Get(0));
            m_service.EndTurn();
            EndNextTurn();
            Assert.AreEqual(2201, tower.Definition.Id);
            EndNextTurn();
            Assert.AreEqual(2101, tower.Definition.Id);
            Assert.AreSame(state, tower.State);
            Assert.AreEqual(2, tower.State.SkillCount);
            Assert.AreEqual(order, tower.BuildOrder);
        }

        [Test]
        public void 파프니르는_티어만_하나_낮추고_정해진_턴에_복구함()
        {
            TowerController tower = Tower(3101);
            m_service.Trigger(Boss(BossMechanicKind.Fafnir), 1, 70);
            Assert.AreEqual(2101, tower.Definition.Id);
            m_service.EndTurn();
            Assert.AreEqual(2101, tower.Definition.Id);
            EndNextTurn();
            Assert.AreEqual(3101, tower.Definition.Id);
        }

        [Test]
        public void 이미르가_모든_대상을_얼린_뒤에는_기믹이_무효이고_기존_시간도_늘어나지_않음()
        {
            TowerController first = Tower(2101, 0);
            TowerController second = Tower(2101, 1);
            EnemyController boss = Boss(BossMechanicKind.Ymir);
            m_service.Trigger(boss, 2, 70);
            m_service.Trigger(boss, 2, 70);
            Assert.IsTrue(first.State.BossEffects.IsFrozen);
            Assert.IsTrue(second.State.BossEffects.IsFrozen);
            m_service.Trigger(boss, 8, 70);
            m_service.EndTurn();
            EndNextTurn();
            EndNextTurn();
            Assert.IsFalse(first.State.BossEffects.IsFrozen);
            Assert.IsFalse(second.State.BossEffects.IsFrozen);
        }

        [Test]
        public void 엘프_족장은_자신과_모든_적을_치유하고_치유감소를_적용함()
        {
            EnemyController boss = Boss(BossMechanicKind.ElfChief);
            EnemyController other = Boss(BossMechanicKind.None);
            boss.Health.TakeDamage(boss.MaxHp * 0.8f);
            other.Health.TakeDamage(other.MaxHp * 0.8f);
            other.Stats.Modifiers.Add(StatModifier.Percent("치유 감소", StatType.HealingTaken, -0.5f));
            m_service.Trigger(boss, 6, 70);
            Assert.AreEqual(boss.MaxHp * 0.8f, boss.CurrentHp, 0.01f);
            Assert.AreEqual(other.MaxHp * 0.5f, other.CurrentHp, 0.01f);
        }

        [Test]
        public void 상급정령은_자신을_포함한_적_전체를_강화하고_정확히_만료함()
        {
            EnemyController boss = Boss(BossMechanicKind.HighSpirit);
            EnemyController other = Boss(BossMechanicKind.None);
            float original = boss.MaxHp;
            m_service.Trigger(boss, 2, 70);
            Assert.AreEqual(original * 1.5f, boss.MaxHp, 0.01f);
            Assert.AreEqual(original * 1.5f, other.MaxHp, 0.01f);
            m_service.EndTurn();
            EndNextTurn();
            Assert.AreEqual(original * 1.5f, boss.MaxHp, 0.01f);
            EndNextTurn();
            Assert.AreEqual(original, boss.MaxHp, 0.01f);
            Assert.AreEqual(original, other.MaxHp, 0.01f);
        }

        [Test]
        public void 전체_디버프는_발동_당시_타워에만_적용되고_새_타워는_다음_발동부터_적용함()
        {
            TowerController first = Tower(2101, 0);
            EnemyController fenrir = Boss(BossMechanicKind.Fenrir);
            EnemyController surtr = Boss(BossMechanicKind.Surtr);
            m_service.Trigger(fenrir, 3, 70);
            m_service.Trigger(surtr, 3, 70);
            TowerController second = Tower(2101, 1);
            Assert.AreEqual(1, first.State.BossEffects.ActionPenalty);
            Assert.AreEqual(0, second.State.BossEffects.ActionPenalty);
            Assert.AreEqual(0, second.State.BossEffects.AttackCountPenalty);
            m_service.Trigger(fenrir, 3, 70);
            m_service.Trigger(surtr, 3, 70);
            Assert.AreEqual(2, first.State.BossEffects.ActionPenalty);
            Assert.AreEqual(1, second.State.BossEffects.ActionPenalty);
            Assert.AreEqual(1, second.State.BossEffects.AttackCountPenalty);
        }

        [Test]
        public void 요르문간드는_정규_소환이_없는_보스_웨이브에도_눈금만큼_일반적을_등록함()
        {
            EnemyDefinition definition = Definition(EnemyKind.Normal);
            GameObject prefab = Child("EnemyPrefab");
            var enemy = prefab.AddComponent<EnemyController>();
            var movement = prefab.AddComponent<EnemyMovement>();
            GameObject visualObject = Child("Cube");
            visualObject.transform.SetParent(prefab.transform);
            visualObject.AddComponent<MeshFilter>().sharedMesh = new Mesh { subMeshCount = 6 };
            m_assets.Add(visualObject.GetComponent<MeshFilter>().sharedMesh);
            visualObject.AddComponent<MeshRenderer>();
            var cube = visualObject.AddComponent<EnemyCubeMeshVisual>();
            Field(cube, "m_faceAssetPool", new Material[0]);
            Field(movement, "m_cube", cube);
            visualObject.SetActive(true);
            prefab.SetActive(true);
            GameObject poolObject = Child("EnemyPool");
            var pool = poolObject.AddComponent<EnemyPool>();
            Field(pool, "m_prefab", enemy);
            Field(pool, "m_container", m_root.transform);
            poolObject.SetActive(true);
            var objectPool = new UnityEngine.Pool.ObjectPool<EnemyController>(() =>
            {
                EnemyController copy = Object.Instantiate(enemy, m_root.transform);
                typeof(EnemyController).GetMethod("Awake", Private).Invoke(copy, null);
                typeof(EnemyMovement).GetMethod("Awake", Private).Invoke(copy.Movement, null);
                var copiedCube = copy.GetComponentInChildren<EnemyCubeMeshVisual>(true);
                typeof(EnemyCubeMeshVisual).GetMethod("Awake", Private).Invoke(copiedCube, null);
                copy.gameObject.SetActive(false);
                return copy;
            });
            Field(pool, "m_pool", objectPool);
            var entry = new EnemySpawnEntry();
            Property(entry, "Definition", definition);
            Property(entry, "Pool", pool);
            var spawner = new EnemySpawner(m_board, m_enemies.Registry, entry, entry, entry, m_enemies.BossTimer);
            Field(m_enemies, "m_spawner", spawner);
            EnemyController boss = Boss(BossMechanicKind.Jormungandr);
            int before = m_enemies.Registry.Count;
            Assert.AreEqual(0, WaveTable.GetNormalCount(10));
            m_service.Trigger(boss, 4, 70);
            List<EnemyController> summoned = m_service.TakeSummons();
            Assert.AreEqual(4, summoned.Count);
            Assert.AreEqual(before + 4, m_enemies.Registry.Count);
            foreach (EnemyController normal in summoned)
            {
                Assert.AreEqual(EnemyKind.Normal, normal.Definition.Kind);
                Assert.AreEqual(BossMechanicKind.None, normal.BossMechanic);
                Assert.Greater(normal.MaxHp, 0);
            }
            Assert.AreEqual(0, m_service.TakeSummons().Count);
        }

        [Test]
        public void 헬은_목표에_도달하면_모든_타워와_존을_파괴하고_카운트를_초기화함()
        {
            TowerController first = Tower(2101, 0);
            TowerController second = Tower(2101, 1);
            var support = new TowerDefinition(1301, 10, 2, 3, 1, 0, 0, 1, 5, 2, 5, TargetPriority.Closest);
            first.ChangeDefinition(support, support,
                new BuffZoneAction(m_board.Grid, m_board.Occupancy, m_board.Zones, m_board.Slots), m_towers.ColorLevels, null);
            m_board.Zones.EnsureZones(first, 1, true);
            m_towers.Select(second);
            EnemyController boss = Boss(BossMechanicKind.Hel);
            for (int i = 0; i < 8; i++) m_service.Trigger(boss, 8, 70);
            Assert.AreEqual(64, boss.DeathCount);
            Assert.AreSame(first, m_board.Slots.Get(0));
            m_service.Trigger(boss, 6, 70);
            Assert.AreEqual(0, boss.DeathCount);
            Assert.IsNull(m_board.Slots.Get(0));
            Assert.IsNull(m_board.Slots.Get(1));
            Assert.IsNull(m_towers.Selected);
            Assert.AreEqual(0, m_board.Zones.All.Count);
        }
    }
}
