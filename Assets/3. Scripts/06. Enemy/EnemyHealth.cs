using UnityEngine;

namespace SquareTD
{
    public class EnemyHealth
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public void Reset(float max)
        {
            Max = max;
            Current = Max;
            IsDead = false;
        }

        public bool TakeDamage(float amount)
        {
            if (IsDead) return false;

            Current -= amount;
            if (Current > 0f) return false;

            Current = 0f;
            IsDead = true;
            return true;
        }

        // 최대 체력에 factor를 곱함. 현재 체력도 같은 비율로 (20번 4-3장 둠: 최대 −20%면 현재도 −20%)
        public void ScaleMax(float factor)
        {
            if (IsDead || factor <= 0f) return;
            Max *= factor;
            Current *= factor;
        }

        public float Heal(float amount)
        {
            if (IsDead || amount <= 0)
            {
                return 0f;
            }

            float before = Current;
            Current = Mathf.Min(before + amount, Max);
            return Current - before;
        }
    }
}
