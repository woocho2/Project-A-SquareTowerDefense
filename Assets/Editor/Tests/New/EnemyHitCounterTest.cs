using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    public class EnemyHitCounterTest
    {
        // ── 수 세기 ──

        [Test]
        public void 처음엔_0()
        {
            var counter = new EnemyHitCounter();
            Assert.AreEqual(0, counter.Get(TowerEmblem.Sword));
        }

        [Test]
        public void 맞을_때마다_1씩_오르고_오른_수를_돌려줌()
        {
            var counter = new EnemyHitCounter();
            Assert.AreEqual(1, counter.Add(TowerEmblem.Sword));
            Assert.AreEqual(2, counter.Add(TowerEmblem.Sword));
            Assert.AreEqual(2, counter.Get(TowerEmblem.Sword));
        }

        [Test]
        public void 문양마다_따로_셈()
        {
            // 20번 4-1장: A가 2회, B가 1회 맞았으면 합쳐서 3회여도 어느 쪽에도 발동하지 않음 → 같은 적이라도 문양이 다르면 따로
            var counter = new EnemyHitCounter();
            counter.Add(TowerEmblem.Sword);
            counter.Add(TowerEmblem.Sword);
            counter.Add(TowerEmblem.Bow);

            Assert.AreEqual(2, counter.Get(TowerEmblem.Sword));
            Assert.AreEqual(1, counter.Get(TowerEmblem.Bow));
            Assert.AreEqual(0, counter.Get(TowerEmblem.Fire));
        }

        [Test]
        public void 발동하면_그_문양만_0()
        {
            var counter = new EnemyHitCounter();
            counter.Add(TowerEmblem.Sword);
            counter.Add(TowerEmblem.Bow);

            counter.Reset(TowerEmblem.Sword);
            Assert.AreEqual(0, counter.Get(TowerEmblem.Sword));
            Assert.AreEqual(1, counter.Get(TowerEmblem.Bow), "다른 문양의 수는 그대로");
        }

        [Test]
        public void 영으로_돌린_뒤_다시_1부터()
        {
            var counter = new EnemyHitCounter();
            counter.Add(TowerEmblem.Sword);
            counter.Add(TowerEmblem.Sword);
            counter.Reset(TowerEmblem.Sword);
            Assert.AreEqual(1, counter.Add(TowerEmblem.Sword));
        }

        [Test]
        public void 없는_문양을_지워도_괜찮음()
        {
            var counter = new EnemyHitCounter();
            Assert.DoesNotThrow(() => counter.Reset(TowerEmblem.Darkness));
        }

        [Test]
        public void 전부_지우기()
        {
            var counter = new EnemyHitCounter();
            counter.Add(TowerEmblem.Sword);
            counter.Add(TowerEmblem.Bow);
            counter.Clear();
            Assert.AreEqual(0, counter.Get(TowerEmblem.Sword));
            Assert.AreEqual(0, counter.Get(TowerEmblem.Bow));
        }

        // ── 20번 4-1장의 예 (하위 10회 · 상위 5회). 발동 판단은 4단계에서 이 모양으로 씀 ──

        [Test]
        public void 상위가_4대_하위가_6대면_10회째에_하위가_터짐()
        {
            var counter = new EnemyHitCounter();
            const int lowNeed = 10;
            for (int i = 0; i < 4; i++) counter.Add(TowerEmblem.Sword);       // 상위 4대 (필요 5라 아직)

            int firedAt = -1;
            for (int i = 1; i <= 6; i++)                                          // 하위 6대
            {
                if (counter.Add(TowerEmblem.Sword) >= lowNeed) { firedAt = i; break; }
            }
            Assert.AreEqual(6, firedAt, "합쳐서 10회째 = 하위의 6번째 공격");
        }

        [Test]
        public void 하위가_7대면_상위의_첫_공격에_바로_터짐()
        {
            var counter = new EnemyHitCounter();
            const int highNeed = 5;
            for (int i = 0; i < 7; i++) counter.Add(TowerEmblem.Sword);       // 하위 7대 (필요 10이라 아직)

            Assert.IsTrue(counter.Add(TowerEmblem.Sword) >= highNeed, "이미 5회를 넘었으므로 상위가 한 대만 맞혀도 터짐");
        }

        // ── 적에 붙은 것 ──

        [Test]
        public void 다시_소환하면_적중_수가_비어_있음()
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 100f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var go = new GameObject("TestEnemy");

            try
            {
                var enemy = go.AddComponent<EnemyController>();
                enemy.Setup(definition, null, 1);
                enemy.HitCounter.Add(TowerEmblem.Sword);
                enemy.HitCounter.Add(TowerEmblem.Sword);

                enemy.Setup(definition, null, 1);                                // 풀에서 다시 꺼낸 것
                Assert.AreEqual(0, enemy.HitCounter.Get(TowerEmblem.Sword));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(definition);
            }
        }
    }
}
