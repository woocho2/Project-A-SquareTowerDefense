using NUnit.Framework;

namespace SquareTD.Tests
{
    // 14문양의 버프·디버프 규칙 28개 (20번 4-3 ~ 4-16장)
    //   버프·디버프 타워: Power 10, AbilityValue 2, 브론즈·컬러 1 → 스킬 1 값 10 (10%), 스킬 2 값 30 (30%), 유지 1번 행동
    //   받는 공격 타워: Power 10, AV 2, AttackCount 1, 최대 Action 3, 사거리 2, 치명타 0. 적: 이동력 2 ~ 4, 행동 주기 3
    //   보드 밖(BoardManager·GameSession 없음)이라 한도는 20, 골드는 0
    public class EmblemRuleTest : EmblemTestBase
    {
        private const float Eps = 0.001f;

        private static DamageInfo Hit(float damage, AttackAttribute attribute = AttackAttribute.None, TowerController attacker = null,
                                      DamageSource source = DamageSource.Basic) =>
            new DamageInfo { BaseDamage = damage, Attribute = attribute, Attacker = attacker, Source = source };

        private void Buff1(int buffId, TowerController receiver) => Receive(MakeZone(MakeTower(buffId), true), receiver);
        private void Buff2(int buffId, TowerController receiver) => ReceiveSkill2(MakeTower(buffId), receiver);

        // ── 검 ──

        [Test]
        public void 검_버프_브레이브_소드는_공격력()
        {
            TowerController receiver = MakeTower(1103);
            Buff1(1301, receiver);
            Assert.AreEqual(11f, receiver.Stats.Power, Eps);
            Assert.AreEqual(0f, receiver.Stats.Get(StatType.FinalDamage), Eps);
        }

        [Test]
        public void 검_버프_글로리_소드는_최종_피해와_베기_치명타_확률()
        {
            TowerController slash = MakeTower(1101), pierce = MakeTower(1103);
            Buff2(1301, slash);
            Buff2(1301, pierce);

            Assert.AreEqual(13f, slash.Stats.Power, Eps);
            Assert.AreEqual(0.1f, slash.Stats.Get(StatType.FinalDamage), Eps);
            Assert.AreEqual(0.1f, slash.Stats.CriticalRate, Eps, "베기 시너지");
            Assert.AreEqual(0f, pierce.Stats.CriticalRate, Eps);
        }

        [Test]
        public void 검_디버프는_최대_체력을_깎고_현재_체력도_같은_비율로()
        {
            EnemyController enemy = MakeEnemy(hp: 100f);
            Debuff(MakeTower(1401), enemy);
            Assert.AreEqual(90f, enemy.MaxHp, Eps);
            Assert.AreEqual(90f, enemy.CurrentHp, Eps);
        }

        [Test]
        public void 검_디버프_둠_레퀴엠은_매_턴_최대_체력의_1퍼센트()
        {
            EnemyController enemy = MakeEnemy(hp: 100f);
            Debuff(MakeTower(1401), enemy, skill2: true);
            Assert.AreEqual(70f, enemy.MaxHp, Eps);

            enemy.Status.OnTurn();
            Assert.AreEqual(0.7f, Lost(enemy), Eps);
        }

        [Test]
        public void 검_디버프_둠_레퀴엠인_적은_베기_피해의_20퍼센트가_고정_피해()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f, defense: 100f);
            Debuff(MakeTower(1401), enemy, skill2: true);

            Assert.AreEqual(60f, enemy.PreviewDamage(Hit(100f, AttackAttribute.Slash)), Eps, "고정 20 + 80 × 방어 계수 0.5");
            Assert.AreEqual(50f, enemy.PreviewDamage(Hit(100f, AttackAttribute.Pierce)), Eps);
        }

        // ── 도끼 ──

        [Test]
        public void 도끼_버프는_치명타_피해_웰컴_투_발할라는_Action_1_줄이고_베기는_더()
        {
            TowerController one = MakeTower(1103), slash = MakeTower(1102);
            Buff1(1302, one);
            Buff2(1302, slash);

            Assert.AreEqual(0.1f, one.Stats.CriticalDamage, Eps);
            Assert.AreEqual(3, one.Stats.Action);
            Assert.AreEqual(0.5f, slash.Stats.CriticalDamage, Eps, "30% + 베기 20%");
            Assert.AreEqual(2, slash.Stats.Action);
        }

        [Test]
        public void 도끼_디버프_데스_센텐스는_밟은_턴에_기준_미만이면_즉사()
        {
            EnemyController low = MakeEnemy(hp: 100f), high = MakeEnemy(hp: 100f);
            low.TakeDamage(Hit(95f));
            high.TakeDamage(Hit(80f));

            TowerController tower = MakeTower(1402);
            Debuff(tower, low);
            Debuff(tower, high);

            Assert.IsFalse(low.IsAlive, "체력 5% < 10%");
            Assert.IsTrue(high.IsAlive, "체력 20%");
        }

        [Test]
        public void 도끼_디버프_엑스큐션은_피해를_받을_때마다_판정하고_받는_피해_10퍼센트()
        {
            EnemyController executed = MakeEnemy(hp: 100f), alive = MakeEnemy(hp: 100f);
            TowerController tower = MakeTower(1402);
            Debuff(tower, executed, skill2: true);
            Debuff(tower, alive, skill2: true);

            Assert.AreEqual(110f, alive.PreviewDamage(Hit(100f)), Eps);
            executed.TakeDamage(Hit(70f));
            alive.TakeDamage(Hit(50f));

            Assert.IsFalse(executed.IsAlive, "77 피해 → 23% < 30%");
            Assert.IsTrue(alive.IsAlive, "55 피해 → 45%");
        }

        [Test]
        public void 도끼_디버프_엑스큐션은_베기에_맞으면_기준이_10퍼센트_높음()
        {
            EnemyController bySlash = MakeEnemy(hp: 100f), byPierce = MakeEnemy(hp: 100f);
            TowerController tower = MakeTower(1402);
            Debuff(tower, bySlash, skill2: true);
            Debuff(tower, byPierce, skill2: true);

            bySlash.TakeDamage(Hit(56f, AttackAttribute.Slash));
            byPierce.TakeDamage(Hit(56f, AttackAttribute.Pierce));

            Assert.IsFalse(bySlash.IsAlive, "38.4% < 40%");
            Assert.IsTrue(byPierce.IsAlive, "38.4% > 30%");
        }

        // ── 활 ──

        [Test]
        public void 활_버프_엘븐_사이트는_거리_비례_추가_피해()
        {
            TowerController receiver = MakeTower(1101);
            Buff1(1303, receiver);

            Assert.AreEqual(1.1f, receiver.Stats.GetDistanceMultiplier(1), Eps);
            Assert.AreEqual(1.2f, receiver.Stats.GetDistanceMultiplier(2), Eps);
            Assert.AreEqual(1.2f, receiver.Stats.GetDistanceMultiplier(5), Eps, "최대치는 사거리 끝");
        }

        [Test]
        public void 활_버프_스피릿_보우는_사거리_1_늘리고_찌르기는_항상_최대치()
        {
            TowerController pierce = MakeTower(1103);
            Buff2(1303, pierce);

            Assert.AreEqual(3, pierce.Stats.Range);
            Assert.AreEqual(1f + 0.3f * 3f, pierce.Stats.GetDistanceMultiplier(0), Eps);
        }

        [Test]
        public void 활_디버프_락_온은_받는_최종_피해()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1403), enemy);
            Assert.AreEqual(110f, enemy.PreviewDamage(Hit(100f)), Eps);
        }

        [Test]
        public void 활_디버프_핀_포인트는_받는_치명타_확률과_찌르기_추가타()
        {
            EnemyController pierced = MakeEnemy(hp: 1000f), slashed = MakeEnemy(hp: 1000f);
            TowerController tower = MakeTower(1403);
            Debuff(tower, pierced, skill2: true);
            Debuff(tower, slashed, skill2: true);

            Assert.AreEqual(0.1f, pierced.Status.GetCritRateBonus(null), Eps);
            pierced.TakeDamage(Hit(100f, AttackAttribute.Pierce));
            slashed.TakeDamage(Hit(100f, AttackAttribute.Slash));

            Assert.AreEqual(130f + 26f, Lost(pierced), Eps, "130 + 그 피해의 20%");
            Assert.AreEqual(130f, Lost(slashed), Eps);
        }

        // ── 창 ──

        [Test]
        public void 창_버프_원_스트라이크는_치명타_확률과_방어_관통()
        {
            TowerController receiver = MakeTower(1101);
            Buff2(1304, receiver);

            Assert.AreEqual(1, receiver.Stats.AttackCount);
            Assert.AreEqual(13f, receiver.Stats.Power, Eps, "AttackCount 1이라 모아 찌르기 × 1, 공격력 +30%");
            Assert.AreEqual(0.1f, receiver.Stats.CriticalRate, Eps);
            Assert.AreEqual(0.1f, receiver.Stats.ArmorPenetration, Eps);
        }

        [Test]
        public void 창_버프_원_스트라이크를_받는_찌르기는_맞은_적의_치유를_40퍼센트_깎음()
        {
            TowerController owner = MakeTower(1304), pierce = MakeTower(1103);
            EnemyController enemy = MakeEnemy();

            SkillRegistry.GetBuffRule(TowerEmblem.Spear).OnReceiverHit(SupportCast.Skill2(owner, false), pierce, enemy);

            Assert.AreEqual(0.6f, enemy.Stats.Get(StatType.HealingTaken), Eps);
        }

        [Test]
        public void 창_디버프는_받은_피해의_일부가_출혈로_다시_들어옴()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1404), enemy);

            enemy.TakeDamage(Hit(100f));
            enemy.Status.OnTurn();
            Assert.AreEqual(110f, Lost(enemy), Eps, "100 + 출혈 10 (1턴)");

            enemy.Status.OnTurn();
            Assert.AreEqual(110f, Lost(enemy), Eps, "출혈 턴이 끝남");
        }

        [Test]
        public void 창_디버프_헤머리지는_이동력_1_줄이고_찌르기의_출혈은_2배()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1404), enemy, skill2: true);

            Assert.AreEqual(1f, enemy.Stats.Get(StatType.SpeedMin), Eps);
            Assert.AreEqual(3f, enemy.Stats.Get(StatType.SpeedMax), Eps);

            enemy.TakeDamage(Hit(100f, AttackAttribute.Pierce));
            enemy.Status.OnTurn();
            Assert.AreEqual(160f, Lost(enemy), Eps, "100 + 100 × 30% × 2");
        }

        // ── 방패 ──

        [Test]
        public void 방패_버프는_한도_비례_최종_피해()
        {
            TowerController receiver = MakeTower(1101);
            Buff1(1305, receiver);
            Assert.AreEqual(1f + 0.1f * 20f, receiver.Stats.FinalDamageMultiplier, Eps, "한도 20");
        }

        [Test]
        public void 방패_버프_이지스는_한도_50퍼센트와_타격의_공격력()
        {
            TowerController blunt = MakeTower(1105);
            Buff2(1305, blunt);

            Assert.AreEqual(0.3f * 20f, blunt.Stats.Get(StatType.FinalDamage), Eps);
            Assert.AreEqual(10f * (1f + 0.002f * 20f), blunt.Stats.Power, Eps, "타격 시너지 +(한도 × 0.2)%");
            Assert.AreEqual(10, SkillRegistry.GetBuffRule(TowerEmblem.Shield).GetLimitModifier(20));
        }

        [Test]
        public void 방패_디버프_서프레스는_저항력을_깎음()
        {
            EnemyController enemy = MakeEnemy(resistance: 30f);
            Debuff(MakeTower(1405), enemy);
            Assert.AreEqual(30f - 10f * 0.7f, enemy.Resistance.Effective, Eps, "깎는 양 10%p도 저항력만큼 줄어 7%p");
        }

        [Test]
        public void 방패_디버프_락다운은_최대_이동력_2_줄이고_타격에_받는_피해_5퍼센트()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1405), enemy, skill2: true);

            Assert.AreEqual(2f, enemy.Stats.Get(StatType.SpeedMax), Eps);
            Assert.AreEqual(105f, enemy.PreviewDamage(Hit(100f, AttackAttribute.Blunt)), Eps);
            Assert.AreEqual(100f, enemy.PreviewDamage(Hit(100f, AttackAttribute.Slash)), Eps);
        }

        // ── 해머 ──

        [Test]
        public void 해머_버프는_현재_골드_비례_추가_피해와_골든_터치의_골드()
        {
            var rule = new HammerBuffRule(() => 100);
            TowerController owner = MakeTower(1306), blunt = MakeTower(1105);

            var gilding = rule.GetModifiers(SupportCast.Skill1(owner), blunt, "s");
            var touch = rule.GetModifiers(SupportCast.Skill2(owner, false), blunt, "s");

            Assert.AreEqual(0.1f * 100f, Sum(gilding, StatType.BonusDamage), Eps);
            Assert.AreEqual(0f, Sum(gilding, StatType.GoldBonus), Eps);
            Assert.AreEqual(0.5f, Sum(touch, StatType.GoldBonus), Eps);
            Assert.AreEqual(0.5f, Sum(touch, StatType.GoldDoubleChance), Eps, "타격 시너지");
        }

        [Test]
        public void 해머_디버프_크랙은_방어력_잭팟은_죽으면_젬()
        {
            EnemyController cracked = MakeEnemy(defense: 100f), jackpot = MakeEnemy(defense: 100f);
            TowerController tower = MakeTower(1406);
            Debuff(tower, cracked);
            Debuff(tower, jackpot, skill2: true);
            var kill = new KillInfo { Damage = Hit(1f, AttackAttribute.Slash) };

            Assert.AreEqual(90f, cracked.Stats.Get(StatType.Defense), Eps);
            Assert.AreEqual(0, EmblemDebuffEffect.On(cracked, TowerEmblem.Hammer).RollGemBonus(kill));
            Assert.AreEqual(70f, jackpot.Stats.Get(StatType.Defense), Eps);
            Assert.AreEqual(1, EmblemDebuffEffect.On(jackpot, TowerEmblem.Hammer).RollGemBonus(kill));
        }

        // ── 불 ──

        [Test]
        public void 불_버프_피닉스_페더는_공격_횟수_2배_공격력은_값_빼기_50()
        {
            TowerController receiver = MakeTower(1101);
            Buff1(1307, receiver);
            Assert.AreEqual(2, receiver.Stats.AttackCount);
            Assert.AreEqual(6f, receiver.Stats.Power, Eps, "10 × (1 + (10 − 50)%)");
        }

        [Test]
        public void 불_버프_라이징_피닉스는_공격력_10퍼센트와_불_타워_치명타_피해()
        {
            TowerController fire = MakeTower(1107);
            Buff2(1307, fire);
            Assert.AreEqual(9f, fire.Stats.Power, Eps, "10 × (1 + (30 − 50)% + 10%)");
            Assert.AreEqual(0.2f, fire.Stats.CriticalDamage, Eps);
        }

        [Test]
        public void 불_디버프_이그나이트는_화상_스택과_치유_감소와_매_턴_피해()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1407), enemy);

            Assert.AreEqual(10f, BurnEffect.Get(enemy), Eps);
            Assert.AreEqual(0.8f, enemy.Stats.Get(StatType.HealingTaken), Eps);
            enemy.Status.OnTurn();
            Assert.AreEqual(1000f * 0.001f * 10f, Lost(enemy), Eps);
        }

        [Test]
        public void 불_디버프_인페르노는_화상_피해_2배_불_타워에게_맞으면_화상_더()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1407), enemy, skill2: true);

            enemy.Status.OnTurn();
            Assert.AreEqual(1000f * 0.001f * 30f * 2f, Lost(enemy), Eps);

            enemy.TakeDamage(Hit(0f, attacker: MakeTower(1107)));
            Assert.AreEqual(31f, BurnEffect.Get(enemy), Eps, "+(Tier × 레벨) = 1");
        }

        // ── 물 ──

        [Test]
        public void 물_버프는_차지_쌓이는_양_오버플로우는_차지_배율과_물_타워의_잃은_체력_비례()
        {
            TowerController one = MakeTower(1101), water = MakeTower(1108);
            Buff1(1308, one);
            Buff2(1308, water);

            Assert.AreEqual(0.1f, one.Stats.Get(StatType.StackGainRate), Eps);
            Assert.AreEqual(0f, one.Stats.Get(StatType.ChargeBonus), Eps);
            Assert.AreEqual(0.5f, water.Stats.Get(StatType.ChargeBonus), Eps);
            Assert.AreEqual(0.1f, water.Stats.Get(StatType.LostHpDamage), Eps);
        }

        [Test]
        public void 물_디버프_미스트는_젖음_10마다_최대_이동력_1()
        {
            EnemyController enemy = MakeEnemy();
            Debuff(MakeTower(1408), enemy);
            Assert.AreEqual(10f, WetEffect.Get(enemy), Eps);
            Assert.AreEqual(3f, enemy.Stats.Get(StatType.SpeedMax), Eps);
        }

        [Test]
        public void 물_디버프_세이렌_송은_받는_피해_물_타워에게_더_맞으면_미끄러짐()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1408), enemy, skill2: true);
            TowerController water = MakeTower(1108);

            Assert.AreEqual(110f, enemy.PreviewDamage(Hit(100f)), Eps);
            Assert.AreEqual(120f, enemy.PreviewDamage(Hit(100f, attacker: water)), Eps);

            enemy.TakeDamage(Hit(1f, attacker: water));
            MoveRange range = enemy.Status.DecideMove(new MoveRange(2, 4, 3));
            Assert.AreEqual(4, range.Min);
            Assert.AreEqual(6, range.Max);
        }

        // ── 대지 ──

        [Test]
        public void 대지_버프는_AbilityValue_블로섬은_SkillCount를_Tier만큼_줄이고_대지_타워는_성장()
        {
            TowerController one = MakeTower(1101), earth = MakeTower(1109);
            TowerController owner = MakeTower(1309);
            Buff1(1309, one);
            ReceiveSkill2(owner, earth);

            Assert.AreEqual(2.2f, one.Stats.AbilityValue, Eps);
            Assert.AreEqual(4f, earth.Stats.SkillCount, Eps, "5 − 1티어");

            SkillRegistry.GetBuffRule(TowerEmblem.Earth).OnReceiverActed(SupportCast.Skill2(owner, false), earth);
            Assert.AreEqual(1f, Growth.Get(earth), Eps);
        }

        [Test]
        public void 대지_디버프_루트_드레인은_밟을_때마다_성장하고_그만큼_최대_체력과_방어력을_깎음()
        {
            EnemyController enemy = MakeEnemy(hp: 100f, defense: 100f);
            TowerController tower = MakeTower(1409);
            Debuff(tower, enemy);

            Assert.AreEqual(1f, Growth.Get(tower), Eps);
            Assert.AreEqual(90f, enemy.MaxHp, Eps, "10% × 성장 1");
            Assert.AreEqual(90f, enemy.Stats.Get(StatType.Defense), Eps);
        }

        [Test]
        public void 대지_디버프_라이프_하베스트는_적응형_능력치_1을_줄이고_대지_타워의_성장만큼_추가_피해()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1409), enemy, skill2: true);
            TowerController earth = MakeTower(1109);
            Growth.Add(earth, 5f);

            Assert.AreEqual(2, EarthDebuffRule.AdaptiveFor(enemy, 3));
            Assert.AreEqual(1, EarthDebuffRule.AdaptiveFor(enemy, 1), "최소 1");

            enemy.TakeDamage(Hit(0f, attacker: earth));
            Assert.AreEqual(5f, Lost(enemy), Eps);
        }

        [Test]
        public void 대지_디버프의_양분은_디버프_타워_자신을_빼고_모든_아군에게()
        {
            var slots = new TowerSlots(3);
            TowerController debuff = MakeTower(1409, slot: 0), attack = MakeTower(1101, slot: 1), buff = MakeTower(1301, slot: 2);
            slots.TryPlace(debuff, 0);
            slots.TryPlace(attack, 1);
            slots.TryPlace(buff, 2);

            Growth.AddToOthers(slots, debuff, SupportCast.Skill1(debuff).Amount);

            Assert.AreEqual(10f, Growth.Get(attack), Eps, "루트 드레인 +Power");
            Assert.AreEqual(10f, Growth.Get(buff), Eps, "문양·컬러 무관");
            Assert.AreEqual(0f, Growth.Get(debuff), Eps);
        }

        [Test]
        public void 보스는_자물쇠_면의_받는_피해_증가가_없음()
        {
            Assert.AreEqual(0, EnemyCubeFaces.GetModifiers(EnemyCubeFaceAsset.Locked, 3, isBoss: true).Length, "그 자리는 보스 고유 스킬");
            Assert.AreEqual(1, EnemyCubeFaces.GetModifiers(EnemyCubeFaceAsset.Hourglass, 3, isBoss: true).Length, "받는 피해 감소는 보스도 받음");
            Assert.AreEqual(1, EnemyCubeFaces.GetModifiers(EnemyCubeFaceAsset.Defense, 3, isBoss: true).Length, "다른 면도 보스가 받음");
            Assert.AreEqual(1, EnemyCubeFaces.GetModifiers(EnemyCubeFaceAsset.Locked, 3).Length, "일반 적은 받음");
        }

        // ── 바람 ──

        [Test]
        public void 바람_버프는_Action_1_줄이고_AttackCount_1_늘리는_대신_치명타를_바꿈()
        {
            TowerController one = MakeTower(1101), two = MakeTower(1103);
            Buff1(1310, one);
            Buff2(1310, two);

            Assert.AreEqual(2, one.Stats.Action);
            Assert.AreEqual(2, one.Stats.AttackCount);
            Assert.AreEqual(-0.04f, one.Stats.CriticalRate, Eps, "(10 × 0.1 − 5)%p");
            Assert.AreEqual(10f, one.Stats.Power, Eps);
            Assert.AreEqual(11f, two.Stats.Power, Eps, "게일 포스 Power +10%");
            Assert.AreEqual(-0.02f, two.Stats.CriticalRate, Eps);
        }

        [Test]
        public void 바람_디버프는_다음_이동을_5_늘리고_이동한_칸만큼_피해()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1410), enemy);

            MoveRange range = enemy.Status.DecideMove(new MoveRange(2, 4, 3));
            Assert.AreEqual(7, range.Min);
            Assert.AreEqual(9, range.Max);

            enemy.Status.OnMoved(3);
            Assert.AreEqual(30f, Lost(enemy), Eps, "3칸 × 10");
            enemy.Status.OnMoved(3);
            Assert.AreEqual(30f, Lost(enemy), Eps, "다음 이동 한 번만");
        }

        [Test]
        public void 바람_디버프의_피해도_저항력만큼_줄어듦()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f, resistance: 50f);
            Debuff(MakeTower(1410), enemy);
            enemy.Status.OnMoved(3);
            Assert.AreEqual(15f, Lost(enemy), Eps);
        }

        [Test]
        public void 바람_디버프_터뷸런스는_최대_Action_3()
        {
            EnemyController enemy = MakeEnemy();
            Debuff(MakeTower(1410), enemy, skill2: true);
            Assert.AreEqual(6f, enemy.Stats.Get(StatType.MaxAction), Eps);
        }

        // ── 얼음 ──

        [Test]
        public void 얼음_버프는_Action_4_늘리는_대신_공격력과_치명타()
        {
            TowerController receiver = MakeTower(1101);
            Buff1(1311, receiver);

            Assert.AreEqual(7, receiver.Stats.Action);
            Assert.AreEqual(11f, receiver.Stats.Power, Eps);
            Assert.AreEqual(0.1f, receiver.Stats.CriticalRate, Eps);
            Assert.AreEqual(0.1f, receiver.Stats.CriticalDamage, Eps);
        }

        [Test]
        public void 얼음_버프_앱솔루트_제로는_Mastery_1_얼음_타워는_Action_2만()
        {
            TowerController ice = MakeTower(1111);
            Buff2(1311, ice);
            Assert.AreEqual(5, ice.Stats.Action);
            Assert.AreEqual(2, ice.Stats.Mastery);
        }

        [Test]
        public void 얼음_디버프_프로스트바이트는_한기_스택이고_50마다_빙결()
        {
            EnemyController enemy = MakeEnemy();
            Debuff(MakeTower(1411), enemy);

            Assert.IsTrue(ChillEffect.IsChilled(enemy));
            Assert.AreEqual(1f, enemy.Stats.Get(StatType.SpeedMin), Eps);
            Assert.AreEqual(3f, enemy.Stats.Get(StatType.SpeedMax), Eps);
            Assert.IsFalse(FreezeEffect.IsFrozen(enemy));

            ChillEffect.Add(enemy, 40f);
            Assert.IsTrue(FreezeEffect.IsFrozen(enemy), "한기 50");
        }

        [Test]
        public void 얼음_디버프_딥_프리즈는_한기를_모두_없애고_빙결_받는_피해_50퍼센트()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            TowerController tower = MakeTower(1411);
            Debuff(tower, enemy);
            Debuff(tower, enemy, skill2: true);

            Assert.IsFalse(ChillEffect.IsChilled(enemy));
            Assert.IsTrue(FreezeEffect.IsFrozen(enemy));
            Assert.AreEqual(150f, enemy.PreviewDamage(Hit(100f)), Eps);
        }

        // ── 전기 ──

        [Test]
        public void 전기_버프_펄스는_AttackCount_3배_대신_공격력과_치명타를_깎고_타격마다_전하()
        {
            TowerController owner = MakeTower(1312), receiver = MakeTower(1101);
            Receive(MakeZone(owner, true), receiver);
            EnemyController enemy = MakeEnemy();

            Assert.AreEqual(3, receiver.Stats.AttackCount);
            Assert.AreEqual(1f, receiver.Stats.Power, Eps, "(10 − 100)% = −90%");
            Assert.AreEqual(-0.9f, receiver.Stats.CriticalRate, Eps);

            SkillRegistry.GetBuffRule(TowerEmblem.Electricity).OnReceiverHit(SupportCast.Skill1(owner), receiver, enemy);
            Assert.AreEqual(10f, ChargeEffect.Get(enemy), Eps);
        }

        [Test]
        public void 전기_버프_계산식이_100을_넘으면_공격력과_치명타가_오름()
        {
            TowerController owner = MakeTower(1312, power: 150f), receiver = MakeTower(1101);
            Receive(MakeZone(owner, true), receiver);

            Assert.AreEqual(3, receiver.Stats.AttackCount);
            Assert.AreEqual(15f, receiver.Stats.Power, Eps, "(150 − 100)% = +50%");
            Assert.AreEqual(0.5f, receiver.Stats.CriticalRate, Eps);
        }

        [Test]
        public void 전기_버프_하이퍼_펄스를_받는_전기_타워는_전하_2배()
        {
            TowerController owner = MakeTower(1312), electric = MakeTower(1112);
            EnemyController enemy = MakeEnemy();

            SkillRegistry.GetBuffRule(TowerEmblem.Electricity).OnReceiverHit(SupportCast.Skill2(owner, false), electric, enemy);
            Assert.AreEqual(60f, ChargeEffect.Get(enemy), Eps);
        }

        [Test]
        public void 전기_디버프는_전하이고_전하만큼_받는_피해()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1412), enemy);
            Assert.AreEqual(10f, ChargeEffect.Get(enemy), Eps);
            Assert.AreEqual(100.1f, enemy.PreviewDamage(Hit(100f)), Eps, "전하 10 × 0.01%");
        }

        // ── 빛 ──

        [Test]
        public void 빛_버프는_추가_피해_어센션은_초월_카운트_1()
        {
            TowerController owner = MakeTower(1313), receiver = MakeTower(1101);
            Receive(MakeZone(owner, true), receiver);
            Assert.AreEqual(1.1f, receiver.Stats.FinalDamageMultiplier, Eps);

            float before = EmblemSharedState.Current.TranscendCount;
            SkillRegistry.GetBuffRule(TowerEmblem.Light).OnSkill2(SupportCast.Skill2(owner, false));
            Assert.AreEqual(before + 1f, EmblemSharedState.Current.TranscendCount, Eps);
        }

        [Test]
        public void 빛_버프_어센션을_받는_빛_타워가_행동하면_버프_타워의_스킬_카운트_1()
        {
            TowerController owner = MakeTower(1313), light = MakeTower(1113);
            SkillRegistry.GetBuffRule(TowerEmblem.Light).OnReceiverActed(SupportCast.Skill2(owner, false), light);
            Assert.AreEqual(1f, owner.State.SkillCount, Eps);
        }

        [Test]
        public void 빛_디버프_쉬머는_실명으로_뒤로_걷게_하고_타워의_실명_100마다_초월_카운트()
        {
            EnemyController enemy = MakeEnemy();
            TowerController tower = MakeTower(1413);
            Debuff(tower, enemy);

            MoveRange range = enemy.Status.DecideMove(new MoveRange(2, 4, 3));
            Assert.AreEqual(-4, range.Min);
            Assert.AreEqual(-2, range.Max);
            Assert.AreEqual(10f, tower.State.GetCounter(LightDebuffRule.TowerBlindKey), Eps);

            float before = EmblemSharedState.Current.TranscendCount;
            LightDebuffRule.AddTowerBlind(tower, 90f);
            Assert.AreEqual(before + 1f, EmblemSharedState.Current.TranscendCount, Eps, "실명 100 → +Tier");
        }

        [Test]
        public void 빛_디버프_배니시는_최대_체력_10퍼센트와_빛_타워의_최종_피해_20퍼센트()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1413), enemy, skill2: true);

            Assert.AreEqual(900f, enemy.MaxHp, Eps);
            Assert.AreEqual(120f, enemy.PreviewDamage(Hit(100f, attacker: MakeTower(1113))), Eps);
            Assert.AreEqual(100f, enemy.PreviewDamage(Hit(100f, attacker: MakeTower(1101))), Eps);
        }

        // ── 어둠 ──

        [Test]
        public void 어둠_버프는_침식_카운트의_단계까지_효과를_모두_받음()
        {
            TowerController owner = MakeTower(1314), receiver = MakeTower(1101);
            var below = new DarknessBuffRule(() => 99f).GetModifiers(SupportCast.Skill1(owner), receiver, "s");
            var third = new DarknessBuffRule(() => 300f).GetModifiers(SupportCast.Skill1(owner), receiver, "s");
            var all = new DarknessBuffRule(() => 1000f).GetModifiers(SupportCast.Skill1(owner), receiver, "s");

            Assert.AreEqual(0, below.Count);
            Assert.AreEqual(3, third.Count);
            Assert.AreEqual(0.1f, Sum(third, StatType.CritDamage), Eps);
            Assert.AreEqual(10, all.Count);
            Assert.AreEqual(1f, Sum(all, StatType.ForceFixedDamage), Eps, "카운트 900");
        }

        [Test]
        public void 어둠_버프는_행동마다_스킬_1_글러트니는_발동할_때_스킬_2만큼_카운트()
        {
            TowerController owner = MakeTower(1314);
            BuffRule rule = SkillRegistry.GetBuffRule(TowerEmblem.Darkness);

            rule.OnOwnerActed(owner);
            Assert.AreEqual(10f, EmblemSharedState.Current.DarknessCount, Eps);
            rule.OnSkill2(SupportCast.Skill2(owner, false));
            Assert.AreEqual(40f, EmblemSharedState.Current.DarknessCount, Eps);
        }

        [Test]
        public void 어둠_디버프_미아즈마는_침식과_같은_양의_카운트_침식인_적이_죽으면_카운트_더()
        {
            EnemyController enemy = MakeEnemy();
            Debuff(MakeTower(1414), enemy);

            Assert.AreEqual(10f, ErosionEffect.Get(enemy), Eps);
            Assert.AreEqual(10f, EmblemSharedState.Current.DarknessCount, Eps);

            enemy.Execute(Hit(0f));
            Assert.AreEqual(20f, EmblemSharedState.Current.DarknessCount, Eps);
        }

        [Test]
        public void 어둠_디버프_다크_고스펠은_침식인_적이_받는_피해_10퍼센트()
        {
            EnemyController enemy = MakeEnemy(hp: 1000f);
            Debuff(MakeTower(1414), enemy, skill2: true);
            Assert.AreEqual(110f, enemy.PreviewDamage(Hit(100f, attacker: MakeTower(1101))), Eps);
        }
    }
}
