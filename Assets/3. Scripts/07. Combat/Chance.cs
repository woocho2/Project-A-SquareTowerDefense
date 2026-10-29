using UnityEngine;

namespace SquareTD
{
    // 확률 굴리기
    public static class Chance
    {
        public static bool Roll(float chance) => Roll(chance, Random.value);

        public static bool Roll(float chance, float randomValue)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;
            return randomValue < chance;
        }
    }
}
