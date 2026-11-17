using System;
using UnityEngine;

namespace SquareTD
{
    // 골드 (게임의 유일한 재화)
    public class CurrencyManager : Singleton<CurrencyManager>
    {
        [SerializeField] private int m_startGold = 500;

        public int Gold { get; private set; }

        public event Action<int> Changed;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            Gold = m_startGold;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            Changed?.Invoke(Gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            Changed?.Invoke(Gold);
            return true;
        }
    }
}
