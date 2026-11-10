using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 36 바람 (20번 4-12장) — 칼바람 · 폭풍 · 순풍→질풍 · 역풍→난기류
    public class WindEmblemTest : EmblemTestBase
    {
        private Zone StrongZone(bool isBuff, float stack) => MakeZone(MakeTower(isBuff ? 1310 : 1410, power: 1000f), isBuff, stack);     // 확률 100% 넘음

        [Test]
        public void 등록표_바람()
        {
            Assert.IsInstanceOf<WindSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Wind, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<WindTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Wind, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<WindBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Wind));
            Assert.IsInstanceOf<WindDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Wind));
        }

        // ── 광역: 칼바람 ──

        [Test]
        public void 칼바람은_반드시_치명이고_범위의_적_수로_나눔()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(0);
            new WindSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1110), a);
            Assert.IsTrue(a.Hits[0].IsCritical);
            Assert.AreEqual(20f * 2f / 2f, a.Hits[0].BaseDamage, 0.001f, "20 × 치명 2 ÷ 2마리");
            Assert.AreEqual(20f, b.Hits[0].BaseDamage, 0.001f);
        }

        [Test]
        public void 칼바람은_혼자면_전부()
        {
            OccupyingFakeTarget a = MakeTarget(0);
            new WindSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1110), a);
            Assert.AreEqual(40f, a.Hits[0].BaseDamage, 0.001f);
        }

        [Test]
        public void 칼바람은_역풍_2배_난기류_3배()
        {
            EnemyController headwind = MakeEnemy(0, hp: 1000f), turbulence = MakeEnemy(2, hp: 1000f);
            Debuff(MakeZone(MakeTower(1410), false, 30f), headwind);
            Debuff(MakeZone(MakeTower(1410), false, 60f), turbulence);
            var skill = new WindSplashSkill(m_grid, m_occupancy, Auto());

            skill.Fire(MakeTower(1110), headwind);
            skill.Fire(MakeTower(1110), turbulence);

            Assert.AreEqual(80f, Lost(headwind), 0.001f);
            Assert.AreEqual(120f, Lost(turbulence), 0.001f);
        }

        [Test]
        public void 역풍_2마리와_난기류_1마리를_맞히면_4를_당김()
        {
            var skill = new WindSplashSkill(m_grid, m_occupancy, Auto());
            TowerController tower = MakeTower(1110, skill: skill);
            EnemyController a = MakeEnemy(0), b = MakeEnemy(0), c = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1410), false, 30f), a);
            Debuff(MakeZone(MakeTower(1410), false, 30f), b);
            Debuff(MakeZone(MakeTower(1410), false, 60f), c);

            tower.OnBasicHitLanded(a);
            tower.OnBasicHitLanded(b);
            tower.OnBasicHitLanded(c);
            Assert.AreEqual(4, tower.State.SkillHitCount, "Duration 5 전이라 줄은 아직");
        }

        // ── 단일: 폭풍 ──

        [Test]
        public void 폭풍_확률은_브론즈_20퍼센트_스택을_쓰면_32퍼센트()
        {
            TowerController tower = MakeTower(1210);
            Assert.AreEqual(0.2f, WindTargetSkill.GetChance(tower, false), 0.0001f);
            Assert.AreEqual(0.32f, WindTargetSkill.GetChance(tower, true), 0.0001f);
        }

        [Test]
        public void 폭풍이_터지면_행동_카운트_2_공격_횟수_1()
        {
            TowerController tower = MakeTower(1210, power: 50f);        // 50 × 2 = 100%
            new WindTargetSkill(m_grid, m_occupancy, Auto()).Fire(tower, null);
            Assert.AreEqual(2f, tower.Stats.Get(StatType.ActionGainPerTurn), 0.0001f);
            Assert.AreEqual(2, tower.Stats.AttackCount);
            Assert.AreEqual(2, TimedTowerBuff.GetRemaining(tower.State, WindTargetSkill.Source), "레벨 1 × 2 행동");
        }

        // ── 버프: 순풍 · 질풍 ──

        [Test]
        public void 질풍_존은_최대_Action_마이너스1()
        {
            Assert.AreEqual(0f, Sum(new WindBuffRule().GetModifiers(MakeZone(MakeTower(1310), true, 30f), MakeTower(1101), "s"), StatType.MaxAction));
            Assert.AreEqual(-1f, Sum(new WindBuffRule().GetModifiers(MakeZone(MakeTower(1310), true, 60f), MakeTower(1101), "s"), StatType.MaxAction));
        }

        [Test]
        public void 바람_광역은_치명_피해_바람_단일은_공격_횟수()
        {
            var rule = new WindBuffRule();
            Assert.AreEqual(0.15f, Sum(rule.GetModifiers(MakeZone(MakeTower(1310), true, 30f), MakeTower(1110), "s"), StatType.CritDamage), 0.0001f);
            Assert.AreEqual(0.3f, Sum(rule.GetModifiers(MakeZone(MakeTower(1310), true, 60f), MakeTower(1110), "s"), StatType.CritDamage), 0.0001f);
            Assert.AreEqual(1f, Sum(rule.GetModifiers(MakeZone(MakeTower(1310), true, 30f), MakeTower(1210), "s"), StatType.AttackCount));
            Assert.AreEqual(2f, Sum(rule.GetModifiers(MakeZone(MakeTower(1310), true, 60f), MakeTower(1210), "s"), StatType.AttackCount));
        }

        [Test]
        public void 질풍이_터지면_행동_카운트_2_공격_횟수_2_존을_벗어나면_끝남()
        {
            TowerController receiver = MakeTower(1101);
            Zone zone = StrongZone(true, 60f);
            Receive(zone, receiver);

            Assert.AreEqual(2f, receiver.Stats.Get(StatType.ActionGainPerTurn), 0.0001f);
            Assert.AreEqual(3, receiver.Stats.AttackCount);

            BuffZoneAction.TakeBuff(zone, receiver);
            Assert.AreEqual(0f, receiver.Stats.Get(StatType.ActionGainPerTurn), 0.0001f);
            Assert.AreEqual(1, receiver.Stats.AttackCount);
        }

        [Test]
        public void 질풍_존의_바람_단일이_질풍과_폭풍을_다_터뜨리면_공격_횟수_플러스5()
        {
            TowerController tower = MakeTower(1210, power: 50f);
            Receive(StrongZone(true, 60f), tower);
            new WindTargetSkill(m_grid, m_occupancy, Auto()).Fire(tower, null);
            Assert.AreEqual(1 + 5, tower.Stats.AttackCount);
        }

        // ── 디버프: 역풍 · 난기류 ──

        [Test]
        public void 역풍이_걸리면_이동력_2_5인_적은_마이너스5_5()
        {
            EnemyController enemy = MakeEnemy(0, speedMin: 2, speedMax: 5);
            Debuff(StrongZone(false, 60f), enemy);          // 확률 120%라 반드시
            MoveRange range = enemy.Status.DecideMove(enemy.Stats.GetMoveRange());
            Assert.AreEqual(-5, range.Min);
            Assert.AreEqual(5, range.Max);
        }

        [Test]
        public void 난기류는_이동한_칸_곱하기_4퍼센트만큼_현재_체력()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1410), false, 60f), enemy);
            enemy.Status.OnMoved(-3);
            Assert.AreEqual(12f, Lost(enemy), 0.001f, "뒤로 3칸도 12%");
        }

        [Test]
        public void 바람_단일은_역풍_10퍼센트_난기류_20퍼센트_치명_확률()
        {
            EnemyController headwind = MakeEnemy(0), turbulence = MakeEnemy(1);
            Debuff(MakeZone(MakeTower(1410), false, 30f), headwind);
            Debuff(MakeZone(MakeTower(1410), false, 60f), turbulence);
            Assert.AreEqual(0.1f, headwind.Status.GetCritRateBonus(MakeTower(1210)), 0.0001f);
            Assert.AreEqual(0.2f, turbulence.Status.GetCritRateBonus(MakeTower(1210)), 0.0001f);
            Assert.AreEqual(0f, turbulence.Status.GetCritRateBonus(MakeTower(1110)), "바람 광역은 없음");
        }
    }
}
