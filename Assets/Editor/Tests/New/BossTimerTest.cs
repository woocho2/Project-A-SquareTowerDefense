using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    public class BossTimerTest
    {
        private readonly List<GameObject> m_created = new List<GameObject>();

        private EnemyController MakeBoss()
        {
            var go = new GameObject("TestBoss");
            m_created.Add(go);
            return go.AddComponent<EnemyController>();
        }

        [TearDown]
        public void CleanUp()
        {
            foreach (GameObject go in m_created) Object.DestroyImmediate(go);
            m_created.Clear();
        }

        [Test]
        public void Add_남은턴은_20()
        {
            var timer = new BossTimer();
            EnemyController boss = MakeBoss();
            timer.Add(boss);
            Assert.AreEqual(20, timer.GetRemaining(boss));
            Assert.AreEqual(1, timer.Count);
        }

        [Test]
        public void Tick_19번까지는_시간이_남음()
        {
            var timer = new BossTimer();
            EnemyController boss = MakeBoss();
            timer.Add(boss);
            for (int i = 0; i < 19; i++)
            {
                Assert.IsFalse(timer.Tick());
            }
            Assert.AreEqual(1, timer.GetRemaining(boss));
        }

        [Test]
        public void Tick_20번째에_시간이_다됨()
        {
            var timer = new BossTimer();
            timer.Add(MakeBoss());
            for (int i = 0; i < 19; i++) timer.Tick();
            Assert.IsTrue(timer.Tick());
        }

        [Test]
        public void Remove_잡은_보스는_더_세지_않음()
        {
            var timer = new BossTimer();
            EnemyController boss = MakeBoss();
            timer.Add(boss);
            timer.Remove(boss);
            Assert.AreEqual(0, timer.Count);
            Assert.AreEqual(0, timer.GetRemaining(boss));
            for (int i = 0; i < 25; i++)
            {
                Assert.IsFalse(timer.Tick());
            }
        }

        [Test]
        public void 보스마다_제한턴이_따로_흐름()
        {
            var timer = new BossTimer();
            EnemyController first = MakeBoss();
            EnemyController second = MakeBoss();

            timer.Add(first);
            for (int i = 0; i < 10; i++) timer.Tick();
            timer.Add(second);

            Assert.AreEqual(10, timer.GetRemaining(first));
            Assert.AreEqual(20, timer.GetRemaining(second));

            for (int i = 0; i < 9; i++) Assert.IsFalse(timer.Tick());
            Assert.IsTrue(timer.Tick());
            Assert.AreEqual(10, timer.GetRemaining(second));
        }

        [Test]
        public void 없는_보스의_남은턴은_0()
        {
            var timer = new BossTimer();
            Assert.AreEqual(0, timer.GetRemaining(MakeBoss()));
        }
    }
}
