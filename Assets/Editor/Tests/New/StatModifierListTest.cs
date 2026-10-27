using NUnit.Framework;

namespace SquareTD.Tests
{
    public class StatModifierListTest
    {
        [Test]
        public void 비어_있으면_합은_0이고_기본값_그대로()
        {
            var list = new StatModifierList();
            (float percent, float flat) = list.Sum(StatType.Power);
            Assert.AreEqual(0f, percent);
            Assert.AreEqual(0f, flat);
            Assert.AreEqual(10f, list.Apply(StatType.Power, 10f));
        }

        [Test]
        public void 퍼센트끼리는_더함_곱하지_않음()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("버프 A", StatType.Power, 0.5f));
            list.Add(StatModifier.Percent("버프 B", StatType.Power, 0.25f));

            Assert.AreEqual(0.75f, list.Sum(StatType.Power).percent, 0.0001f);
            Assert.AreEqual(17.5f, list.Apply(StatType.Power, 10f), 0.0001f, "×1.75여야 함 (곱하면 18.75)");
        }

        [Test]
        public void 디버프는_음수로_같이_더함()
        {
            // 기획 3장 예: 10에 +50%, +25% → 17.5, 여기에 -30% → 14.5
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("버프 A", StatType.Power, 0.5f));
            list.Add(StatModifier.Percent("버프 B", StatType.Power, 0.25f));
            list.Add(StatModifier.Percent("디버프", StatType.Power, -0.3f));

            Assert.AreEqual(14.5f, list.Apply(StatType.Power, 10f), 0.0001f);
        }

        [Test]
        public void 정수는_퍼센트를_곱한_뒤에_따로_더함()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("버프", StatType.AttackCount, 0.5f));
            list.Add(StatModifier.Flat("타일", StatType.AttackCount, 1f));

            (float percent, float flat) = list.Sum(StatType.AttackCount);
            Assert.AreEqual(0.5f, percent, 0.0001f);
            Assert.AreEqual(1f, flat, 0.0001f);
            Assert.AreEqual(4f, list.Apply(StatType.AttackCount, 2f), 0.0001f);   // 2 × 1.5 + 1
        }

        [Test]
        public void 다른_스탯은_섞이지_않음()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("버프", StatType.Power, 0.5f));
            list.Add(StatModifier.Flat("타일", StatType.MaxAction, -1f));

            Assert.AreEqual(15f, list.Apply(StatType.Power, 10f), 0.0001f);
            Assert.AreEqual(2f, list.Apply(StatType.MaxAction, 3f), 0.0001f);
            Assert.AreEqual(5f, list.Apply(StatType.CritRate, 5f), 0.0001f);
        }

        [Test]
        public void 출처로_지우면_그_출처의_것만_전부_지워짐()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("검기", StatType.Power, 0.5f));
            list.Add(StatModifier.Percent("검기", StatType.CritRate, 0.1f));
            list.Add(StatModifier.Percent("공격력 타일", StatType.Power, 0.2f));

            Assert.AreEqual(2, list.RemoveBySource("검기"));
            Assert.AreEqual(1, list.All.Count);
            Assert.AreEqual(12f, list.Apply(StatType.Power, 10f), 0.0001f);
            Assert.AreEqual(0f, list.Sum(StatType.CritRate).percent);
        }

        [Test]
        public void 없는_출처를_지우면_0()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Percent("버프", StatType.Power, 0.5f));
            Assert.AreEqual(0, list.RemoveBySource("없는 것"));
            Assert.AreEqual(1, list.All.Count);
        }

        [Test]
        public void 더하면_Changed()
        {
            var list = new StatModifierList();
            int count = 0;
            list.Changed += () => count++;

            list.Add(StatModifier.Flat("타일", StatType.Mastery, 1f));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void 지운_것이_있을_때만_Changed()
        {
            var list = new StatModifierList();
            list.Add(StatModifier.Flat("타일", StatType.Mastery, 1f));
            int count = 0;
            list.Changed += () => count++;

            list.RemoveBySource("없는 것");
            Assert.AreEqual(0, count, "지운 게 없으면 다시 계산할 이유가 없음");

            list.RemoveBySource("타일");
            Assert.AreEqual(1, count);
        }
    }
}
