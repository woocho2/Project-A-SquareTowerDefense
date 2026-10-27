using UnityEngine;

namespace SquareTD
{
    public static class CriticalRoll
    {
        public static bool Roll(float rate)
        {
            return Roll(rate, Random.value);
        }

        public static bool Roll(float rate, float randomValue)
        {
            if (rate <= 0f) return false;
            if (rate >= 1f) return true;
            return randomValue < rate;
        }

        public static float Multiplier(float criticalDamage)
        {
            return 2 + criticalDamage;
        }
    }
}
