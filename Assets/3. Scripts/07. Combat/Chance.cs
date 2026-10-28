using UnityEngine;

namespace SquareTD
{
    // 확률 굴리기 (망치질 50%, 화염구 60%, 순풍, 동상 …). 판정은 Random.value < 확률 (B11: <= 가 아님)
    //   0 이하면 늘 실패, 1 이상이면 늘 성공 → 테스트는 이 둘로 결과를 정해 놓고 봄
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
