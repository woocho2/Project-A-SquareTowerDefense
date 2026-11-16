using System.Collections.Generic;
using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerStatCalculatorTest
    {
        private static TowerDefinition Def(int id, float power = 10f, float critRate = 0.1f) =>
            new TowerDefinition(id, power, 2, 3, 1, critRate, 1.5f, 1, 60, 5, 2f, 5f, TargetPriority.Closest);

        private static TowerFinalStats Calc(TowerDefinition definition, TowerDefinition tier1, int level, params StatModifier[] modifiers)
        {
            var state = new TowerState(definition);
            foreach (StatModifier modifier in modifiers) state.Modifiers.Add(modifier);
            return TowerStatCalculator.Calculate(definition, tier1, state, level);
        }

        [Test]
        public void 브론즈_레벨1은_CSV_값_그대로()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 1);

            Assert.AreEqual(10f, stats.Power, 0.0001f);
            Assert.AreEqual(2, stats.Range);
            Assert.AreEqual(3, stats.Action);
            Assert.AreEqual(1, stats.AttackCount);
            Assert.AreEqual(0.1f, stats.CriticalRate, 0.0001f);
            Assert.AreEqual(1.5f, stats.CriticalDamage, 0.0001f);
            Assert.AreEqual(1, stats.Mastery);
            Assert.AreEqual(60, stats.Stack);
            Assert.AreEqual(5f, stats.SkillCount, 0.0001f);
            Assert.AreEqual(2f, stats.AbilityValue, 0.0001f);
            Assert.AreEqual(1f, stats.StackValue, 0.0001f);
            Assert.AreEqual(1, stats.ColorLevel);
        }

        [Test]
        public void 다이아는_Power_x6_15_Action_마이너스1_Mastery_2()
        {
            TowerFinalStats stats = Calc(Def(4101), Def(1101), 1);

            Assert.AreEqual(61.5f, stats.Power, 0.0001f);
            Assert.AreEqual(2, stats.Action, "3 − 1");
            Assert.AreEqual(2, stats.Mastery);
            Assert.AreEqual(2, stats.AttackCount);
            Assert.AreEqual(45, stats.Stack, "60 × 0.75");
            Assert.AreEqual(2f, stats.StackValue, 0.0001f);
            Assert.AreEqual(4f, stats.SkillCount, 0.0001f, "5 × 0.8 = 4. SkillCount는 소수로 둠 (20번 10장)");
        }

        [Test]
        public void 스탯은_1티어_정의에서_계산함()
        {
            TowerFinalStats stats = Calc(Def(4101, power: 999f), Def(1101), 1);

            Assert.AreEqual(61.5f, stats.Power, 0.0001f, "4티어(다이아) CSV의 Power가 아니라 1티어 10 × 6.15");
        }

        [Test]
        public void 퍼플도_티어가_오르면_Duration이_줄어듦()
        {
            TowerFinalStats stats = Calc(Def(3401), Def(1401), 1);

            Assert.AreEqual(4.5f, stats.SkillCount, 0.0001f, "5 × 0.9 (골드)");
        }

        [Test]
        public void 다이아에_레벨4면_최대_스택과_StackValue가_곱해짐()
        {
            TowerFinalStats stats = Calc(Def(4101), Def(1101), 4);

            Assert.AreEqual(22, stats.Stack, "60 × 0.75 × 0.5 = 22.5 → 반올림 (짝수로) 22");
            Assert.AreEqual(3f, stats.StackValue, 0.0001f, "2 × 1.5");
        }

        [Test]
        public void 레벨5는_AbilityValue_x3()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 5);

            Assert.AreEqual(6f, stats.AbilityValue, 0.0001f, "2 × 3");
            Assert.AreEqual(1, stats.AttackCount, "컬러 강화는 AttackCount를 올리지 않음");
            Assert.AreEqual(2, stats.Mastery, "레벨 5는 Mastery +1");
            Assert.AreEqual(5f, stats.SkillCount, 0.0001f, "공격 타워의 Duration은 컬러 강화로 안 바뀜");
        }

        [Test]
        public void 블루도_컬러_강화로_AttackCount가_오르지_않음()
        {
            TowerFinalStats stats = Calc(Def(1201), null, 5);

            Assert.AreEqual(1, stats.AttackCount);
        }

        [Test]
        public void 레벨_5는_ColorLevel_5()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 5);

            Assert.AreEqual(5, stats.ColorLevel);
            Assert.AreEqual(6f, stats.AbilityValue, 0.0001f, "2 × 3");
        }

        [Test]
        public void 버프_50과_25는_x1_75()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 1,
                StatModifier.Percent("버프 A", StatType.Power, 0.5f),
                StatModifier.Percent("버프 B", StatType.Power, 0.25f));

            Assert.AreEqual(17.5f, stats.Power, 0.0001f, "곱하면 18.75라 틀림");
        }

        [Test]
        public void 버프는_티어_표를_지난_값에_붙음()
        {
            TowerFinalStats stats = Calc(Def(4101), Def(1101), 1, StatModifier.Percent("버프", StatType.Power, 0.5f));

            Assert.AreEqual(92.25f, stats.Power, 0.0001f, "61.5 × 1.5");
        }

        [Test]
        public void 버프_타워는_수정자를_받지_않음()
        {
            TowerFinalStats stats = Calc(Def(1301), null, 5, StatModifier.Percent("버프", StatType.Power, 0.5f));

            Assert.AreEqual(10f, stats.Power, 0.0001f);
            Assert.AreEqual(5, stats.ColorLevel);
        }

        [Test]
        public void 치명_확률은_최대_1()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 1, StatModifier.Flat("버프", StatType.CritRate, 2f));

            Assert.AreEqual(1f, stats.CriticalRate, 0.0001f);
        }

        [Test]
        public void 수정자가_Action을_1_아래로_깎아도_최소_1()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 1, StatModifier.Flat("버프", StatType.MaxAction, -5f));

            Assert.AreEqual(1, stats.Action);
        }

        [Test]
        public void 결과는_만든_뒤_바뀌지_않음()
        {
            var values = new float[TowerFinalStats.StatCount];
            values[(int)StatType.Power] = 10f;

            var stats = new TowerFinalStats(values, values, 1, new List<StatSource>());
            values[(int)StatType.Power] = 999f;

            Assert.AreEqual(10f, stats.Power, 0.0001f);
            Assert.AreEqual(10f, stats.GetBase(StatType.Power), 0.0001f);
        }

        [Test]
        public void 출처_퍼센트는_기본값_x_비율()
        {
            TowerFinalStats stats = Calc(Def(4101), Def(1101), 1, StatModifier.Percent("블레싱", StatType.Power, 0.5f));

            Assert.AreEqual(61.5f, stats.GetBase(StatType.Power), 0.0001f);
            Assert.AreEqual(92.25f, stats.Power, 0.0001f);
            Assert.AreEqual(1, stats.Sources.Count);
            Assert.AreEqual("블레싱", stats.Sources[0].Source);
            Assert.AreEqual(StatType.Power, stats.Sources[0].Stat);
            Assert.AreEqual(30.75f, stats.Sources[0].Amount, 0.0001f, "위력 92.25 = 기본 61.5 + 블레싱 30.75");
        }

        [Test]
        public void 출처_정수는_값_그대로()
        {
            TowerFinalStats stats = Calc(Def(1101), null, 1, StatModifier.Flat("카우치", StatType.AttackCount, 1f));

            Assert.AreEqual(1f, stats.GetBase(StatType.AttackCount), 0.0001f);
            Assert.AreEqual(2, stats.AttackCount);
            Assert.AreEqual(1f, stats.Sources[0].Amount, 0.0001f);
        }

        [Test]
        public void 버프_타워는_출처가_비어_있음()
        {
            TowerFinalStats stats = Calc(Def(1301), null, 1, StatModifier.Percent("버프", StatType.Power, 0.5f));

            Assert.AreEqual(0, stats.Sources.Count);
        }
    }
}
