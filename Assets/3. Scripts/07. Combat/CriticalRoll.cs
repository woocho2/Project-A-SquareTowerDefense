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

        // 이 적을 칠 때의 치명 확률: 타워의 치명 확률 + 적에게 걸린 효과가 더해 주는 값 (활 락 온·핀 포인트, 수업 29)
        //   광역은 착탄점으로 고른 적, 단일은 그 탄의 적, 스킬은 발동시킨 적으로 한 번 (20번 4-5장)
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
