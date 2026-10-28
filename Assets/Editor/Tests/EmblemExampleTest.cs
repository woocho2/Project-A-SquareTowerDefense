using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    // T8: 기획서(20번)의 문양별 예시값 (수업 27 ~ 40에서 문양마다 더함)
    public class EmblemExampleTest
    {
        private GameObject m_root;
        private BoardGrid m_grid;
        private PathOccupancy m_occupancy;
        private readonly List<Object> m_assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            m_root = new GameObject("EmblemExampleTest");
            m_grid = TestBoards.MakeGrid();
            m_occupancy = new PathOccupancy();
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_root);
            foreach (Object asset in m_assets) Object.DestroyImmediate(asset);
            m_assets.Clear();
        }

        // 위력 10, 스킬 계수 2, 최대 스택 60 → 스킬 피해 20 (AttackSkillTest와 같은 타워). 기획서 예시의 브론즈. 다이아는 power 150
        //   타워는 0번 스폰 칸 (0,1). 사거리 2면 길 0·1·3번이 거리 1, 2·4번이 거리 2(끝 칸)
        //   스폰 칸은 0번 (0,1), 1번 (3,0), 2번 (1,-1) — 0번에서 1번은 거리 3, 2번은 거리 2
        private TowerController MakeTower(int id, float critRate = 0f, float power = 10f, int range = 2, int slot = 0, AttackSkill skill = null, int duration = 5)
        {
            var go = new GameObject($"Tower {id}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, power, range, 3, 1, critRate, 0f, 1, 60, duration, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, slot, null, null, null, skill);
            return tower;
        }

        private OccupyingFakeTarget MakeTarget(int pathIndex, float hp = 1000f) =>
            new OccupyingFakeTarget(m_occupancy, pathIndex, hp, m_root.transform);

        private static float Total(OccupyingFakeTarget target)
        {
            float sum = 0f;
            foreach (DamageInfo hit in target.Hits) sum += hit.BaseDamage;
            return sum;
        }

        // ── 등록표 ──

        [Test]
        public void 등록표_검_광역은_보이드_슬래시()
        {
            Assert.IsInstanceOf<SwordSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Sword, m_grid, m_occupancy, null));
        }

        [Test]
        public void 등록표_검_단일은_블레이드_템페스트()
        {
            Assert.IsInstanceOf<SwordTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Sword, m_grid, m_occupancy, null));
        }

        [Test]
        public void 등록표에_아직_없는_문양은_null()
        {
            Assert.IsNull(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Fire, m_grid, m_occupancy, null));
            Assert.IsNull(SkillRegistry.CreateAttackSkill(TowerColor.Yellow, TowerEmblem.Sword, m_grid, m_occupancy, null), "버프 타워는 공격 스킬이 없음");
        }

        [Test]
        public void 등록표는_부를_때마다_새_스킬()
        {
            AttackSkill a = SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Sword, m_grid, m_occupancy, null);
            AttackSkill b = SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Sword, m_grid, m_occupancy, null);
            Assert.AreNotSame(a, b, "타워마다 자기 스킬을 가짐");
        }

        // ── 검 광역: 보이드 슬래시 ──

        [Test]
        public void 보이드_슬래시는_맵_전체에_한_번씩_피해만()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(2), c = MakeTarget(4);
            new SwordSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1101), null);

            foreach (OccupyingFakeTarget target in new[] { a, b, c })
            {
                Assert.AreEqual(1, target.Hits.Count, $"{target.PathIndex}번 칸");
                Assert.AreEqual(20f, target.Hits[0].BaseDamage, 0.0001f);
            }
        }

        [Test]
        public void 보이드_슬래시는_베기()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            new SwordSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1101), null);
            Assert.AreEqual(AttackAttribute.Slash, target.Hits[0].Attribute);
        }

        // ── 검 단일: 블레이드 템페스트 ──

        [Test]
        public void 블레이드_템페스트는_24번에_나누고_마지막은_2점4배()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201), target);

            Assert.AreEqual(24, target.Hits.Count);
            Assert.AreEqual(20f / 24f, target.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(20f / 24f * 2.4f, target.Hits[23].BaseDamage, 0.0001f, "마지막 일격");
            Assert.AreEqual(20f * 25.4f / 24f, Total(target), 0.001f, "(23 + 2.4) ÷ 24");
        }

        [Test]
        public void 블레이드_템페스트의_치명은_한_번에_24발_전부()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201, critRate: 1f), target);

            Assert.AreEqual(24, target.Hits.Count);
            foreach (DamageInfo hit in target.Hits) Assert.IsTrue(hit.IsCritical);
            Assert.AreEqual(40f * 25.4f / 24f, Total(target), 0.001f, "치명 배율 2");
        }

        [Test]
        public void 블레이드_템페스트는_스택을_쓰면_총_피해가_1점6배()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            TowerController tower = MakeTower(1201);
            tower.State.Stack.Add(60f);

            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, target);

            Assert.AreEqual(32f * 25.4f / 24f, Total(target), 0.001f);
        }

        [Test]
        public void 블레이드_템페스트는_발동시킨_적에게만()
        {
            OccupyingFakeTarget main = MakeTarget(2), other = MakeTarget(2);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201), main);

            Assert.AreEqual(24, main.Hits.Count);
            Assert.AreEqual(0, other.Hits.Count, "같은 칸이어도 다른 적은 안 맞음");
        }

        [Test]
        public void 블레이드_템페스트_도중에_죽으면_남은_조각은_안_들어감()
        {
            OccupyingFakeTarget target = MakeTarget(2, hp: 5f);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201), target);

            Assert.IsFalse(target.IsAlive);
            Assert.Less(target.Hits.Count, 24);
        }

        // ── 존 도우미 ──

        // 존 주인 타워와 쌓인 스택. 스택이 최대(60)면 스킬 2
        private static Zone MakeZone(TowerController owner, bool isBuff, float stack)
        {
            var zone = new Zone(owner, isBuff);
            zone.AddStack(stack, owner.Stats.Stack);
            return zone;
        }

        private static float Sum(List<StatModifier> modifiers, StatType stat)
        {
            float sum = 0f;
            foreach (StatModifier modifier in modifiers)
            {
                if (modifier.Stat == stat) sum += modifier.Value;
            }
            return sum;
        }

        // 최대 체력 100 (웨이브 1), 방어·저항은 고를 수 있음
        private EnemyController MakeEnemy(float defense = 0f, float resistance = 0f)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            m_assets.Add(definition);
            var so = new SerializedObject(definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 100f;
            so.FindProperty("<Defense>k__BackingField").floatValue = defense;
            so.FindProperty("<BaseResistance>k__BackingField").floatValue = resistance;
            so.ApplyModifiedPropertiesWithoutUndo();

            var go = new GameObject("Enemy");
            go.transform.SetParent(m_root.transform);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Setup(definition, null, 1);
            return enemy;
        }

        // ── 검 버프: 블레싱 · 글로리 ──

        [Test]
        public void 등록표_검_버프_디버프_규칙()
        {
            Assert.IsInstanceOf<SwordBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Sword));
            Assert.IsInstanceOf<SwordDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Sword));
            Assert.IsInstanceOf<TestBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Darkness), "아직 없는 문양은 시험 규칙");
        }

        [Test]
        public void 블레싱은_지금_스택만큼_공격력()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 30f);
            List<StatModifier> modifiers = new SwordBuffRule().GetModifiers(zone, MakeTower(1101), "검 버프존");

            Assert.AreEqual(0.006f, Sum(modifiers, StatType.Power), 0.00001f, "0.001 × 10 × 2 × 30 = 0.6%");
            Assert.AreEqual(0f, Sum(modifiers, StatType.FinalDamage), "최종 피해는 글로리만");
        }

        [Test]
        public void 글로리_브론즈는_1점2퍼센트_다이아는_18퍼센트()
        {
            Zone bronze = MakeZone(MakeTower(1301), true, 60f);
            Zone diamond = MakeZone(MakeTower(1301, power: 150f), true, 60f);
            TowerController receiver = MakeTower(1101);

            Assert.AreEqual(ZoneStage.Skill2, diamond.Stage);
            Assert.AreEqual(0.012f, Sum(new SwordBuffRule().GetModifiers(bronze, receiver, "검 버프존"), StatType.Power), 0.00001f);
            Assert.AreEqual(0.18f, Sum(new SwordBuffRule().GetModifiers(diamond, receiver, "검 버프존"), StatType.Power), 0.00001f);
        }

        [Test]
        public void 글로리는_최종_피해_10퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 60f);
            List<StatModifier> modifiers = new SwordBuffRule().GetModifiers(zone, MakeTower(1101), "검 버프존");
            Assert.AreEqual(0.1f, Sum(modifiers, StatType.FinalDamage), 0.00001f);
        }

        [Test]
        public void 베기_타워는_치명_확률이_블레싱_5퍼센트_글로리_10퍼센트()
        {
            TowerController receiver = MakeTower(1101);
            Zone skill1 = MakeZone(MakeTower(1301), true, 30f);
            Zone skill2 = MakeZone(MakeTower(1301), true, 60f);

            Assert.AreEqual(0.05f, Sum(new SwordBuffRule().GetModifiers(skill1, receiver, "검 버프존"), StatType.CritRate), 0.00001f);
            Assert.AreEqual(0.1f, Sum(new SwordBuffRule().GetModifiers(skill2, receiver, "검 버프존"), StatType.CritRate), 0.00001f);
        }

        [Test]
        public void 도끼도_베기라_치명_보너스를_받음()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 30f);
            Assert.AreEqual(0.05f, Sum(new SwordBuffRule().GetModifiers(zone, MakeTower(1102), "검 버프존"), StatType.CritRate), 0.00001f);
        }

        [Test]
        public void 베기가_아닌_타워는_치명_보너스_없음()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 60f);
            List<StatModifier> modifiers = new SwordBuffRule().GetModifiers(zone, MakeTower(1103), "검 버프존");   // 활 = 찌르기

            Assert.AreEqual(0f, Sum(modifiers, StatType.CritRate));
            Assert.AreEqual(0.012f, Sum(modifiers, StatType.Power), 0.00001f, "공격력은 누구나");
        }

        [Test]
        public void 검_버프존에_서면_스탯에_들어감()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 60f);
            TowerController receiver = MakeTower(1101);
            BuffZoneAction.GiveBuff(zone, receiver);

            Assert.AreEqual(10.12f, receiver.Stats.Power, 0.0001f, "10 × (1 + 1.2%)");
            Assert.AreEqual(0.1f, receiver.Stats.CriticalRate, 0.00001f);
            Assert.AreEqual(1.1f, receiver.Stats.FinalDamageMultiplier, 0.00001f);
        }

        [Test]
        public void 최종_피해는_나가는_피해에_곱해짐()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1101);
            tower.State.Modifiers.Add(StatModifier.Flat("검 버프존", StatType.FinalDamage, 0.1f));

            new SwordSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, null);
            Assert.AreEqual(22f, target.Hits[0].BaseDamage, 0.0001f, "20 × 1.1");
        }

        // ── 검 디버프: 커스 · 둠 ──

        [Test]
        public void 둠은_최대_체력_20퍼센트_현재_체력도_같은_비율()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp;
            enemy.Health.TakeDamage(max * 0.5f);                    // 절반 남음

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(max * 0.8f, enemy.MaxHp, 0.001f);
            Assert.AreEqual(max * 0.4f, enemy.CurrentHp, 0.001f, "절반 × 0.8");
        }

        [Test]
        public void 둠은_저항_50퍼센트면_최대_체력_10퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy(resistance: 50f);
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(max * 0.9f, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 커스는_최대_체력을_줄이지_않음()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 30f);
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(max, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 둠이_다시_걸려도_최대_체력은_한_번만_줄어듦()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(max * 0.8f, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 정화하면_줄었던_최대_체력이_돌아옴()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.RemoveAll();
            Assert.AreEqual(max, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 커스는_매_턴_최대_체력의_퍼센트_피해이고_방어를_무시함()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 30f);       // 0.6%
            EnemyController enemy = MakeEnemy(defense: 100f);
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.OnTurn();
            Assert.AreEqual(max - max * 0.006f, enemy.CurrentHp, 0.001f);
        }

        [Test]
        public void 둠의_매_턴_피해는_줄어든_최대_체력_기준()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);       // 1.2%
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp * 0.8f;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.OnTurn();
            Assert.AreEqual(max - max * 0.012f, enemy.CurrentHp, 0.001f);
        }

        [Test]
        public void 커스면_베기_피해의_10퍼센트가_고정_피해()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 30f);
            EnemyController enemy = MakeEnemy(defense: 100f);          // 방어 100 → 피해 절반
            var slash = new DamageInfo { BaseDamage = 100f, Attribute = AttackAttribute.Slash };
            Assert.AreEqual(50f, enemy.PreviewDamage(slash), 0.001f);

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(55f, enemy.PreviewDamage(slash), 0.001f, "고정 10 + 나머지 90의 절반 45");
        }

        [Test]
        public void 둠이면_베기_피해의_20퍼센트가_고정_피해()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy(defense: 100f);
            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));

            var slash = new DamageInfo { BaseDamage = 100f, Attribute = AttackAttribute.Slash };
            Assert.AreEqual(60f, enemy.PreviewDamage(slash), 0.001f, "고정 20 + 나머지 80의 절반 40");
        }

        [Test]
        public void 베기가_아니면_고정_피해로_바뀌지_않음()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy(defense: 100f);
            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));

            var pierce = new DamageInfo { BaseDamage = 100f, Attribute = AttackAttribute.Pierce };
            Assert.AreEqual(50f, enemy.PreviewDamage(pierce), 0.001f);
        }

        // ══ 도끼 (수업 28, 20번 4-4장) ══

        [Test]
        public void 등록표_도끼_넷()
        {
            Assert.IsInstanceOf<AxeSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Axe, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<AxeTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Axe, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<AxeBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Axe));
            Assert.IsInstanceOf<AxeDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Axe));
        }

        // ── 도끼 광역: 대회전 ──

        [Test]
        public void 대회전은_사거리_안_전체에_반드시_치명()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(1);
            new AxeSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1102, critRate: 0f), null);

            foreach (OccupyingFakeTarget target in new[] { a, b })
            {
                Assert.AreEqual(1, target.Hits.Count, $"{target.PathIndex}번 칸");
                Assert.IsTrue(target.Hits[0].IsCritical, "치명 확률 0이어도");
                Assert.AreEqual(40f, target.Hits[0].BaseDamage, 0.0001f, "20 × 치명 배율 2");
            }
        }

        [Test]
        public void 대회전은_사거리_끝_칸이_1점5배()
        {
            OccupyingFakeTarget inner = MakeTarget(0), edge = MakeTarget(2);
            new AxeSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1102), null);

            Assert.AreEqual(40f, inner.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(60f, edge.Hits[0].BaseDamage, 0.0001f, "40 × 1.5");
        }

        [Test]
        public void 대회전은_사거리_밖은_안_맞음()
        {
            OccupyingFakeTarget inside = MakeTarget(0), outside = MakeTarget(2);
            new AxeSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1102, range: 1), null);

            Assert.AreEqual(0, outside.Hits.Count, "거리 2는 사거리 1 밖");
            Assert.AreEqual(60f, inside.Hits[0].BaseDamage, 0.0001f, "사거리 1이면 거리 1이 끝 칸");
        }

        // ── 도끼 단일: 광폭화 ──

        [Test]
        public void 광폭화는_피해_없이_자기에게_버프()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            TowerController tower = MakeTower(1202);
            new AxeTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, target);

            Assert.AreEqual(0, target.Hits.Count);
            Assert.AreEqual(1f, tower.Stats.CriticalRate, 0.00001f, "치명 100%");
            Assert.AreEqual(12f, tower.Stats.Power, 0.0001f, "10 × (1 + 20%)");
            Assert.AreEqual(1f, tower.Stats.Get(StatType.ActionGainPerTurn), 0.00001f);
            Assert.AreEqual(0f, tower.Stats.CriticalDamage, 0.00001f, "치명 피해는 안 오름");
        }

        [Test]
        public void 광폭화_다이아는_공격력_300퍼센트()
        {
            TowerController tower = MakeTower(1202, power: 150f);
            new AxeTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, MakeTarget(2));
            Assert.AreEqual(600f, tower.Stats.Power, 0.001f, "150 × (1 + 300%)");
        }

        [Test]
        public void 광폭화는_레벨_1이면_행동_2번_뒤에_끝남()
        {
            TowerController tower = MakeTower(1202);
            new AxeTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, MakeTarget(2));
            Assert.AreEqual(2, TimedTowerBuff.GetRemaining(tower.State, AxeTargetSkill.Source));

            TimedTowerBuff.CountDown(tower.State);
            Assert.AreEqual(1f, tower.Stats.CriticalRate, 0.00001f, "1번 남음");

            TimedTowerBuff.CountDown(tower.State);
            Assert.AreEqual(0, TimedTowerBuff.GetRemaining(tower.State, AxeTargetSkill.Source));
            Assert.AreEqual(0f, tower.Stats.CriticalRate, 0.00001f);
            Assert.AreEqual(10f, tower.Stats.Power, 0.0001f);
            Assert.AreEqual(0f, tower.Stats.Get(StatType.ActionGainPerTurn), 0.00001f);
        }

        [Test]
        public void 광폭화를_다시_쓰면_겹치지_않고_처음부터()
        {
            TowerController tower = MakeTower(1202);
            var skill = new AxeTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            skill.Fire(tower, MakeTarget(2));
            TimedTowerBuff.CountDown(tower.State);

            skill.Fire(tower, MakeTarget(2));
            Assert.AreEqual(2, TimedTowerBuff.GetRemaining(tower.State, AxeTargetSkill.Source), "유지가 처음부터");
            Assert.AreEqual(12f, tower.Stats.Power, 0.0001f, "공격력이 겹쳐 오르지 않음 (지난 광폭화의 공격력이 식에 끼지 않음)");
        }

        [Test]
        public void 광폭화는_스택을_쓰면_공격력_값이_1점6배()
        {
            TowerController tower = MakeTower(1202);
            tower.State.Stack.Add(60f);
            new AxeTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, MakeTarget(2));

            Assert.AreEqual(13.2f, tower.Stats.Power, 0.0001f, "20% × 1.6 = 32%");
            Assert.AreEqual(0f, tower.State.Stack.Current, 0.0001f, "스택을 씀");
        }

        [Test]
        public void 티어_강화는_광폭화를_이어받음()
        {
            TowerController old = MakeTower(1202);
            new AxeTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(old, MakeTarget(2));
            TimedTowerBuff.CountDown(old.State);

            TowerController created = MakeTower(2202);
            created.State.CopyInheritedFrom(old.State);
            Assert.AreEqual(1, TimedTowerBuff.GetRemaining(created.State, AxeTargetSkill.Source));
            Assert.AreEqual(1f, created.Stats.CriticalRate, 0.00001f);
        }

        // ── 도끼 버프: 출정가 · 발할라의 찬가 ──

        [Test]
        public void 출정가는_지금_스택만큼_치명_피해()
        {
            Zone zone = MakeZone(MakeTower(1302), true, 30f);
            List<StatModifier> modifiers = new AxeBuffRule().GetModifiers(zone, MakeTower(1103), "도끼 버프존");   // 활 = 시너지 없음

            Assert.AreEqual(0.006f, Sum(modifiers, StatType.CritDamage), 0.00001f, "0.001 × 10 × 2 × 30 = 0.6%");
            Assert.AreEqual(0f, Sum(modifiers, StatType.Mastery), "Mastery는 발할라의 찬가만");
        }

        [Test]
        public void 발할라의_찬가는_치명_피해_Action_감소_Mastery_증가()
        {
            Zone bronze = MakeZone(MakeTower(1302), true, 60f);
            Zone diamond = MakeZone(MakeTower(1302, power: 150f), true, 60f);
            TowerController receiver = MakeTower(1103);
            List<StatModifier> modifiers = new AxeBuffRule().GetModifiers(bronze, receiver, "도끼 버프존");

            Assert.AreEqual(0.012f, Sum(modifiers, StatType.CritDamage), 0.00001f, "브론즈 1.2%");
            Assert.AreEqual(0.18f, Sum(new AxeBuffRule().GetModifiers(diamond, receiver, "도끼 버프존"), StatType.CritDamage), 0.00001f, "다이아 18%");
            Assert.AreEqual(-1f, Sum(modifiers, StatType.MaxAction), 0.00001f);
            Assert.AreEqual(1f, Sum(modifiers, StatType.Mastery), 0.00001f);
        }

        [Test]
        public void 베기_타워는_치명_피해가_출정가_25퍼센트_발할라_50퍼센트_더()
        {
            TowerController sword = MakeTower(1101);
            Zone skill1 = MakeZone(MakeTower(1302), true, 30f);
            Zone skill2 = MakeZone(MakeTower(1302), true, 60f);

            Assert.AreEqual(0.256f, Sum(new AxeBuffRule().GetModifiers(skill1, sword, "도끼 버프존"), StatType.CritDamage), 0.00001f);
            Assert.AreEqual(0.512f, Sum(new AxeBuffRule().GetModifiers(skill2, sword, "도끼 버프존"), StatType.CritDamage), 0.00001f);
        }

        [Test]
        public void 발할라의_찬가가_스탯에_들어감()
        {
            Zone zone = MakeZone(MakeTower(1302), true, 60f);
            TowerController receiver = MakeTower(1101);
            int action = receiver.Stats.Action, mastery = receiver.Stats.Mastery;
            BuffZoneAction.GiveBuff(zone, receiver);

            Assert.AreEqual(action - 1, receiver.Stats.Action);
            Assert.AreEqual(mastery + 1, receiver.Stats.Mastery);
            Assert.AreEqual(0.512f, receiver.Stats.CriticalDamage, 0.00001f);
        }

        [TestCase(1302, 20, -10)]
        [TestCase(2302, 20, -4)]
        [TestCase(3302, 20, -2)]
        [TestCase(4302, 20, -1)]
        [TestCase(5302, 20, 0)]
        [TestCase(1302, 80, -40)]
        [TestCase(5302, 80, -2)]
        [TestCase(1302, 15, -7)]
        public void 도끼_버프는_한도를_티어_비율만큼_깎음(int id, int baseLimit, int expected)
        {
            Assert.AreEqual(expected, new AxeBuffRule().GetLimitModifier(MakeTower(id), baseLimit));
        }

        [Test]
        public void 다른_문양_버프는_한도를_바꾸지_않음()
        {
            Assert.AreEqual(0, new SwordBuffRule().GetLimitModifier(MakeTower(1301), 20));
        }

        // ── 도끼 디버프: 사형 선고 · 사형 집행 ──

        [Test]
        public void 사형_선고는_적의_최대_체력_20퍼센트_현재_체력도_같은_비율()
        {
            Zone zone = MakeZone(MakeTower(1402), false, 30f);
            EnemyController enemy = MakeEnemy();
            enemy.Health.TakeDamage(50f);

            enemy.Status.Add(new AxeDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(120f, enemy.MaxHp, 0.001f);
            Assert.AreEqual(60f, enemy.CurrentHp, 0.001f, "50 × 1.2");
        }

        [Test]
        public void 사형_선고는_적의_최대_이동력_1()
        {
            Zone zone = MakeZone(MakeTower(1402), false, 30f);
            EnemyController enemy = MakeEnemy();
            float before = enemy.Stats.Get(StatType.SpeedMax);

            enemy.Status.Add(new AxeDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(before + 1f, enemy.Stats.Get(StatType.SpeedMax), 0.0001f);
        }

        [Test]
        public void 사형_집행이_되면_사형_선고의_이득이_사라짐()
        {
            TowerController owner = MakeTower(1402);
            EnemyController enemy = MakeEnemy();
            float speed = enemy.Stats.Get(StatType.SpeedMax);

            enemy.Status.Add(new AxeDebuffRule().CreateEffect(MakeZone(owner, false, 30f), enemy));
            enemy.Status.Add(new AxeDebuffRule().CreateEffect(MakeZone(owner, false, 60f), enemy));

            Assert.AreEqual(100f, enemy.MaxHp, 0.001f);
            Assert.AreEqual(speed, enemy.Stats.Get(StatType.SpeedMax), 0.0001f);
        }

        [Test]
        public void 사형_선고가_다시_걸려도_최대_체력은_한_번만_늘어남()
        {
            Zone zone = MakeZone(MakeTower(1402), false, 30f);
            EnemyController enemy = MakeEnemy();

            enemy.Status.Add(new AxeDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.Add(new AxeDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(120f, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 사형_집행은_체력_1점2퍼센트_미만이면_즉사()
        {
            Zone zone = MakeZone(MakeTower(1402), false, 60f);
            EnemyController alive = MakeEnemy(), dead = MakeEnemy();
            alive.Status.Add(new AxeDebuffRule().CreateEffect(zone, alive));
            dead.Status.Add(new AxeDebuffRule().CreateEffect(zone, dead));

            alive.TakeDamage(new DamageInfo { BaseDamage = 98.7f, ForceFixed = true });    // 1.3 남음
            dead.TakeDamage(new DamageInfo { BaseDamage = 98.9f, ForceFixed = true });     // 1.1 남음
            Assert.IsTrue(alive.IsAlive);
            Assert.IsFalse(dead.IsAlive);
        }

        [Test]
        public void 사형_집행은_베기면_11점2퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1402), false, 60f);
            EnemyController alive = MakeEnemy(), dead = MakeEnemy();
            alive.Status.Add(new AxeDebuffRule().CreateEffect(zone, alive));
            dead.Status.Add(new AxeDebuffRule().CreateEffect(zone, dead));

            alive.TakeDamage(new DamageInfo { BaseDamage = 88.7f, ForceFixed = true, Attribute = AttackAttribute.Slash });   // 11.3 남음
            dead.TakeDamage(new DamageInfo { BaseDamage = 88.9f, ForceFixed = true, Attribute = AttackAttribute.Slash });    // 11.1 남음
            Assert.IsTrue(alive.IsAlive);
            Assert.IsFalse(dead.IsAlive);
        }

        [Test]
        public void 사형_집행은_저항_50퍼센트면_기준도_절반()
        {
            Zone zone = MakeZone(MakeTower(1402), false, 60f);
            EnemyController alive = MakeEnemy(resistance: 50f), dead = MakeEnemy(resistance: 50f);
            alive.Status.Add(new AxeDebuffRule().CreateEffect(zone, alive));
            dead.Status.Add(new AxeDebuffRule().CreateEffect(zone, dead));

            alive.TakeDamage(new DamageInfo { BaseDamage = 99.3f, ForceFixed = true });    // 0.7 남음 (기준 0.6%)
            dead.TakeDamage(new DamageInfo { BaseDamage = 99.5f, ForceFixed = true });     // 0.5 남음
            Assert.IsTrue(alive.IsAlive);
            Assert.IsFalse(dead.IsAlive);
        }

        // ══ 활 (수업 29, 20번 4-5장) ══

        [Test]
        public void 등록표_활_넷()
        {
            Assert.IsInstanceOf<ArrowSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Bow, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<ArrowTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Bow, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<BowBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Bow));
            Assert.IsInstanceOf<BowDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Bow));
        }

        // ── 활 광역: 화살비 ──

        private ArrowSplashSkill MakeArrowRain(GroundEffectRegistry grounds) =>
            new ArrowSplashSkill(m_grid, m_occupancy, new StackModeSettings(), grounds);

        [Test]
        public void 화살비는_발동_때_피해_없이_장판만_깖()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            var grounds = new GroundEffectRegistry();
            MakeArrowRain(grounds).Fire(MakeTower(1103), null);

            Assert.AreEqual(0, target.Hits.Count, "쏘아 올리기만");
            Assert.AreEqual(1, grounds.All.Count);
            var rain = (ArrowRainGround)grounds.All[0];
            Assert.AreEqual(8f, rain.Damage, 0.0001f, "10 × 2 × 0.4");
            Assert.AreEqual(2, rain.RemainingTurns, "레벨 1 × 2턴");
        }

        [Test]
        public void 화살비는_매_턴_맵_전체에_떨어지고_2턴_뒤_사라짐()
        {
            OccupyingFakeTarget a = MakeTarget(0), b = MakeTarget(4);
            var grounds = new GroundEffectRegistry();
            MakeArrowRain(grounds).Fire(MakeTower(1103), null);

            grounds.RunTurn();
            grounds.RunTurn();
            grounds.RunTurn();                                      // 이미 사라짐

            foreach (OccupyingFakeTarget target in new[] { a, b })
            {
                Assert.AreEqual(2, target.Hits.Count, $"{target.PathIndex}번 칸");
                Assert.AreEqual(8f, target.Hits[0].BaseDamage, 0.0001f);
                Assert.AreEqual(DamageSource.Ground, target.Hits[0].Source);
                Assert.AreEqual(AttackAttribute.Pierce, target.Hits[0].Attribute);
            }
            Assert.AreEqual(0, grounds.All.Count);
        }

        [Test]
        public void 화살비는_스택을_쓰면_매_턴_12점8()
        {
            var grounds = new GroundEffectRegistry();
            TowerController tower = MakeTower(1103);
            tower.State.Stack.Add(60f);
            MakeArrowRain(grounds).Fire(tower, null);

            Assert.AreEqual(12.8f, ((ArrowRainGround)grounds.All[0]).Damage, 0.0001f);
        }

        [Test]
        public void 화살비_두_개가_따로_남음()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            var grounds = new GroundEffectRegistry();
            ArrowSplashSkill skill = MakeArrowRain(grounds);
            TowerController tower = MakeTower(1103);

            skill.Fire(tower, null);
            grounds.RunTurn();                                      // 첫째: 1턴 남음
            skill.Fire(tower, null);                                // 둘째: 덮어쓰지 않고 따로

            Assert.AreEqual(2, grounds.All.Count);
            Assert.AreEqual(1, grounds.All[0].RemainingTurns);
            Assert.AreEqual(2, grounds.All[1].RemainingTurns);

            grounds.RunTurn();
            Assert.AreEqual(3, target.Hits.Count, "1 + 두 개가 한 번씩");
            Assert.AreEqual(1, grounds.All.Count, "첫째는 끝남");
        }

        // ── 활 단일: 은화살 ──

        [Test]
        public void 은화살은_최대_체력의_20퍼센트()
        {
            OccupyingFakeTarget target = MakeTarget(2, hp: 1000f);
            new ArrowTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1203), target);

            Assert.AreEqual(1, target.Hits.Count);
            Assert.AreEqual(200f, target.Hits[0].BaseDamage, 0.001f, "10 × 2 = 20 → 1000의 20%");
        }

        [Test]
        public void 은화살은_스택을_쓰면_1점6배()
        {
            OccupyingFakeTarget target = MakeTarget(2, hp: 1000f);
            TowerController tower = MakeTower(1203);
            tower.State.Stack.Add(60f);
            new ArrowTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, target);

            Assert.AreEqual(320f, target.Hits[0].BaseDamage, 0.001f);
        }

        [Test]
        public void 은화살은_발동시킨_적에게만()
        {
            OccupyingFakeTarget main = MakeTarget(2), other = MakeTarget(2);
            new ArrowTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1203), main);
            Assert.AreEqual(0, other.Hits.Count);
        }

        // ── 활 버프: 요정의 활 · 정령의 활 ──

        [Test]
        public void 거리_비례_추가_피해는_날아간_칸마다_사거리까지()
        {
            TowerController tower = MakeTower(1101, range: 5);
            tower.State.Modifiers.Add(StatModifier.Flat("활 버프존", StatType.DistanceDamage, 0.012f));

            Assert.AreEqual(1.024f, tower.Stats.GetDistanceMultiplier(2), 0.00001f);
            Assert.AreEqual(1.06f, tower.Stats.GetDistanceMultiplier(5), 0.00001f, "사거리 5 끝에서 +6%");
            Assert.AreEqual(1.06f, tower.Stats.GetDistanceMultiplier(9), 0.00001f, "사거리 넘어도 최대치");
        }

        [Test]
        public void 버프가_없으면_거리_배율은_1()
        {
            Assert.AreEqual(1f, MakeTower(1101, range: 5).Stats.GetDistanceMultiplier(5), 0.00001f);
        }

        [Test]
        public void 정령의_활은_사거리_1칸_늘고_끝에서_7점2퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1303), true, 60f);
            TowerController receiver = MakeTower(1101, range: 5);         // 검 = 찌르기 아님
            BuffZoneAction.GiveBuff(zone, receiver);

            Assert.AreEqual(6, receiver.Stats.Range);
            Assert.AreEqual(1.072f, receiver.Stats.GetDistanceMultiplier(6), 0.00001f);
            Assert.AreEqual(1.012f, receiver.Stats.GetDistanceMultiplier(1), 0.00001f, "가까우면 조금");
        }

        [Test]
        public void 찌르기_타워는_거리와_무관하게_최대치_더하기_10퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1303), true, 60f);
            TowerController receiver = MakeTower(1103, range: 5);         // 활 = 찌르기
            BuffZoneAction.GiveBuff(zone, receiver);

            Assert.AreEqual(1.172f, receiver.Stats.GetDistanceMultiplier(1), 0.00001f, "7.2% + 10%");
        }

        [Test]
        public void 요정의_활은_지금_스택만큼이고_찌르기는_5퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1303), true, 30f);
            List<StatModifier> pierce = new BowBuffRule().GetModifiers(zone, MakeTower(1203), "활 버프존");
            List<StatModifier> slash = new BowBuffRule().GetModifiers(zone, MakeTower(1101), "활 버프존");

            Assert.AreEqual(0.006f, Sum(slash, StatType.DistanceDamage), 0.00001f);
            Assert.AreEqual(0f, Sum(slash, StatType.Range), "사거리는 정령의 활만");
            Assert.AreEqual(0f, Sum(slash, StatType.DistanceDamageBonus));
            Assert.AreEqual(0.05f, Sum(pierce, StatType.DistanceDamageBonus), 0.00001f);
        }

        [Test]
        public void 거리_비례_추가_피해는_스킬_피해에도_들어감()
        {
            OccupyingFakeTarget near = MakeTarget(0), far = MakeTarget(2);    // 거리 1, 2
            TowerController tower = MakeTower(1101);
            tower.State.Modifiers.Add(StatModifier.Flat("활 버프존", StatType.DistanceDamage, 0.012f));

            new SwordSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, null);
            Assert.AreEqual(20.24f, near.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(20.48f, far.Hits[0].BaseDamage, 0.0001f);
        }

        // ── 활 디버프: 락 온 · 핀 포인트 ──

        [Test]
        public void 락_온은_받는_최종_피해()
        {
            Zone zone = MakeZone(MakeTower(1403), false, 30f);
            EnemyController enemy = MakeEnemy();
            enemy.Status.Add(new BowDebuffRule().CreateEffect(zone, enemy));

            Assert.AreEqual(1.006f, enemy.Stats.Get(StatType.DamageTaken), 0.00001f);
            Assert.AreEqual(100.6f, enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f }), 0.001f);
        }

        [Test]
        public void 핀_포인트는_받는_치명_피해_50퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1403), false, 60f);
            EnemyController enemy = MakeEnemy();
            enemy.Status.Add(new BowDebuffRule().CreateEffect(zone, enemy));

            var crit = new DamageInfo { BaseDamage = 200f, IsCritical = true, CritMultiplier = 2f };     // 100 × 치명 2
            Assert.AreEqual(253f, enemy.PreviewDamage(crit), 0.01f, "200 × 1.012 × 2.5 ÷ 2");
            Assert.AreEqual(101.2f, enemy.PreviewDamage(new DamageInfo { BaseDamage = 100f }), 0.001f, "치명이 아니면 받는 피해만");
        }

        [Test]
        public void 락_온은_치명_피해를_올리지_않음()
        {
            Zone zone = MakeZone(MakeTower(1403), false, 30f);
            EnemyController enemy = MakeEnemy();
            enemy.Status.Add(new BowDebuffRule().CreateEffect(zone, enemy));

            var crit = new DamageInfo { BaseDamage = 200f, IsCritical = true, CritMultiplier = 2f };
            Assert.AreEqual(201.2f, enemy.PreviewDamage(crit), 0.001f);
        }

        [Test]
        public void 찌르기_타워의_치명_확률이_락_온_5퍼센트_핀_포인트_10퍼센트()
        {
            TowerController owner = MakeTower(1403);
            EnemyController lockOn = MakeEnemy(), pinPoint = MakeEnemy();
            lockOn.Status.Add(new BowDebuffRule().CreateEffect(MakeZone(owner, false, 30f), lockOn));
            pinPoint.Status.Add(new BowDebuffRule().CreateEffect(MakeZone(owner, false, 60f), pinPoint));
            TowerController bow = MakeTower(1103, critRate: 0.2f);

            Assert.AreEqual(0.25f, CriticalRoll.RateAgainst(bow, bow.Stats, lockOn), 0.00001f);
            Assert.AreEqual(0.3f, CriticalRoll.RateAgainst(bow, bow.Stats, pinPoint), 0.00001f);
        }

        [Test]
        public void 찌르기가_아니면_치명_확률이_오르지_않음()
        {
            Zone zone = MakeZone(MakeTower(1403), false, 60f);
            EnemyController enemy = MakeEnemy();
            enemy.Status.Add(new BowDebuffRule().CreateEffect(zone, enemy));
            TowerController sword = MakeTower(1101, critRate: 0.2f);

            Assert.AreEqual(0.2f, CriticalRoll.RateAgainst(sword, sword.Stats, enemy), 0.00001f);
            Assert.AreEqual(0f, enemy.Status.GetCritRateBonus(null), "타워가 없는 피해");
        }

        [Test]
        public void 핀_포인트는_저항_50퍼센트면_치명_피해_25퍼센트_치명_확률_5퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1403), false, 60f);
            EnemyController enemy = MakeEnemy(resistance: 50f);
            enemy.Status.Add(new BowDebuffRule().CreateEffect(zone, enemy));

            var crit = new DamageInfo { BaseDamage = 200f, IsCritical = true, CritMultiplier = 2f };
            Assert.AreEqual(200f * 1.006f * 2.25f / 2f, enemy.PreviewDamage(crit), 0.01f);
            Assert.AreEqual(0.05f, enemy.Status.GetCritRateBonus(MakeTower(1103)), 0.00001f);
        }

        // ══ 창 (수업 30, 20번 4-6장) ══

        [Test]
        public void 등록표_창_넷()
        {
            Assert.IsInstanceOf<SpearSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Spear, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<SpearTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Spear, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<SpearBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Spear));
            Assert.IsInstanceOf<SpearDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Spear));
        }

        // ── 창 광역: 용맹한 깃창 ──

        private SpearSplashSkill MakeBanner(TowerSlots slots = null) => new SpearSplashSkill(m_grid, m_occupancy, new StackModeSettings(), slots);

        [Test]
        public void 깃창은_피해_없이_자기도_공격력_치명_확률_20퍼센트()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1104);
            MakeBanner().Fire(tower, target);

            Assert.AreEqual(0, target.Hits.Count);
            Assert.AreEqual(12f, tower.Stats.Power, 0.0001f, "10 × (1 + 20%)");
            Assert.AreEqual(0.2f, tower.Stats.CriticalRate, 0.00001f);
            Assert.AreEqual(2, TimedTowerBuff.GetRemaining(tower.State, SpearSplashSkill.Source), "레벨 1 × 2번 행동");
        }

        [Test]
        public void 깃창은_사거리_안_공격_타워만()
        {
            var slots = new TowerSlots(m_grid.SpawnCount);
            TowerController owner = MakeTower(1104, slot: 0);               // 사거리 2
            TowerController far = MakeTower(1201, slot: 1);                 // 거리 3
            TowerController near = MakeTower(1101, slot: 2);                // 거리 2
            slots.TryPlace(owner, 0);
            slots.TryPlace(far, 1);
            slots.TryPlace(near, 2);

            MakeBanner(slots).Fire(owner, null);
            Assert.AreEqual(12f, near.Stats.Power, 0.0001f);
            Assert.AreEqual(10f, far.Stats.Power, 0.0001f, "사거리 밖");
            Assert.AreEqual(12f, owner.Stats.Power, 0.0001f, "자기 포함");
        }

        [Test]
        public void 깃창은_버프_디버프_타워에게_걸리지_않음()
        {
            var slots = new TowerSlots(m_grid.SpawnCount);
            TowerController owner = MakeTower(1104, slot: 0);
            TowerController support = MakeTower(1301, slot: 2);
            slots.TryPlace(owner, 0);
            slots.TryPlace(support, 2);

            MakeBanner(slots).Fire(owner, null);
            Assert.AreEqual(0, TimedTowerBuff.GetRemaining(support.State, SpearSplashSkill.Source));
        }

        [Test]
        public void 깃창_다이아는_공격력_300퍼센트_치명_100퍼센트()
        {
            TowerController tower = MakeTower(1104, power: 150f);
            MakeBanner().Fire(tower, null);

            Assert.AreEqual(600f, tower.Stats.Power, 0.001f);
            Assert.AreEqual(1f, tower.Stats.CriticalRate, 0.00001f, "100%에서 멈춤");
        }

        [Test]
        public void 깃창을_다시_받으면_겹치지_않고_처음부터()
        {
            TowerController tower = MakeTower(1104);
            SpearSplashSkill skill = MakeBanner();
            skill.Fire(tower, null);
            TimedTowerBuff.CountDown(tower.State);
            skill.Fire(tower, null);

            Assert.AreEqual(12f, tower.Stats.Power, 0.0001f, "지난 깃창의 공격력이 식에 끼지 않음");
            Assert.AreEqual(2, TimedTowerBuff.GetRemaining(tower.State, SpearSplashSkill.Source));
        }

        [Test]
        public void 깃창은_스택을_쓰면_1점6배()
        {
            TowerController tower = MakeTower(1104);
            tower.State.Stack.Add(60f);
            MakeBanner().Fire(tower, null);

            Assert.AreEqual(13.2f, tower.Stats.Power, 0.0001f);
            Assert.AreEqual(0.32f, tower.Stats.CriticalRate, 0.00001f);
        }

        // ── 창 단일: 뽑아찢기 ──

        [Test]
        public void 기본_공격이_맞을_때마다_창이_1개씩()
        {
            var skill = new SpearTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            TowerController tower = MakeTower(1204);
            OccupyingFakeTarget target = MakeTarget(2);

            for (int i = 0; i < 3; i++) skill.OnBasicHit(tower, target);
            Assert.AreEqual(3, skill.GetSpears(target));
        }

        [Test]
        public void 죽은_적에게는_창이_꽂히지_않음()
        {
            var skill = new SpearTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            OccupyingFakeTarget target = MakeTarget(2, hp: 1f);
            target.TakeDamage(new DamageInfo { BaseDamage = 5f });

            skill.OnBasicHit(MakeTower(1204), target);
            Assert.AreEqual(0, skill.GetSpears(target));
        }

        [Test]
        public void 뽑아찢기는_창이_꽂힌_적_전부에게_창_수만큼_피해_후_다_뽑음()
        {
            var skill = new SpearTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            TowerController tower = MakeTower(1204);
            OccupyingFakeTarget three = MakeTarget(0), one = MakeTarget(4), none = MakeTarget(2);
            for (int i = 0; i < 3; i++) skill.OnBasicHit(tower, three);
            skill.OnBasicHit(tower, one);

            skill.Fire(tower, three);
            Assert.AreEqual(60f, three.Hits[0].BaseDamage, 0.0001f, "창 3 × 20");
            Assert.AreEqual(20f, one.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(0, none.Hits.Count, "창이 없으면 안 맞음");
            Assert.AreEqual(0, skill.GetSpears(three), "다 뽑음");
            Assert.AreEqual(0, skill.GetSpears(one));
        }

        [Test]
        public void 다른_창술사의_창은_뽑지_않음()
        {
            var mine = new SpearTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            var other = new SpearTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            OccupyingFakeTarget target = MakeTarget(2);
            other.OnBasicHit(MakeTower(1204), target);

            mine.Fire(MakeTower(1204), target);
            Assert.AreEqual(0, target.Hits.Count);
            Assert.AreEqual(1, other.GetSpears(target), "남의 창은 그대로");
        }

        [Test]
        public void 뽑아찢기는_발동시킨_적이_죽었어도_다른_적의_창을_뽑음()
        {
            var skill = new SpearTargetSkill(m_grid, m_occupancy, new StackModeSettings());
            TowerController tower = MakeTower(1204, skill: skill, duration: 1);
            OccupyingFakeTarget trigger = MakeTarget(0), other = MakeTarget(4);
            skill.OnBasicHit(tower, other);

            tower.CountSkillHit(trigger);                                   // Duration 1 → 줄 섬
            trigger.TakeDamage(new DamageInfo { BaseDamage = 5000f });      // 같은 행동의 다른 탄에 죽음

            Assert.IsTrue(tower.FireNextPendingSkill());
            Assert.AreEqual(1, other.Hits.Count);
        }

        // ── 창 버프: 카우치 · 랜스 차지 ──

        [Test]
        public void 카우치는_Action_2만()
        {
            Zone zone = MakeZone(MakeTower(1304), true, 30f);
            List<StatModifier> modifiers = new SpearBuffRule().GetModifiers(zone, MakeTower(1103), "창 버프존");

            Assert.AreEqual(2f, Sum(modifiers, StatType.MaxAction), 0.00001f);
            Assert.AreEqual(1, modifiers.Count, "좋은 효과는 없음");
        }

        [Test]
        public void 랜스_차지는_대가_없이_관통_20퍼센트_치명_확률_치명_피해()
        {
            Zone zone = MakeZone(MakeTower(1304), true, 60f);
            List<StatModifier> modifiers = new SpearBuffRule().GetModifiers(zone, MakeTower(1101), "창 버프존");   // 검 = 찌르기 아님

            Assert.AreEqual(0f, Sum(modifiers, StatType.MaxAction));
            Assert.AreEqual(0.2f, Sum(modifiers, StatType.ArmorPenetration), 0.00001f);
            Assert.AreEqual(0.012f, Sum(modifiers, StatType.CritRate), 0.00001f);
            Assert.AreEqual(0.012f, Sum(modifiers, StatType.CritDamage), 0.00001f);
        }

        [Test]
        public void 찌르기_타워는_랜스_차지의_관통이_30퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1304), true, 60f);
            Assert.AreEqual(0.3f, Sum(new SpearBuffRule().GetModifiers(zone, MakeTower(1103), "창 버프존"), StatType.ArmorPenetration), 0.00001f);
        }

        [Test]
        public void 카우치를_받으면_행동_주기가_2_늘어남()
        {
            Zone zone = MakeZone(MakeTower(1304), true, 30f);
            TowerController receiver = MakeTower(1101);
            int action = receiver.Stats.Action;
            BuffZoneAction.GiveBuff(zone, receiver);
            Assert.AreEqual(action + 2, receiver.Stats.Action);
        }

        [Test]
        public void 방어_관통은_나가는_피해에_실림()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            TowerController tower = MakeTower(1101);
            tower.State.Modifiers.Add(StatModifier.Flat("창 버프존", StatType.ArmorPenetration, 0.2f));

            new SwordSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, null);
            Assert.AreEqual(0.2f, target.Hits[0].ArmorPenetration, 0.00001f);
        }

        // ── 창 디버프: 출혈 · 과다출혈 ──

        [Test]
        public void 출혈은_받는_치유를_줄임()
        {
            Zone zone = MakeZone(MakeTower(1404), false, 30f);          // 0.6%
            EnemyController enemy = MakeEnemy();
            enemy.Health.TakeDamage(50f);
            enemy.Status.Add(new SpearDebuffRule().CreateEffect(zone, enemy));

            Assert.AreEqual(9.94f, enemy.Heal(10f), 0.0001f);
        }

        [Test]
        public void 디버프가_없으면_치유는_그대로()
        {
            EnemyController enemy = MakeEnemy();
            enemy.Health.TakeDamage(50f);
            Assert.AreEqual(10f, enemy.Heal(10f), 0.0001f);
        }

        [Test]
        public void 과다출혈은_이동한_칸마다_최대_체력_2퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1404), false, 60f);
            EnemyController enemy = MakeEnemy(defense: 100f);
            enemy.Status.Add(new SpearDebuffRule().CreateEffect(zone, enemy));

            enemy.Status.OnMoved(3);
            Assert.AreEqual(94f, enemy.CurrentHp, 0.001f, "3칸 × 2%, 방어 무시");
        }

        [Test]
        public void 과다출혈_이동_피해는_저항_50퍼센트면_칸당_1퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1404), false, 60f);
            EnemyController enemy = MakeEnemy(resistance: 50f);
            enemy.Status.Add(new SpearDebuffRule().CreateEffect(zone, enemy));

            enemy.Status.OnMoved(-3);                                       // 뒤로 간 칸도 셈
            Assert.AreEqual(97f, enemy.CurrentHp, 0.001f);
        }

        [Test]
        public void 출혈은_이동_피해가_없음()
        {
            Zone zone = MakeZone(MakeTower(1404), false, 30f);
            EnemyController enemy = MakeEnemy();
            enemy.Status.Add(new SpearDebuffRule().CreateEffect(zone, enemy));

            enemy.Status.OnMoved(3);
            Assert.AreEqual(100f, enemy.CurrentHp, 0.001f);
        }

        [Test]
        public void 찌르기는_잃은_체력만큼_피해가_오름()
        {
            TowerController owner = MakeTower(1404);
            EnemyController bleed = MakeEnemy(), overBleed = MakeEnemy();
            bleed.Health.TakeDamage(50f);                                   // 절반 잃음
            overBleed.Health.TakeDamage(50f);
            bleed.Status.Add(new SpearDebuffRule().CreateEffect(MakeZone(owner, false, 30f), bleed));
            overBleed.Status.Add(new SpearDebuffRule().CreateEffect(MakeZone(owner, false, 60f), overBleed));

            var pierce = new DamageInfo { BaseDamage = 100f, Attribute = AttackAttribute.Pierce };
            Assert.AreEqual(105f, bleed.PreviewDamage(pierce), 0.001f, "최대 10%의 절반");
            Assert.AreEqual(110f, overBleed.PreviewDamage(pierce), 0.001f, "최대 20%의 절반");

            var slash = new DamageInfo { BaseDamage = 100f, Attribute = AttackAttribute.Slash };
            Assert.AreEqual(100f, bleed.PreviewDamage(slash), 0.001f, "찌르기만");
        }

        // ══ 방패 (수업 31, 20번 4-7장) ══

        [Test]
        public void 등록표_방패_넷()
        {
            Assert.IsInstanceOf<ShieldSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Shield, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<ShieldTargetSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Blue, TowerEmblem.Shield, m_grid, m_occupancy, null));
            Assert.IsInstanceOf<ShieldBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Shield));
            Assert.IsInstanceOf<ShieldDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Shield));
            Assert.IsTrue(SkillRegistry.GetBuffRule(TowerEmblem.Shield).UsesOwnerStack, "방패만 스택을 타워가 쌓음");
            Assert.IsFalse(SkillRegistry.GetBuffRule(TowerEmblem.Sword).UsesOwnerStack);
        }

        // ── 방패 광역: 배쉬 ──

        [Test]
        public void 배쉬는_착탄_범위의_적에게_피해와_기절_확률_20퍼센트()
        {
            OccupyingFakeTarget main = MakeTarget(2), same = MakeTarget(2), far = MakeTarget(0);    // Mastery 1 = 착탄 칸 하나
            new ShieldSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1105), main);

            foreach (OccupyingFakeTarget target in new[] { main, same })
            {
                Assert.AreEqual(20f, target.Hits[0].BaseDamage, 0.0001f);
                Assert.AreEqual(0.2f, target.Hits[0].StunChance, 0.00001f);
                Assert.AreEqual(AttackAttribute.Blunt, target.Hits[0].Attribute);
            }
            Assert.AreEqual(0, far.Hits.Count, "착탄 범위 밖");
        }

        [Test]
        public void 기절_확률이_실린_공격에_맞으면_기절()
        {
            EnemyController enemy = MakeEnemy();
            enemy.TakeDamage(new DamageInfo { BaseDamage = 1f, StunChance = 1f });
            Assert.IsTrue(enemy.Status.Has(ImmobilizeEffect.ImmobilizeKey));
        }

        [Test]
        public void 기절_확률이_없으면_기절하지_않음()
        {
            EnemyController enemy = MakeEnemy();
            enemy.TakeDamage(new DamageInfo { BaseDamage = 1f, Attribute = AttackAttribute.Blunt, Source = DamageSource.Basic });
            Assert.IsFalse(enemy.Status.Has(ImmobilizeEffect.ImmobilizeKey));
        }

        // ── 방패 단일: 아머 브레이크 ──

        [Test]
        public void 아머_브레이크는_방어력_20퍼센트_레벨_2배_행동()
        {
            EnemyController enemy = MakeEnemy(defense: 100f);
            ShieldTargetSkill.ApplyArmorBreak(MakeTower(1205), enemy);

            Assert.AreEqual(80f, enemy.Stats.Get(StatType.Defense), 0.001f);
            Assert.AreEqual(2, enemy.Status.Find(ShieldTargetSkill.Key).RemainingActions, "레벨 1 × 2");
        }

        [Test]
        public void 아머_브레이크는_저항_50퍼센트면_10퍼센트()
        {
            EnemyController enemy = MakeEnemy(defense: 100f, resistance: 50f);
            ShieldTargetSkill.ApplyArmorBreak(MakeTower(1205), enemy);
            Assert.AreEqual(90f, enemy.Stats.Get(StatType.Defense), 0.001f);
        }

        [Test]
        public void 아머_브레이크는_겹치지_않고_유지만_처음부터()
        {
            EnemyController enemy = MakeEnemy(defense: 100f);
            TowerController tower = MakeTower(1205);
            ShieldTargetSkill.ApplyArmorBreak(tower, enemy);
            enemy.Status.OnActed();                                         // 1번 행동 → 1 남음
            ShieldTargetSkill.ApplyArmorBreak(tower, enemy);

            Assert.AreEqual(80f, enemy.Stats.Get(StatType.Defense), 0.001f, "−40%가 아님");
            Assert.AreEqual(2, enemy.Status.Find(ShieldTargetSkill.Key).RemainingActions);
        }

        // ── 방패 버프: 프로텍트 · 이지스 ──

        [Test]
        public void 프로텍트는_스택_곱하기_한도만큼_최종_피해()
        {
            Zone zone = MakeZone(MakeTower(1305), true, 30f);
            List<StatModifier> modifiers = new ShieldBuffRule(() => 20).GetModifiers(zone, MakeTower(1101), "방패 버프존");
            Assert.AreEqual(0.12f, Sum(modifiers, StatType.FinalDamage), 0.00001f, "0.6% × 한도 20");
        }

        [Test]
        public void 이지스는_브론즈_한도_20에서_24퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1305), true, 60f);
            Assert.AreEqual(0.24f, Sum(new ShieldBuffRule(() => 20).GetModifiers(zone, MakeTower(1101), "방패 버프존"), StatType.FinalDamage), 0.00001f);
        }

        [Test]
        public void 한도가_늘면_방패_버프도_커짐()
        {
            Zone zone = MakeZone(MakeTower(1305), true, 60f);
            Assert.AreEqual(0.36f, Sum(new ShieldBuffRule(() => 30).GetModifiers(zone, MakeTower(1101), "방패 버프존"), StatType.FinalDamage), 0.00001f);
        }

        [Test]
        public void 타격_타워는_한도_곱하기_0점1_0점2퍼센트_더()
        {
            TowerController blunt = MakeTower(1105);
            Zone skill1 = MakeZone(MakeTower(1305), true, 30f);
            Zone skill2 = MakeZone(MakeTower(1305), true, 60f);
            var rule = new ShieldBuffRule(() => 20);

            Assert.AreEqual(0.12f + 0.02f, Sum(rule.GetModifiers(skill1, blunt, "방패 버프존"), StatType.FinalDamage), 0.00001f);
            Assert.AreEqual(0.24f + 0.04f, Sum(rule.GetModifiers(skill2, blunt, "방패 버프존"), StatType.FinalDamage), 0.00001f);
        }

        [TestCase(1305, 20, 0)]
        [TestCase(2305, 20, 1)]
        [TestCase(3305, 20, 2)]
        [TestCase(4305, 20, 4)]
        [TestCase(5305, 20, 10)]
        [TestCase(1305, 80, 2)]
        [TestCase(5305, 80, 40)]
        public void 이지스를_펼치면_한도가_티어_비율만큼_늘어남(int id, int baseLimit, int expected)
        {
            TowerController guardian = MakeTower(id);
            guardian.State.Stack.Add(60f);                                  // 스택이 다 참 = 이지스
            Assert.AreEqual(expected, new ShieldBuffRule(() => baseLimit).GetLimitModifier(guardian, baseLimit));
        }

        [Test]
        public void 이지스_전에는_한도가_늘지_않음()
        {
            TowerController guardian = MakeTower(5305);
            guardian.State.Stack.Add(30f);
            Assert.AreEqual(0, new ShieldBuffRule(() => 20).GetLimitModifier(guardian, 20));
        }

        [Test]
        public void 타워_스택을_옮겨_적은_존은_다_차면_스킬_2로_남음()
        {
            var zone = new Zone(MakeTower(1305), true);
            zone.SetStack(30f, 60);
            Assert.AreEqual(ZoneStage.Skill1, zone.Stage);
            Assert.AreEqual(30f, zone.Stack, 0.0001f);

            zone.SetStack(60f, 60);
            zone.SetStack(10f, 60);
            Assert.AreEqual(ZoneStage.Skill2, zone.Stage, "한 번 펼친 이지스는 그대로");
        }

        // ── 방패 디버프: 서프레스 · 락다운 ──

        [Test]
        public void 서프레스는_방어력을_깎음()
        {
            Zone zone = MakeZone(MakeTower(1405), false, 30f);
            EnemyController enemy = MakeEnemy(defense: 100f);
            int action = enemy.Stats.GetMoveRange().Action;
            enemy.Status.Add(new ShieldDebuffRule().CreateEffect(zone, enemy));

            Assert.AreEqual(99.4f, enemy.Stats.Get(StatType.Defense), 0.001f, "−0.6%");
            Assert.AreEqual(action, enemy.Stats.GetMoveRange().Action, "행동 주기는 락다운만");
        }

        [Test]
        public void 락다운은_방어력과_행동_주기_1()
        {
            Zone zone = MakeZone(MakeTower(1405), false, 60f);
            EnemyController enemy = MakeEnemy(defense: 100f, resistance: 50f);
            int action = enemy.Stats.GetMoveRange().Action;
            enemy.Status.Add(new ShieldDebuffRule().CreateEffect(zone, enemy));

            Assert.AreEqual(99.4f, enemy.Stats.Get(StatType.Defense), 0.001f, "−1.2%의 저항 50% = −0.6%");
            Assert.AreEqual(action + 1, enemy.Stats.GetMoveRange().Action, "정수형은 저항하지 않음");
        }

        [Test]
        public void 타격_공격의_기절_확률이_서프레스_5퍼센트_락다운_10퍼센트()
        {
            TowerController owner = MakeTower(1405);
            EnemyController suppress = MakeEnemy(), lockdown = MakeEnemy();
            suppress.Status.Add(new ShieldDebuffRule().CreateEffect(MakeZone(owner, false, 30f), suppress));
            lockdown.Status.Add(new ShieldDebuffRule().CreateEffect(MakeZone(owner, false, 60f), lockdown));

            var blunt = new DamageInfo { Attribute = AttackAttribute.Blunt, Source = DamageSource.Basic };
            Assert.AreEqual(0.05f, suppress.Status.GetStunChanceBonus(blunt), 0.00001f);
            Assert.AreEqual(0.1f, lockdown.Status.GetStunChanceBonus(blunt), 0.00001f);

            var pierce = new DamageInfo { Attribute = AttackAttribute.Pierce, Source = DamageSource.Basic };
            var dot = new DamageInfo { Attribute = AttackAttribute.Blunt, Source = DamageSource.Dot };
            Assert.AreEqual(0f, lockdown.Status.GetStunChanceBonus(pierce), "타격만");
            Assert.AreEqual(0f, lockdown.Status.GetStunChanceBonus(dot), "타워의 기본 공격·스킬만");
        }

        [Test]
        public void 락다운인_적에게_배쉬_기절_확률이_더해짐()
        {
            Zone zone = MakeZone(MakeTower(1405), false, 60f);
            EnemyController enemy = MakeEnemy();
            enemy.Status.Add(new ShieldDebuffRule().CreateEffect(zone, enemy));

            // 0.95 + 락다운 0.1 ≥ 1 → 반드시 기절 (더하지 않았다면 5%로 빗나갈 수 있음)
            enemy.TakeDamage(new DamageInfo { BaseDamage = 1f, Attribute = AttackAttribute.Blunt, Source = DamageSource.Skill, StunChance = 0.95f });
            Assert.IsTrue(enemy.Status.Has(ImmobilizeEffect.ImmobilizeKey));
        }
    }
}
