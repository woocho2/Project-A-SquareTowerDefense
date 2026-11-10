using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    // 수업 38 전기 (20번 4-14장) — 라이트닝 볼텍스 · 과부하 · 충전→과충전→방전 · 누전→감전 · 전하
    public class ElectricEmblemTest : EmblemTestBase
    {
        [Test]
        public void 등록표_전기()
        {
            Assert.IsInstanceOf<ElectricSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Electricity, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<ElectricTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Electricity, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<ElectricBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Electricity));
            Assert.IsInstanceOf<ElectricDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Electricity));
        }

        // ── 광역: 라이트닝 볼텍스 ──

        [Test]
        public void 볼텍스는_맵_전체에_전하_10_곱하기_레벨()
        {
            EnemyController a = MakeEnemy(0, hp: 1000f), b = MakeEnemy(4, hp: 1000f);
            new ElectricSplashSkill(m_grid, m_occupancy, Auto()).Fire(MakeTower(1112), a);
            Assert.AreEqual(10f, ChargeEffect.Get(a), 0.0001f);
            Assert.AreEqual(10f, ChargeEffect.Get(b), 0.0001f);
            Assert.AreEqual(20f, Lost(b), 0.001f);
        }

        [Test]
        public void 충전_존은_전하_2배_과충전_존은_4배()
        {
            TowerController charge = MakeTower(1112, slot: 0), overcharge = MakeTower(1112, slot: 1);
            Receive(MakeZone(MakeTower(1312, slot: 2), true, 30f), charge);
            Receive(MakeZone(MakeTower(1312, slot: 2), true, 60f), overcharge);
            Assert.AreEqual(20f, ElectricSplashSkill.GetVortexCharge(charge), 0.0001f);
            Assert.AreEqual(40f, ElectricSplashSkill.GetVortexCharge(overcharge), 0.0001f);
        }

        [Test]
        public void 전기_광역의_기본_공격은_전하_1()
        {
            EnemyController enemy = MakeEnemy(0);
            var skill = new ElectricSplashSkill(m_grid, m_occupancy, Auto());
            MakeTower(1112, skill: skill).OnBasicHitLanded(enemy);
            Assert.AreEqual(1f, ChargeEffect.Get(enemy), 0.0001f);
        }

        // ── 단일: 과부하 ──

        [Test]
        public void 과부하는_1_더하기_레벨_곱하기_Mastery명()
        {
            Assert.AreEqual(2, ElectricTargetSkill.GetChainCount(MakeTower(1212)), "브론즈 레벨 1, Mastery 1");
            Assert.AreEqual(4, ElectricTargetSkill.GetChainCount(MakeTower(1212, mastery: 3)));
        }

        [Test]
        public void 과부하는_전하_30이면_1점3배_감전이면_전하_2배로_셈_터뜨린_뒤_0()
        {
            EnemyController plain = MakeEnemy(1, hp: 1000f), shocked = MakeEnemy(3, hp: 1000f);
            ChargeEffect.Add(plain, 30f);
            ChargeEffect.Add(shocked, 30f);
            Debuff(MakeZone(MakeTower(1412), false, 60f), shocked);
            var skill = new ElectricTargetSkill(m_grid, m_occupancy, Auto());

            skill.Fire(MakeTower(1212), plain);       // 연쇄 2명: plain + 가까운 shocked
            Assert.AreEqual(26f, Lost(plain), 0.001f, "20 × 1.3");
            Assert.AreEqual(32f, Lost(shocked), 0.001f, "20 × (1 + 30 × 2 × 1%)");
            Assert.AreEqual(0f, ChargeEffect.Get(plain), 0.0001f);
            Assert.AreEqual(0f, ChargeEffect.Get(shocked), 0.0001f);
        }

        [Test]
        public void 연쇄는_가까운_적부터_거리가_같으면_전하가_높은_적부터()
        {
            EnemyController first = MakeEnemy(0), low = MakeEnemy(1), high = MakeEnemy(3), far = MakeEnemy(2);
            ChargeEffect.Add(low, 5f);
            ChargeEffect.Add(high, 50f);
            ChargeEffect.Add(far, 100f);

            List<ITarget> chain = SkillArea.Chain(m_grid, m_occupancy, first, 3, ChargeEffect.Get);
            CollectionAssert.AreEqual(new ITarget[] { first, high, low }, chain);
        }

        [Test]
        public void 충전_존이면_과부하_1점5배_과충전_존이면_2배()
        {
            EnemyController a = MakeEnemy(1, hp: 1000f, occupy: false), b = MakeEnemy(1, hp: 1000f, occupy: false);   // 점유 밖: 연쇄가 서로를 맞히지 않게
            TowerController charge = MakeTower(1212, slot: 0), overcharge = MakeTower(1212, slot: 1);
            Receive(MakeZone(MakeTower(1312, slot: 2), true, 30f), charge);
            Receive(MakeZone(MakeTower(1312, slot: 2), true, 60f), overcharge);
            charge.State.Modifiers.Add(StatModifier.Flat("시험: 치명 끔", StatType.CritRate, -1f));          // 존이 주는 치명 확률 때문에 결과가 흔들리지 않게
            overcharge.State.Modifiers.Add(StatModifier.Flat("시험: 치명 끔", StatType.CritRate, -1f));

            var skill = new ElectricTargetSkill(m_grid, m_occupancy, Auto());
            skill.Fire(charge, a);
            skill.Fire(overcharge, b);
            Assert.AreEqual(30f, Lost(a), 0.001f, "20 × 1.5");
            Assert.AreEqual(40f, Lost(b), 0.001f, "20 × 2");
        }

        // ── 버프: 충전 · 과충전 · 방전 ──

        [Test]
        public void 충전과_과충전은_치명_확률과_치명_피해_방전은_아무것도()
        {
            var rule = new ElectricBuffRule();
            List<StatModifier> overcharge = rule.GetModifiers(MakeZone(MakeTower(1312), true, 60f), MakeTower(1101), "s");
            Assert.AreEqual(0.012f, Sum(overcharge, StatType.CritRate), 0.00001f);
            Assert.AreEqual(0.012f, Sum(overcharge, StatType.CritDamage), 0.00001f);

            Zone discharged = MakeZone(MakeTower(1312), true, 60f);
            discharged.StartDischarge(5);
            Assert.AreEqual(0, rule.GetModifiers(discharged, MakeTower(1101), "s").Count);
        }

        [Test]
        public void 과충전_존의_타워가_맞히면_광역_2_단일_5()
        {
            var rule = new ElectricBuffRule();
            Zone overcharge = MakeZone(MakeTower(1312), true, 60f), charge = MakeZone(MakeTower(1312), true, 30f);
            EnemyController a = MakeEnemy(0), b = MakeEnemy(1), c = MakeEnemy(2);

            rule.OnReceiverHit(overcharge, MakeTower(1101), a);
            rule.OnReceiverHit(overcharge, MakeTower(1201), b);
            rule.OnReceiverHit(charge, MakeTower(1201), c);

            Assert.AreEqual(2f, ChargeEffect.Get(a), 0.0001f);
            Assert.AreEqual(5f, ChargeEffect.Get(b), 0.0001f);
            Assert.AreEqual(0f, ChargeEffect.Get(c), 0.0001f, "충전 존은 전하 없음");
        }

        [Test]
        public void 과충전은_레벨_곱하기_5턴_방전_5턴_뒤_0스택의_충전()
        {
            Zone zone = MakeZone(MakeTower(1312), true, 60f);
            var rule = new ElectricBuffRule();

            Assert.IsFalse(rule.OnZoneTurn(zone), "켜진 다음 턴: 재기 시작");
            for (int turn = 1; turn < 5; turn++) Assert.IsFalse(rule.OnZoneTurn(zone));
            Assert.IsTrue(rule.OnZoneTurn(zone));
            Assert.AreEqual(ZoneStage.Discharge, zone.Stage);

            zone.AddStack(10f, 60);
            Assert.AreEqual(0f, zone.Stack, "방전 중에는 스택을 쌓지 않음");

            for (int turn = 1; turn < 5; turn++) Assert.IsFalse(rule.OnZoneTurn(zone));
            Assert.IsTrue(rule.OnZoneTurn(zone));
            Assert.AreEqual(ZoneStage.Skill1, zone.Stage);
            Assert.AreEqual(0f, zone.Stack);
        }

        // ── 디버프: 누전 · 감전 ──

        [Test]
        public void 누전은_밟을_때마다_식의_값만큼_전하()
        {
            EnemyController enemy = MakeEnemy(0, resistance: 100f);
            Zone zone = MakeZone(MakeTower(1412), false, 30f);
            new ElectricDebuffRule().OnStep(zone, enemy);
            new ElectricDebuffRule().OnStep(zone, enemy);
            Assert.AreEqual(1.2f, ChargeEffect.Get(enemy), 0.0001f, "0.6씩 두 번, 저항과 무관");
        }

        [Test]
        public void 감전은_매_턴_전하()
        {
            EnemyController enemy = MakeEnemy(0);
            Debuff(MakeZone(MakeTower(1412), false, 60f), enemy);
            enemy.Status.OnTurn();
            enemy.Status.OnTurn();
            Assert.AreEqual(2.4f, ChargeEffect.Get(enemy), 0.0001f);
        }
    }
}
