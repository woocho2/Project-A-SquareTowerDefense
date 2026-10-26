using NUnit.Framework;

namespace SquareTD.Tests
{
    // T7 — 20번 4-1장 [스킬의 공통 식]의 예를 그대로
    public class SkillFormulaTest
    {
        private const float Eps = 0.0001f;

        // ── 공격 스킬 ──

        [Test]
        public void 기본_스킬은_위력_곱하기_계수()
        {
            Assert.AreEqual(20f, SkillFormula.AttackSkill(10f, 2f), Eps);
        }

        [Test]
        public void 스택_60_스택값_1이면_1점6배()
        {
            Assert.AreEqual(32f, SkillFormula.WithStack(20f, 60f, 1f), Eps);
        }

        [Test]
        public void 스택_30_스택값_2여도_1점6배()
        {
            Assert.AreEqual(32f, SkillFormula.WithStack(20f, 30f, 2f), Eps, "미스릴: 최대 스택 절반, 스택값 2배 → 다 쌓았을 때의 값은 그대로");
        }

        [Test]
        public void 스택_15_스택값_4여도_1점6배()
        {
            Assert.AreEqual(32f, SkillFormula.WithStack(20f, 15f, 4f), Eps, "미스릴 + 컬러 레벨 4");
        }

        [Test]
        public void 스택이_0이면_기본과_같음()
        {
            Assert.AreEqual(20f, SkillFormula.WithStack(20f, 0f, 1f), Eps);
        }

        // ── 버프·디버프 ──

        [Test]
        public void 스킬1은_스택이_없으면_0()
        {
            Assert.AreEqual(0f, SkillFormula.SupportSkill1(10f, 2f, 0f, 1f), Eps);
        }

        [Test]
        public void 스킬1은_스택만큼_오름()
        {
            // 기획 예: Power 10, AbilityValue 2, maxStack 60 → 스킬 1은 0 ~ 1.2%
            Assert.AreEqual(0.006f, SkillFormula.SupportSkill1(10f, 2f, 30f, 1f), Eps, "반쯤 쌓이면 0.6%");
            Assert.AreEqual(0.012f, SkillFormula.SupportSkill1(10f, 2f, 60f, 1f), Eps, "다 쌓이면 1.2%");
        }

        [Test]
        public void 스킬2는_최대_스택의_값()
        {
            Assert.AreEqual(0.012f, SkillFormula.SupportSkill2(10f, 2f, 60f, 1f), Eps);
        }

        [Test]
        public void 다_쌓은_스킬1과_스킬2는_같음()
        {
            Assert.AreEqual(SkillFormula.SupportSkill1(10f, 2f, 60f, 1f), SkillFormula.SupportSkill2(10f, 2f, 60f, 1f), Eps);
        }

        [Test]
        public void 컬러_레벨_2에서_4까지는_2점4퍼센트()
        {
            // 레벨 2: AbilityValue ×2
            Assert.AreEqual(0.024f, SkillFormula.SupportSkill2(10f, 4f, 60f, 1f), Eps);
            // 레벨 4: AbilityValue ×2, 최대 스택 30, StackValue 2 → 값은 그대로
            Assert.AreEqual(0.024f, SkillFormula.SupportSkill2(10f, 4f, 30f, 2f), Eps);
        }

        [Test]
        public void 컬러_레벨_5에서는_4점8퍼센트()
        {
            Assert.AreEqual(0.048f, SkillFormula.SupportSkill2(10f, 8f, 30f, 2f), Eps);
        }

        // ── 저항 ──

        [Test]
        public void 저항력_50이면_절반()
        {
            Assert.AreEqual(0.09f, SkillFormula.Resist(0.18f, 50f), Eps, "기획 예: 18%에 저항 50% → 9%");
        }

        [Test]
        public void 저항력_0이면_그대로()
        {
            Assert.AreEqual(0.18f, SkillFormula.Resist(0.18f, 0f), Eps);
        }

        [Test]
        public void 저항력은_100에서_멈춤()
        {
            Assert.AreEqual(0f, SkillFormula.Resist(0.18f, 100f), Eps);
            Assert.AreEqual(0f, SkillFormula.Resist(0.18f, 150f), Eps, "100을 넘어도 음수가 되지 않음");
        }

        // ── 유지 시간 ──

        [Test]
        public void 유지_시간은_레벨_곱하기_2()
        {
            Assert.AreEqual(2, SkillFormula.DefaultDuration(1));
            Assert.AreEqual(6, SkillFormula.DefaultDuration(3));
            Assert.AreEqual(10, SkillFormula.DefaultDuration(5));
            Assert.AreEqual(12, SkillFormula.DefaultDuration(6), "대지의 만개로 레벨 6이면 12");
        }
    }
}
