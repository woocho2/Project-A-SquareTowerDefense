using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 32 해머 (20번 4-8장) — 100T 해머 · 망치질 · 제련→세공 · 원석→금맥
    public class HammerEmblemTest : EmblemTestBase
    {
        [Test]
        public void 등록표_해머()
        {
            Assert.IsInstanceOf<HammerSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Hammer, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<HammerTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Hammer, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<HammerBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Hammer));
            Assert.IsInstanceOf<HammerDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Hammer));
        }

        // ── 광역: 100T 해머 ──

        [Test]
        public void 백T_해머는_착탄_범위에_피해와_최대_이동력_마이너스1()
        {
            EnemyController enemy = MakeEnemy(0);
            new HammerSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1106), enemy);

            Assert.AreEqual(20f, Lost(enemy), 0.001f);
            Assert.AreEqual(3, enemy.Stats.GetMoveRange().Max, "4 − 1");
            Assert.AreEqual(2, enemy.Status.Find(HammerSplashSkill.Key).RemainingActions, "레벨 1 × 2 행동");
        }

        [Test]
        public void 백T_해머는_이동력을_바꾼_공격()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            new HammerSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1106), target);
            Assert.IsTrue(target.Hits[0].ChangesMovement);
        }

        [Test]
        public void 백T_해머의_감소는_저항할_수_없음()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 100f);
            new HammerSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1106), enemy);
            Assert.AreEqual(3, enemy.Stats.GetMoveRange().Max);
        }

        // ── 단일: 망치질 ──

        [Test]
        public void 망치질_고정은_1_1()
        {
            EnemyController enemy = MakeEnemy(0, speedMin: 3, speedMax: 6);
            enemy.Status.Add(new SpeedLockEffect(2));
            MoveRange range = enemy.Status.DecideMove(enemy.Stats.GetMoveRange());
            Assert.AreEqual(1, range.Min);
            Assert.AreEqual(1, range.Max);
        }

        [Test]
        public void 망치질의_1_1이_백T_해머보다_우선()
        {
            EnemyController enemy = MakeEnemy(0, speedMin: 3, speedMax: 6);
            HammerSplashSkill.ApplySlow(MakeTower(1106), enemy);
            enemy.Status.Add(new SpeedLockEffect(2));
            MoveRange range = enemy.Status.DecideMove(enemy.Stats.GetMoveRange());
            Assert.AreEqual(1, range.Max);
        }

        [Test]
        public void 망치질_고정은_행동_불가를_풀지_않음()
        {
            EnemyController enemy = MakeEnemy(0);
            enemy.Status.Add(new SpeedLockEffect(2));
            enemy.Status.Add(new ImmobilizeEffect("기절"));
            MoveRange range = enemy.Status.DecideMove(enemy.Stats.GetMoveRange());
            Assert.AreEqual(0, range.Max);
        }

        [Test]
        public void 망치질은_저항_100이면_고정되지_않고_피해만()
        {
            EnemyController enemy = MakeEnemy(2, resistance: 100f);
            new HammerTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1206), enemy);
            Assert.AreEqual(20f, Lost(enemy), 0.001f);
            Assert.IsFalse(enemy.Status.Has(SpeedLockEffect.LockKey));
        }

        // ── 버프: 제련 · 세공 ──

        [Test]
        public void 제련은_식의_값_곱하기_골드()
        {
            Zone zone = MakeZone(MakeTower(1306), true, 30f);
            List<StatModifier> modifiers = new HammerBuffRule(() => 500, () => 1, _ => { }).GetModifiers(zone, MakeTower(1101), "해머 버프존");
            Assert.AreEqual(0.006f * 500f, Sum(modifiers, StatType.BonusDamage), 0.0001f, "0.6% × 500 = +300%");
        }

        [Test]
        public void 세공은_최대_스택_기준()
        {
            Zone zone = MakeZone(MakeTower(1306), true, 60f);
            List<StatModifier> modifiers = new HammerBuffRule(() => 500, () => 1, _ => { }).GetModifiers(zone, MakeTower(1101), "해머 버프존");
            Assert.AreEqual(6f, Sum(modifiers, StatType.BonusDamage), 0.0001f, "1.2% × 500 = +600%");
        }

        [Test]
        public void 추가_피해는_최종_피해와_더해_나가는_피해에_곱함()
        {
            Zone zone = MakeZone(MakeTower(1306), true, 60f);
            TowerController receiver = MakeTower(1101);
            receiver.State.Modifiers.Add(StatModifier.Flat("시험", StatType.FinalDamage, 0.1f));
            foreach (StatModifier modifier in new HammerBuffRule(() => 100, () => 1, _ => { }).GetModifiers(zone, receiver, "해머 버프존"))
                receiver.State.Modifiers.Add(modifier);
            Assert.AreEqual(1f + 0.1f + 1.2f, receiver.Stats.FinalDamageMultiplier, 0.0001f);
        }

        [Test]
        public void 타격_타워는_공격_횟수가_제련_1_세공_2()
        {
            var rule = new HammerBuffRule(() => 0, () => 1, _ => { });
            Assert.AreEqual(1f, Sum(rule.GetModifiers(MakeZone(MakeTower(1306), true, 30f), MakeTower(1106), "s"), StatType.AttackCount));
            Assert.AreEqual(2f, Sum(rule.GetModifiers(MakeZone(MakeTower(1306), true, 60f), MakeTower(1105), "s"), StatType.AttackCount), "방패도 타격");
            Assert.AreEqual(0f, Sum(rule.GetModifiers(MakeZone(MakeTower(1306), true, 60f), MakeTower(1101), "s"), StatType.AttackCount), "검은 베기");
        }

        [Test]
        public void 세공만_행동할_때_젬을_사이클만큼()
        {
            int gems = 0;
            var rule = new HammerBuffRule(() => 0, () => 3, amount => gems += amount);
            Random.InitState(1);
            for (int i = 0; i < 200; i++) rule.OnReceiverActed(MakeZone(MakeTower(1306), true, 30f), null);
            Assert.AreEqual(0, gems, "제련은 젬이 없음");

            Zone gilded = MakeZone(MakeTower(1306), true, 60f);
            for (int i = 0; i < 200; i++) rule.OnReceiverActed(gilded, null);
            Assert.Greater(gems, 0);
            Assert.AreEqual(0, gems % 3, "한 번에 사이클 번호(3)개");
        }

        // ── 디버프: 원석 · 금맥 ──

        private KillInfo Kill(EnemyController enemy, TowerController attacker) => new KillInfo
        {
            Enemy = enemy,
            Damage = new DamageInfo { Attacker = attacker, Attribute = attacker != null ? attacker.Definition.Attribute : AttackAttribute.None },
            SpawnWave = 1,
            Kind = EnemyKind.Normal,
        };

        [Test]
        public void 금맥_1점2퍼센트_더하기_타격_10퍼센트는_처치_골드_11점2퍼센트()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1406), false, 60f), enemy);
            int gold = 1000, gem = 0;
            DeathResolver.GetBonusReward(Kill(enemy, MakeTower(1106)), ref gold, ref gem);
            Assert.AreEqual(1112, gold);
        }

        [Test]
        public void 원석은_지금_스택_기준이고_타격은_5퍼센트()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1406), false, 30f), enemy);
            int gold = 1000, gem = 0;
            DeathResolver.GetBonusReward(Kill(enemy, MakeTower(1205)), ref gold, ref gem);
            Assert.AreEqual(1056, gold, "1000 × (1 + 0.6% + 5%)");
        }

        [Test]
        public void 원석_골드는_저항과_무관하고_소수는_버림()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 100f);
            Debuff(MakeZone(MakeTower(1406), false, 60f), enemy);
            int gold = 40, gem = 0;
            DeathResolver.GetBonusReward(Kill(enemy, null), ref gold, ref gem);
            Assert.AreEqual(40, gold, "40 × 1.012 = 40.48 → 40");

            gold = 1000;
            DeathResolver.GetBonusReward(Kill(enemy, null), ref gold, ref gem);
            Assert.AreEqual(1012, gold);
        }
    }
}
