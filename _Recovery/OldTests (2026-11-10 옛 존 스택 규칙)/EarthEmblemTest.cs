using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 35 대지 (20번 4-11장) — 대지의 분노 · 씨뿌리기 · 성장→만개 · 진흙→석화
    public class EarthEmblemTest : EmblemTestBase
    {
        [Test]
        public void 등록표_대지()
        {
            Assert.IsInstanceOf<EarthSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Earth, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<EarthTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Earth, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<EarthBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Earth));
            Assert.IsInstanceOf<EarthDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Earth));
        }

        // ── 광역: 대지의 분노 ──

        [Test]
        public void 대지의_분노를_쓸_때마다_AbilityValue_1퍼센트_복리가_아님()
        {
            MakeTarget(0);
            TowerController tower = MakeTower(1109);
            var skill = new EarthSplashSkill(m_grid, m_occupancy, Auto());

            skill.Fire(tower, null);
            Assert.AreEqual(2.02f, tower.Stats.AbilityValue, 0.0001f);
            skill.Fire(tower, null);
            Assert.AreEqual(2.04f, tower.Stats.AbilityValue, 0.0001f, "2 × 1.02 × 1.02 = 2.0808이 아님");
        }

        [Test]
        public void 성장_존이면_1점5퍼센트_만개_존이면_2퍼센트()
        {
            MakeTarget(0);
            TowerController growth = MakeTower(1109, slot: 0), bloom = MakeTower(1109, slot: 1);
            Receive(MakeZone(MakeTower(1309, slot: 2), true, 30f), growth);
            Receive(MakeZone(MakeTower(1309, slot: 2), true, 60f), bloom);

            Assert.AreEqual(0.015f, EarthSplashSkill.GrowthPerUse(growth), 0.00001f);
            Assert.AreEqual(0.02f, EarthSplashSkill.GrowthPerUse(bloom), 0.00001f);
        }

        [Test]
        public void 진흙인_적에게는_방어력_곱하기_0점1퍼센트_더_세게()
        {
            EnemyController enemy = MakeEnemy(0, defense: 200f);
            Debuff(MakeZone(MakeTower(1409), false, 30f), enemy);           // 진흙: 방어 +20% → 240
            enemy.Stats.Modifiers.Add(StatModifier.Percent("방패벽", StatType.Defense, -0.5f));     // 깎은 것은 넣지 않음

            Assert.AreEqual(240f, EarthSplashSkill.RaisedDefense(enemy.Stats), 0.001f);
        }

        [Test]
        public void 대지의_분노의_성장은_티어_강화로_이어받음()
        {
            MakeTarget(0);
            TowerController tower = MakeTower(1109);
            new EarthSplashSkill(m_grid, m_occupancy, Auto()).Fire(tower, null);

            TowerController upgraded = MakeTower(2109);
            upgraded.State.CopyInheritedFrom(tower.State);
            Assert.AreEqual(2.02f, upgraded.Stats.AbilityValue, 0.0001f);
        }

        // ── 단일: 씨뿌리기 ──

        [Test]
        public void 씨뿌리기를_쓸_때마다_Power_1퍼센트()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            TowerController tower = MakeTower(1209);
            new EarthTargetSkill(m_grid, m_occupancy, Auto()).Fire(tower, target);
            Assert.AreEqual(10.1f, tower.Stats.Power, 0.0001f);
        }

        [Test]
        public void 진흙인_적은_최소_이동력_마이너스1_석화면_마이너스2()
        {
            EnemyController mud = MakeEnemy(1, speedMin: 3, speedMax: 5), stone = MakeEnemy(2, speedMin: 3, speedMax: 5);
            Debuff(MakeZone(MakeTower(1409), false, 30f), mud);
            Debuff(MakeZone(MakeTower(1409), false, 60f), stone);
            TowerController tower = MakeTower(1209);

            EarthTargetSkill.Bury(tower, mud);
            EarthTargetSkill.Bury(tower, stone);

            Assert.AreEqual(2, mud.Stats.GetMoveRange().Min);
            Assert.AreEqual(1, stone.Stats.GetMoveRange().Min);
        }

        [Test]
        public void 씨뿌리기도_진흙인_적을_파묻음()
        {
            EnemyController mud = MakeEnemy(1, hp: 1000f, speedMin: 3, speedMax: 5);
            Debuff(MakeZone(MakeTower(1409), false, 30f), mud);
            new EarthTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1209), mud);
            Assert.AreEqual(2, mud.Stats.GetMoveRange().Min);
        }

        [Test]
        public void 진흙이_아닌_적은_파묻지_않음()
        {
            EnemyController enemy = MakeEnemy(1, speedMin: 3, speedMax: 5);
            EarthTargetSkill.Bury(MakeTower(1209), enemy);
            Assert.AreEqual(3, enemy.Stats.GetMoveRange().Min);
        }

        // ── 버프: 성장 · 만개 ──

        [Test]
        public void 성장은_AbilityValue_퍼센트_만개는_컬러_레벨_플러스1()
        {
            List<StatModifier> growth = new EarthBuffRule().GetModifiers(MakeZone(MakeTower(1309), true, 30f), MakeTower(1101), "s");
            List<StatModifier> bloom = new EarthBuffRule().GetModifiers(MakeZone(MakeTower(1309), true, 60f), MakeTower(1101), "s");
            Assert.AreEqual(0.006f, Sum(growth, StatType.AbilityValue), 0.00001f);
            Assert.AreEqual(0f, Sum(growth, StatType.ColorLevelBonus));
            Assert.AreEqual(1f, Sum(bloom, StatType.ColorLevelBonus));
        }

        [Test]
        public void 만개_존에_서면_레벨이_하나_오름()
        {
            TowerController receiver = MakeTower(1101);
            Receive(MakeZone(MakeTower(1309, slot: 2), true, 60f), receiver);
            Assert.AreEqual(2, receiver.Stats.ColorLevel);
        }

        // ── 디버프: 진흙 · 석화 ──

        [Test]
        public void 진흙은_방어_20퍼센트_석화는_10퍼센트와_행동_주기_2()
        {
            EnemyController mud = MakeEnemy(0, defense: 100f), stone = MakeEnemy(1, defense: 100f);
            Debuff(MakeZone(MakeTower(1409), false, 30f), mud);
            Debuff(MakeZone(MakeTower(1409), false, 60f), stone);

            Assert.AreEqual(120f, mud.Stats.Get(StatType.Defense), 0.001f);
            Assert.AreEqual(110f, stone.Stats.Get(StatType.Defense), 0.001f);
            Assert.AreEqual(3, mud.Stats.GetMoveRange().Action);
            Assert.AreEqual(5, stone.Stats.GetMoveRange().Action);
        }

        [Test]
        public void 진흙의_방어_증가는_저항과_무관하고_속박_확률은_저항만큼()
        {
            EnemyController enemy = MakeEnemy(0, defense: 100f, resistance: 50f);
            Debuff(MakeZone(MakeTower(1409), false, 60f), enemy);
            Assert.AreEqual(110f, enemy.Stats.Get(StatType.Defense), 0.001f);
            Assert.AreEqual(0.006f, enemy.Status.Find(EmblemDebuffEffect.KeyOf(TowerEmblem.Earth)).Strength, 0.00001f, "1.2% × (1 − 50%)");
        }

        [Test]
        public void 속박_확률이_100퍼센트면_턴마다_속박()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1409, power: 1000f), false, 60f), enemy);     // 0.001 × 1000 × 2 × 60 = 120%
            enemy.Status.OnTurn();
            Assert.AreEqual(0, enemy.Status.DecideMove(enemy.Stats.GetMoveRange()).Max);
        }
    }
}
