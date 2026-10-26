using System;
using UnityEngine;

namespace SquareTD
{
    public class TowerStack
    {
        public float Current { get; private set; }          // 소수 (물결: 쌓이는 양이 % 로 늘어남)
        public int Max { get; private set; }
        public bool IsFull => Current >= Max;
        public bool ManualRequested { get; private set; }   // 수동 소모: 플레이어가 스택 소모 버튼을 눌렀나

        public event Action Full;                           // 막 다 찼을 때 한 번 (수동 소모 타워가 빛남, 카드의 버튼 켜기)

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
