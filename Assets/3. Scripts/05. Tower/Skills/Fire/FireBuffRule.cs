using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 불 버프 — 피닉스 페더 · 라이징 피닉스 (공격 횟수 ×2, 화력 합계 +(브론즈 10%)가 되게 공격력을 깎음, 라이징 피닉스 공격력 +10%, 불 치명타 피해 +20%)
    public class FireBuffRule : BuffRule
    {
        public const float BronzeTotalGain = 0.1f;
        public const float AttackCountMultiplier = 2f;
        public const float RisingPower = 0.1f;
        public const float FireCritDamage = 0.2f;

        public static float PowerChange(SupportCast cast) => (1f + cast.Bronze(BronzeTotalGain)) / AttackCountMultiplier - 1f;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            int attackCount = Mathf.Max(1, Mathf.RoundToInt(receiver.Stats.GetBase(StatType.AttackCount)));
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.AttackCount, attackCount),
                StatModifier.Percent(source, StatType.Power, PowerChange(cast)),
            };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Percent(source, StatType.Power, RisingPower));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, FireCritDamage));
            return modifiers;
        }
    }
}
