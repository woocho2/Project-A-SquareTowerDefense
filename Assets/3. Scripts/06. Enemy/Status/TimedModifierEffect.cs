using System.Collections.Generic;

namespace SquareTD
{
    // 수정자 + 남은 행동 수로 된 보통의 시간 디버프
    public class TimedModifierEffect : StatusEffect
    {
        private readonly string m_key;
        private readonly string m_description;
        private readonly float m_strength;
        private readonly StatModifier[] m_modifiers;

        public TimedModifierEffect(string key, int durationActions, float strength, string description, params StatModifier[] modifiers)
            : base(durationActions)
        {
            m_key = key;
            m_strength = strength;
            m_description = description;
            m_modifiers = modifiers;
        }

        public override string Key => m_key;
        public override float Strength => m_strength;
        public override IReadOnlyList<StatModifier> GetModifiers() => m_modifiers;
        public override string Describe() => $"{m_description} ({RemainingActions}회 행동 남음)";
    }
}
