using NUnit.Framework;

namespace SquareTD.Tests
{
    // 원소 문양 공격 스킬 16개 (20번 4-9 ~ 4-16장)
    //   공격 타워: Power 10, AbilityValue 2 → 스킬 피해 20. 3티어는 Power 40 → 80, 유지 3
    //   판: 길 0번 (0,0), 1번 (1,0), 2번 (2,1), 3번 (1,1), 4번 (2,-1)
    public class ElementSkillTest : EmblemTestBase
    {
        private const float Eps = 0.001f;

        private static float SkillDamage(TowerController tower) => tower.Stats.Power * tower.Stats.AbilityValue;

        private static void StartTranscendWave()
        {
            EmblemSharedState shared = EmblemSharedState.Current;
            shared.AddTranscend(EmblemSharedState.TranscendGoal);
            shared.OnWaveStarted();
        }

        // ── 불 ──

        [Test]
        public void 메테오는_맵_전체에_화상_스택_1당_1퍼센트_더()
        {
            EnemyController burning = MakeEnemy(0, hp: 1000f);
            BurnEffect.Add(burning, 50f);
            OccupyingFakeTarget plain = MakeTarget(2);
            TowerController tower = MakeTower(1107);

            new FireSplashSkill(m_grid, m_occupancy).Fire(tower, plain);

            Assert.AreEqual(SkillDamage(tower) * 1.5f, Lost(burning), Eps);
            Assert.AreEqual(SkillDamage(tower), plain.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 브레스는_레벨_곱하기_티어_턴_동안_체력이_가장_많은_적을_쫓고_화상을_쌓음()
        {
            EnemyController big = MakeEnemy(0, hp: 2000f), small = MakeEnemy(1, hp: 1000f);
            TowerController tower = MakeTower(3207);
            float damage = SkillDamage(tower);
            var skill = new FireTargetSkill(m_grid, m_occupancy);

            skill.Fire(tower, small);
            Assert.IsTrue(skill.BlocksBasicAttack(tower), "뿜는 동안 기본 공격 없음");

            for (int i = 0; i < 4; i++) skill.OnEnemyTurnStart(tower);

            Assert.AreEqual(damage * (1f + 1.01f + 1.02f), Lost(big), Eps, "3턴, 맞을 때마다 화상 +1 → 1% 씩 셈");
            Assert.AreEqual(3f, BurnEffect.Get(big), Eps);
            Assert.AreEqual(0f, Lost(small), Eps);
            Assert.IsFalse(skill.BlocksBasicAttack(tower));
        }

        // ── 물 ──

        [Test]
        public void 쓰나미는_젖음_스택_1당_1퍼센트_더_저항력_100이면_밀리지_않음()
        {
            EnemyController enemy = MakeEnemy(1, hp: 1000f, resistance: 100f);
            WetEffect.Add(enemy, 50f);
            TowerController tower = MakeTower(1108);

            new WaterSplashSkill(m_grid, m_occupancy).Fire(tower, enemy);

            Assert.AreEqual(SkillDamage(tower) * 1.5f, Lost(enemy), Eps);
            Assert.AreEqual(1, enemy.PathIndex);
        }

        [Test]
        public void 아쿠아_프리즌은_가두고_매_턴_피해()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            TowerController tower = MakeTower(1208);

            new WaterTargetSkill(m_grid, m_occupancy).Fire(tower, enemy);

            MoveRange range = enemy.Status.DecideMove(new MoveRange(2, 4, 3));
            Assert.AreEqual(0, range.Min);
            Assert.AreEqual(0, range.Max);

            enemy.Status.OnTurn();
            Assert.AreEqual(SkillDamage(tower), Lost(enemy), Eps);
            Assert.IsFalse(enemy.Status.Has(AquaPrisonEffect.PrisonKey), "1턴(1티어 × 컬러 1) 뒤 풀림");
        }

        [Test]
        public void 아쿠아_프리즌을_저항하면_갇히지_않고_피해도_저항력만큼_줄어듦()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f, resistance: 100f);

            new WaterTargetSkill(m_grid, m_occupancy).Fire(MakeTower(1208), enemy);

            Assert.AreEqual(2, enemy.Status.DecideMove(new MoveRange(2, 4, 3)).Min);
            enemy.Status.OnTurn();
            Assert.AreEqual(0f, Lost(enemy), Eps);
        }

        // ── 대지 ──

        [Test]
        public void 어스퀘이크는_맞은_적을_멈추고_쓸_때마다_AbilityValue가_성장만큼_쌓임()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            TowerController tower = MakeTower(1109);
            Growth.Add(tower, 10f);
            var skill = new EarthSplashSkill(m_grid, m_occupancy);

            skill.Fire(tower, enemy);
            Assert.IsTrue(enemy.Status.Has(ImmobilizeEffect.ImmobilizeKey));
            Assert.AreEqual(2.2f, tower.Stats.AbilityValue, Eps);

            skill.Fire(tower, enemy);
            Assert.AreEqual(2.4f, tower.Stats.AbilityValue, Eps, "원본 기준으로 더함 (복리 아님)");
        }

        [Test]
        public void 소잉은_고른_스탯이_성장만큼_오름()
        {
            TowerController tower = MakeTower(1209);
            Growth.Add(tower, 10f);

            EarthTargetSkill.Sow(tower, 0);
            EarthTargetSkill.Sow(tower, 1);
            EarthTargetSkill.Sow(tower, 2);

            Assert.AreEqual(11f, tower.Stats.Power, Eps);
            Assert.AreEqual(0.1f, tower.Stats.CriticalDamage, Eps);
            Assert.AreEqual(0.1f, tower.Stats.CriticalRate, Eps);
        }

        [Test]
        public void 소잉의_AttackCount는_소수를_모아_정수만_더함()
        {
            TowerController tower = MakeTower(1209);
            Growth.Add(tower, 40f);

            EarthTargetSkill.Sow(tower, 3);
            EarthTargetSkill.Sow(tower, 3);
            Assert.AreEqual(1, tower.Stats.AttackCount, "0.8은 아직");

            EarthTargetSkill.Sow(tower, 3);
            Assert.AreEqual(2, tower.Stats.AttackCount, "1.2 중 +1");
            Assert.AreEqual(1.2f, tower.State.GetCounter(EarthTargetSkill.AttackCountKey), Eps);
        }

        // ── 바람 ──

        [Test]
        public void 윈드_슬래쉬는_반드시_치명타이고_범위의_적_수로_나눔()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(0);
            TowerController tower = MakeTower(1110);

            new WindSplashSkill(m_grid, m_occupancy).Fire(tower, a);

            Assert.IsTrue(a.Hits[0].IsCritical);
            Assert.AreEqual(SkillDamage(tower) * 2f / 2f, a.Hits[0].BaseDamage, Eps);
            Assert.AreEqual(a.Hits[0].BaseDamage, b.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 윈드_슬래쉬는_가속_상태면_치명타_배율에_가속_스택_퍼센트를_더함()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1110);
            Acceleration.Add(tower, 100f);

            new WindSplashSkill(m_grid, m_occupancy).Fire(tower, target);

            Assert.AreEqual(SkillDamage(tower) * (2f + 1f), target.Hits[0].BaseDamage, Eps);
        }

        [Test]
        public void 템페스트는_가속_상태면_확률에_가속을_더하고_AttackCount_4()
        {
            TowerController tower = MakeTower(1210);
            Acceleration.Add(tower, 100f);

            new WindTargetSkill(m_grid, m_occupancy).Fire(tower, null);

            Assert.AreEqual(5, tower.Stats.AttackCount, "확률 20% + 가속 100% → 반드시 발동");
            Assert.AreEqual(1, TimedTowerBuff.GetRemaining(tower.State, WindTargetSkill.Source));
        }

        // ── 얼음 ──

        [Test]
        public void 블리자드는_빙결인_적에게_150퍼센트_더_한기인_적은_빙결시킴()
        {
            EnemyController frozen = MakeEnemy(0, hp: 1000f), chilled = MakeEnemy(1, hp: 1000f);
            frozen.Status.Add(new FreezeEffect());
            ChillEffect.Add(chilled, 10f);
            TowerController tower = MakeTower(1111);

            new IceSplashSkill(m_grid, m_occupancy).Fire(tower, frozen);

            Assert.AreEqual(SkillDamage(tower) * 2.5f, Lost(frozen), Eps);
            Assert.AreEqual(SkillDamage(tower), Lost(chilled), Eps, "이번 블리자드로 빙결된 적은 +150%를 받지 않음");
            Assert.IsTrue(FreezeEffect.IsFrozen(chilled));
        }

        [Test]
        public void 아이시클_레인은_한기면_치명타_빙결이면_치명타_피해_300퍼센트_더()
        {
            EnemyController chilled = MakeEnemy(0, hp: 1000f), frozen = MakeEnemy(1, hp: 1000f);
            ChillEffect.Add(chilled, 10f);
            frozen.Status.Add(new FreezeEffect());
            TowerController tower = MakeTower(1211);
            var skill = new IceTargetSkill(m_grid, m_occupancy);

            skill.Fire(tower, chilled);
            skill.Fire(tower, frozen);

            Assert.AreEqual(SkillDamage(tower) * 2f, Lost(chilled), Eps);
            Assert.AreEqual(SkillDamage(tower) * (2f + 3f), Lost(frozen), Eps);
        }

        // ── 전기 ──

        [Test]
        public void 라이트닝_볼텍스는_맞은_적에게_전하_티어_곱하기_레벨_곱하기_최대_차지()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            TowerController tower = MakeTower(1112);

            new ElectricSplashSkill(m_grid, m_occupancy).Fire(tower, enemy);

            Assert.AreEqual(SkillDamage(tower), Lost(enemy), Eps);
            Assert.AreEqual(1f * 1f * 60f, ChargeEffect.Get(enemy), Eps);
        }

        [Test]
        public void 오버로드는_전하만큼_세게_치고_전하를_0으로()
        {
            EnemyController enemy = MakeEnemy(0, hp: 1000f);
            ChargeEffect.Add(enemy, 100f);
            TowerController tower = MakeTower(1212);

            new ElectricTargetSkill(m_grid, m_occupancy).Fire(tower, enemy);

            Assert.AreEqual(SkillDamage(tower) * 2f * 1.01f, Lost(enemy), Eps, "× (1 + 전하 100 × 0.01), 맞는 순간엔 전하의 받는 피해 +1%도");
            Assert.AreEqual(0f, ChargeEffect.Get(enemy), Eps);
        }

        // ── 빛 ──

        [Test]
        public void 프리즘은_아군_공격_타워의_공격력과_치명타를_올림()
        {
            TowerController tower = MakeTower(1113);

            new LightSplashSkill(m_grid, m_occupancy).Fire(tower, null);

            Assert.AreEqual(12f, tower.Stats.Power, Eps);
            Assert.AreEqual(0.2f, tower.Stats.CriticalRate, Eps);
            Assert.AreEqual(0.2f, tower.Stats.CriticalDamage, Eps);
            Assert.AreEqual(1, TimedTowerBuff.GetRemaining(tower.State, LightSplashSkill.Source));
        }

        [Test]
        public void 프리즘은_초월_웨이브에_2배와_AttackCount_Mastery_1()
        {
            StartTranscendWave();
            TowerController tower = MakeTower(1113);

            new LightSplashSkill(m_grid, m_occupancy).Fire(tower, null);

            Assert.AreEqual(14f, tower.Stats.Power, Eps);
            Assert.AreEqual(2, tower.Stats.AttackCount);
            Assert.AreEqual(2, tower.Stats.Mastery);
        }

        [Test]
        public void 걸려_있는_프리즘도_초월_웨이브_동안만_2배()
        {
            TowerController tower = MakeTower(3113);
            new LightSplashSkill(m_grid, m_occupancy).Fire(tower, null);
            float rate = tower.State.GetCounter(LightSplashSkill.RateKey);
            float power = tower.Stats.GetBase(StatType.Power);
            int attackCount = tower.Stats.AttackCount, mastery = tower.Stats.Mastery;
            Assert.AreEqual(power * (1f + rate), tower.Stats.Power, Eps, "보통 웨이브에 씀");

            StartTranscendWave();
            LightSplashSkill.RefreshTranscend(tower);
            Assert.AreEqual(power * (1f + rate * 2f), tower.Stats.Power, Eps, "초월 웨이브가 되면 2배");
            Assert.AreEqual(attackCount + 1, tower.Stats.AttackCount);
            Assert.AreEqual(mastery + 1, tower.Stats.Mastery);

            EmblemSharedState.Current.OnWaveStarted();
            LightSplashSkill.RefreshTranscend(tower);
            Assert.AreEqual(power * (1f + rate), tower.Stats.Power, Eps, "초월 웨이브가 끝나면 원래대로");
            Assert.AreEqual(attackCount, tower.Stats.AttackCount);
            Assert.AreEqual(mastery, tower.Stats.Mastery);
            Assert.AreEqual(3, TimedTowerBuff.GetRemaining(tower.State, LightSplashSkill.Source), "프리즘 자체는 남은 행동만큼 그대로");
        }

        [Test]
        public void 하이페리온은_0점1배에서_매_턴_2배()
        {
            EnemyController enemy = MakeEnemy(0, hp: 10000f);
            TowerController tower = MakeTower(3213);
            float damage = SkillDamage(tower);

            new LightTargetSkill(m_grid, m_occupancy).Fire(tower, enemy);
            for (int i = 0; i < 4; i++) enemy.Status.OnTurn();

            Assert.AreEqual(damage * (0.1f + 0.2f + 0.4f), Lost(enemy), Eps, "3티어 × 컬러 1 = 3턴");
            Assert.IsFalse(enemy.Status.Has(HyperionEffect.HyperionKey));
        }

        [Test]
        public void 하이페리온은_초월_웨이브에_맵_전체에_1배에서_매_턴_4배()
        {
            StartTranscendWave();
            EnemyController a = MakeEnemy(0, hp: 100000f), b = MakeEnemy(4, hp: 100000f);
            TowerController tower = MakeTower(1213);
            float damage = SkillDamage(tower);

            new LightTargetSkill(m_grid, m_occupancy).Fire(tower, a);
            a.Status.OnTurn();
            b.Status.OnTurn();

            Assert.AreEqual(damage, Lost(a), Eps);
            Assert.AreEqual(damage, Lost(b), Eps);
        }

        // ── 어둠 ──

        [Test]
        public void 블랙홀은_맞은_적의_칸에_남아_매_턴_0점3배()
        {
            var grounds = new GroundEffectRegistry();
            OccupyingFakeTarget inside = MakeTarget(0), outside = MakeTarget(1);
            TowerController tower = MakeTower(1114);

            new DarknessSplashSkill(m_grid, m_occupancy, grounds).Fire(tower, inside);
            Assert.AreEqual(1, grounds.All.Count);
            grounds.RunTurn();

            Assert.AreEqual(SkillDamage(tower) * DarknessSplashSkill.TurnRatio, inside.Hits[0].BaseDamage, Eps);
            Assert.AreEqual(0, outside.Hits.Count, "Mastery 1은 그 칸만");
            Assert.AreEqual(0, grounds.All.Count, "1턴 뒤 사라짐");
        }

        [Test]
        public void 심연은_침식_카운트_1당_1퍼센트_더()
        {
            EmblemSharedState.Current.AddDarkness(100f);
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1214);

            new DarknessTargetSkill(m_grid, m_occupancy).Fire(tower, target);

            Assert.AreEqual(SkillDamage(tower) * 2f, target.Hits[0].BaseDamage, Eps);
        }
    }
}
