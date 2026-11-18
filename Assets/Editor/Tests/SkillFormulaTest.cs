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
        public void 유지_시간은_행동_2번으로_고정()
        {
            Assert.AreEqual(2, SkillFormula.EffectActions);
        }

        [Test]
        public void 개수는_레벨_곱하기_Tier()
        {
            Assert.AreEqual(1, SkillFormula.LevelTierCount(1, 1));
            Assert.AreEqual(12, SkillFormula.LevelTierCount(3, 4));
        }
    }
}
