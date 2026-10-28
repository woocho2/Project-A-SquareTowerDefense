using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 33 불 (20번 4-9장) — 메테오 · 화염구 · 불씨→불길 · 화상→연소
    public class FireEmblemTest : EmblemTestBase
    {
        private Zone BurnZone(float stack) => MakeZone(MakeTower(1407), false, stack);

        [Test]
        public void 등록표_불()
        {
            Assert.IsInstanceOf<FireSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Fire, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<FireTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Fire, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<FireBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Fire));
            Assert.IsInstanceOf<FireDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Fire));
        }

        // ── 광역: 메테오 ──

        [Test]
        public void 메테오는_맵_전체_화상_50퍼센트_연소_100퍼센트_더()
        {
            EnemyController plain = MakeEnemy(0, hp: 1000f), burnt = MakeEnemy(2, hp: 1000f), blazing = MakeEnemy(4, hp: 1000f);
            Debuff(BurnZone(30f), burnt);
            Debuff(BurnZone(60f), blazing);

            new FireSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1107), plain);

            Assert.AreEqual(20f, Lost(plain), 0.001f);
            Assert.AreEqual(30f, Lost(burnt), 0.001f);
            Assert.AreEqual(40f, Lost(blazing), 0.001f);
        }

        // ── 단일: 화염구 ──

        [Test]
        public void 화염구는_화상_10퍼센트_연소_20퍼센트_더()
        {
            EnemyController burnt = MakeEnemy(1, hp: 1000f, resistance: 100f), blazing = MakeEnemy(2, hp: 1000f, resistance: 100f);
            Debuff(BurnZone(30f), burnt);
            Debuff(BurnZone(60f), blazing);
            var skill = new FireTargetSkill(m_grid, m_occupancy, Auto());
            TowerController tower = MakeTower(1207, skill: skill);

            skill.Fire(tower, burnt);
            skill.Fire(tower, blazing);

            Assert.AreEqual(22f, Lost(burnt), 0.001f);
            Assert.AreEqual(24f, Lost(blazing), 0.001f);
        }

        [Test]
        public void 불_단일의_기본_공격도_화상인_적에게_더_셈()
        {
            EnemyController burnt = MakeEnemy(1);
            Debuff(BurnZone(30f), burnt);
            var skill = new FireTargetSkill(m_grid, m_occupancy, Auto());
            Assert.AreEqual(1.1f, skill.ModifyBasicDamage(MakeTower(1207, skill: skill), burnt), 0.0001f);
        }

        [Test]
        public void 화염구의_화상은_스택_1_고정이고_레벨_곱하기_2_행동()
        {
            FireDebuffEffect burn = FireDebuffEffect.Fireball(MakeTower(1207));
            Assert.AreEqual(0.0002f, burn.Strength, 0.000001f, "0.001 × 10 × 2 × 1 = 0.02%");
            Assert.AreEqual(2, burn.RemainingActions);
            Assert.AreEqual(ZoneStage.Skill1, burn.Stage, "화상");
        }

        [Test]
        public void 화염구는_저항_100이면_화상을_걸지_못함()
        {
            EnemyController enemy = MakeEnemy(1, resistance: 100f);
            new FireTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1207), enemy);
            Assert.AreEqual(ZoneStage.None, EmblemDebuffEffect.StageOn(enemy, TowerEmblem.Fire));
        }

        // ── 버프: 불씨 · 불길 ──

        [Test]
        public void 불씨는_공격력_불길은_치명_10_치명피해_30_공격횟수_1도()
        {
            TowerController receiver = MakeTower(1101);
            List<StatModifier> ember = new FireBuffRule().GetModifiers(MakeZone(MakeTower(1307), true, 30f), receiver, "s");
            List<StatModifier> blaze = new FireBuffRule().GetModifiers(MakeZone(MakeTower(1307), true, 60f), receiver, "s");

            Assert.AreEqual(0.006f, Sum(ember, StatType.Power), 0.00001f);
            Assert.AreEqual(0f, Sum(ember, StatType.CritRate));
            Assert.AreEqual(0.012f, Sum(blaze, StatType.Power), 0.00001f);
            Assert.AreEqual(0.1f, Sum(blaze, StatType.CritRate), 0.00001f);
            Assert.AreEqual(0.3f, Sum(blaze, StatType.CritDamage), 0.00001f);
            Assert.AreEqual(1f, Sum(blaze, StatType.AttackCount));
        }

        [Test]
        public void 불_단일은_공격_횟수_불씨_1_불길_2()
        {
            TowerController fireTarget = MakeTower(1207);
            Assert.AreEqual(1f, Sum(new FireBuffRule().GetModifiers(MakeZone(MakeTower(1307), true, 30f), fireTarget, "s"), StatType.AttackCount));
            Assert.AreEqual(2f, Sum(new FireBuffRule().GetModifiers(MakeZone(MakeTower(1307), true, 60f), fireTarget, "s"), StatType.AttackCount), "불길의 +1 포함");
        }

        [Test]
        public void 불_광역은_불씨_불길_모두_StackValue_20퍼센트()
        {
            TowerController fireSplash = MakeTower(1107);
            Assert.AreEqual(0.2f, Sum(new FireBuffRule().GetModifiers(MakeZone(MakeTower(1307), true, 30f), fireSplash, "s"), StatType.StackValue), 0.00001f);
            Assert.AreEqual(0.2f, Sum(new FireBuffRule().GetModifiers(MakeZone(MakeTower(1307), true, 60f), fireSplash, "s"), StatType.StackValue), 0.00001f);
        }

        [Test]
        public void 불길은_레벨_곱하기_5턴_뒤_0스택_불씨로()
        {
            Zone zone = MakeZone(MakeTower(1307), true, 60f);
            var rule = new FireBuffRule();

            Assert.IsFalse(rule.OnZoneTurn(zone), "켜진 다음 턴: 시간을 재기 시작");
            for (int turn = 1; turn < 5; turn++) Assert.IsFalse(rule.OnZoneTurn(zone), $"{turn}턴째");
            Assert.IsTrue(rule.OnZoneTurn(zone), "5턴째에 끝");

            Assert.AreEqual(ZoneStage.Skill1, zone.Stage);
            Assert.AreEqual(0f, zone.Stack);
        }

        [Test]
        public void 불씨는_돌지_않음()
        {
            Zone zone = MakeZone(MakeTower(1307), true, 30f);
            for (int turn = 0; turn < 20; turn++) Assert.IsFalse(new FireBuffRule().OnZoneTurn(zone));
        }

        // ── 디버프: 화상 · 연소 ──

        [Test]
        public void 화상은_매_턴_최대_체력_퍼센트_피해와_같은_값의_치유_감소()
        {
            EnemyController enemy = MakeEnemy(0, defense: 100f);
            Debuff(BurnZone(60f), enemy);

            enemy.Status.OnTurn();

            Assert.AreEqual(1.2f, Lost(enemy), 0.0001f, "방어 무시");
            Assert.AreEqual(1f - 0.012f, enemy.Stats.Get(StatType.HealingTaken), 0.00001f);
        }

        [Test]
        public void 화상은_저항력만큼_줄어듦()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 50f);
            Debuff(BurnZone(60f), enemy);
            enemy.Status.OnTurn();
            Assert.AreEqual(0.6f, Lost(enemy), 0.0001f);
        }

        [Test]
        public void 연소는_같은_칸의_적에게_화상을_옮기고_칸이_갈리면_꺼짐()
        {
            EnemyController blazing = MakeEnemy(0), neighbor = MakeEnemy(0), far = MakeEnemy(2);
            blazing.Status.Add(new FireDebuffRule(() => m_occupancy).CreateEffect(BurnZone(60f), blazing));     // 게임 밖이라 점유를 직접 넘김

            blazing.Status.OnTurn();
            Assert.AreEqual(ZoneStage.Skill1, EmblemDebuffEffect.StageOn(neighbor, TowerEmblem.Fire), "옮겨붙은 화상");
            Assert.AreEqual(ZoneStage.None, EmblemDebuffEffect.StageOn(far, TowerEmblem.Fire));

            MoveEnemyTo(neighbor, 1);
            neighbor.Status.OnTurn();
            Assert.AreEqual(ZoneStage.None, EmblemDebuffEffect.StageOn(neighbor, TowerEmblem.Fire), "칸이 갈림");
        }

        [Test]
        public void 연소가_더_세면_화상을_밀어냄()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(BurnZone(30f), enemy);
            Debuff(BurnZone(60f), enemy);
            Assert.AreEqual(ZoneStage.Skill2, EmblemDebuffEffect.StageOn(enemy, TowerEmblem.Fire));
        }
    }
}
