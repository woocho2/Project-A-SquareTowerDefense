using UnityEngine;

namespace SquareTD
{
    // 스킬의 공통 식 (공격 스킬, 스택, 존의 값, 저항)
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

        public static float ZoneValue(Zone zone)
        {
            TowerFinalStats stats = zone.Owner.Stats;
            if (zone.Stage == ZoneStage.Skill2) return SupportSkill2(stats.Power, stats.AbilityValue, stats.Stack, stats.StackValue);
            return SupportSkill1(stats.Power, stats.AbilityValue, zone.Stack, stats.StackValue);
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
