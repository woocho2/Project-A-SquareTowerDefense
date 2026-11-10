using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 해머 버프 — 그리드 · 골드 러쉬 (현재 골드 비례 추가 피해, 골드 러쉬 골드 보상 +50%, 타격 처치 50%로 골드 +100%)
    public class HammerBuffRule : BuffRule
    {
        public const float RushGoldBonus = 0.5f;
        public const float BluntDoubleChance = 0.5f;

        private readonly Func<int> m_currentGold;

        public HammerBuffRule(Func<int> currentGold)
        {
            m_currentGold = currentGold;
        }

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Flat(source, StatType.BonusDamage, cast.Rate * m_currentGold()) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.GoldBonus, RushGoldBonus));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.GoldDoubleChance, BluntDoubleChance));
            return modifiers;
        }

        public override void OnReceiverActed(SupportCast cast, TowerController receiver) => SupportBuffs.Rebuild(receiver, TowerEmblem.Hammer);
    }
}
