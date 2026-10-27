using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class EnemyControllerTest
    {
        private GameObject m_go;
        private EnemyDefinition m_definition;

        private EnemyController MakeEnemy(float defense)
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
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
            enemy.Stats.Modifiers.Add(StatModifier.Flat("귀기", StatType.FixedDamageRatio, 0.5f));

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
    }
}
