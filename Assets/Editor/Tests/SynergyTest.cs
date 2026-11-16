using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    public class SynergyTest : EmblemTestBase
    {
        private const float Eps = 0.0001f;

        private static DamageInfo Hit(float damage) => new DamageInfo { BaseDamage = damage, Source = DamageSource.Basic };

        [Test]
        public void 조합표는_14종이고_문양마다_정확히_3번()
        {
            Assert.AreEqual(14, SynergyRecipes.All.Count);
            var counts = new Dictionary<TowerEmblem, int>();
            foreach (SynergyRecipes.Recipe recipe in SynergyRecipes.All)
                foreach (TowerEmblem emblem in recipe.Emblems)
                    counts[emblem] = counts.TryGetValue(emblem, out int n) ? n + 1 : 1;

            Assert.AreEqual(14, counts.Count);
            foreach (KeyValuePair<TowerEmblem, int> pair in counts) Assert.AreEqual(3, pair.Value, pair.Key.ToString());
        }

        [Test]
        public void 두_시너지가_재료를_2개_이상_나누지_않음()
        {
            IReadOnlyList<SynergyRecipes.Recipe> all = SynergyRecipes.All;
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                    Assert.LessOrEqual(SynergyRecipes.Overlap(all[i], all[j].Emblems), 1, $"{all[i].Name} / {all[j].Name}");
        }

        [Test]
        public void 티어_합은_재료_문양_3개_곱하기_컬러_4개의_가장_높은_티어()
        {
            SynergyRecipes.TryGet(SynergyEffects.OdinId, out SynergyRecipes.Recipe odin);
            var towers = new List<TowerDefinition>
            {
                Def(TowerId.Make(4, TowerColor.Red, TowerEmblem.Spear)),
                Def(TowerId.Make(2, TowerColor.Red, TowerEmblem.Spear)),
                Def(TowerId.Make(3, TowerColor.Purple, TowerEmblem.Darkness)),
                Def(TowerId.Make(4, TowerColor.Blue, TowerEmblem.Sword)),
            };

            Assert.AreEqual(7, SynergyRecipes.TierSum(odin, SynergyRecipes.HighestTiers(towers)), "창 광역 4(같은 종류는 가장 높은 것) + 어둠 디버프 3. 검은 재료가 아님");
        }

        [Test]
        public void 티어_합의_최대는_48()
        {
            SynergyRecipes.TryGet(SynergyEffects.OdinId, out SynergyRecipes.Recipe odin);
            Assert.AreEqual(48, SynergyRecipes.MaxTierSum);
            Assert.AreEqual(48, SynergyRecipes.TierSum(odin, (color, emblem) => TowerId.MaxTier));
        }

        [Test]
        public void 덱과_겹치는_수가_많은_시너지부터_칸_수만큼()
        {
            var deck = new HashSet<TowerEmblem> { TowerEmblem.Spear, TowerEmblem.Electricity, TowerEmblem.Darkness };

            List<SynergyRecipes.Recipe> picked = SynergyRecipes.Pick(deck, 2);

            Assert.AreEqual(2, picked.Count);
            Assert.AreEqual(SynergyEffects.OdinId, picked[0].Id, "3개 모두 겹침");
        }

        [TestCase(12, 0.06f)]
        [TestCase(48, 0.24f)]
        [TestCase(0, 0f)]
        public void 시너지_계수는_티어_합_곱하기_0점5퍼센트(int tierSum, float rate)
        {
            Assert.AreEqual(rate, SynergyEffects.TierRate(tierSum), Eps);
        }

        [Test]
        public void 토르의_번개_폭풍은_그_칸의_적에게_최대_체력의_비율_고정_피해()
        {
            EnemyController inside = MakeEnemy(0, hp: 1000f, defense: 100f), outside = MakeEnemy(1, hp: 1000f);

            int hits = SynergyEffects.ThorStorm(m_occupancy, 0, 12);

            Assert.AreEqual(1, hits);
            Assert.AreEqual(60f, Lost(inside), Eps, "1000 × 6%, 방어력과 무관");
            Assert.AreEqual(0f, Lost(outside), Eps);
        }

        [Test]
        public void 토르의_자리는_길_타일_중_하나()
        {
            for (int i = 0; i < 50; i++)
            {
                int index = SynergyEffects.PickThorStormIndex(40);
                Assert.That(index, Is.InRange(0, 39));
            }
            Assert.AreEqual(-1, SynergyEffects.PickThorStormIndex(0));
        }

        [Test]
        public void 오딘은_기준_이하인_적을_즉사시키고_보스도_포함()
        {
            EnemyController low = MakeEnemy(0, hp: 1000f), high = MakeEnemy(1, hp: 1000f);
            low.TakeDamage(Hit(950f));
            high.TakeDamage(Hit(900f));

            int killed = SynergyEffects.OdinExecute(new List<EnemyController> { low, high }, 12);

            Assert.AreEqual(1, killed);
            Assert.IsFalse(low.IsAlive, "5% ≤ 6%");
            Assert.IsTrue(high.IsAlive, "10% > 6%");
        }

        [Test]
        public void 로키는_보스를_고르지_않음()
        {
            EnemyController normal = MakeEnemy(0), boss = MakeEnemy(1);
            var so = new UnityEditor.SerializedObject(boss.Definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.MiddleBoss;
            so.ApplyModifiedPropertiesWithoutUndo();

            List<EnemyController> candidates = SynergyEffects.LokiCandidates(new List<EnemyController> { normal, boss });

            Assert.AreEqual(1, candidates.Count);
            Assert.AreSame(normal, candidates[0]);
        }

        [Test]
        public void 로키가_데려오는_자리는_그_타워에서_가장_가까운_길_타일()
        {
            for (int slot = 0; slot < m_grid.SpawnCount; slot++)
            {
                int index = SynergyEffects.NearestPathIndex(m_grid, slot);
                int distance = BoardGrid.GetTileDistance(m_grid.GetPathCell(index), m_grid.GetSpawnCell(slot));
                for (int i = 0; i < m_grid.PathCount; i++)
                    Assert.LessOrEqual(distance, BoardGrid.GetTileDistance(m_grid.GetPathCell(i), m_grid.GetSpawnCell(slot)), $"칸 {slot}");
            }
        }

        [Test]
        public void 헬은_매_턴_무작위_적_1기의_최대_체력_2퍼센트를_깎고_스택을_n만큼()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f, defense: 100f);
            var count = new HelDeathCount();

            int killed = count.Tick(new List<EnemyController> { enemy }, 12);

            Assert.AreEqual(0, killed);
            Assert.AreEqual(20f, Lost(enemy), Eps, "1000 × 2%, 방어력과 무관");
            Assert.AreEqual(6f, count.Stack.Stacks, Eps, "0.5 × 티어 합 12");
        }

        [Test]
        public void 헬의_데스_카운트가_차면_보스를_뺀_적_전체_즉사하고_0으로()
        {
            EnemyController big = MakeEnemy(0, hp: 10000f), other = MakeEnemy(1, hp: 1000f), boss = MakeEnemy(2, hp: 100000f);
            var so = new UnityEditor.SerializedObject(boss.Definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.FinalBoss;
            so.ApplyModifiedPropertiesWithoutUndo();
            var count = new HelDeathCount();
            var enemies = new List<EnemyController> { big, other, boss };

            for (int i = 0; i < 41; i++) Assert.AreEqual(0, count.Tick(enemies, 48), $"{i + 1}턴");
            int killed = count.Tick(enemies, 48);

            Assert.AreEqual(2, killed, "티어 합 48이면 24씩, 42턴째 1000");
            Assert.IsTrue(boss.IsAlive, "보스는 즉사하지 않음");
            Assert.IsFalse(other.IsAlive);
            Assert.IsFalse(big.IsAlive);
            Assert.AreEqual(0f, count.Stack.Stacks, Eps);
        }

        [Test]
        public void 수르트는_잃은_체력의_10퍼센트_고정_피해이고_다친_적만_맞음()
        {
            EnemyController hurt = MakeEnemy(0, hp: 1000f, defense: 100f), full = MakeEnemy(1, hp: 1000f);
            hurt.TakeDamage(Hit(500f));
            float lostBefore = Lost(hurt);

            int hits = SynergyEffects.SurtrBurn(new List<EnemyController> { hurt, full });

            Assert.AreEqual(1, hits);
            Assert.AreEqual(lostBefore * 1.1f, Lost(hurt), Eps, "잃은 체력 × 10%, 방어력과 무관");
            Assert.AreEqual(0f, Lost(full), Eps, "잃은 체력이 없으면 0");
        }

        [Test]
        public void 시너지_스택은_매_턴_0점5_곱하기_티어_합씩_쌓이고_100이면_터지고_0으로()
        {
            var stack = new SynergyStack();

            for (int i = 0; i < 16; i++) Assert.IsFalse(stack.Gain(12), $"{i + 1}턴: 6씩");
            Assert.AreEqual(0.96f, stack.Ratio, Eps);

            Assert.IsTrue(stack.Gain(12), "17턴째 100");
            Assert.AreEqual(0f, stack.Stacks, Eps);
        }

        [Test]
        public void 수르트와_프레이는_스택이_차는_턴에_터짐()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            enemy.TakeDamage(Hit(500f));
            var sword = new SurtrSword();
            var blessing = new FreyBlessing();
            var towers = new List<TowerController> { MakeTower(1101) };

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(0, sword.Tick(new List<EnemyController> { enemy }, 48));
                Assert.AreEqual(0, blessing.Tick(towers, 48));
            }

            Assert.AreEqual(1, sword.Tick(new List<EnemyController> { enemy }, 48), "티어 합 48이면 24씩, 5턴째 100");
            Assert.AreEqual(1, blessing.Tick(towers, 48));
        }

        [Test]
        public void 프레이의_가호는_모든_타워의_다음_행동_한_번에_AttackCount_플러스1()
        {
            TowerController attack = MakeTower(1101, slot: 0), buff = MakeTower(1301, slot: 1);
            int attackBefore = attack.Stats.AttackCount, buffBefore = buff.Stats.AttackCount;

            int blessed = SynergyEffects.FreyBless(new List<TowerController> { attack, buff });

            Assert.AreEqual(2, blessed, "버프·디버프 타워도 받음");
            Assert.AreEqual(attackBefore + 1, attack.Stats.AttackCount);
            Assert.AreEqual(buffBefore + 1, buff.Stats.AttackCount);
            Assert.AreEqual(1, TimedTowerBuff.GetRemaining(attack.State, SynergyEffects.FreySource), "행동 한 번 동안");
        }

        private static TowerDefinition Def(int id) =>
            new TowerDefinition(id, 10f, 2, 3, 1, 0f, 0f, 1, 60, 5, 1f, 5f, TargetPriority.Closest);
    }
}
