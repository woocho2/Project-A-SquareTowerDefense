using NUnit.Framework;

namespace SquareTD.Tests
{
    // 새 공통 틀 (20번 4-1장): 버프·디버프의 스킬 1 + 스킬 2, 스킬 카운트, 차지, 키워드 스택, 판 공용 카운트
    //   타워: Power 10, AbilityValue 2, AttackCount 1, 최대 차지 60, SkillCount 5, 브론즈·컬러 1 → 스킬 1 값 10, 스킬 2 값 30, 유지 1번 행동
    public class SupportSkillTest : EmblemTestBase
    {
        private const float Eps = 0.0001f;

        // ── 버프 ──

        [Test]
        public void 버프_스킬1은_존_위_타워에게_Power퍼센트()
        {
            TowerController buff = MakeTower(1301, slot: 1);
            TowerController receiver = MakeTower(1101, slot: 0);

            Receive(MakeZone(buff, true), receiver);

            Assert.AreEqual(11f, receiver.Stats.Power, Eps, "10 × (1 + 10%)");
        }

        [Test]
        public void 같은_문양의_스킬1과_스킬2가_함께면_스킬2만()
        {
            TowerController buff = MakeTower(1301, slot: 1);
            TowerController receiver = MakeTower(1101, slot: 0);

            Receive(MakeZone(buff, true), receiver);
            ReceiveSkill2(buff, receiver);

            Assert.AreEqual(13f, receiver.Stats.Power, Eps, "스킬 2만: 10 × (1 + 30%). 스킬 1의 10%는 더하지 않음");
        }

        [Test]
        public void 스킬2가_끝나면_존의_스킬1로_돌아감()
        {
            TowerController buff = MakeTower(1301, slot: 1);
            TowerController receiver = MakeTower(1101, slot: 0);
            Receive(MakeZone(buff, true), receiver);
            ReceiveSkill2(buff, receiver);

            SupportBuffs.CountDown(receiver);

            Assert.AreEqual(11f, receiver.Stats.Power, Eps, "브론즈·컬러 1은 1번 행동 뒤에 끝남");
        }

        // ── 스킬 카운트 ──

        [Test]
        public void 버프_디버프_타워는_행동마다_스킬_카운트_AttackCount와_차지_1()
        {
            TowerController buff = MakeTower(1301);

            SupportSkillCount.Advance(buff, 1f);

            Assert.AreEqual(1f, buff.State.SkillCount, Eps);
            Assert.AreEqual(1f, buff.State.Stack.Current, Eps);
        }

        [Test]
        public void 치명이면_스킬_카운트가_AttackCount_곱하기_2_더하기_치명타_피해()
        {
            TowerController buff = MakeTower(1301, critRate: 1f);

            SupportSkillCount.Advance(buff, 0f);

            Assert.AreEqual(2f, buff.State.SkillCount, Eps);
        }

        [Test]
        public void 스킬_카운트가_SkillCount에_닿으면_스킬2를_쓰고_0부터()
        {
            TowerController buff = MakeTower(1301);

            int fires = 0;
            for (int i = 0; i < 5; i++) fires += SupportSkillCount.Advance(buff, 1f);

            Assert.AreEqual(1, fires);
            Assert.AreEqual(0f, buff.State.SkillCount, Eps);
        }

        [Test]
        public void 다_찬_차지는_스킬2에_쓰고_0으로()
        {
            TowerController buff = MakeTower(1301);
            buff.State.Stack.Add(60f);

            SupportCast cast = SupportSkillCount.TakeSkill2Cast(buff);

            Assert.AreEqual(42f, cast.Amount, Eps, "10 × (1 + 2 × (1 + 60 × 0.01))");
            Assert.AreEqual(0f, buff.State.Stack.Current, Eps);
        }

        // ── 디버프 ──

        [Test]
        public void 디버프_스킬1은_존을_벗어난_뒤부터_유지_시간을_셈()
        {
            TowerController debuff = MakeTower(1401);
            EnemyController enemy = MakeEnemy(0);

            Debuff(debuff, enemy);
            enemy.Status.OnActed();
            Assert.IsTrue(EmblemDebuffEffect.Has(enemy, TowerEmblem.Sword), "존(0번) 위에 서 있는 동안은 줄지 않음");

            MoveEnemyTo(enemy, 1);
            enemy.Status.OnActed();
            Assert.IsFalse(EmblemDebuffEffect.Has(enemy, TowerEmblem.Sword), "벗어난 뒤 1번 행동하면 꺼짐");
        }

        [Test]
        public void 디버프_스킬2가_걸려_있으면_스킬1은_걸리지_않음()
        {
            TowerController debuff = MakeTower(1401);
            EnemyController enemy = MakeEnemy(0);

            Debuff(debuff, enemy, skill2: true);
            Debuff(debuff, enemy);

            Assert.IsTrue(EmblemDebuffEffect.HasSkill2(enemy, TowerEmblem.Sword));
        }

        // ── 키워드 스택 ──

        [Test]
        public void 화상은_매_턴_최대_체력의_0점1_곱하기_스택퍼센트()
        {
            EnemyController enemy = MakeEnemy(0, hp: 100f);

            BurnEffect.Add(enemy, 10f);
            enemy.Status.OnTurn();

            Assert.AreEqual(99f, enemy.CurrentHp, Eps, "100 × 0.1% × 10");
        }

        [Test]
        public void 한기가_50에_닿으면_빙결()
        {
            EnemyController enemy = MakeEnemy(0);

            ChillEffect.Add(enemy, 49f);
            Assert.IsFalse(FreezeEffect.IsFrozen(enemy));

            ChillEffect.Add(enemy, 1f);
            Assert.IsTrue(FreezeEffect.IsFrozen(enemy));
        }

        [Test]
        public void 전하는_받는_피해를_전하_곱하기_0점01퍼센트_올림()
        {
            EnemyController enemy = MakeEnemy(0);

            ChargeEffect.Add(enemy, 100f);

            Assert.AreEqual(1.01f, enemy.Stats.Get(StatType.DamageTaken), Eps);
        }

        [Test]
        public void 침식인_적이_죽으면_스택만큼_침식_카운트()
        {
            EnemyController enemy = MakeEnemy(0, hp: 10f);
            ErosionEffect.Add(enemy, 5f);

            enemy.TakeDamage(new DamageInfo { BaseDamage = 100f, ForceFixed = true });

            Assert.AreEqual(5f, EmblemSharedState.Current.DarknessCount, Eps);
        }

        // ── 아군 쪽 스택과 판 공용 값 ──

        [Test]
        public void 가속_스택이_있으면_최대_Action_1_감소_버프_타워도()
        {
            TowerController attack = MakeTower(1101);
            TowerController buff = MakeTower(1301);

            Acceleration.Add(attack, 5f);
            Acceleration.Add(buff, 5f);

            Assert.AreEqual(2, attack.Stats.Action);
            Assert.AreEqual(2, buff.Stats.Action, "가속은 컬러와 무관하게 받음");
        }

        [Test]
        public void 초월_카운트가_20이_되면_다음_웨이브가_초월()
        {
            var shared = new EmblemSharedState();
            shared.AddTranscend(19f);

            shared.OnWaveStarted();
            Assert.IsFalse(shared.IsTranscendWave, "이 웨이브에 20이 됨");
            Assert.AreEqual(0f, shared.TranscendCount, Eps, "20이 되면 0으로");

            shared.OnWaveStarted();
            Assert.IsTrue(shared.IsTranscendWave);

            shared.OnWaveStarted();
            Assert.IsFalse(shared.IsTranscendWave, "연장하지 않으면 한 웨이브뿐");
        }

        [Test]
        public void 초월_웨이브에_연장하면_다음_웨이브도_초월_여러_번_써도_한_번()
        {
            var shared = new EmblemSharedState();
            shared.AddTranscend(20f);
            shared.OnWaveStarted();

            shared.ExtendTranscend();
            shared.ExtendTranscend();
            shared.OnWaveStarted();
            Assert.IsTrue(shared.IsTranscendWave);

            shared.OnWaveStarted();
            Assert.IsFalse(shared.IsTranscendWave);
        }
    }
}
