using System;
using UnityEngine;

namespace SquareTD
{
    // 타워의 스킬 2용 스택
    public class TowerStack
    {
        public float Current { get; private set; }
        public int Max { get; private set; }
        public bool IsFull => Current >= Max;
        public bool ManualRequested { get; private set; }

        public event Action Full;

        public TowerStack(int max)
        {
            Max = Mathf.Max(1, max);
        }

        public void Add(float amount)
        {
            if (IsFull || amount <= 0) return;

            Current = Mathf.Min(Current + amount, Max);

            if (IsFull) Full?.Invoke();
        }

        public void Consume(bool keepHalf = false)
        {
            Current = keepHalf ? Max * 0.5f : 0f;
            ManualRequested = false;
        }

        public void SetMax(int max)
        {
            bool wasFull = IsFull;

            Max = Mathf.Max(1, max);
            Current = Mathf.Min(Current, Max);

            if (!wasFull && IsFull) Full?.Invoke();
        }

        public void RequestManualConsume()
        {
            if (IsFull) ManualRequested = true;
        }

        public void CopyFrom(TowerStack old)
        {
            Current = Mathf.Min(old.Current, Max);
        }
    }
}
