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
        private TowerController MakeTower(int id, float critRate = 0f, float power = 10f)
        {
            var go = new GameObject($"Tower {id}");
            go.transform.SetParent(m_root.transform);
            var tower = go.AddComponent<TowerController>();
            var definition = new TowerDefinition(id, power, 2, 3, 1, critRate, 0f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);
            tower.Init(definition, 0, null);
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
        public void 등록표_검_광역은_천지일섬()
        {
            Assert.IsInstanceOf<SwordSplashSkill>(SkillRegistry.CreateAttackSkill(TowerColor.Red, TowerEmblem.Sword, m_grid, m_occupancy, null));
        }

        [Test]
        public void 등록표_검_단일은_이십사수매화검법()
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

        // ── 검 광역: 천지일섬 ──

        [Test]
        public void 천지일섬은_맵_전체에_한_번씩_피해만()
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
        public void 천지일섬은_베기()
        {
            OccupyingFakeTarget target = MakeTarget(0);
            new SwordSplashSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1101), null);
            Assert.AreEqual(AttackAttribute.Slash, target.Hits[0].Attribute);
        }

        // ── 검 단일: 이십사수매화검법 ──

        [Test]
        public void 이십사수매화는_24번에_나눠_총_피해는_같음()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201), target);

            Assert.AreEqual(24, target.Hits.Count);
            Assert.AreEqual(20f / 24f, target.Hits[0].BaseDamage, 0.0001f);
            Assert.AreEqual(20f, Total(target), 0.001f);
        }

        [Test]
        public void 이십사수매화의_치명은_한_번에_24발_전부()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201, critRate: 1f), target);

            Assert.AreEqual(24, target.Hits.Count);
            foreach (DamageInfo hit in target.Hits) Assert.IsTrue(hit.IsCritical);
            Assert.AreEqual(40f, Total(target), 0.001f, "20 × 치명 배율 2");
        }

        [Test]
        public void 이십사수매화는_스택을_쓰면_총_피해가_1점6배()
        {
            OccupyingFakeTarget target = MakeTarget(2);
            TowerController tower = MakeTower(1201);
            tower.State.Stack.Add(60f);

            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(tower, target);

            Assert.AreEqual(32f, Total(target), 0.001f);
        }

        [Test]
        public void 이십사수매화는_발동시킨_적에게만()
        {
            OccupyingFakeTarget main = MakeTarget(2), other = MakeTarget(2);
            new SwordTargetSkill(m_grid, m_occupancy, new StackModeSettings()).Fire(MakeTower(1201), main);

            Assert.AreEqual(24, main.Hits.Count);
            Assert.AreEqual(0, other.Hits.Count, "같은 칸이어도 다른 적은 안 맞음");
        }

        [Test]
        public void 이십사수매화_도중에_죽으면_남은_조각은_안_들어감()
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

        // ── 검 버프: 검기 · 검강 ──

        [Test]
        public void 등록표_검_버프_디버프_규칙()
        {
            Assert.IsInstanceOf<SwordBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Sword));
            Assert.IsInstanceOf<SwordDebuffRule>(SkillRegistry.GetDebuffRule(TowerEmblem.Sword));
            Assert.IsInstanceOf<TestBuffRule>(SkillRegistry.GetBuffRule(TowerEmblem.Darkness), "아직 없는 문양은 시험 규칙");
        }

        [Test]
        public void 검기는_지금_스택만큼_공격력()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 30f);
            List<StatModifier> modifiers = new SwordBuffRule().GetModifiers(zone, MakeTower(1101), "검 버프존");

            Assert.AreEqual(0.006f, Sum(modifiers, StatType.Power), 0.00001f, "0.001 × 10 × 2 × 30 = 0.6%");
            Assert.AreEqual(0f, Sum(modifiers, StatType.FinalDamage), "최종 피해는 검강만");
        }

        [Test]
        public void 검강_브론즈는_1점2퍼센트_다이아는_18퍼센트()
        {
            Zone bronze = MakeZone(MakeTower(1301), true, 60f);
            Zone diamond = MakeZone(MakeTower(1301, power: 150f), true, 60f);
            TowerController receiver = MakeTower(1101);

            Assert.AreEqual(ZoneStage.Skill2, diamond.Stage);
            Assert.AreEqual(0.012f, Sum(new SwordBuffRule().GetModifiers(bronze, receiver, "검 버프존"), StatType.Power), 0.00001f);
            Assert.AreEqual(0.18f, Sum(new SwordBuffRule().GetModifiers(diamond, receiver, "검 버프존"), StatType.Power), 0.00001f);
        }

        [Test]
        public void 검강은_최종_피해_10퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1301), true, 60f);
            List<StatModifier> modifiers = new SwordBuffRule().GetModifiers(zone, MakeTower(1101), "검 버프존");
            Assert.AreEqual(0.1f, Sum(modifiers, StatType.FinalDamage), 0.00001f);
        }

        [Test]
        public void 베기_타워는_치명_확률이_검기_5퍼센트_검강_10퍼센트()
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

        // ── 검 디버프: 사기 · 귀기 ──

        [Test]
        public void 귀기는_최대_체력_20퍼센트_현재_체력도_같은_비율()
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
        public void 귀기는_저항_50퍼센트면_최대_체력_10퍼센트()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);
            EnemyController enemy = MakeEnemy(resistance: 50f);
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(max * 0.9f, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 사기는_최대_체력을_줄이지_않음()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 30f);
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(max, enemy.MaxHp, 0.001f);
        }

        [Test]
        public void 귀기가_다시_걸려도_최대_체력은_한_번만_줄어듦()
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
        public void 사기는_매_턴_최대_체력의_퍼센트_피해이고_방어를_무시함()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 30f);       // 0.6%
            EnemyController enemy = MakeEnemy(defense: 100f);
            float max = enemy.MaxHp;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.OnTurn();
            Assert.AreEqual(max - max * 0.006f, enemy.CurrentHp, 0.001f);
        }

        [Test]
        public void 귀기의_매_턴_피해는_줄어든_최대_체력_기준()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 60f);       // 1.2%
            EnemyController enemy = MakeEnemy();
            float max = enemy.MaxHp * 0.8f;

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            enemy.Status.OnTurn();
            Assert.AreEqual(max - max * 0.012f, enemy.CurrentHp, 0.001f);
        }

        [Test]
        public void 사기면_베기_피해의_10퍼센트가_고정_피해()
        {
            Zone zone = MakeZone(MakeTower(1401), false, 30f);
            EnemyController enemy = MakeEnemy(defense: 100f);          // 방어 100 → 피해 절반
            var slash = new DamageInfo { BaseDamage = 100f, Attribute = AttackAttribute.Slash };
            Assert.AreEqual(50f, enemy.PreviewDamage(slash), 0.001f);

            enemy.Status.Add(new SwordDebuffRule().CreateEffect(zone, enemy));
            Assert.AreEqual(55f, enemy.PreviewDamage(slash), 0.001f, "고정 10 + 나머지 90의 절반 45");
        }

        [Test]
        public void 귀기면_베기_피해의_20퍼센트가_고정_피해()
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
    }
}
