using NUnit.Framework;

namespace SquareTD.Tests
{
    // 무기 문양 공격 스킬 12개 (20번 4-3 ~ 4-8장)와 2단계에서 함께 고친 것 (차지 보너스, 얼음 시너지, 창 모아 찌르기, 아머 크러시)
    //   판: 길 0번 (0,0), 1번 (1,0), 2번 (2,1), 3번 (1,1), 4번 (2,-1) / 스폰 0번 (0,1), 1번 (3,0), 2번 (1,-1)
    //   스폰 0번에서 길까지의 거리: 0·1·3번 칸 1, 2·4번 칸 2
    public class WeaponSkillTest : EmblemTestBase
    {
        private const float Eps = 0.001f;

        private static float SkillDamage(TowerController tower) => tower.Stats.Power * tower.Stats.AbilityValue;

        // ── 검 ──

        [Test]
        public void 보이드_슬래시는_맵_전체에_피해만()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(2), c = MakeTarget(4);
            TowerController tower = MakeTower(1101);

            new SwordSplashSkill(m_grid, m_occupancy).Fire(tower, a);

            foreach (OccupyingFakeTarget target in new[] { a, b, c })
            {
                Assert.AreEqual(1, target.Hits.Count, $"{target.PathIndex}번 칸");
                Assert.AreEqual(SkillDamage(tower), target.Hits[0].BaseDamage, Eps);
            }
        }

        [Test]
        public void 블레이드_스톰은_검기_10번에_나누고_잃은_체력_비례로_터짐()
        {
            OccupyingFakeTarget target = MakeTarget(0, hp: 1000f);
            TowerController tower = MakeTower(1201);
            float damage = SkillDamage(tower);

            new SwordTargetSkill(m_grid, m_occupancy).Fire(tower, target);

            Assert.AreEqual(SwordTargetSkill.StormHits + 1, target.Hits.Count, "검기 10번 + 폭발 1번");
            for (int i = 0; i < SwordTargetSkill.StormHits; i++)
                Assert.AreEqual(damage / SwordTargetSkill.StormHits, target.Hits[i].BaseDamage, Eps);
            Assert.AreEqual(damage * damage * 0.01f, target.Hits[SwordTargetSkill.StormHits].BaseDamage, Eps, "잃은 체력(검기 합) × (Power × AV)%");
        }

        [Test]
        public void 블레이드_스톰_도중에_죽으면_폭발은_없음()
        {
            TowerController tower = MakeTower(1201);
            OccupyingFakeTarget target = MakeTarget(0, hp: SkillDamage(tower) * 0.45f);

            new SwordTargetSkill(m_grid, m_occupancy).Fire(tower, target);

            Assert.IsFalse(target.IsAlive);
            Assert.AreEqual(5, target.Hits.Count, "검기 다섯 번째에 죽고 그 뒤로는 안 맞음");
        }

        // ── 도끼 ──

        [Test]
        public void 휠윈드는_맵_전체이고_사거리_안의_적만_반드시_치명타()
        {
            OccupyingFakeTarget inside = MakeTarget(0), outside = MakeTarget(2);
            TowerController tower = MakeTower(1102, range: 1);
            float damage = SkillDamage(tower);

            new AxeSplashSkill(m_grid, m_occupancy).Fire(tower, inside);

            Assert.IsTrue(inside.Hits[0].IsCritical);
            Assert.AreEqual(damage * CriticalRoll.Multiplier(tower.Stats.CriticalDamage), inside.Hits[0].BaseDamage, Eps);
            Assert.IsFalse(outside.Hits[0].IsCritical, "사거리 밖은 치명타 없음");
            Assert.AreEqual(damage, outside.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 블러드_레이지는_자기에게_치명_100_공격력_행동_카운트를_레벨_곱하기_티어번()
        {
            TowerController tower = MakeTower(3202);
            float power = tower.Stats.Power;
            float rate = power * tower.Stats.AbilityValue * 0.01f;

            new AxeTargetSkill(m_grid, m_occupancy).Fire(tower, null);

            Assert.AreEqual(1f, tower.Stats.Get(StatType.CritRate), Eps);
            Assert.AreEqual(power * (1f + rate), tower.Stats.Power, Eps);
            Assert.AreEqual(1f, tower.Stats.Get(StatType.ActionGainPerTurn), Eps);
            Assert.AreEqual(3, TimedTowerBuff.GetRemaining(tower.State, AxeTargetSkill.Source), "컬러 1 × 3티어");
        }

        [Test]
        public void 블러드_레이지를_다시_쓰면_겹치지_않고_처음부터()
        {
            TowerController tower = MakeTower(1202);
            float power = tower.Stats.Power;
            var skill = new AxeTargetSkill(m_grid, m_occupancy);

            skill.Fire(tower, null);
            float once = tower.Stats.Power;
            skill.Fire(tower, null);

            Assert.AreEqual(once, tower.Stats.Power, Eps, "자기 버프로 오른 공격력이 다시 계산에 들어가지 않음");
            Assert.Greater(once, power);
        }

        // ── 활 ──

        [Test]
        public void 애로우_레인은_레벨_곱하기_티어_턴_동안_매_턴_0점2배()
        {
            var grounds = new GroundEffectRegistry();
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(4);
            TowerController tower = MakeTower(3103);
            float perTurn = SkillDamage(tower) * ArrowRainGround.TurnRatio;

            new ArrowSplashSkill(m_grid, m_occupancy, grounds).Fire(tower, a);
            Assert.AreEqual(0, a.Hits.Count, "쏘아 올릴 때는 피해 없음");
            Assert.AreEqual(3, grounds.All[0].RemainingTurns, "컬러 1 × 3티어");

            for (int i = 0; i < 3; i++) grounds.RunTurn();

            Assert.AreEqual(3, a.Hits.Count);
            Assert.AreEqual(3, b.Hits.Count);
            Assert.AreEqual(perTurn, a.Hits[0].BaseDamage, Eps);
            Assert.AreEqual(0, grounds.All.Count, "턴이 다 지나면 사라짐");
        }

        [Test]
        public void 애로우_레인은_다시_쓰면_따로_깔림()
        {
            var grounds = new GroundEffectRegistry();
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1103);
            var skill = new ArrowSplashSkill(m_grid, m_occupancy, grounds);

            skill.Fire(tower, target);
            skill.Fire(tower, target);
            grounds.RunTurn();

            Assert.AreEqual(2, target.Hits.Count);
        }

        [Test]
        public void 헤비_볼트는_최대_체력의_위력_곱하기_계수_퍼센트()
        {
            OccupyingFakeTarget target = MakeTarget(0, hp: 1000f);
            TowerController tower = MakeTower(1203);

            new ArrowTargetSkill(m_grid, m_occupancy).Fire(tower, target);

            Assert.AreEqual(1000f * SkillDamage(tower) * 0.01f, target.Hits[0].BaseDamage, Eps);
        }

        // ── 창 ──

        [Test]
        public void 랠리_배너는_사거리_안의_아군_공격_타워와_자기에게()
        {
            var slots = new TowerSlots(3);
            TowerController owner = MakeTower(1104, slot: 0);
            TowerController near = MakeTower(1201, slot: 2);
            TowerController far = MakeTower(1201, slot: 1);
            TowerController buffTower = MakeTower(1301, slot: 2);
            slots.TryPlace(owner, 0);
            slots.TryPlace(near, 2);
            slots.TryPlace(far, 1);
            float rate = SkillDamage(owner) * 0.01f;
            float nearPower = near.Stats.Power;

            new SpearSplashSkill(m_grid, m_occupancy, slots).Fire(owner, null);

            Assert.AreEqual(nearPower * (1f + rate), near.Stats.Power, Eps, "스폰 0번 → 2번은 거리 2 (사거리 2 안)");
            Assert.AreEqual(rate, near.Stats.Get(StatType.CritRate), Eps);
            Assert.AreEqual(1, TimedTowerBuff.GetRemaining(near.State, SpearSplashSkill.Source));
            Assert.AreEqual(0, TimedTowerBuff.GetRemaining(far.State, SpearSplashSkill.Source), "스폰 1번은 거리 3");
            Assert.AreEqual(1, TimedTowerBuff.GetRemaining(owner.State, SpearSplashSkill.Source), "자기 자신도");
            Assert.AreEqual(0, TimedTowerBuff.GetRemaining(buffTower.State, SpearSplashSkill.Source), "버프 타워는 받지 않음");
        }

        [Test]
        public void 헤드헌트는_체력_높은_적부터_창을_꽂고_죽으면_다음으로()
        {
            TowerController tower = MakeTower(3204, range: 2);
            float spear = SkillDamage(tower) * SpearTargetSkill.SpearRatio;
            OccupyingFakeTarget first = MakeTarget(0, hp: spear * 1.5f);
            OccupyingFakeTarget second = MakeTarget(1, hp: spear * 1.2f);
            OccupyingFakeTarget third = MakeTarget(3, hp: spear * 0.5f);

            new SpearTargetSkill(m_grid, m_occupancy).Fire(tower, third);

            Assert.AreEqual(3, SpearTargetSkill.SpearCount(tower), "3티어 × 컬러 1");
            Assert.AreEqual(2, first.Hits.Count, "가장 높은 적이 죽을 때까지");
            Assert.AreEqual(1, second.Hits.Count, "남은 창은 다음으로 높은 적에게");
            Assert.AreEqual(0, third.Hits.Count);
            Assert.AreEqual(spear, first.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 헤드헌트는_사거리_밖의_적은_노리지_않음()
        {
            TowerController tower = MakeTower(1204, range: 1);
            OccupyingFakeTarget outside = MakeTarget(2, hp: 5000f);
            OccupyingFakeTarget inside = MakeTarget(0, hp: 1000f);

            new SpearTargetSkill(m_grid, m_occupancy).Fire(tower, inside);

            Assert.AreEqual(0, outside.Hits.Count);
            Assert.AreEqual(1, inside.Hits.Count);
        }

        // ── 방패 ──

        [Test]
        public void 콘커션은_착탄_범위의_적을_기절시킴()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);

            new ShieldSplashSkill(m_grid, m_occupancy).Fire(MakeTower(1105), enemy);

            Assert.IsTrue(enemy.Status.Has(ImmobilizeEffect.ImmobilizeKey));
        }

        [Test]
        public void 콘커션은_강한_CC라_저항력_100이면_막힘()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f, resistance: 100f);

            new ShieldSplashSkill(m_grid, m_occupancy).Fire(MakeTower(1105), enemy);

            Assert.IsFalse(enemy.Status.Has(ImmobilizeEffect.ImmobilizeKey));
        }

        [Test]
        public void 리바운드는_직전_적을_빼고_가장_가까운_적에게_튕김()
        {
            TowerController tower = MakeTower(3205);
            OccupyingFakeTarget main = MakeTarget(0), close = MakeTarget(3), far = MakeTarget(4);

            new ShieldTargetSkill(m_grid, m_occupancy).Fire(tower, main);

            Assert.AreEqual(3, ShieldTargetSkill.BounceCount(tower));
            Assert.AreEqual(2, close.Hits.Count, "0번 → 3번 → 0번 → 3번");
            Assert.AreEqual(1, main.Hits.Count);
            Assert.AreEqual(0, far.Hits.Count);
            Assert.AreEqual(SkillDamage(tower) * ShieldTargetSkill.BounceRatio, close.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 리바운드는_튕길_적이_없으면_남은_피해를_합쳐_한_번에()
        {
            TowerController tower = MakeTower(3205);
            OccupyingFakeTarget only = MakeTarget(0, hp: 100000f);

            new ShieldTargetSkill(m_grid, m_occupancy).Fire(tower, only);

            Assert.AreEqual(1, only.Hits.Count);
            Assert.AreEqual(SkillDamage(tower) * ShieldTargetSkill.BounceRatio * 3f, only.Hits[0].BaseDamage, Eps);
        }

        // ── 해머 ──

        [Test]
        public void 저지먼트는_착탄_범위에_피해()
        {
            OccupyingFakeTarget main = MakeTarget(1), beside = MakeTarget(0);
            TowerController tower = MakeTower(1106);

            new HammerSplashSkill(m_grid, m_occupancy).Fire(tower, main);

            Assert.AreEqual(SkillDamage(tower), main.Hits[0].BaseDamage, Eps);
            Assert.AreEqual(0, beside.Hits.Count, "Mastery 1은 착탄 칸만");
        }

        [Test]
        public void 저지먼트의_맞은편은_비프로스트와_같은_칸()
        {
            Assert.AreEqual(3, m_grid.GetBifrostPartner(1), "1 + 5 / 2");
        }

        [Test]
        public void 저지먼트는_저항력_100인_적을_날리지_않음()
        {
            EnemyController enemy = MakeEnemy(1, hp: 1000f, resistance: 100f);

            new HammerSplashSkill(m_grid, m_occupancy).Fire(MakeTower(1106), enemy);

            Assert.AreEqual(1, enemy.PathIndex);
        }

        [Test]
        public void 아머_크러시는_8번에_나눠_침()
        {
            OccupyingFakeTarget target = MakeTarget(0, hp: 1000f);
            TowerController tower = MakeTower(1206);

            new HammerTargetSkill(m_grid, m_occupancy).Fire(tower, target);

            Assert.AreEqual(HammerTargetSkill.Strikes, target.Hits.Count);
            Assert.AreEqual(SkillDamage(tower) / HammerTargetSkill.Strikes, target.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 아머_크러시를_맞은_적은_1번_행동하는_동안_방어력_0()
        {
            EnemyController enemy = MakeEnemy(0, hp: 100000f, defense: 100f);
            var probe = new DamageInfo { BaseDamage = 100f };
            Assert.AreEqual(50f, enemy.PreviewDamage(probe), Eps, "방어력 100 → 절반");

            new HammerTargetSkill(m_grid, m_occupancy).Fire(MakeTower(1206), enemy);

            Assert.AreEqual(100f, enemy.PreviewDamage(probe), Eps, "방어력 0");
            enemy.Status.OnActed();
            Assert.AreEqual(50f, enemy.PreviewDamage(probe), Eps, "1번 행동 뒤 돌아옴");
        }

        [Test]
        public void 아머_크러시는_저항력만큼_덜_깎음()
        {
            EnemyController enemy = MakeEnemy(0, hp: 100000f, defense: 100f, resistance: 50f);

            new HammerTargetSkill(m_grid, m_occupancy).Fire(MakeTower(1206), enemy);

            Assert.AreEqual(100f / 1.5f, enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f }), Eps, "방어력 100의 절반만 깎여 50");
        }

        // ── 함께 고친 것 ──

        [Test]
        public void 오버플로우의_차지_배율은_대신_쓰는_스킬에도_들어감()
        {
            var grounds = new GroundEffectRegistry();
            TowerController tower = MakeTower(1103);
            tower.State.Modifiers.Add(StatModifier.Flat("오버플로우", StatType.ChargeBonus, 0.5f));
            tower.State.Stack.Add(60f);
            float charged = tower.Stats.Power * tower.Stats.AbilityValue * 1.6f * 1.5f * ArrowRainGround.TurnRatio;

            new ArrowSplashSkill(m_grid, m_occupancy, grounds).Fire(tower, null);

            Assert.AreEqual(charged, ((ArrowRainGround)grounds.All[0]).Damage, Eps, "AV × (1 + 60 × 0.01) × 1.5");
        }

        [Test]
        public void 딥_프리즈인_적을_치면_얼음_광역은_AttackCount_1_단일은_2_더()
        {
            EnemyController enemy = MakeEnemy(0, hp: 100000f);
            TowerController debuff = MakeTower(1411);
            Debuff(debuff, enemy);
            Debuff(debuff, enemy, skill2: true);

            Assert.AreEqual(1, IceDebuffRule.AttackCountBonus(MakeTower(1111), enemy));
            Assert.AreEqual(2, IceDebuffRule.AttackCountBonus(MakeTower(1211), enemy));
            Assert.AreEqual(0, IceDebuffRule.AttackCountBonus(MakeTower(1107), enemy), "얼음 타워만");
        }

        [Test]
        public void 딥_프리즈가_없으면_얼음_시너지도_없음()
        {
            EnemyController enemy = MakeEnemy(0, hp: 100000f);
            Debuff(MakeTower(1411), enemy);

            Assert.AreEqual(0, IceDebuffRule.AttackCountBonus(MakeTower(1111), enemy), "프로스트바이트(스킬 1)만으로는 안 됨");
        }

        [Test]
        public void 모아_찌르기는_AttackCount를_1로_고정하고_공격력에_곱함()
        {
            var go = new UnityEngine.GameObject("Tower 1101");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            tower.Init(new TowerDefinition(1101, 10f, 2, 3, 3, 0f, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest), 0, null);
            float power = tower.Stats.Power;

            tower.State.Modifiers.Add(StatModifier.Flat("시험 버프", StatType.AttackCount, 1f));
            tower.State.Modifiers.Add(StatModifier.Flat("원 포인트", StatType.AttackCountLock, 1f));
            tower.State.Modifiers.Add(StatModifier.Percent("원 포인트", StatType.Power, 0.1f));

            Assert.AreEqual(1, tower.Stats.AttackCount);
            Assert.AreEqual(power * 1.1f * 4f, tower.Stats.Power, Eps, "다른 버프로 늘어난 AttackCount(3 + 1)까지 모아 찌름");
        }
    }
}
