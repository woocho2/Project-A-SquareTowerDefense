using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SquareTD.Tests
{
    public class EnemyCubeFacesTest
    {
        private EnemyDefinition m_definition;

        private EnemyStats MakeStats()
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<Defense>k__BackingField").floatValue = 100f;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = 2;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
            return new EnemyStats(m_definition, 1);
        }

        private static int CountFrom(EnemyStats stats, string source) => stats.Modifiers.All.Count(m => m.Source == source);

        [TearDown]
        public void CleanUp()
        {
            if (m_definition != null) Object.DestroyImmediate(m_definition);
        }

        [Test]
        public void 면_6종이_모두_표에_있음()
        {
            foreach (EnemyCubeFaceAsset face in Enum.GetValues(typeof(EnemyCubeFaceAsset)))
            {
                Assert.IsNotNull(EnemyCubeFaces.GetModifiers(face), $"{face}가 표에 없음");
            }
        }

        [Test]
        public void 같은_면을_두_번_적용해도_쌓이지_않음()
        {
            EnemyStats stats = MakeStats();

            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.Defense);
            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.Defense);

            Assert.AreEqual(1, CountFrom(stats, EnemyCubeFaces.Source));
        }

        [Test]
        public void 면이_바뀌면_지난_면의_효과는_지워짐()
        {
            EnemyStats stats = MakeStats();

            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.Defense);
            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.Mobility);

            Assert.AreEqual(2, CountFrom(stats, EnemyCubeFaces.Source), "이동력 면은 최소·최대 둘");
            Assert.IsFalse(stats.Modifiers.All.Any(m => m.Source == EnemyCubeFaces.Source && m.Stat == StatType.Defense));
        }

        [Test]
        public void 효과가_없는_면이면_큐브_면_수정자도_없음()
        {
            EnemyStats stats = MakeStats();
            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.Defense);

            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.SelfHeal);

            Assert.AreEqual(0, CountFrom(stats, EnemyCubeFaces.Source));
        }

        [Test]
        public void 다른_출처의_수정자는_건드리지_않음()
        {
            EnemyStats stats = MakeStats();
            stats.Modifiers.Add(StatModifier.Percent("방어 타일", StatType.Defense, 0.2f));

            EnemyCubeFaces.Apply(stats, EnemyCubeFaceAsset.Mobility);

            Assert.AreEqual(1, CountFrom(stats, "방어 타일"));
            Assert.AreEqual(120f, stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 수치가_정해지기_전에는_스탯이_그대로()
        {
            EnemyStats stats = MakeStats();

            foreach (EnemyCubeFaceAsset face in Enum.GetValues(typeof(EnemyCubeFaceAsset)))
            {
                EnemyCubeFaces.Apply(stats, face);
                MoveRange range = stats.GetMoveRange();
                Assert.AreEqual(100f, stats.Get(StatType.Defense), 0.0001f, $"{face}");
                Assert.AreEqual(2, range.Min, $"{face}");
                Assert.AreEqual(4, range.Max, $"{face}");
                Assert.AreEqual(3, range.Action, $"{face}");
                Assert.AreEqual(1f, stats.Get(StatType.DamageTaken), 0.0001f, $"{face}");
            }
        }
    }
}
