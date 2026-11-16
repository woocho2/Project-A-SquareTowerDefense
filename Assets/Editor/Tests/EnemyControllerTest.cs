using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class EnemyControllerTest
    {
        private GameObject m_go;
        private EnemyDefinition m_definition;

        private EnemyController MakeEnemy(float defense, EnemyKind kind = EnemyKind.Normal)
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)kind;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 1000f;
            so.FindProperty("<Defense>k__BackingField").floatValue = defense;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = 2;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            m_go = new GameObject("TestEnemy");
            var enemy = m_go.AddComponent<EnemyController>();
            enemy.Setup(m_definition, null, 1);
            return enemy;
        }

        private static DamageInfo Hit(float damage) => new DamageInfo { BaseDamage = damage };

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
            if (m_definition != null) Object.DestroyImmediate(m_definition);
        }

        [Test]
        public void 방어력_100이면_피해_절반()
        {
            EnemyController enemy = MakeEnemy(100f);

            Assert.AreEqual(50f, enemy.PreviewDamage(Hit(100f)), 0.0001f);
        }

        [Test]
        public void 방어력_수정자가_피해에_반영됨()
        {
            EnemyController enemy = MakeEnemy(100f);
            enemy.Stats.Modifiers.Add(StatModifier.Percent("방패벽", StatType.Defense, -1f));      // 방어 0

            Assert.AreEqual(100f, enemy.PreviewDamage(Hit(100f)), 0.0001f);
        }

        [Test]
        public void 받는_피해_증가는_방어_뒤에_곱함()
        {
            EnemyController enemy = MakeEnemy(100f);
            enemy.Stats.Modifiers.Add(StatModifier.Percent("락 온", StatType.DamageTaken, 0.1f));    // ×1.1

            Assert.AreEqual(55f, enemy.PreviewDamage(Hit(100f)), 0.0001f, "100 × 0.5 × 1.1");
        }

        [Test]
        public void 고정_피해_비율만큼은_방어를_무시함()
        {
            EnemyController enemy = MakeEnemy(100f);
            enemy.Stats.Modifiers.Add(StatModifier.Flat("둠", StatType.FixedDamageRatio, 0.5f));

            Assert.AreEqual(75f, enemy.PreviewDamage(Hit(100f)), 0.0001f, "고정 50 + 나머지 50 × 0.5");
        }

        [Test]
        public void 다시_소환하면_지난번_수정자가_없음()
        {
            EnemyController enemy = MakeEnemy(100f);
            enemy.Stats.Modifiers.Add(StatModifier.Percent("방패벽", StatType.Defense, -1f));

            enemy.Setup(m_definition, null, 1);                                                    // 풀에서 다시 꺼낸 것

            Assert.AreEqual(50f, enemy.PreviewDamage(Hit(100f)), 0.0001f);
        }

        [TestCase(EnemyKind.Normal, false)]
        [TestCase(EnemyKind.MiddleBoss, false)]
        [TestCase(EnemyKind.FinalBoss, true)]
        public void 자물쇠_면은_최종보스만_기믹을_호출함(EnemyKind kind, bool expected)
        {
            EnemyController enemy = MakeEnemy(0f, kind);
            int calls = 0;
            enemy.BossSkillTriggered += owner =>
            {
                Assert.AreSame(enemy, owner);
                calls++;
            };

            EnemyCubeFaces.Apply(enemy.Stats, EnemyCubeFaceAsset.Locked, 5, kind);
            Assert.AreEqual(expected, enemy.TryTriggerBossSkill(EnemyCubeFaceAsset.Locked));
            Assert.AreEqual(expected ? 1 : 0, calls);
            Assert.AreEqual(expected ? 1f : 1.5f, enemy.Stats.Get(StatType.DamageTaken), 0.0001f);
        }

        [TestCase(EnemyCubeFaceAsset.SelfHeal)]
        [TestCase(EnemyCubeFaceAsset.Defense)]
        [TestCase(EnemyCubeFaceAsset.Action)]
        [TestCase(EnemyCubeFaceAsset.Mobility)]
        [TestCase(EnemyCubeFaceAsset.Hourglass)]
        public void 최종보스의_다른_면은_기믹을_호출하지_않음(EnemyCubeFaceAsset face)
        {
            EnemyController enemy = MakeEnemy(0f, EnemyKind.FinalBoss);
            int calls = 0;
            enemy.BossSkillTriggered += _ => calls++;

            Assert.IsFalse(enemy.TryTriggerBossSkill(face));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void 죽은_최종보스는_기믹을_호출하지_않음()
        {
            EnemyController enemy = MakeEnemy(0f, EnemyKind.FinalBoss);
            enemy.Health.TakeDamage(enemy.CurrentHp);
            int calls = 0;
            enemy.BossSkillTriggered += _ => calls++;

            Assert.IsFalse(enemy.TryTriggerBossSkill(EnemyCubeFaceAsset.Locked));
            Assert.AreEqual(0, calls);
        }

        private EnemyMovement MakeMovement(EnemyCubeFaceAsset face)
        {
            var visualObject = new GameObject("TestCube");
            visualObject.transform.SetParent(m_go.transform);
            visualObject.SetActive(false);
            var cube = visualObject.AddComponent<EnemyCubeMeshVisual>();
            var indices = (int[])typeof(EnemyCubeMeshVisual)
                .GetField("m_faceAssetIndices", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cube);
            for (int i = 0; i < indices.Length; i++) indices[i] = (int)face;

            var movement = m_go.AddComponent<EnemyMovement>();
            typeof(EnemyMovement).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(movement, null);
            var serialized = new SerializedObject(movement);
            serialized.FindProperty("m_cube").objectReferenceValue = cube;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return movement;
        }

        private static void Land(EnemyMovement movement) => typeof(EnemyMovement)
            .GetMethod("Land", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(movement, null);

        [Test]
        public void 기믹은_착지마다_한번만_호출되고_면_수치_갱신으로는_호출되지_않음()
        {
            EnemyController enemy = MakeEnemy(0f, EnemyKind.FinalBoss);
            EnemyMovement movement = MakeMovement(EnemyCubeFaceAsset.Locked);
            int calls = 0;
            enemy.BossSkillTriggered += _ => calls++;

            Land(movement);
            movement.RefreshFace();
            movement.RefreshFace();
            Assert.AreEqual(0, calls);

            Assert.IsTrue(movement.ApplyAdaptive(), "기믹도 적응형 효과의 단계 간격을 적용함");
            Assert.IsFalse(movement.ApplyAdaptive());
            Assert.AreEqual(1, calls);

            Land(movement);
            movement.ApplyAdaptive();
            Assert.AreEqual(2, calls);
        }

    }
}
