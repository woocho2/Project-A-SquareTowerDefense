using System;
using UnityEngine;

namespace SquareTD
{
    public class CurrencyManager : Singleton<CurrencyManager>
    {
        [SerializeField] private int m_startGold = 300;
        [SerializeField] private int m_startGem = 0;

        public int Gold { get; private set; }
        public int Gem { get; private set; }

        public event Action<int, int> Changed;        

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            Gold = m_startGold;
            Gem = m_startGem;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            Changed?.Invoke(Gold, Gem);
        }

        public void AddGem(int amount)
        {
            Gem += amount;
            Changed?.Invoke(Gold, Gem);
        }

        public bool TrySpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            Changed?.Invoke(Gold, Gem);
            return true;
        }

        public bool TrySpendGem(int amount)
        {
            if (Gem < amount) return false;
            Gem -= amount;
            Changed?.Invoke(Gold, Gem);
            return true;
        }
    }
}
