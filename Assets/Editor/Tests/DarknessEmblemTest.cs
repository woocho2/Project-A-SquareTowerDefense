using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 40 어둠 (20번 4-16장) — 블랙홀 · 심연 · 포식→폭식 · 잠식→침식 · 카운트
    public class DarknessEmblemTest : EmblemTestBase
    {
        [Test]
        public void 등록표_어둠()
        {
            Assert.IsInstanceOf<DarknessSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Darkness, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<DarknessTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Darkness, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<DarknessBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Darkness));
            Assert.IsInstanceOf<DarknessDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Darkness));
        }

        // ── 카운트 ──

        private static KillInfo KillBy(DamageSource source, TowerEmblem emblem) => new KillInfo
        {
            Damage = new DamageInfo { Source = source, Emblem = emblem },
            Kind = EnemyKind.Normal,
        };

        [Test]
        public void 처치_1은_어둠_버프_타워가_있을_때만_블랙홀_막타는_5()
        {
            Assert.AreEqual(0f, EmblemSharedState.GetKillCount(KillBy(DamageSource.Basic, TowerEmblem.Sword), false));
            Assert.AreEqual(1f, EmblemSharedState.GetKillCount(KillBy(DamageSource.Basic, TowerEmblem.Sword), true));
            Assert.AreEqual(5f, EmblemSharedState.GetKillCount(KillBy(DamageSource.Ground, TowerEmblem.Darkness), false), "어둠 버프 타워가 없어도");
            Assert.AreEqual(6f, EmblemSharedState.GetKillCount(KillBy(DamageSource.Ground, TowerEmblem.Darkness), true));
            Assert.AreEqual(0f, EmblemSharedState.GetKillCount(KillBy(DamageSource.Ground, TowerEmblem.Bow), false), "화살비 막타는 아님");
        }

        // ── 광역: 블랙홀 ──

        [Test]
        public void 블랙홀은_그_자리에서는_피해가_없고_매_턴_소환한_때의_값()
        {
            var grounds = new GroundEffectRegistry();
            EnemyController enemy = MakeEnemy(2, hp: 1000f);
            new DarknessSplashSkill(m_grid, m_occupancy, Auto(), grounds).Fire(MakeTower(1114), enemy);

            Assert.AreEqual(0f, Lost(enemy));
            Assert.AreEqual(1, grounds.All.Count);
            Assert.AreEqual(2, grounds.All[0].RemainingTurns, "레벨 1 × 2턴");

            EmblemSharedState.Current.AddDarkness(500f);        // 소환한 뒤에 카운트가 올라도 그대로
            grounds.RunTurn();
            Assert.AreEqual(20f, Lost(enemy), 0.001f);
        }

        [Test]
        public void 포식_존이면_카운트_1당_0점04퍼센트_폭식은_0점08퍼센트()
        {
            TowerController feast = MakeTower(1114, slot: 0), gluttony = MakeTower(1114, slot: 1);
            Receive(MakeZone(MakeTower(1314, slot: 2), true, 30f), feast);
            Receive(MakeZone(MakeTower(1314, slot: 2), true, 60f), gluttony);
            Assert.AreEqual(1.4f, DarknessSplashSkill.GetCountMultiplier(feast, 1000f), 0.0001f);
            Assert.AreEqual(1.8f, DarknessSplashSkill.GetCountMultiplier(gluttony, 1000f), 0.0001f);
        }

        [Test]
        public void 블랙홀_구역은_Mastery_크기()
        {
            var grounds = new GroundEffectRegistry();
            new DarknessSplashSkill(m_grid, m_occupancy, Auto(), grounds).Fire(MakeTower(1114, mastery: 2), MakeEnemy(0));
            GroundEffect hole = grounds.All[0];
            Assert.IsTrue(hole.Covers(0));
            Assert.IsTrue(hole.Covers(3), "3×3 안 (1,1)");
            Assert.IsFalse(hole.Covers(2), "(2,1)은 두 칸 떨어짐");
        }

        [Test]
        public void 블랙홀은_낮은_쪽_적을_빨리_높은_쪽_적은_되돌림()
        {
            var hole = new BlackHoleGround(MakeTower(1114), m_grid, m_occupancy, 2, 1, 20f, 2);
            EnemyController lower = MakeEnemy(0), higher = MakeEnemy(4, resistance: 100f), eroded = MakeEnemy(4);
            Debuff(MakeZone(MakeTower(1414), false, 60f), eroded);

            MoveRange low = hole.ModifyMove(lower, lower.Stats.GetMoveRange());
            Assert.AreEqual(4, low.Min, "2 + 2");
            Assert.AreEqual(6, low.Max);

            MoveRange high = hole.ModifyMove(higher, higher.Stats.GetMoveRange());
            Assert.AreEqual(5, high.Min, "저항 100이라 뒤로는 안 감, 이동력 +3만");
            Assert.AreEqual(7, high.Max);

            MoveRange back = hole.ModifyMove(eroded, eroded.Stats.GetMoveRange());
            Assert.AreEqual(-9, back.Min, "침식: +5에 100% 뒤로 → −9 ~ −7");
            Assert.AreEqual(-7, back.Max);
        }

        [Test]
        public void 블랙홀이_여러_개면_인덱스가_가장_가까운_것을_따름()
        {
            var grounds = new GroundEffectRegistry();
            grounds.Add(new BlackHoleGround(MakeTower(1114), m_grid, m_occupancy, 4, 1, 1f, 2));
            grounds.Add(new BlackHoleGround(MakeTower(1114), m_grid, m_occupancy, 1, 1, 1f, 2));
            EnemyController enemy = MakeEnemy(0);
            Assert.AreEqual(4, grounds.DecideMove(enemy, enemy.Stats.GetMoveRange()).Min, "1번 블랙홀(낮은 쪽 +2)을 따름");
        }

        // ── 단일: 심연 ──

        [Test]
        public void 심연은_카운트_100이면_2배()
        {
            EnemyController enemy = MakeEnemy(1, hp: 1000f);
            EmblemSharedState.Current.AddDarkness(100f);
            new DarknessTargetSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1214), enemy);
            Assert.AreEqual(40f, Lost(enemy), 0.001f);
            Assert.AreEqual(100f, EmblemSharedState.Current.DarknessCount, "쓰기만 하고 줄이지 않음");
        }

        [Test]
        public void 카운트_1000에_포식_존은_16배_폭식_존은_21배()
        {
            TowerController feast = MakeTower(1214, slot: 0), gluttony = MakeTower(1214, slot: 1);
            Receive(MakeZone(MakeTower(1314, slot: 2), true, 30f), feast);
            Receive(MakeZone(MakeTower(1314, slot: 2), true, 60f), gluttony);
            Assert.AreEqual(16f, DarknessTargetSkill.GetAbyssMultiplier(feast, 1000f), 0.0001f);
            Assert.AreEqual(21f, DarknessTargetSkill.GetAbyssMultiplier(gluttony, 1000f), 0.0001f);
        }

        // ── 버프: 포식 · 폭식 ──

        [Test]
        public void 카운트_단계는_닿은_데까지_전부()
        {
            var rule = new DarknessBuffRule(() => 1000f);
            List<StatModifier> all = rule.GetModifiers(MakeZone(MakeTower(1314), true, 60f), MakeTower(1101), "s");
            Assert.AreEqual(0.012f, Sum(all, StatType.Power), 0.00001f);
            Assert.AreEqual(0.012f, Sum(all, StatType.CritRate), 0.00001f);
            Assert.AreEqual(0.012f, Sum(all, StatType.CritDamage), 0.00001f);
            Assert.AreEqual(1f, Sum(all, StatType.AttackCount));
            Assert.AreEqual(-1f, Sum(all, StatType.MaxAction));
            Assert.AreEqual(1.5f, Sum(all, StatType.RewardBonus), 0.00001f);
            Assert.AreEqual(0.012f, Sum(all, StatType.AbilityValue), 0.00001f);
            Assert.AreEqual(1f, Sum(all, StatType.Mastery));
            Assert.AreEqual(1f, Sum(all, StatType.ForceFixedDamage));
            Assert.AreEqual(0.012f, Sum(all, StatType.FinalDamage), 0.00001f);
            Assert.AreEqual(1f, Sum(all, StatType.Range), "폭식");

            List<StatModifier> some = new DarknessBuffRule(() => 250f).GetModifiers(MakeZone(MakeTower(1314), true, 30f), MakeTower(1101), "s");
            Assert.AreEqual(2, some.Count, "100·200만 (포식이라 사거리 없음)");
        }

        [Test]
        public void 카운트_600_존의_타워가_처치하면_골드_젬_150퍼센트_더()
        {
            TowerController killer = MakeTower(1101);
            killer.State.Modifiers.Add(StatModifier.Flat("어둠 버프존", StatType.RewardBonus, 1.5f));
            var kill = new KillInfo { Damage = new DamageInfo { Attacker = killer }, Kind = EnemyKind.Normal };
            int gold = 10, gem = 2;
            DeathResolver.GetBonusReward(kill, ref gold, ref gem);
            Assert.AreEqual(25, gold);
            Assert.AreEqual(5, gem);
        }

        [Test]
        public void 카운트_900이면_모든_공격이_고정_피해()
        {
            EnemyController enemy = MakeEnemy(1, hp: 1000f, defense: 100f);
            TowerController tower = MakeTower(1201);
            tower.State.Modifiers.Add(StatModifier.Flat("어둠 버프존", StatType.ForceFixedDamage, 1f));
            new SwordTargetSkill(m_grid, m_occupancy, Auto()).Fire(tower, enemy);
            Assert.AreEqual(20f * (23f + 2.4f) / 24f, Lost(enemy), 0.01f, "방어 100을 무시 (블레이드 템페스트 전체)");
        }

        // ── 디버프: 잠식 · 침식 ──

        [Test]
        public void 침식인_적이_죽으면_카운트_더하기_식의_값()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1414), false, 60f), enemy);
            enemy.TakeDamage(new DamageInfo { BaseDamage = 1000f, ForceFixed = true });
            Assert.AreEqual(1.2f, EmblemSharedState.Current.DarknessCount, 0.0001f, "버프 타워가 없어도 쌓임 (처치의 +1은 GameSession이)");
        }

        [Test]
        public void 침식은_받는_피해_10퍼센트_저항만큼_줄고_잠식은_없음()
        {
            EnemyController eroded = MakeEnemy(0, resistance: 50f), corroded = MakeEnemy(1);
            Debuff(MakeZone(MakeTower(1414), false, 60f), eroded);
            Debuff(MakeZone(MakeTower(1414), false, 30f), corroded);
            Assert.AreEqual(1.05f, eroded.Stats.Get(StatType.DamageTaken), 0.0001f);
            Assert.AreEqual(1f, corroded.Stats.Get(StatType.DamageTaken), 0.0001f);
        }
    }
}
