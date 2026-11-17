using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    public class SynergyTest : EmblemTestBase
    {
        private const float Eps = 0.0001f;

        private static DamageInfo Hit(float damage) => new DamageInfo { BaseDamage = damage, Source = DamageSource.Basic };

        // ── 조합표·티어 합 ──

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

        // ── 공용 스택·버튼 ──

        [Test]
        public void 스택은_매_턴_티어_합만큼_쌓이고_다_차면_멈춤()
        {
            var stack = new SynergyStack(100f);

            for (int i = 0; i < 8; i++) Assert.IsFalse(stack.Gain(12), $"{i + 1}턴: 12씩");
            Assert.AreEqual(0.96f, stack.Ratio, Eps);
            Assert.IsTrue(stack.Gain(12), "9턴째 100");
            Assert.IsFalse(stack.Gain(12), "다 찬 뒤에는 더 쌓지 않음");
            Assert.AreEqual(100f, stack.Stacks, Eps);

            stack.Consume();
            Assert.AreEqual(0f, stack.Stacks, Eps);
        }

        [Test]
        public void 스택이_다_차기_전에는_버튼을_눌러도_발동하지_않고_쓰면_0으로()
        {
            var surtr = new SurtrSkill();
            var context = new SynergyContext { Enemies = new List<EnemyController>() };

            Assert.IsFalse(surtr.TryUse(context));
            for (int i = 0; i < 10; i++) surtr.Tick(context, 48);
            Assert.IsFalse(surtr.IsReady, "48씩 10턴이면 480");
            surtr.Tick(context, 48);
            Assert.IsTrue(surtr.IsReady, "11턴째 500");
            Assert.IsTrue(surtr.TryUse(context));
            Assert.AreEqual(0f, surtr.Stack.Stacks, Eps);
            Assert.IsFalse(surtr.IsReady);
        }

        [TestCase(SynergyEffects.OdinId, 1000f)]
        [TestCase(SynergyEffects.ThorId, 250f)]
        [TestCase(SynergyEffects.LokiId, 250f)]
        [TestCase(SynergyEffects.HelId, 1000f)]
        [TestCase(SynergyEffects.SurtrId, 500f)]
        [TestCase(SynergyEffects.FreyId, 500f)]
        [TestCase(SynergyEffects.BaldrId, 500f)]
        [TestCase(SynergyEffects.SkadiId, 250f)]
        [TestCase(SynergyEffects.FreyjaId, 500f)]
        [TestCase(SynergyEffects.YmirId, 1000f)]
        [TestCase(SynergyEffects.UllrId, 750f)]
        [TestCase(SynergyEffects.BrynhildId, 1000f)]
        [TestCase(SynergyEffects.BrokkrId, 750f)]
        [TestCase(SynergyEffects.NjordId, 250f)]
        public void 시너지마다_스택_최대(int id, float max)
        {
            SynergySkill skill = SynergyEffects.Create(id);
            Assert.AreEqual(id, skill.Id);
            Assert.AreEqual(max, skill.Stack.MaxStacks, Eps);
        }

        private static void Fill(SynergySkill skill, SynergyContext context)
        {
            for (int i = 0; i < 100 && !skill.Stack.IsFull; i++) skill.Tick(context, 48);
        }

        // ── 효과 ──

        [Test]
        public void 오딘은_체력_50퍼센트_이하인_일반_적과_10퍼센트_이하인_보스를_즉사()
        {
            EnemyController half = MakeEnemy(0, hp: 1000f), high = MakeEnemy(1, hp: 1000f), bossLow = MakeBoss(2, 1000f), bossMid = MakeBoss(3, 1000f);
            half.TakeDamage(Hit(500f));
            high.TakeDamage(Hit(400f));
            bossLow.TakeDamage(Hit(900f));
            bossMid.TakeDamage(Hit(600f));
            var context = new SynergyContext { Enemies = new List<EnemyController> { half, high, bossLow, bossMid } };
            var odin = new OdinSkill();
            Fill(odin, context);

            Assert.IsTrue(odin.TryUse(context));

            Assert.IsFalse(half.IsAlive, "일반 50%");
            Assert.IsTrue(high.IsAlive, "일반 60%");
            Assert.IsFalse(bossLow.IsAlive, "보스 10%");
            Assert.IsTrue(bossMid.IsAlive, "보스 40%는 살아남음");
        }

        [Test]
        public void 토르는_길_타일_5곳에_5턴_동안_매_턴_최대_체력_4퍼센트()
        {
            var context = new SynergyContext { Enemies = new List<EnemyController>(), Grid = m_grid, Occupancy = m_occupancy };
            var thor = new ThorSkill();
            Fill(thor, context);
            Assert.IsTrue(thor.TryUse(context));

            Assert.AreEqual(5, thor.Storms.Count);
            var cells = new HashSet<int>();
            foreach (var storm in thor.Storms) cells.Add(storm.pathIndex);
            Assert.AreEqual(5, cells.Count, "서로 다른 5칸");

            EnemyController inside = MakeEnemy(thor.Storms[0].pathIndex, hp: 1000f, defense: 100f);
            for (int i = 0; i < 6; i++) thor.Tick(context, 0);

            Assert.AreEqual(200f, Lost(inside), Eps, "1000 × 4% × 5턴, 방어력과 무관");
            Assert.AreEqual(0, thor.Storms.Count);
        }

        [Test]
        public void 로키는_플레이어_턴에_서로_다른_두_칸일_때만()
        {
            var context = new SynergyContext { Grid = m_grid, Occupancy = m_occupancy, PathA = 0, PathB = 3 };
            Assert.IsFalse(LokiSkill.CanSwap(context), "적 턴에는 못 씀");

            context.IsPlayerTurn = true;
            Assert.IsTrue(LokiSkill.CanSwap(context));

            context.PathB = 0;
            Assert.IsFalse(LokiSkill.CanSwap(context), "같은 칸");
        }

        [Test]
        public void 로키는_A의_적을_B로_B의_적을_A로()
        {
            EnemyController a1 = MakeEnemy(0), a2 = MakeEnemy(0), b1 = MakeEnemy(3), other = MakeEnemy(5);

            (List<EnemyController> toB, List<EnemyController> toA) = LokiSkill.Plan(m_occupancy, 0, 3);

            CollectionAssert.AreEquivalent(new[] { a1, a2 }, toB);
            CollectionAssert.AreEquivalent(new[] { b1 }, toA);
            CollectionAssert.DoesNotContain(toB, other);
        }

        [Test]
        public void 헬은_스택만_쌓고_스킬은_보스를_뺀_전체_즉사()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f), boss = MakeBoss(1, 100000f);
            var context = new SynergyContext { Enemies = new List<EnemyController> { enemy, boss } };
            var hel = new HelSkill();

            hel.Tick(context, 12);
            Assert.AreEqual(0f, Lost(enemy), Eps, "매 턴 체력을 깎지 않음");
            Assert.AreEqual(12f, hel.Stack.Stacks, Eps, "티어 합 12");

            Fill(hel, context);
            Assert.IsTrue(hel.TryUse(context));
            Assert.IsFalse(enemy.IsAlive);
            Assert.IsTrue(boss.IsAlive, "보스는 즉사하지 않음");
        }

        [Test]
        public void 수르트는_잃은_체력의_10퍼센트_고정_피해이고_다친_적만_맞음()
        {
            EnemyController hurt = MakeEnemy(0, hp: 1000f, defense: 100f), full = MakeEnemy(1, hp: 1000f);
            hurt.TakeDamage(Hit(500f));
            float lostBefore = Lost(hurt);
            var context = new SynergyContext { Enemies = new List<EnemyController> { hurt, full } };
            var surtr = new SurtrSkill();
            Fill(surtr, context);

            Assert.IsTrue(surtr.TryUse(context));

            Assert.AreEqual(lostBefore * 1.1f, Lost(hurt), Eps, "잃은 체력 × 10%, 방어력과 무관");
            Assert.AreEqual(0f, Lost(full), Eps);
        }

        [Test]
        public void 프레이는_모든_타워의_다음_행동_5번_동안_AttackCount_플러스1()
        {
            TowerController attack = MakeTower(1101, slot: 0), buff = MakeTower(1301, slot: 1);
            int attackBefore = attack.Stats.AttackCount, buffBefore = buff.Stats.AttackCount;
            var context = new SynergyContext { Towers = new List<TowerController> { attack, buff } };
            var frey = new FreySkill();
            Fill(frey, context);

            Assert.IsTrue(frey.TryUse(context));

            Assert.AreEqual(attackBefore + 1, attack.Stats.AttackCount);
            Assert.AreEqual(buffBefore + 1, buff.Stats.AttackCount, "버프·디버프 타워도 받음");
            Assert.AreEqual(5, TimedTowerBuff.GetRemaining(attack.State, FreySkill.Source), "행동 5번 동안");
        }

        [Test]
        public void 프레이야는_5턴_동안_처치_골드_2배()
        {
            var context = new SynergyContext();
            var freyja = new FreyjaSkill();
            Fill(freyja, context);
            Assert.IsTrue(freyja.TryUse(context));
            Assert.AreEqual(5, EmblemSharedState.Current.GoldDoubleTurns);

            int gold = 10;
            DeathResolver.GetBonusReward(new KillInfo(), ref gold);
            Assert.AreEqual(20, gold, "골드 2배");

            for (int i = 0; i < 5; i++) freyja.Tick(context, 0);
            Assert.AreEqual(0, EmblemSharedState.Current.GoldDoubleTurns);
            gold = 10;
            DeathResolver.GetBonusReward(new KillInfo(), ref gold);
            Assert.AreEqual(10, gold, "5턴이 지나면 원래대로");
        }

        [Test]
        public void 발드르는_스택_500마다_무력화_1회_최대_2회_버튼으로_다음_기믹_한_번()
        {
            var context = new SynergyContext();
            var baldr = new BaldrSkill();
            Assert.IsFalse(baldr.TryUse(context), "무력화가 없으면 못 씀");

            for (int i = 0; i < 11; i++) baldr.Tick(context, 48);
            Assert.AreEqual(1, baldr.Charges, "48씩 11턴이면 500");
            for (int i = 0; i < 11; i++) baldr.Tick(context, 48);
            Assert.AreEqual(2, baldr.Charges);
            for (int i = 0; i < 30; i++) baldr.Tick(context, 48);
            Assert.AreEqual(2, baldr.Charges, "2회가 차 있으면 더 쌓지 않음");
            Assert.AreEqual(0f, baldr.Stack.Stacks, Eps);

            Assert.IsTrue(baldr.TryUse(context));
            Assert.AreEqual(1, baldr.Charges);
            Assert.IsTrue(baldr.TryBlockMechanic(), "다음 기믹을 막음");
            Assert.IsFalse(baldr.TryBlockMechanic(), "그다음 기믹은 막지 않음");
        }

        [Test]
        public void 스카디는_맵_전체_적의_이동력을_5번_행동_동안_5씩()
        {
            EnemyController a = MakeEnemy(0, speedMin: 6, speedMax: 10), b = MakeEnemy(1, speedMin: 6, speedMax: 10);
            var context = new SynergyContext { Enemies = new List<EnemyController> { a, b } };
            var skadi = new SkadiSkill();
            Fill(skadi, context);

            Assert.IsTrue(skadi.TryUse(context));

            Assert.AreEqual(1f, a.Stats.Get(StatType.SpeedMin), Eps);
            Assert.AreEqual(5f, a.Stats.Get(StatType.SpeedMax), Eps);
            Assert.IsTrue(b.Status.Has(SkadiSkill.Key));
        }

        [Test]
        public void 이미르는_맵_전체_적이_다음_행동_5번_동안_행동_불가()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 100f), boss = MakeBoss(1, 1000f);
            enemy.Status.Add(new ImmobilizeEffect("기절"));
            var context = new SynergyContext { Enemies = new List<EnemyController> { enemy, boss } };
            var ymir = new YmirSkill();
            Fill(ymir, context);

            Assert.IsTrue(ymir.TryUse(context));

            Assert.IsTrue(enemy.Status.Has(YmirFrozenEffect.FrozenKey), "저항력 100, 이미 기절이어도 걸림");
            Assert.IsTrue(boss.Status.Has(YmirFrozenEffect.FrozenKey), "보스도");
            MoveRange range = boss.Status.DecideMove(new MoveRange(3, 5, 4));
            Assert.AreEqual(0, range.Min);
            Assert.AreEqual(0, range.Max);

            for (int i = 0; i < 5; i++) boss.Status.OnActed();
            Assert.IsFalse(boss.Status.Has(YmirFrozenEffect.FrozenKey), "5번 행동 뒤 풀림");
        }

        [Test]
        public void 울르는_모든_타워의_다음_행동_5번_동안_공격마다_50퍼센트_추가타()
        {
            TowerController blessed = MakeTower(1101, slot: 0), plain = MakeTower(1201, slot: 1);
            EnemyController a = MakeEnemy(0, hp: 1000f), b = MakeEnemy(1, hp: 1000f);
            var context = new SynergyContext { Towers = new List<TowerController> { blessed } };
            var ullr = new UllrSkill();
            Fill(ullr, context);

            Assert.IsTrue(ullr.TryUse(context));
            Assert.AreEqual(5, TimedTowerBuff.GetRemaining(blessed.State, UllrSkill.Source));

            a.TakeDamage(new DamageInfo { Attacker = blessed, BaseDamage = 100f, Source = DamageSource.Basic });
            b.TakeDamage(new DamageInfo { Attacker = plain, BaseDamage = 100f, Source = DamageSource.Basic });

            Assert.AreEqual(150f, Lost(a), Eps, "100 + 추가타 50 (추가타가 또 추가타를 부르지 않음)");
            Assert.AreEqual(100f, Lost(b), Eps, "울르를 받지 않은 타워는 그대로");
        }

        [Test]
        public void 브륀힐드는_한도_게임_오버를_막는_보호를_한_번_줌()
        {
            var context = new SynergyContext();
            var brynhild = new BrynhildSkill();
            Fill(brynhild, context);

            Assert.IsTrue(brynhild.TryUse(context));

            Assert.AreEqual(1, EmblemSharedState.Current.LimitShields);
            Assert.IsTrue(EmblemSharedState.Current.TryUseLimitShield(), "한 번 막음");
            Assert.IsFalse(EmblemSharedState.Current.TryUseLimitShield(), "두 번째는 못 막음");
        }

        [Test]
        public void 브록과_에이트리는_일반_타워의_티어_합_곱하기_200골드()
        {
            var towers = new List<TowerController>
            {
                MakeTower(1101, slot: 0),
                MakeTower(3201, slot: 1),
                MakeTower(4301, slot: 2),
            };

            Assert.AreEqual((1 + 3 + 4) * 200, BrokkrSkill.GoldAmount(towers));
            Assert.AreEqual(0, BrokkrSkill.GoldAmount(new List<TowerController>()));
        }

        [Test]
        public void 뇨르드는_플레이어_턴에_고른_칸을_5턴_동안_멈춤_칸으로()
        {
            var njord = new NjordSkill();
            var context = new SynergyContext { Grid = m_grid, PathA = 4 };
            Fill(njord, context);

            Assert.IsFalse(njord.TryUse(context), "적 턴에는 못 씀");
            context.IsPlayerTurn = true;
            Assert.IsTrue(njord.TryUse(context));
            Assert.IsTrue(EmblemSharedState.Current.IsStopTile(4));
            Assert.IsFalse(EmblemSharedState.Current.IsStopTile(5));

            for (int i = 0; i < 4; i++) njord.Tick(context, 0);
            Assert.IsTrue(EmblemSharedState.Current.IsStopTile(4), "4턴 뒤에도 남음");
            njord.Tick(context, 0);
            Assert.IsFalse(EmblemSharedState.Current.IsStopTile(4), "5턴 뒤 사라짐");
        }

        private EnemyController MakeBoss(int pathIndex, float hp)
        {
            EnemyController boss = MakeEnemy(pathIndex, hp: hp);
            var so = new UnityEditor.SerializedObject(boss.Definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.FinalBoss;
            so.ApplyModifiedPropertiesWithoutUndo();
            return boss;
        }

        private static TowerDefinition Def(int id) =>
            new TowerDefinition(id, 10f, 2, 3, 1, 0f, 0f, 1, 60, 5, 1f, 5f, TargetPriority.Closest);
    }
}
