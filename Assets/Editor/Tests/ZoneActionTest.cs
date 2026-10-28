using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 26 2단계: 버프·디버프 타워의 행동, 존 버프 받기, 디버프존 밟기 (시험 규칙: 위력 +10% / 방어 −10%, 스킬 2면 두 배)
    //   수업 27부터 문양마다 규칙이 생김 → 14문양이 다 들어온 뒤로는 "문양 없음(None)"으로 시험 규칙을 봄 (표에 없는 문양 = 시험 규칙)
    public class ZoneActionTest
    {
        private const TowerEmblem TestEmblem = TowerEmblem.None;

        private GameObject m_root;
        private BoardGrid m_grid;                   // 길 5칸, 스폰 3칸
        private PathOccupancy m_occupancy;
        private TowerSlots m_slots;
        private ZoneRegistry m_zones;
        private EnemyDefinition m_enemyDefinition;

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("ZoneActionTest");
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
            m_slots = new TowerSlots(m_grid.SpawnCount);
            m_zones = new ZoneRegistry(m_grid, null);
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_root);
            if (m_enemyDefinition != null) Object.DestroyImmediate(m_enemyDefinition);
        }

        // 위력 10, 최대 스택 60. slot에 놓음
        private TowerController MakeTower(TowerColor color, int slot, int attackCount = 10, TowerAction action = null, int tier = 1)
        {
            var go = new GameObject($"{color} {slot}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(TowerId.Make(tier, color, TestEmblem), 10f, 2, 3, attackCount, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, slot, action);
            m_slots.Remove(slot);
            m_slots.TryPlace(tower, slot);
            return tower;
        }

        private (TowerController buffTower, BuffZoneAction action, Zone zone) MakeBuffTower(int attackCount, int zoneCell)
        {
            var action = new BuffZoneAction(m_grid, m_occupancy, m_zones, m_slots);
            TowerController buffTower = MakeTower(TowerColor.Yellow, 0, attackCount, action);
            m_zones.EnsureZones(buffTower, 1, true);
            Zone zone = m_zones.GetByOwner(buffTower)[0];
            if (zoneCell >= 0) m_zones.TryPlace(zone, zoneCell);
            return (buffTower, action, zone);
        }

        private static void RunAct(TowerController tower) => RunToEnd(tower.Act(new AttackHandle()));

        // 코루틴을 끝까지: 안에서 yield return으로 넘긴 코루틴(m_action.Act)도 같이 돌림 — Unity의 StartCoroutine이 하는 일
        private static void RunToEnd(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator inner) RunToEnd(inner);
            }
        }

        private EnemyController MakeEnemy(float resistance = 0f)
        {
            if (m_enemyDefinition == null)
            {
                m_enemyDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
                var so = new SerializedObject(m_enemyDefinition);
                so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
                so.FindProperty("<MaxHP>k__BackingField").floatValue = 100f;
                so.FindProperty("<Defense>k__BackingField").floatValue = 100f;
                so.FindProperty("<BaseResistance>k__BackingField").floatValue = resistance;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var go = new GameObject("Enemy");
            go.transform.SetParent(m_root.transform);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Setup(m_enemyDefinition, null, 1);
            return enemy;
        }

        // ── 버프 타워 ──

        [Test]
        public void 존을_안_놓았으면_스택도_버프도_없음()
        {
            var (buffTower, _, zone) = MakeBuffTower(10, -1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            Assert.AreEqual(0f, zone.Stack);
            Assert.AreEqual(10f, attacker.Stats.Power, 0.0001f);
        }

        [Test]
        public void 행동하면_존에_스택이_쌓이고_존_위_공격_타워가_버프를_받음()
        {
            var (buffTower, _, zone) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            Assert.AreEqual(10f, zone.Stack, 0.0001f, "AttackCount만큼");
            Assert.AreEqual(11f, attacker.Stats.Power, 0.0001f, "시험 규칙 스킬 1: 위력 +10%");
        }

        [Test]
        public void 다시_행동해도_버프가_겹치지_않음()
        {
            var (buffTower, _, _) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);
            RunAct(buffTower);
            Assert.AreEqual(11f, attacker.Stats.Power, 0.0001f);
        }

        [Test]
        public void 문턱에_닿으면_스킬2_버프()
        {
            var (buffTower, _, zone) = MakeBuffTower(60, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            Assert.AreEqual(ZoneStage.Skill2, zone.Stage);
            Assert.AreEqual(12f, attacker.Stats.Power, 0.0001f, "시험 규칙 스킬 2: 위력 +20%");
        }

        [Test]
        public void 버프_디버프_타워는_버프를_받지_않음()
        {
            var (buffTower, _, _) = MakeBuffTower(10, 1);
            TowerController otherBuff = MakeTower(TowerColor.Yellow, 1);
            RunAct(buffTower);
            Assert.AreEqual(0, otherBuff.State.ReceivedZoneBuffs.Count);
        }

        [Test]
        public void 존을_벗어나면_적_턴_시작에_버프가_끝남()
        {
            var (buffTower, _, _) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            attacker.SetSlot(2, Vector3.zero);                       // 다른 칸으로 옮김
            BuffZoneAction.RefreshReceived(attacker, m_zones);       // ⓪
            Assert.AreEqual(10f, attacker.Stats.Power, 0.0001f);
            Assert.AreEqual(0, attacker.State.ReceivedZoneBuffs.Count);
        }

        [Test]
        public void 그대로_서_있으면_버프가_유지됨()
        {
            var (buffTower, _, _) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            BuffZoneAction.RefreshReceived(attacker, m_zones);
            Assert.AreEqual(11f, attacker.Stats.Power, 0.0001f);
        }

        [Test]
        public void 버프_타워가_사라지면_존도_버프도_사라짐()
        {
            var (buffTower, _, _) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            buffTower.OnRemoved();
            Assert.AreEqual(0, m_zones.All.Count);
            Assert.AreEqual(10f, attacker.Stats.Power, 0.0001f);
        }

        [Test]
        public void 티어_강화한_타워는_받던_버프를_이어받음()
        {
            var (buffTower, _, zone) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);

            TowerController upgraded = MakeTower(TowerColor.Red, 1, tier: 2);
            upgraded.State.CopyInheritedFrom(attacker.State);

            Assert.IsTrue(upgraded.State.ReceivedZoneBuffs.ContainsKey(zone));
            Assert.AreEqual(11f, upgraded.Stats.Power, 0.0001f, "새로 올라온 것으로 치지 않음 (20번 8장)");
        }

        // ── 3단계 연결 ──

        [Test]
        public void 적_턴_시작에_존을_벗어난_공격_타워의_버프가_꺼짐()
        {
            var (buffTower, _, _) = MakeBuffTower(10, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 1);
            RunAct(buffTower);
            attacker.SetSlot(2, Vector3.zero);

            new TowerTurnRunner(m_slots, 10f, m_zones).RunEnemyTurnStart();     // ⓪
            Assert.AreEqual(10f, attacker.Stats.Power, 0.0001f);
        }

        [Test]
        public void 버프_디버프_타워는_Mastery만큼_존을_받고_공격_타워는_없음()
        {
            TowerController buff = MakeTower(TowerColor.Yellow, 0);
            TowerController debuff = MakeTower(TowerColor.Purple, 1);
            TowerController attacker = MakeTower(TowerColor.Red, 2);

            m_zones.EnsureZonesFor(buff);
            m_zones.EnsureZonesFor(debuff);
            m_zones.EnsureZonesFor(attacker);

            Assert.AreEqual(buff.Stats.Mastery, m_zones.GetByOwner(buff).Count);
            Assert.IsTrue(m_zones.GetByOwner(buff)[0].IsBuff);
            Assert.IsFalse(m_zones.GetByOwner(debuff)[0].IsBuff);
            Assert.AreEqual(0, m_zones.GetByOwner(attacker).Count);
        }

        [Test]
        public void 두_번_불러도_존이_늘지_않음()
        {
            TowerController buff = MakeTower(TowerColor.Yellow, 0);
            m_zones.EnsureZonesFor(buff);
            m_zones.EnsureZonesFor(buff);
            Assert.AreEqual(buff.Stats.Mastery, m_zones.GetByOwner(buff).Count);
        }

        // ── 디버프 타워 ──

        private (TowerController debuffTower, Zone zone) MakeDebuffTower(int attackCount, int zoneCell)
        {
            var action = new DebuffZoneAction(m_grid, m_occupancy, m_zones);
            TowerController debuffTower = MakeTower(TowerColor.Purple, 0, attackCount, action);
            m_zones.EnsureZones(debuffTower, 1, false);
            Zone zone = m_zones.GetByOwner(debuffTower)[0];
            m_zones.TryPlace(zone, zoneCell);
            return (debuffTower, zone);
        }

        [Test]
        public void 디버프_타워는_밟지_않아도_행동마다_스택이_쌓임()
        {
            var (debuffTower, zone) = MakeDebuffTower(10, 2);
            RunAct(debuffTower);
            RunAct(debuffTower);
            Assert.AreEqual(20f, zone.Stack, 0.0001f, "저점 보장");
        }

        [Test]
        public void 밟으면_존에_스택이_쌓이고_적에게_디버프()
        {
            var (_, zone) = MakeDebuffTower(10, 2);
            EnemyController enemy = MakeEnemy();

            DebuffZoneAction.Step(zone, enemy, m_zones);
            Assert.AreEqual(10f, zone.Stack, 0.0001f);
            Assert.AreEqual(90f, enemy.Stats.Get(StatType.Defense), 0.0001f, "시험 규칙 스킬 1: 방어 −10%");
            Assert.IsTrue(enemy.Status.Has(EmblemDebuffEffect.KeyOf(TestEmblem)));
        }

        [Test]
        public void 디버프는_기본_저항만큼_줄어듦()
        {
            var (_, zone) = MakeDebuffTower(10, 2);
            EnemyController enemy = MakeEnemy(resistance: 50f);

            DebuffZoneAction.Step(zone, enemy, m_zones);
            Assert.AreEqual(95f, enemy.Stats.Get(StatType.Defense), 0.0001f, "−10% × (1 − 50%) = −5%");
        }

        [Test]
        public void 같은_문양_디버프는_겹치지_않음()
        {
            var (_, zone) = MakeDebuffTower(10, 2);
            EnemyController enemy = MakeEnemy();

            DebuffZoneAction.Step(zone, enemy, m_zones);
            DebuffZoneAction.Step(zone, enemy, m_zones);
            Assert.AreEqual(1, enemy.Status.Effects.Count);
            Assert.AreEqual(90f, enemy.Stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 스킬1_디버프는_Duration번_행동하면_꺼짐()
        {
            var (debuffTower, zone) = MakeDebuffTower(10, 2);
            EnemyController enemy = MakeEnemy();
            DebuffZoneAction.Step(zone, enemy, m_zones);

            int duration = debuffTower.Stats.Duration;
            for (int i = 0; i < duration; i++) enemy.Status.OnActed();
            Assert.AreEqual(100f, enemy.Stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 스킬2_디버프는_영구()
        {
            var (_, zone) = MakeDebuffTower(60, 2);
            EnemyController enemy = MakeEnemy();
            DebuffZoneAction.Step(zone, enemy, m_zones);

            Assert.AreEqual(ZoneStage.Skill2, zone.Stage);
            for (int i = 0; i < 20; i++) enemy.Status.OnActed();
            Assert.AreEqual(80f, enemy.Stats.Get(StatType.Defense), 0.0001f, "시험 규칙 스킬 2: 방어 −20%, 죽을 때까지");
        }

        [Test]
        public void 디버프_타워가_사라져도_이미_걸린_디버프는_남음()
        {
            var (debuffTower, zone) = MakeDebuffTower(10, 2);
            EnemyController enemy = MakeEnemy();
            DebuffZoneAction.Step(zone, enemy, m_zones);

            debuffTower.OnRemoved();
            Assert.AreEqual(0, m_zones.All.Count);
            Assert.AreEqual(90f, enemy.Stats.Get(StatType.Defense), 0.0001f);
        }
    }
}
