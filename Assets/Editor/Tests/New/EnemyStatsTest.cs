using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class EnemyStatsTest
    {
        private EnemyDefinition m_definition;

        // EnemyDefinition은 값이 private set이라, 에디터의 SerializedObject로 칸을 채움
        private EnemyDefinition MakeDefinition(float defense, int action, int speedMin, int speedMax, EnemyKind kind = EnemyKind.Normal)
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)kind;
            so.FindProperty("<Defense>k__BackingField").floatValue = defense;
            so.FindProperty("<Action>k__BackingField").intValue = action;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = speedMin;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = speedMax;
            so.ApplyModifiedPropertiesWithoutUndo();
            return m_definition;
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_definition != null) Object.DestroyImmediate(m_definition);
        }

        [Test]
        public void 기본값은_정의_그대로_받는_피해는_1()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);

            Assert.AreEqual(100f, stats.Get(StatType.Defense), 0.0001f);
            Assert.AreEqual(3f, stats.Get(StatType.MaxAction), 0.0001f);
            Assert.AreEqual(2f, stats.Get(StatType.SpeedMin), 0.0001f);
            Assert.AreEqual(4f, stats.Get(StatType.SpeedMax), 0.0001f);
            Assert.AreEqual(1f, stats.Get(StatType.DamageTaken), 0.0001f);
            Assert.AreEqual(0f, stats.Get(StatType.FixedDamageRatio), 0.0001f);
        }

        [Test]
        public void 방어력에는_웨이브_배율이_붙음()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 11);    // 11웨이브 = 사이클 2, 순서 1 → ×2

            Assert.AreEqual(200f, stats.GetBase(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 방어_20과_마이너스50은_x0_7()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Percent("방어 타일", StatType.Defense, 0.2f));
            stats.Modifiers.Add(StatModifier.Percent("방패벽", StatType.Defense, -0.5f));

            Assert.AreEqual(70f, stats.Get(StatType.Defense), 0.0001f);
            Assert.AreEqual(100f, stats.GetBase(StatType.Defense), 0.0001f, "기본값은 그대로 (덮어쓰지 않음)");
        }

        [Test]
        public void 방어력은_음수가_될_수_있음()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Percent("파쇄", StatType.Defense, -1.5f));

            Assert.AreEqual(-50f, stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 수정자가_없으면_이동_범위는_정의_그대로()
        {
            MoveRange range = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1).GetMoveRange();

            Assert.AreEqual(2, range.Min);
            Assert.AreEqual(4, range.Max);
            Assert.AreEqual(3, range.Action);
        }

        [Test]
        public void 이동력_2_4에_마이너스3이면_1_1이고_행동_주기_플러스2()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Flat("저체온증", StatType.SpeedMin, -3f));
            stats.Modifiers.Add(StatModifier.Flat("저체온증", StatType.SpeedMax, -3f));

            MoveRange range = stats.GetMoveRange();

            Assert.AreEqual(1, range.Min);
            Assert.AreEqual(1, range.Max);
            Assert.AreEqual(5, range.Action, "3 + (1 − (2 − 3))");
        }

        [Test]
        public void 딱_1이_되면_행동_주기는_그대로()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Flat("디버프", StatType.SpeedMin, -1f));

            MoveRange range = stats.GetMoveRange();

            Assert.AreEqual(1, range.Min);
            Assert.AreEqual(3, range.Action);
        }

        [Test]
        public void 최소가_최대를_넘지_않음()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Flat("버프", StatType.SpeedMin, 3f));     // 최소 5, 최대 4

            MoveRange range = stats.GetMoveRange();

            Assert.AreEqual(5, range.Min);
            Assert.AreEqual(5, range.Max);
        }

        [Test]
        public void 행동_주기는_최소_1()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 1, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Flat("버프", StatType.MaxAction, -3f));

            Assert.AreEqual(1, stats.GetMoveRange().Action);
        }

        [Test]
        public void 출처로_지우면_원래대로()
        {
            var stats = new EnemyStats(MakeDefinition(100f, 3, 2, 4), 1);
            stats.Modifiers.Add(StatModifier.Percent("방어 타일", StatType.Defense, 0.2f));
            stats.Modifiers.Add(StatModifier.Percent("방패벽", StatType.Defense, -0.5f));

            stats.Modifiers.RemoveBySource("방패벽");

            Assert.AreEqual(120f, stats.Get(StatType.Defense), 0.0001f, "방어 타일 보정은 남음 (B13)");
        }
    }
}
