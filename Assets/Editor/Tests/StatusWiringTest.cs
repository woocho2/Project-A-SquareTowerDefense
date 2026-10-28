using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 25 3단계: 적에 붙은 저항·상태 효과 (피해 앞뒤 훅, 사망 훅, 정화 타일, 다시 소환)
    public class StatusWiringTest
    {
        // 받는 피해 ×2 + 훅이 불린 횟수를 세는 시험 효과
        private class DoubleDamageEffect : StatusEffect
        {
            public int DamageTakenCalls;
            public int DeathCalls;

            public DoubleDamageEffect() : base(5) { }
            public override string Key => "두 배";
            public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage) => damage * 2f;
            public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage) => DamageTakenCalls++;
            public override void OnDeath(EnemyController enemy, KillInfo kill) => DeathCalls++;
            public override string Describe() => "받는 피해 ×2";
        }

        private GameObject m_go;
        private EnemyDefinition m_definition;
        private EnemyController m_enemy;

        [SetUp]
        public void SetUp()
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 100f;
            so.FindProperty("<Defense>k__BackingField").floatValue = 0f;
            so.FindProperty("<BaseResistance>k__BackingField").floatValue = 10f;
            so.ApplyModifiedPropertiesWithoutUndo();

            m_go = new GameObject("TestEnemy");
            m_enemy = m_go.AddComponent<EnemyController>();
            m_enemy.Setup(m_definition, null, 1);
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_go);
            Object.DestroyImmediate(m_definition);
        }

        private static DamageInfo Hit(float damage) => new DamageInfo { BaseDamage = damage };

        [Test]
        public void 소환하면_저항과_효과_목록이_생김()
        {
            Assert.IsNotNull(m_enemy.Status);
            Assert.AreEqual(10f, m_enemy.Resistance.Base, "적 데이터의 BaseResistance");
            Assert.AreEqual(0, m_enemy.Status.Effects.Count);
        }

        [Test]
        public void 상태_효과가_피해_계산에_끼어듦()
        {
            m_enemy.Status.Add(new DoubleDamageEffect());
            Assert.AreEqual(20f, m_enemy.PreviewDamage(Hit(10f)), 0.0001f, "타워가 미리 세는 값에도 들어감");

            m_enemy.TakeDamage(Hit(10f));
            Assert.AreEqual(80f, m_enemy.CurrentHp, 0.0001f);
        }

        [Test]
        public void 맞은_뒤_훅이_불림()
        {
            var effect = new DoubleDamageEffect();
            m_enemy.Status.Add(effect);
            m_enemy.TakeDamage(Hit(10f));
            Assert.AreEqual(1, effect.DamageTakenCalls);
        }

        [Test]
        public void 죽으면_사망_훅이_한_번()
        {
            var effect = new DoubleDamageEffect();
            m_enemy.Status.Add(effect);
            m_enemy.TakeDamage(Hit(500f));

            Assert.IsFalse(m_enemy.IsAlive);
            Assert.AreEqual(1, effect.DeathCalls);
            Assert.AreEqual(1, effect.DamageTakenCalls, "죽는 한 방에도 맞은 뒤 훅은 불림");
        }

        [Test]
        public void 정화_타일은_효과를_다_지우고_저항을_기본으로()
        {
            m_enemy.Status.Add(new DoubleDamageEffect());
            m_enemy.Resistance.TryStrongCC(99.9f);                  // 10 → 20
            Assert.AreEqual(20f, m_enemy.Resistance.Current, 0.0001f);

            PathTileEffects.Get(PathTileType.Purify).OnStopped(m_enemy, null, null);

            Assert.AreEqual(0, m_enemy.Status.Effects.Count, "영구까지 전부");
            Assert.AreEqual(10f, m_enemy.Resistance.Current, 0.0001f);
        }

        [Test]
        public void 다시_소환하면_지난번_효과와_저항이_없음()
        {
            m_enemy.Status.Add(new DoubleDamageEffect());
            m_enemy.Resistance.TryStrongCC(99.9f);

            m_enemy.Setup(m_definition, null, 1);                    // 풀에서 다시 꺼낸 것

            Assert.AreEqual(0, m_enemy.Status.Effects.Count);
            Assert.AreEqual(10f, m_enemy.Resistance.Current, 0.0001f);
            Assert.AreEqual(10f, m_enemy.PreviewDamage(Hit(10f)), 0.0001f, "×2 효과가 남아 있지 않음");
        }
    }
}
