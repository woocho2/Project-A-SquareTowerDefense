using UnityEngine;

namespace SquareTD
{
    // 적의 체력 (현재·최대)
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
