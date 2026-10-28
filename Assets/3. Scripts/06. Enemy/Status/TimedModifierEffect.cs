using System.Collections.Generic;

namespace SquareTD
{
    // 수정자 + 남은 행동 수 (21번 #134): 아머 브레이크(방어 −20%), 100T 해머(최대 이동력 −1) 같은 보통의 시간 디버프
    //   수정자의 출처는 Key로 맞춰 둠 (EnemyStatusEffects가 출처로 지우고 다시 모음)
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
