using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 방패 버프 — 프로텍트 · 이지스 (한도 비례 최종 피해, 이지스 동안 한도 +50%, 타격 공격력 +(한도 × 0.2)%)
    public class ShieldBuffRule : BuffRule
    {
        public const float AegisLimitRate = 0.5f;
        public const float BluntPowerPerLimit = 0.002f;

        private readonly Func<int> m_currentLimit;

        public ShieldBuffRule(Func<int> currentLimit)
        {
            m_currentLimit = currentLimit;
        }

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            int limit = m_currentLimit();
            var modifiers = new List<StatModifier> { StatModifier.Flat(source, StatType.FinalDamage, cast.Rate * limit) };
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Percent(source, StatType.Power, BluntPowerPerLimit * limit));
            return modifiers;
        }

        public override int GetLimitModifier(int baseLimit) => (int)(baseLimit * AegisLimitRate);
    }
}
