using UnityEngine;

namespace SquareTD
{
    public static class DamageCalculator
    {
        public static float GetOutgoing(float power, float distanceBonus, float critMultiplier)
        {
            return power * distanceBonus * critMultiplier;
        }

        public static float ApplyPenetration(float defense, float penetration)
        {
            if (defense >= 0f) 
            {
                return defense * (1f - penetration);
            }

            return defense + defense * penetration;
        }

        public static float GetDefenseCoefficient(float defense)
        {
            if (defense >= 0f) 
            {
                return 1f / (1f + 0.01f * defense);
            }

            return 1 + 0.01f * Mathf.Abs(-defense);
        }

        public static float Calculate(DamageInfo info, float defense, float damageTakenMultiplier, float fixedRatio)
        {
            float ratio = info.ForceFixed ? 1f : fixedRatio;
            float fixedDamage = info.BaseDamage * ratio;
            float defenseAfter = ApplyPenetration(defense, info.ArmorPenetration);
            float defended = (info.BaseDamage - fixedDamage) * GetDefenseCoefficient(defenseAfter) * damageTakenMultiplier;
            return defended + fixedDamage;
        }
    }
}
