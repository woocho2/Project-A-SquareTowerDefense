using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SquareTD.Tests
{
    // 수업 25 2단계: 상태 효과 목록 (겹침 규칙, 유지, 수정자, 훅)
    public class EnemyStatusEffectsTest
    {
        // 훅이 불렸는지 세는 시험 효과
        private class CountingEffect : StatusEffect
        {
            private readonly string m_key;
            public int TurnCalls;
            public int ExpireCalls;
            public System.Action<EnemyController> OnTurnAction;

            public CountingEffect(string key, int duration) : base(duration) { m_key = key; }
            public override string Key => m_key;
            public override void OnTurn(EnemyController enemy) { TurnCalls++; OnTurnAction?.Invoke(enemy); }
            public override void OnExpire(EnemyController enemy) => ExpireCalls++;
            public override string Describe() => m_key;
        }

        private class PermanentEffect : StatusEffect
        {
            public PermanentEffect() : base(0) { }
            public override string Key => "파쇄";
            public override bool IsPermanent => true;
            public override string Describe() => "영구 파쇄";
        }

        private GameObject m_go;
        private EnemyDefinition m_definition;
        private EnemyController m_enemy;

        [SetUp]
        public void SetUp()
        {
            m_definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(m_definition);
            so.FindProperty("<Kind>k__BackingField").enumValueIndex = (int)EnemyKind.Normal;
            so.FindProperty("<MaxHP>k__BackingField").floatValue = 100f;
            so.FindProperty("<Defense>k__BackingField").floatValue = 100f;
            so.FindProperty("<Action>k__BackingField").intValue = 3;
            so.FindProperty("<SpeedMin>k__BackingField").intValue = 2;
            so.FindProperty("<SpeedMax>k__BackingField").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            m_go = new GameObject("TestEnemy");
            m_enemy = m_go.AddComponent<EnemyController>();
            m_enemy.Setup(m_definition, null, 1);
        }

        [TearDown]
        public void CleanUp()
        {
            Object.DestroyImmediate(m_go);
            Object.DestroyImmediate(m_definition);
        }

        private EnemyStatusEffects MakeStatus(float resistance = 0f) =>
            new EnemyStatusEffects(m_enemy, m_enemy.Stats.Modifiers, new EnemyResistance(resistance));

        private static TimedModifierEffect Shatter(float percent, int duration) =>
            new TimedModifierEffect("파쇄", duration, percent, $"방어 −{percent * 100f:0}%",
                StatModifier.Percent("파쇄", StatType.Defense, -percent));

        // ── 넣기와 겹침 ──

        [Test]
        public void 걸면_목록에_들어감()
        {
            EnemyStatusEffects status = MakeStatus();
            Assert.IsTrue(status.Add(Shatter(0.2f, 4)));
            Assert.IsTrue(status.Has("파쇄"));
            Assert.AreEqual(1, status.Effects.Count);
        }

        [Test]
        public void 걸려_있는_동안_수정자가_붙고_빠지면_떨어짐()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(Shatter(0.2f, 4));
            Assert.AreEqual(80f, m_enemy.Stats.Get(StatType.Defense), 0.0001f, "방어 100 × (1 − 20%)");

            status.Remove("파쇄");
            Assert.AreEqual(100f, m_enemy.Stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 같은_Key는_더_센_것으로_바뀜()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(Shatter(0.2f, 4));
            Assert.IsTrue(status.Add(Shatter(0.3f, 4)));

            Assert.AreEqual(1, status.Effects.Count, "겹치지 않고 하나만");
            Assert.AreEqual(70f, m_enemy.Stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 더_약한_것은_무시()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(Shatter(0.3f, 4));
            Assert.IsFalse(status.Add(Shatter(0.2f, 4)));
            Assert.AreEqual(70f, m_enemy.Stats.Get(StatType.Defense), 0.0001f);
        }

        [Test]
        public void 같은_세기면_유지_시간만_새로()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(Shatter(0.2f, 4));
            status.OnActed();
            status.OnActed();
            Assert.AreEqual(2, status.Find("파쇄").RemainingActions);

            status.Add(Shatter(0.2f, 4));
            Assert.AreEqual(4, status.Find("파쇄").RemainingActions);
        }

        [Test]
        public void 영구가_있으면_같은_Key의_스킬1은_무시()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(new PermanentEffect());
            Assert.IsFalse(status.Add(Shatter(0.5f, 4)));
            Assert.IsTrue(status.Find("파쇄").IsPermanent);
        }

        [Test]
        public void 영구는_스킬1을_밀어냄()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(Shatter(0.5f, 4));
            Assert.IsTrue(status.Add(new PermanentEffect()));
            Assert.IsTrue(status.Find("파쇄").IsPermanent);
        }

        [Test]
        public void 다른_Key는_함께_걸림()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(Shatter(0.2f, 4));
            status.Add(new CountingEffect("출혈", 3));
            Assert.AreEqual(2, status.Effects.Count);
        }

        // ── 유지 시간 ──

        [Test]
        public void 행동할_때마다_한_칸씩_줄고_다_되면_빠짐()
        {
            EnemyStatusEffects status = MakeStatus();
            var effect = new CountingEffect("출혈", 2);
            status.Add(effect);

            status.OnActed();
            Assert.IsTrue(status.Has("출혈"));
            Assert.AreEqual(1, effect.RemainingActions);

            status.OnActed();
            Assert.IsFalse(status.Has("출혈"));
            Assert.AreEqual(1, effect.ExpireCalls, "끝날 때 OnExpire 한 번");
        }

        [Test]
        public void 영구는_행동해도_줄지_않음()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(new PermanentEffect());
            for (int i = 0; i < 10; i++) status.OnActed();
            Assert.IsTrue(status.Has("파쇄"));
        }

        [Test]
        public void 정화는_영구까지_전부_지움()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(new PermanentEffect());
            status.Add(new CountingEffect("출혈", 3));
            Assert.AreEqual(2, status.RemoveAll());
            Assert.AreEqual(0, status.Effects.Count);
        }

        // ── 행동 불가 (강한 CC기) ──

        [Test]
        public void 행동_불가는_다음_행동_한_번만_0_0()
        {
            EnemyStatusEffects status = MakeStatus();
            status.Add(new ImmobilizeEffect("기절"));

            MoveRange stunned = status.DecideMove(new MoveRange(2, 4, 3));
            Assert.AreEqual(0, stunned.Min);
            Assert.AreEqual(0, stunned.Max);
            Assert.AreEqual(3, stunned.Action, "행동 주기는 그대로");

            status.OnActed();                                       // 그 행동이 끝남
            MoveRange next = status.DecideMove(new MoveRange(2, 4, 3));
            Assert.AreEqual(2, next.Min, "다음 행동은 정상");
            Assert.AreEqual(4, next.Max);
        }

        [Test]
        public void 강한_CC에_걸리면_저항이_오름()
        {
            var resistance = new EnemyResistance(0f);
            var status = new EnemyStatusEffects(m_enemy, m_enemy.Stats.Modifiers, resistance);
            Assert.IsTrue(status.Add(new ImmobilizeEffect("기절")));
            Assert.AreEqual(0f, resistance.Current, "기본 0이면 올라도 0");

            var resistance10 = new EnemyResistance(10f);
            var status10 = new EnemyStatusEffects(m_enemy, m_enemy.Stats.Modifiers, resistance10);
            if (status10.Add(new ImmobilizeEffect("기절"))) Assert.AreEqual(20f, resistance10.Current, 0.0001f);
        }

        [Test]
        public void 이미_행동_불가면_무시하고_저항도_안_바뀜()
        {
            var resistance = new EnemyResistance(0f);
            var status = new EnemyStatusEffects(m_enemy, m_enemy.Stats.Modifiers, resistance);
            status.Add(new ImmobilizeEffect("기절"));
            Assert.IsFalse(status.Add(new ImmobilizeEffect("속박")));
            Assert.AreEqual(1, status.Effects.Count);
        }

        [Test]
        public void 저항_100이면_강한_CC가_안_걸림()
        {
            EnemyStatusEffects status = MakeStatus(resistance: 100f);
            Assert.IsFalse(status.Add(new ImmobilizeEffect("기절")));
            Assert.IsFalse(status.Has(ImmobilizeEffect.ImmobilizeKey));
        }

        // ── 훅 ──

        [Test]
        public void 매_턴_훅이_목록_전체에_불림()
        {
            EnemyStatusEffects status = MakeStatus();
            var a = new CountingEffect("가", 5);
            var b = new CountingEffect("나", 5);
            status.Add(a);
            status.Add(b);

            status.OnTurn();
            Assert.AreEqual(1, a.TurnCalls);
            Assert.AreEqual(1, b.TurnCalls);
        }

        [Test]
        public void 훅을_도는_중에_걸린_효과는_다_돈_뒤에_들어감()
        {
            EnemyStatusEffects status = MakeStatus();
            var spreader = new CountingEffect("번지는 것", 5);
            var spawned = new CountingEffect("번진 것", 5);
            spreader.OnTurnAction = _ => status.Add(spawned);
            status.Add(spreader);

            Assert.DoesNotThrow(() => status.OnTurn(), "도는 중에 목록이 바뀌어 터지지 않음");
            Assert.IsTrue(status.Has("번진 것"), "다 돈 뒤에 들어감");
            Assert.AreEqual(0, spawned.TurnCalls, "같은 차례에는 불리지 않음");
        }

        [Test]
        public void 도는_중에_적이_죽으면_남은_훅은_부르지_않음()
        {
            EnemyStatusEffects status = MakeStatus();
            var killer = new CountingEffect("죽이는 것", 5) { OnTurnAction = enemy => enemy.Health.TakeDamage(9999f) };
            var after = new CountingEffect("뒤의 것", 5);
            status.Add(killer);
            status.Add(after);

            status.OnTurn();
            Assert.AreEqual(1, killer.TurnCalls);
            Assert.AreEqual(0, after.TurnCalls);
        }

        [Test]
        public void 설명_문구()
        {
            Assert.AreEqual("방어 −20% (4회 행동 남음)", StatusTextFormatter.Describe(Shatter(0.2f, 4)));
            StringAssert.Contains("[영구]", StatusTextFormatter.Describe(new PermanentEffect()));
            StringAssert.Contains("[강한 CC]", StatusTextFormatter.Describe(new ImmobilizeEffect("기절")));
        }
    }
}
