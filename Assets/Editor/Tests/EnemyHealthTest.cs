using NUnit.Framework;

namespace SquareTD.Tests
{
    public class EnemyHealthTest
    {
        [Test]
        public void Reset하면_꽉_차고_살아_있음()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            Assert.AreEqual(100f, health.Max, 0.0001f);
            Assert.AreEqual(100f, health.Current, 0.0001f);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void 맞아도_남으면_false()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            Assert.IsFalse(health.TakeDamage(30f));
            Assert.AreEqual(70f, health.Current, 0.0001f);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void 딱_0이_되면_죽음()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            Assert.IsTrue(health.TakeDamage(100f));
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(0f, health.Current, 0.0001f);
        }

        [Test]
        public void 넘치게_맞아도_체력은_0()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            Assert.IsTrue(health.TakeDamage(250f));
            Assert.AreEqual(0f, health.Current, 0.0001f);
        }

        [Test]
        public void 죽은_뒤의_피해는_무시_죽음은_한_번만()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            Assert.IsTrue(health.TakeDamage(100f));
            Assert.IsFalse(health.TakeDamage(50f));
            Assert.IsFalse(health.TakeDamage(50f));
            Assert.AreEqual(0f, health.Current, 0.0001f);
        }

        [Test]
        public void 여러_발로_나눠_맞아도_죽는_발에서만_true()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            int deaths = 0;
            for (int i = 0; i < 10; i++)
            {
                if (health.TakeDamage(15f)) deaths++;
            }
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void 풀에서_다시_꺼내_Reset하면_다시_죽을_수_있음()
        {
            var health = new EnemyHealth();
            health.Reset(100f);
            health.TakeDamage(100f);

            health.Reset(200f);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(200f, health.Current, 0.0001f);
            Assert.IsTrue(health.TakeDamage(200f));
        }
    }
}
