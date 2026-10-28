using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class PathTileEffectsTest
    {
        private GameObject m_go;
        private EnemyDefinition m_definition;

        private EnemyController MakeEnemy()
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 1000f;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = 2;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            m_go = new GameObject("TestEnemy");
            var enemy = m_go.AddComponent<EnemyController>();
            enemy.Setup(m_definition, null, 1);
            return enemy;
        }

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
            if (m_definition != null) Object.DestroyImmediate(m_definition);
        }

        private static float ApplyAll(IReadOnlyList<StatModifier> modifiers, StatType stat, float baseValue)
        {
            var list = new StatModifierList();
            foreach (StatModifier modifier in modifiers) list.Add(modifier);
            return list.Apply(stat, baseValue);
        }

        // ── 표 ──

        [Test]
        public void 종류마다_그_효과가_나옴()
        {
            foreach (PathTileType type in new[] { PathTileType.Defense, PathTileType.Speed, PathTileType.ActionCount,
                                                  PathTileType.Heal, PathTileType.Purify, PathTileType.Bifrost })
            {
                Assert.IsNotNull(PathTileEffects.Get(type), type.ToString());
                Assert.AreEqual(type, PathTileEffects.Get(type).Type);
            }
        }

        [Test]
        public void 평범한_칸은_효과_없음()
        {
            Assert.IsNull(PathTileEffects.Get(PathTileType.None));
        }

        // ── 멈춰 있는 동안의 수정자 ──

        [Test]
        public void 방어_타일은_방어력_20퍼센트()
        {
            IReadOnlyList<StatModifier> modifiers = PathTileEffects.Get(PathTileType.Defense).GetStandingModifiers();

            Assert.AreEqual(1, modifiers.Count);
            Assert.AreEqual(120f, ApplyAll(modifiers, StatType.Defense, 100f), 0.0001f);
            Assert.AreEqual(PathTileEffect.Source, modifiers[0].Source, "칸을 떠날 때 이 출처로 지움");
        }

        [Test]
        public void 이동력_타일은_최소_최대_둘_다_플러스2()
        {
            IReadOnlyList<StatModifier> modifiers = PathTileEffects.Get(PathTileType.Speed).GetStandingModifiers();

            Assert.AreEqual(2, modifiers.Count);
            Assert.AreEqual(4f, ApplyAll(modifiers, StatType.SpeedMin, 2f), 0.0001f);
            Assert.AreEqual(6f, ApplyAll(modifiers, StatType.SpeedMax, 4f), 0.0001f);
            foreach (StatModifier modifier in modifiers) Assert.AreEqual(PathTileEffect.Source, modifier.Source);
        }

        [Test]
        public void 수정자는_방어와_이동력_타일만()
        {
            Assert.AreEqual(0, PathTileEffects.Get(PathTileType.ActionCount).GetStandingModifiers().Count);
            Assert.AreEqual(0, PathTileEffects.Get(PathTileType.Heal).GetStandingModifiers().Count);
            Assert.AreEqual(0, PathTileEffects.Get(PathTileType.Purify).GetStandingModifiers().Count);
            Assert.AreEqual(0, PathTileEffects.Get(PathTileType.Bifrost).GetStandingModifiers().Count);
        }

        [Test]
        public void 이동력_타일을_밟으면_적의_이동_범위가_오름()
        {
            EnemyController enemy = MakeEnemy();
            foreach (StatModifier modifier in PathTileEffects.Get(PathTileType.Speed).GetStandingModifiers())
                enemy.Stats.Modifiers.Add(modifier);

            MoveRange range = enemy.Stats.GetMoveRange();
            Assert.AreEqual(4, range.Min);
            Assert.AreEqual(6, range.Max);

            enemy.Stats.Modifiers.RemoveBySource(PathTileEffect.Source);       // 칸을 떠남
            Assert.AreEqual(2, enemy.Stats.GetMoveRange().Min);
        }

        // ── 회복 ──

        [Test]
        public void 회복은_최대_체력을_넘지_않음()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            health.TakeDamage(10f);

            Assert.AreEqual(10f, health.Heal(30f), 0.0001f, "실제로 찬 양은 10");
            Assert.AreEqual(100f, health.Current, 0.0001f);
        }

        [Test]
        public void 회복하면_실제로_찬_양을_돌려줌()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            health.TakeDamage(50f);

            Assert.AreEqual(20f, health.Heal(20f), 0.0001f);
            Assert.AreEqual(70f, health.Current, 0.0001f);
        }

        [Test]
        public void 죽은_적은_회복하지_않음()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            health.TakeDamage(100f);

            Assert.AreEqual(0f, health.Heal(50f));
            Assert.AreEqual(0f, health.Current);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void 영_이하는_회복이_아님()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            health.TakeDamage(50f);

            Assert.AreEqual(0f, health.Heal(-10f));
            Assert.AreEqual(50f, health.Current, 0.0001f);
        }

        [Test]
        public void 회복_타일은_매_턴_최대_체력의_5퍼센트()
        {
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp;
            enemy.Health.TakeDamage(max * 0.5f);

            PathTileEffects.Get(PathTileType.Heal).OnTurnStanding(enemy);
            Assert.AreEqual(max * 0.55f, enemy.CurrentHp, 0.001f);

            PathTileEffects.Get(PathTileType.Heal).OnTurnStanding(enemy);
            Assert.AreEqual(max * 0.60f, enemy.CurrentHp, 0.001f);
        }

        // ── 행동 카운트 ──

        [Test]
        public void 행동_카운트를_더하면_다음_행동까지_남은_수가_줆()
        {
            m_go = new GameObject("TestMovement");
            var movement = m_go.AddComponent<EnemyMovement>();
            int before = movement.ActionCounter;

            movement.AddActionProgress(ActionCountTileEffect.Amount);
            Assert.AreEqual(before - 2, movement.ActionCounter);
        }
    }
}
