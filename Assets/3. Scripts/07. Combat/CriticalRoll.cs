using UnityEngine;

namespace SquareTD
{
    // 치명타 판정과 배율
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

        public static float RateAgainst(TowerController attacker, TowerFinalStats stats, ITarget target)
        {
            float bonus = target != null && target.Status != null ? target.Status.GetCritRateBonus(attacker) : 0f;
            return stats.CriticalRate + bonus;
        }

        public static float Multiplier(float criticalDamage)
        {
            return 2 + criticalDamage;
        }
    }
}
