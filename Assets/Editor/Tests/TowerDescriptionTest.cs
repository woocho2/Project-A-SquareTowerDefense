using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SquareTD.Tests
{
    public class TowerDescriptionTest
    {
        private static readonly TowerDescriptionContext Context = new TowerDescriptionContext(20, 100, 320, false);
        private static TowerDefinition Definition(TowerColor color, TowerEmblem emblem) =>
            new TowerDefinition(TowerId.Make(1, color, emblem), 30, 3, 4, 3, .1f, .5f, 2, 12, 1.7f, 1, TargetPriority.Closest);

        private static TowerFinalStats SupportStats() => Stats(0, SkillFormula.SupportBasePower, 0.1f);

        private static TowerFinalStats Stats(float skillDamageBonus = 0, float power = 30, float abilityValue = 1.7f)
        {
            var values = new float[TowerFinalStats.StatCount];
            values[(int)StatType.Power] = power; values[(int)StatType.AbilityValue] = abilityValue;
            values[(int)StatType.Range] = 3; values[(int)StatType.MaxAction] = 4; values[(int)StatType.AttackCount] = 3;
            values[(int)StatType.Mastery] = 2;
            values[(int)StatType.Duration] = 12; values[(int)StatType.CritDamage] = .5f; values[(int)StatType.SkillDamageBonus] = skillDamageBonus;
            return new TowerFinalStats(values, values, 1, new List<StatSource>());
        }

        [Test]
        public void SwordSkill_Shows51AndExplainsItsActualFactors()
        {
            var doc = TowerDescriptionBuilder.Build(Definition(TowerColor.Red, TowerEmblem.Sword), Stats(), null, Context);
            Assert.That(TowerDescription.Plain(doc.Skill), Is.EqualTo("적 전체에게 51의 피해를 줍니다."));
            Assert.That(doc.Values["skill.damage"].Amount, Is.EqualTo(51).Within(.001));
            Assert.That(doc.Values["skill.damage"].Explanation, Does.Contain("30 x 1.7"));
        }

        [Test]
        public void SkillDamageBonus_MultipliesAttackSkill()
        {
            var doc = TowerDescriptionBuilder.Build(Definition(TowerColor.Red, TowerEmblem.Sword), Stats(.5f), null, Context);
            Assert.That(doc.Values["skill.damage"].Amount, Is.EqualTo(76.5f).Within(.001));
            Assert.That(doc.Values["skill.damage"].Explanation, Does.Contain("스킬 피해 보정"));
        }

        [Test]
        public void Support_BasicAndSkillUseDifferentFormulaAndNames()
        {
            var doc = TowerDescriptionBuilder.Build(Definition(TowerColor.Yellow, TowerEmblem.Sword), SupportStats(), null, Context);
            Assert.That(doc.BasicTitle, Is.EqualTo("브레이브 소드 / 기본 버프"));
            Assert.That(doc.SkillTitle, Is.EqualTo("글로리 소드 / 스킬 버프"));
            Assert.That(doc.Values["basic.amount"].Amount, Is.EqualTo(50).Within(.001));
            Assert.That(doc.Values["skill.amount"].Amount, Is.EqualTo(55).Within(.001));
            Assert.That(doc.Basic, Does.Not.Contain("스킬 카운트"));
            Assert.That(doc.Basic, Does.Not.Contain("최종 피해"));
            Assert.That(doc.Skill, Does.Contain("최종 피해"));
        }

        [Test]
        public void Support_IgnoresAttackSkillDamageBonus()
        {
            var doc = TowerDescriptionBuilder.Build(Definition(TowerColor.Yellow, TowerEmblem.Sword), Stats(.5f, SkillFormula.SupportBasePower, 0.1f), null, Context);
            Assert.That(doc.Values["skill.amount"].Amount, Is.EqualTo(55).Within(.001));
        }

        [Test]
        public void HammerDebuff_ExplainsGoldInsteadOfRemovedGems()
        {
            var doc = TowerDescriptionBuilder.Build(Definition(TowerColor.Purple, TowerEmblem.Hammer), SupportStats(), null, Context);
            Assert.That(doc.BasicTitle, Is.EqualTo("크랙 / 기본 디버프"));
            Assert.That(doc.SkillTitle, Is.EqualTo("잭팟 / 스킬 디버프"));
            Assert.That(doc.Skill, Does.Contain("골드"));
            Assert.That(doc.Skill, Does.Not.Contain("젬"));
            Assert.That(doc.Values["skill.gold"].Amount, Is.EqualTo(100));
        }

        [Test]
        public void WindBuff_PenaltyEasesWithScale()
        {
            var doc = TowerDescriptionBuilder.Build(Definition(TowerColor.Yellow, TowerEmblem.Wind), SupportStats(), null, Context);
            Assert.That(doc.Values["basic.critRate"].Amount, Is.EqualTo(-6).Within(.001));
            Assert.That(doc.Values["basic.critDamage"].Amount, Is.EqualTo(-20).Within(.001));
            Assert.That(doc.Values["skill.critDamage"].Amount, Is.EqualTo(-18).Within(.001), "−20 × (2 − 1.1)");
        }

        [Test]
        public void EarthDebuff_PreviewsNextGrowthWithoutMutatingState()
        {
            TowerDefinition definition = Definition(TowerColor.Purple, TowerEmblem.Earth);
            var state = new TowerState(definition); state.Counters[Growth.Key] = 1;
            var doc = TowerDescriptionBuilder.Build(definition, SupportStats(), state, Context);
            Assert.That(doc.Values["basic.growthcut"].Amount, Is.EqualTo(5).Within(.001), "2.5% × 다음 성장 2");
            Assert.That(doc.Values["skill.growthcut"].Amount, Is.EqualTo(2.75f).Within(.001), "2.5% × 1.1 × 성장 1");
            Assert.That(state.GetCounter(Growth.Key), Is.EqualTo(1));
        }

        [Test]
        public void DurationAndCounts_AreTakenFromActualRules()
        {
            var spear = TowerDescriptionBuilder.Build(Definition(TowerColor.Blue, TowerEmblem.Spear), Stats(), null, Context);
            Assert.That(spear.Values["skill.hits"].Amount, Is.EqualTo(1));
            var fire = TowerDescriptionBuilder.Build(Definition(TowerColor.Blue, TowerEmblem.Fire), Stats(), null, Context);
            Assert.That(fire.Values["skill.duration"].Amount, Is.EqualTo(3));
            var axe = TowerDescriptionBuilder.Build(Definition(TowerColor.Blue, TowerEmblem.Axe), Stats(), null, Context);
            Assert.That(TowerDescription.Plain(axe.Skill), Does.Contain("행동 카운트를 1"));
            Assert.That(TowerDescription.Plain(axe.Skill), Does.Not.Contain("절반"));
        }


        [Test]
        public void FireAndPrism_IncludeTheirAdditionalPowerBonuses()
        {
            var fire = TowerDescriptionBuilder.Build(Definition(TowerColor.Yellow, TowerEmblem.Fire), SupportStats(), null, Context);
            Assert.That(fire.Values["basic.power"].Amount, Is.EqualTo(-45).Within(.001), "2배 공격과 합쳐 +10%");
            Assert.That(fire.Values["skill.power"].Amount, Is.EqualTo(-34.5f).Within(.001), "(1 + 11%) / 2 − 1 + 라이징 10%");
            var prism = TowerDescriptionBuilder.Build(Definition(TowerColor.Red, TowerEmblem.Light), Stats(), null, new TowerDescriptionContext(20, 100, 0, true));
            Assert.That(prism.Values["skill.percent"].Amount, Is.EqualTo(102).Within(.001));
        }

        [Test]
        public void All56Towers_HaveReadableEffectsAndValidValueLinks()
        {
            for (int color = 1; color <= 4; color++)
            for (int emblem = 1; emblem <= 14; emblem++)
            {
                TowerDefinition definition = Definition((TowerColor)color, (TowerEmblem)emblem);
                var doc = TowerDescriptionBuilder.Build(definition, Stats(), new TowerState(definition), Context);
                Assert.That(doc.Basic, Is.Not.Empty, definition.Id.ToString());
                Assert.That(doc.Skill, Is.Not.Empty, definition.Id.ToString());
                foreach (Match link in Regex.Matches(doc.Basic + doc.Skill, "<link=\"([^\"]+)\">"))
                    Assert.That(doc.Values.ContainsKey(link.Groups[1].Value), Is.True, definition.Id + ": " + link.Groups[1].Value);
                Assert.That(doc.Basic + doc.Skill, Does.Not.Contain("미정"));
                Assert.That(doc.Basic + doc.Skill, Does.Not.Contain("계산식"));
                Assert.That(TowerDescription.Plain(doc.Basic + doc.Skill), Does.Not.Contain("<link"));
            }
        }
    }
}
