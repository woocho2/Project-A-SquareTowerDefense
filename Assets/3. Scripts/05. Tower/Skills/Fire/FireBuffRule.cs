using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 불 버프 — 피닉스 페더 · 라이징 피닉스 (공격 횟수 ×2, 공격력 +(값 - 50)%, 라이징 피닉스 공격력 +10%, 불 치명타 피해 +20%)
    public class FireBuffRule : BuffRule
    {
        public const float PowerOffset = 50f;
        public const float RisingPower = 0.1f;
        public const float FireCritDamage = 0.2f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            int attackCount = Mathf.Max(1, Mathf.RoundToInt(receiver.Stats.GetBase(StatType.AttackCount)));
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.AttackCount, attackCount),
                StatModifier.Percent(source, StatType.Power, (cast.Amount - PowerOffset) * SkillFormula.PercentRate),
            };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Percent(source, StatType.Power, RisingPower));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, FireCritDamage));
            return modifiers;
        }
    }
}
