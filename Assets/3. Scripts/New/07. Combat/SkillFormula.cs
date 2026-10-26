using UnityEngine;

namespace SquareTD
{
    public static class SkillFormula
    {
        public const float StackRate = 0.01f;
        public const float SupportRate = 0.001f;
        
        public static float AttackSkill(float power, float abilityValue)
        {
            float basicAttack = power * abilityValue;
            return basicAttack;
        }

        public static float WithStack(float value, float stack, float stackValue)
        {
            float skillAttack = value * (1 + stack * 0.01f * stackValue);
            return skillAttack;
        }

        public static float SupportSkill1(float power, float abilityValue, float currentStack, float stackValue)
        {
            float basicSupport = 0.001f * power * abilityValue * currentStack * stackValue / 100f;            
            return basicSupport;
        }

        public static float SupportSkill2(float power, float abilityValue, float maxStack, float stackValue)
        {
            float skillSupport = SupportSkill1(power, abilityValue, maxStack, stackValue);
            return skillSupport;
        }

        public static float Resist(float value, float resistancePercent)
        {
            float resist = value * (1 - Mathf.Min(resistancePercent, 100f) / 100f);
            return resist;
        }
        
        public static int DefaultDuration(int colorLevel)
        {
            int defaultDuration = colorLevel * 2;            
            return defaultDuration;
        }
    }
}
