using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 34 물 (20번 4-10장) — 쓰나미 · 급류 · 물결→해류 · 젖음→침수
    public class WaterEmblemTest : EmblemTestBase
    {
        [Test]
        public void 등록표_물()
        {
            Assert.IsInstanceOf<WaterSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Water, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<WaterTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Water, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<WaterBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Water));
            Assert.IsInstanceOf<WaterDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Water));
        }

        // ── 광역: 쓰나미 ──

        [Test]
        public void 쓰나미는_맵_전체_피해와_다음_행동_이동력_마이너스2()
        {
            EnemyController enemy = MakeEnemy(2, speedMin: 3, speedMax: 5);
            new WaterSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1108), null);

            Assert.AreEqual(20f, Lost(enemy), 0.001f);
            MoveRange range = enemy.Stats.GetMoveRange();
            Assert.AreEqual(1, range.Min);
            Assert.AreEqual(3, range.Max);

            enemy.Status.OnActed();
            Assert.AreEqual(5, enemy.Stats.GetMoveRange().Max, "다음 행동 한 번만");
        }

        [Test]
        public void 쓰나미의_감소는_저항_100이면_안_걸리고_피해는_그대로()
        {
            EnemyController enemy = MakeEnemy(2, resistance: 100f);
            new WaterSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1108), null);
            Assert.AreEqual(20f, Lost(enemy), 0.001f);
            Assert.AreEqual(4, enemy.Stats.GetMoveRange().Max);
        }

        [Test]
        public void 물결_존의_쓰나미는_1점25배_해류_존은_1점5배()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(1);
            TowerController wave = MakeTower(1108, slot: 0), current = MakeTower(1108, slot: 1);
            Receive(MakeZone(MakeTower(1308, slot: 2), true, 30f), wave);
            Receive(MakeZone(MakeTower(1308, slot: 2), true, 60f), current);

            new WaterSplashSkill(m_grid, m_occupancy, Auto()).Fire(wave, null);
            Assert.AreEqual(25f, a.Hits[0].BaseDamage, 0.001f);

            new WaterSplashSkill(m_grid, m_occupancy, Auto()).Fire(current, null);
            Assert.AreEqual(30f, b.Hits[1].BaseDamage, 0.001f);
        }

        [Test]
        public void 해류_존에서_스택을_쓴_쓰나미는_1점6_곱하기_1점5는_2점4배()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1108);
            Receive(MakeZone(MakeTower(1308, slot: 2), true, 60f), tower);
            tower.State.Stack.Add(60f);

            new WaterSplashSkill(m_grid, m_occupancy, Auto()).Fire(tower, null);
            Assert.AreEqual(20f * 2.4f, target.Hits[0].BaseDamage, 0.001f);
        }

        // ── 단일: 급류 ──

        [Test]
        public void 급류는_다음_행동을_뒤로_1칸()
        {
            EnemyController enemy = MakeEnemy(2);
            new WaterTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1208), enemy);

            Assert.AreEqual(20f, Lost(enemy), 0.001f, "피해를 준 뒤 밀어냄");
            MoveRange range = enemy.Status.DecideMove(enemy.Stats.GetMoveRange());
            Assert.AreEqual(-1, range.Min);
            Assert.AreEqual(-1, range.Max);
        }

        [Test]
        public void 젖은_적은_급류에_2칸()
        {
            EnemyController enemy = MakeEnemy(2);
            Debuff(MakeZone(MakeTower(1408), false, 30f), enemy);
            new WaterTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1208), enemy);
            Assert.AreEqual(-2, enemy.Status.DecideMove(enemy.Stats.GetMoveRange()).Max);
        }

        [Test]
        public void 급류의_밀어내기는_한_번_행동_뒤_끝남()
        {
            EnemyController enemy = MakeEnemy(2);
            new WaterTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1208), enemy);
            enemy.Status.OnActed();
            Assert.AreEqual(4, enemy.Status.DecideMove(enemy.Stats.GetMoveRange()).Max);
        }

        // ── 버프: 물결 · 해류 ──

        [Test]
        public void 물결은_스택이_쌓이는_양_해류는_절반에서_다시도()
        {
            List<StatModifier> ripple = new WaterBuffRule().GetModifiers(MakeZone(MakeTower(1308), true, 30f), MakeTower(1101), "s");
            List<StatModifier> current = new WaterBuffRule().GetModifiers(MakeZone(MakeTower(1308), true, 60f), MakeTower(1101), "s");
            Assert.AreEqual(0.006f, Sum(ripple, StatType.StackGainRate), 0.00001f);
            Assert.AreEqual(0f, Sum(ripple, StatType.StackKeepHalf));
            Assert.AreEqual(0.012f, Sum(current, StatType.StackGainRate), 0.00001f);
            Assert.AreEqual(1f, Sum(current, StatType.StackKeepHalf));
        }

        [Test]
        public void 해류_존의_타워는_스택을_써도_절반에서_다시()
        {
            MakeTarget(0);
            TowerController tower = MakeTower(1101);
            Receive(MakeZone(MakeTower(1308, slot: 2), true, 60f), tower);
            tower.State.Stack.Add(60f);

            new SwordSplashSkill(m_grid, m_occupancy, Auto()).Fire(tower, null);
            Assert.AreEqual(30f, tower.State.Stack.Current, 0.001f, "물이 아닌 타워도 (버프존은 문양을 가리지 않음)");
        }

        // ── 디버프: 젖음 · 침수 ──

        [Test]
        public void 젖음은_이동력을_바꾼_공격에_전부_일반_공격에_절반()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1408), false, 60f), enemy);

            Assert.AreEqual(101.2f, enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f, ChangesMovement = true }), 0.001f);
            Assert.AreEqual(100.6f, enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f }), 0.001f);
        }

        [Test]
        public void 젖음은_다른_받는_피해_증가와_더함()
        {
            EnemyController enemy = MakeEnemy(0);
            enemy.Stats.Modifiers.Add(StatModifier.Flat("락 온", StatType.DamageTaken, 0.1f));
            Debuff(MakeZone(MakeTower(1408), false, 60f), enemy);
            Assert.AreEqual(111.2f, enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f, ChangesMovement = true }), 0.001f, "× (1 + 0.1 + 0.012), 곱하면 111.32");
        }

        [Test]
        public void 침수는_맵_버프를_절반만_저항_50이면_75퍼센트()
        {
            EnemyController full = MakeEnemy(0), boss = MakeEnemy(1, resistance: 50f);
            Debuff(MakeZone(MakeTower(1408), false, 60f), full);
            Debuff(MakeZone(MakeTower(1408), false, 60f), boss);
            Assert.AreEqual(0.5f, full.Stats.Get(StatType.MapBuffRate), 0.0001f);
            Assert.AreEqual(0.75f, boss.Stats.Get(StatType.MapBuffRate), 0.0001f);
        }

        [Test]
        public void 침수인_적은_방어_타일을_10퍼센트만()
        {
            var layout = new SpecialTileLayout();         // 길 다섯 칸을 전부 방어 타일로
            layout.Generate(m_grid, new SpecialTileCounts
            {
                Bifrost = false, Defense = m_grid.PathCount, Speed = 0, PathActionCount = 0, Heal = 0, Purify = 0,
                Power = 0, SpawnActionCount = 0, AttackCount = 0, Mastery = 0,
            }, new System.Random(1));
            EnemyController enemy = MakeEnemy(0, defense: 100f);
            Debuff(MakeZone(MakeTower(1408), false, 60f), enemy);

            new EnemyTileEffectApplier(m_grid, layout).Apply(enemy);
            Assert.AreEqual(110f, enemy.Stats.Get(StatType.Defense), 0.001f);
        }
    }
}
