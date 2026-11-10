using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 37 얼음 (20번 4-13장) — 블리자드 · 스노우볼 · 냉기→한기 · 동상→저체온증 · 빙결과 해동
    public class IceEmblemTest : EmblemTestBase
    {
        [Test]
        public void 등록표_얼음()
        {
            Assert.IsInstanceOf<IceSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Ice, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<IceTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Ice, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<IceBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Ice));
            Assert.IsInstanceOf<IceDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Ice));
        }

        // ── 광역: 블리자드 ──

        [Test]
        public void 블리자드는_동상_저체온증인_적만_빙결()
        {
            EnemyController frostbite = MakeEnemy(0), plain = MakeEnemy(2);
            Debuff(MakeZone(MakeTower(1411), false, 30f), frostbite);

            new IceSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1111), null);

            Assert.IsTrue(FreezeEffect.IsFrozen(frostbite));
            Assert.IsFalse(FreezeEffect.IsFrozen(plain));
            Assert.AreEqual(20f, Lost(plain), 0.001f);
        }

        [Test]
        public void 빙결은_다음_행동_0_0()
        {
            EnemyController enemy = MakeEnemy(0);
            enemy.Status.Add(new FreezeEffect());
            Assert.AreEqual(0, enemy.Status.DecideMove(enemy.Stats.GetMoveRange()).Max);
        }

        [Test]
        public void 빙결_중에_받은_피해의_25퍼센트가_풀릴_때_들어감()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            enemy.Status.Add(new FreezeEffect());
            enemy.TakeDamage(new DamageInfo { BaseDamage = 40f, ForceFixed = true });

            enemy.Status.OnActed();                 // 행동 불가 턴이 지나며 풀림
            Assert.AreEqual(50f, Lost(enemy), 0.001f, "40 + 40 × 25%");
            Assert.IsFalse(FreezeEffect.IsFrozen(enemy));
        }

        [Test]
        public void 이미_얼어_있으면_다시_빙결되지_않음()
        {
            EnemyController enemy = MakeEnemy(0);
            Assert.IsTrue(enemy.Status.Add(new FreezeEffect()));
            Assert.IsFalse(enemy.Status.Add(new FreezeEffect()), "같은 행동 불가 키");
            Assert.IsFalse(enemy.Status.Add(new ImmobilizeEffect("기절")), "기절·속박과도 겹치지 않음");
        }

        // ── 단일: 스노우볼 ──

        [Test]
        public void 저체온증인_적은_감소된_이동력_2라_1점5배()
        {
            EnemyController enemy = MakeEnemy(2, hp: 1000f, speedMin: 3, speedMax: 5);
            Debuff(MakeZone(MakeTower(1411), false, 60f), enemy);
            Assert.AreEqual(2, IceTargetSkill.ReducedSpeed(enemy.Stats));

            new IceTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1211), enemy);
            Assert.AreEqual(30f, Lost(enemy), 0.001f);
        }

        [Test]
        public void 한기까지_걸리면_감소된_이동력_4라_2배()
        {
            EnemyController enemy = MakeEnemy(2, hp: 1000f, speedMin: 3, speedMax: 5);
            Debuff(MakeZone(MakeTower(1411), false, 60f), enemy);
            new IceBuffRule().OnReceiverHit(MakeZone(MakeTower(1311, power: 1000f), true, 60f), MakeTower(1101), enemy);

            Assert.AreEqual(4, IceTargetSkill.ReducedSpeed(enemy.Stats));
            new IceTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1211), enemy);
            Assert.AreEqual(40f, Lost(enemy), 0.001f);
        }

        [Test]
        public void 원본보다_늘어난_이동력은_세지_않음()
        {
            EnemyController enemy = MakeEnemy(2, speedMin: 3, speedMax: 5);
            enemy.Stats.Modifiers.Add(StatModifier.Flat("이동력 타일", StatType.SpeedMax, 2f));
            enemy.Stats.Modifiers.Add(StatModifier.Flat("시험", StatType.SpeedMin, -1f));
            Assert.AreEqual(1, IceTargetSkill.ReducedSpeed(enemy.Stats));
        }

        // ── 버프: 냉기 · 한기 ──

        [Test]
        public void 얼음_타워는_Mastery_냉기_1_한기_2_다른_타워는_없음()
        {
            var rule = new IceBuffRule();
            Assert.AreEqual(1f, Sum(rule.GetModifiers(MakeZone(MakeTower(1311), true, 30f), MakeTower(1111), "s"), StatType.Mastery));
            Assert.AreEqual(2f, Sum(rule.GetModifiers(MakeZone(MakeTower(1311), true, 60f), MakeTower(1211), "s"), StatType.Mastery));
            Assert.AreEqual(0f, Sum(rule.GetModifiers(MakeZone(MakeTower(1311), true, 60f), MakeTower(1101), "s"), StatType.Mastery));
        }

        [Test]
        public void 냉기에_맞은_적은_최대_이동력_마이너스1()
        {
            EnemyController enemy = MakeEnemy(0, speedMin: 3, speedMax: 5);
            new IceBuffRule().OnReceiverHit(MakeZone(MakeTower(1311, power: 2000f), true, 30f), MakeTower(1101), enemy);
            MoveRange range = enemy.Stats.GetMoveRange();
            Assert.AreEqual(3, range.Min);
            Assert.AreEqual(4, range.Max);
        }

        [Test]
        public void 한기_존의_타워는_빙결된_적에게_25퍼센트_더()
        {
            EnemyController enemy = MakeEnemy(0);
            enemy.Status.Add(new FreezeEffect());
            TowerController cold = MakeTower(1101);
            Receive(MakeZone(MakeTower(1311, slot: 2), true, 60f), cold);

            Assert.AreEqual(125f, enemy.PreviewDamage(new DamageInfo { Attacker = cold, BaseDamage = 100f }), 0.001f);
            Assert.AreEqual(100f, enemy.PreviewDamage(new DamageInfo { Attacker = MakeTower(1102, slot: 1), BaseDamage = 100f }), 0.001f);
        }

        // ── 디버프: 동상 · 저체온증 ──

        [Test]
        public void 저체온증은_최소_최대_이동력_마이너스1_저항과_무관()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 50f, speedMin: 3, speedMax: 5);
            Debuff(MakeZone(MakeTower(1411), false, 60f), enemy);
            MoveRange range = enemy.Stats.GetMoveRange();
            Assert.AreEqual(2, range.Min);
            Assert.AreEqual(4, range.Max);
        }

        [Test]
        public void 동상_확률이_100퍼센트면_턴마다_빙결()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1411, power: 1000f), false, 60f), enemy);
            enemy.Status.OnTurn();
            Assert.IsTrue(FreezeEffect.IsFrozen(enemy));
        }
    }
}
