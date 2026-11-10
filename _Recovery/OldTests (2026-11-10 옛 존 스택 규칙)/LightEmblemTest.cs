using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 39 빛 (20번 4-15장) — 프리즘 · 도깨비불 · 광채→광휘 · 잔영→잔상(더미)
    public class LightEmblemTest : EmblemTestBase
    {
        private LightDebuffRule DummyRule() => new LightDebuffRule(() => m_occupancy, _ => Vector3.zero);     // 게임 밖이라 점유를 직접 넘김

        [TearDown]
        public void ClearDummies()
        {
            foreach (EnemyController enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                DummyTarget.Find(enemy)?.Despawn();
        }

        [Test]
        public void 등록표_빛()
        {
            Assert.IsInstanceOf<LightSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Light, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<LightTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Light, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<LightBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Light));
            Assert.IsInstanceOf<LightDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Light));
        }

        // ── 광역: 프리즘 ──

        [Test]
        public void 프리즘은_기본_2스택_다시_걸면_더함()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            var skill = new LightSplashSkill(m_grid, m_occupancy, Auto());
            skill.Fire(MakeTower(1113), enemy);
            Assert.AreEqual(2, PrismEffect.GetStacks(enemy));
            skill.Fire(MakeTower(1113), enemy);
            Assert.AreEqual(4, PrismEffect.GetStacks(enemy));
        }

        [Test]
        public void 광채_존이면_3스택_광휘_존이면_4스택()
        {
            TowerController radiance = MakeTower(1113, slot: 0), brilliance = MakeTower(1113, slot: 1);
            Receive(MakeZone(MakeTower(1313, slot: 2), true, 30f), radiance);
            Receive(MakeZone(MakeTower(1313, slot: 2), true, 60f), brilliance);
            Assert.AreEqual(3, LightSplashSkill.GetPrismStacks(radiance));
            Assert.AreEqual(4, LightSplashSkill.GetPrismStacks(brilliance));
        }

        [Test]
        public void 프리즘은_기본_공격에_터져_최대_체력_5퍼센트_곱하기_스택()
        {
            EnemyController enemy = MakeEnemy(0);
            PrismEffect.Apply(enemy, 4);
            enemy.TakeDamage(new DamageInfo { Source = DamageSource.Basic, BaseDamage = 10f });
            Assert.AreEqual(10f + 20f, Lost(enemy), 0.001f, "광휘 4스택 → 20%");
            Assert.AreEqual(0, PrismEffect.GetStacks(enemy), "터지면 사라짐");
        }

        [Test]
        public void 프리즘은_스킬이나_지속_피해로는_안_터짐()
        {
            EnemyController enemy = MakeEnemy(0);
            PrismEffect.Apply(enemy, 2);
            enemy.TakeDamage(new DamageInfo { Source = DamageSource.Skill, BaseDamage = 10f });
            enemy.TakeDamage(new DamageInfo { Source = DamageSource.Dot, BaseDamage = 10f });
            Assert.AreEqual(20f, Lost(enemy), 0.001f);
            Assert.AreEqual(2, PrismEffect.GetStacks(enemy));
        }

        [Test]
        public void 프리즘_폭발은_저항력만큼_줄어듦()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 50f);
            PrismEffect.Apply(enemy, 2);
            enemy.TakeDamage(new DamageInfo { Source = DamageSource.Basic, BaseDamage = 10f });
            Assert.AreEqual(10f + 5f, Lost(enemy), 0.001f);
        }

        [Test]
        public void 프리즘은_적이_행동할_때마다_1씩_줄어_사라짐()
        {
            EnemyController enemy = MakeEnemy(0);
            PrismEffect.Apply(enemy, 2);
            enemy.Status.OnActed();
            Assert.AreEqual(1, PrismEffect.GetStacks(enemy));
            enemy.Status.OnActed();
            Assert.IsFalse(enemy.Status.Has(PrismEffect.PrismKey));
        }

        // ── 단일: 도깨비불 ──

        [Test]
        public void 도깨비불은_맞을_때마다_2퍼센트씩_32발째는_64퍼센트()
        {
            EnemyController enemy = MakeEnemy(1);
            var skill = new LightTargetSkill(m_grid, m_occupancy, Auto());
            TowerController tower = MakeTower(1213, skill: skill);

            float last = 0f;
            for (int shot = 0; shot < 32; shot++) last = skill.ModifyBasicDamage(tower, enemy);
            Assert.AreEqual(1.64f, last, 0.0001f);
        }

        [Test]
        public void 도깨비불_스킬도_스택이_붙어_피해()
        {
            EnemyController enemy = MakeEnemy(1, hp: 1000f);
            new LightTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1213), enemy);
            Assert.AreEqual(20f * 1.02f, Lost(enemy), 0.001f, "스킬도 맞을 때 한 번 쌓임");
        }

        [Test]
        public void 도깨비불은_적마다_따로_세고_다음_턴에_비움()
        {
            EnemyController a = MakeEnemy(1), b = MakeEnemy(2);
            var skill = new LightTargetSkill(m_grid, m_occupancy, Auto());
            TowerController tower = MakeTower(1213, skill: skill);

            skill.ModifyBasicDamage(tower, a);
            skill.ModifyBasicDamage(tower, a);
            Assert.AreEqual(1.02f, skill.ModifyBasicDamage(tower, b), 0.0001f);

            tower.OnEnemyTurnStart();
            Assert.AreEqual(0, skill.GetMarks(a));
        }

        [Test]
        public void 더미에게는_도깨비불_스택당_4퍼센트()
        {
            EnemyController body = MakeEnemy(1, hp: 1000f);
            body.Status.Add(DummyRule().CreateEffect(MakeZone(MakeTower(1413), false, 60f), body));
            DummyTarget dummy = DummyTarget.Find(body);

            var skill = new LightTargetSkill(m_grid, m_occupancy, Auto());
            Assert.AreEqual(1.04f, skill.ModifyBasicDamage(MakeTower(1213, skill: skill), dummy), 0.0001f);
        }

        // ── 버프: 광채 · 광휘 ──

        [Test]
        public void 빛_단일은_존에_서_있는_동안_공격_횟수_1_2()
        {
            var rule = new LightBuffRule();
            Assert.AreEqual(1f, Sum(rule.GetModifiers(MakeZone(MakeTower(1313), true, 30f), MakeTower(1213), "s"), StatType.AttackCount));
            Assert.AreEqual(2f, Sum(rule.GetModifiers(MakeZone(MakeTower(1313), true, 60f), MakeTower(1213), "s"), StatType.AttackCount));
            Assert.AreEqual(0f, Sum(rule.GetModifiers(MakeZone(MakeTower(1313), true, 60f), MakeTower(1101), "s"), StatType.AttackCount));
        }

        [Test]
        public void 광휘가_터지면_모든_스탯과_AbilityValue_20퍼센트()
        {
            TowerController receiver = MakeTower(1101);
            Receive(MakeZone(MakeTower(1313, slot: 2, power: 1000f), true, 60f), receiver);     // 확률 120%라 반드시

            TowerFinalStats stats = receiver.Stats;
            Assert.AreEqual(11f, stats.Power, 0.0001f);
            Assert.AreEqual(2, stats.AttackCount);
            Assert.AreEqual(2, stats.Mastery);
            Assert.AreEqual(0.05f, stats.CriticalRate, 0.0001f);
            Assert.AreEqual(0.1f, stats.CriticalDamage, 0.0001f);
            Assert.AreEqual(2, stats.Action, "3 − 1");
            Assert.AreEqual(2.4f, stats.AbilityValue, 0.0001f);
            Assert.AreEqual(2, TimedTowerBuff.GetRemaining(receiver.State, LightBuffRule.BurstSource));
        }

        [Test]
        public void 존을_벗어나면_광채_광휘도_바로_끝남()
        {
            TowerController receiver = MakeTower(1101);
            Zone zone = MakeZone(MakeTower(1313, slot: 2, power: 1000f), true, 60f);     // 확률 120%라 반드시
            Receive(zone, receiver);
            Assert.AreEqual(11f, receiver.Stats.Power, 0.0001f);

            BuffZoneAction.TakeBuff(zone, receiver);
            Assert.AreEqual(10f, receiver.Stats.Power, 0.0001f);
            Assert.AreEqual(0, TimedTowerBuff.GetRemaining(receiver.State, LightBuffRule.BurstSource));
        }

        // ── 디버프: 잔영 · 잔상 (더미) ──

        [Test]
        public void 잔상_더미는_본체_최대_체력의_식의_퍼센트이고_받은_피해의_4배를_본체에게()
        {
            EnemyController body = MakeEnemy(1, hp: 1000f);
            body.Status.Add(DummyRule().CreateEffect(MakeZone(MakeTower(1413), false, 60f), body));
            DummyTarget dummy = DummyTarget.Find(body);

            Assert.AreEqual(12f, dummy.MaxHp, 0.001f, "1000 × 1.2%");
            Assert.IsTrue(m_occupancy.GetAt(1).Contains(dummy), "타워의 타겟이 됨");

            dummy.TakeDamage(new DamageInfo { BaseDamage = 5f });
            Assert.AreEqual(20f, Lost(body), 0.001f, "5 × 4");
        }

        [Test]
        public void 잔영은_2배이고_본체가_받는_피해는_저항만큼_줄어듦()
        {
            EnemyController body = MakeEnemy(1, hp: 1000f, resistance: 50f);
            body.Status.Add(DummyRule().CreateEffect(MakeZone(MakeTower(1413), false, 30f), body));
            DummyTarget.Find(body).TakeDamage(new DamageInfo { BaseDamage = 5f });
            Assert.AreEqual(5f, Lost(body), 0.001f, "5 × 2 × (1 − 50%)");
        }

        [Test]
        public void 다시_밟아도_더미는_하나이고_체력이_그대로()
        {
            EnemyController body = MakeEnemy(1, hp: 1000f);
            Zone zone = MakeZone(MakeTower(1413), false, 60f);
            body.Status.Add(DummyRule().CreateEffect(zone, body));
            DummyTarget dummy = DummyTarget.Find(body);
            dummy.TakeDamage(new DamageInfo { BaseDamage = 5f });

            body.Status.Add(DummyRule().CreateEffect(zone, body));
            Assert.AreSame(dummy, DummyTarget.Find(body));
            Assert.AreEqual(7f, dummy.CurrentHp, 0.001f);
        }

        [Test]
        public void 정화되면_더미도_사라짐()
        {
            EnemyController body = MakeEnemy(1, hp: 1000f);
            body.Status.Add(DummyRule().CreateEffect(MakeZone(MakeTower(1413), false, 60f), body));
            DummyTarget dummy = DummyTarget.Find(body);

            body.Status.RemoveAll();
            Assert.IsNull(DummyTarget.Find(body));
            Assert.IsFalse(m_occupancy.GetAt(1).Contains(dummy));
        }

        [Test]
        public void 더미에_남은_프리즘은_본체_최대_체력의_10퍼센트_곱하기_스택()
        {
            EnemyController body = MakeEnemy(1, hp: 1000f);
            body.Status.Add(DummyRule().CreateEffect(MakeZone(MakeTower(1413, power: 1000f), false, 60f), body));     // 더미 체력 넉넉히
            DummyTarget dummy = DummyTarget.Find(body);
            dummy.AddPrism(2);

            dummy.TakeDamage(new DamageInfo { Source = DamageSource.Basic, BaseDamage = 1f });
            Assert.AreEqual((1f + 200f) * 4f, Lost(body), 0.01f, "(1 + 1000 × 20%) × 4");
        }
    }
}
