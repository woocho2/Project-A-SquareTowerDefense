using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    // 문양 테스트(수업 32 ~ 40)가 같이 쓰는 도우미. 판은 TestBoards (길 5칸, 스폰 3칸)
    //   타워: 위력 10, 스킬 계수 2, 최대 스택 60 (기획서 예시의 브론즈) → 스킬 피해 20, 존 값(최대 스택) 0.012 = 1.2%
    //   적: 최대 체력 100 (웨이브 1), 이동력 2 ~ 4, 행동 주기 3. 길 칸에 세우고 점유에 넣음
    public abstract class EmblemTestBase
    {
        protected GameObject m_root;
        protected BoardGrid m_grid;
        protected PathOccupancy m_occupancy;
        private readonly List<Object> m_assets = new List<Object>();

        [SetUp]
        public void SetUpBase()
        {
            m_root = new GameObject(GetType().Name);
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
            EmblemSharedState.ResetOutsideGame();
        }

        [TearDown]
        public void CleanUpBase()
        {
            Object.DestroyImmediate(m_root);
            foreach (Object asset in m_assets) Object.DestroyImmediate(asset);
            m_assets.Clear();
        }

        protected TowerController MakeTower(int id, float power = 10f, int range = 2, int slot = 0, AttackSkill skill = null,
                                            float critRate = 0f, int mastery = 1)
        {
            var go = new GameObject($"Tower {id}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, power, range, 3, 1, critRate, 0f, mastery, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, slot, null, null, null, skill);
            return tower;
        }

        private protected OccupyingFakeTarget MakeTarget(int pathIndex, float hp = 1000f) =>
            new OccupyingFakeTarget(m_occupancy, pathIndex, hp, m_root.transform);

        // 진짜 적 (상태 효과·저항력·이동 범위가 있음). 에디트 모드라 Awake가 돌지 않아 이동 컴포넌트와 칸 번호를 직접 넣음
        protected EnemyController MakeEnemy(int pathIndex = 0, float hp = 100f, float defense = 0f, float resistance = 0f,
                                            int speedMin = 2, int speedMax = 4, bool occupy = true)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            m_assets.Add(definition);
            var so = new SerializedObject(definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = hp;
            so.FindProperty("<Defense>k__BackingField").floatValue = defense;
            so.FindProperty("<BaseResistance>k__BackingField").floatValue = resistance;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = speedMin;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = speedMax;
            so.ApplyModifiedPropertiesWithoutUndo();

            var go = new GameObject($"Enemy {pathIndex}");
            go.transform.SetParent(m_root.transform);
            var movement = go.AddComponent<EnemyMovement>();
            var enemy = go.AddComponent<EnemyController>();
            SetPrivate(typeof(EnemyController), enemy, "Movement", movement);
            SetPrivate(typeof(EnemyMovement), movement, "PathIndex", pathIndex);
            enemy.Setup(definition, null, 1);
            if (occupy) m_occupancy.Add(enemy, pathIndex);
            return enemy;
        }

        protected static void MoveEnemyTo(EnemyController enemy, int pathIndex) =>
            SetPrivate(typeof(EnemyMovement), enemy.Movement, "PathIndex", pathIndex);

        private static void SetPrivate(System.Type type, object target, string property, object value) =>
            type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance).GetSetMethod(true).Invoke(target, new[] { value });

        // 존 주인 타워와 쌓인 스택. 60(최대)이면 스킬 2
        protected static Zone MakeZone(TowerController owner, bool isBuff, float stack)
        {
            var zone = new Zone(owner, isBuff);
            zone.AddStack(stack, owner.Stats.Stack);
            return zone;
        }

        // 그 존 위에 선 타워가 버프를 받음 (버프 타워가 행동했을 때와 같음)
        protected static void Receive(Zone zone, TowerController receiver)
        {
            zone.PlaceAt(receiver.SlotIndex);
            BuffZoneAction.GiveBuff(zone, receiver);
        }

        // 디버프존을 밟은 것과 같음: 그 문양의 규칙이 만든 효과를 적에게 검
        protected static void Debuff(Zone zone, EnemyController enemy) =>
            enemy.Status.Add(SkillRegistry.GetDebuffRule(zone.Emblem).CreateEffect(zone, enemy));

        protected static float Sum(List<StatModifier> modifiers, StatType stat)
        {
            float sum = 0f;
            foreach (StatModifier modifier in modifiers)
            {
                if (modifier.Stat == stat) sum += modifier.Value;
            }
            return sum;
        }

        private protected static float Total(OccupyingFakeTarget target)
        {
            float sum = 0f;
            foreach (DamageInfo hit in target.Hits) sum += hit.BaseDamage;
            return sum;
        }

        protected static float Lost(EnemyController enemy) => enemy.MaxHp - enemy.CurrentHp;

        protected static StackModeSettings Auto() => new StackModeSettings();

        // 코루틴을 끝까지 (안에서 넘긴 코루틴도)
        protected static void RunToEnd(IEnumerator routine)
        {
            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator inner) RunToEnd(inner);
            }
        }
    }
}
