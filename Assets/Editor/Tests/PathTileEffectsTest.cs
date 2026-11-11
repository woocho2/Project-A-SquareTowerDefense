using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class PathTileEffectsTest
    {
        private GameObject m_go;

        [TearDown]
        public void CleanUp()
        {
            if (m_go != null) Object.DestroyImmediate(m_go);
        }

        // ── 표 ──

        [Test]
        public void 종류마다_그_효과가_나옴()
        {
            foreach (PathTileType type in new[] { PathTileType.Purify, PathTileType.Bifrost })
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

        // ── 회복 (큐브 면의 자가치유가 씀) ──

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

        // ── 행동 카운트 (게일 포스 시너지가 씀) ──

        [Test]
        public void 행동_카운트를_더하면_다음_행동까지_남은_수가_줆()
        {
            m_go = new GameObject("TestMovement");
            var movement = m_go.AddComponent<EnemyMovement>();
            int before = movement.ActionCounter;

            movement.AddActionProgress(2);
            Assert.AreEqual(before - 2, movement.ActionCounter);
        }
    }
}
