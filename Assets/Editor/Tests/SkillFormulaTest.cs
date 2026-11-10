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
        public void 스킬1은_Power()
        {
            Assert.AreEqual(1f, SkillFormula.SupportSkill1(1f), Eps);
        }

        [Test]
        public void 스킬2는_Power_곱하기_1_더하기_AbilityValue()
        {
            Assert.AreEqual(1.05f, SkillFormula.SupportSkill2(1f, 0.05f), Eps, "기준값: Power 1, AbilityValue 0.05");
            Assert.AreEqual(1.6f, SkillFormula.SupportSkill2(1f, 0.6f), Eps);
        }

        [Test]
        public void 차지를_쓰면_AbilityValue에_1_더하기_차지_곱하기_ChargeValue()
        {
            Assert.AreEqual(125f, SkillFormula.ChargedAbility(50f, 100f, 1.5f), Eps, "50 × (1 + 100 × 0.01 × 1.5) = 125");
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
        public void 유지_시간은_레벨_곱하기_Tier()
        {
            Assert.AreEqual(1, SkillFormula.EffectDuration(1, 1));
            Assert.AreEqual(12, SkillFormula.EffectDuration(3, 4));
            Assert.AreEqual(25, SkillFormula.EffectDuration(5, 5));
        }
    }
}
