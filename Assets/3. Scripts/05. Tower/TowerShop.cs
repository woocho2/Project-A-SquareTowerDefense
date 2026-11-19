using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 타워 상점 — 카드 5장(문양은 덱에서, 티어는 레벨 확률), 플레이어 턴마다 무료 새로고침, 잠금
    public class TowerShop
    {
        public struct Card
        {
            public int TowerId;
            public int Price;
            public bool Sold;

            public bool IsAvailable => TowerId != 0 && !Sold;
        }

        private readonly Card[] m_cards = new Card[BalanceTables.ShopSize];
        private readonly TowerDatabase m_database;
        private readonly Func<int> m_level;
        private readonly Func<IReadOnlyList<TowerEmblem>> m_emblems;
        private readonly Func<float> m_random;

        public IReadOnlyList<Card> Cards => m_cards;
        public bool Locked { get; private set; }
        public event Action Changed;

        public TowerShop(TowerDatabase database, Func<int> level, Func<IReadOnlyList<TowerEmblem>> emblems, Func<float> random = null)
        {
            m_database = database;
            m_level = level;
            m_emblems = emblems;
            m_random = random ?? (() => UnityEngine.Random.value);
        }

        public bool IsEmpty
        {
            get
            {
                foreach (Card card in m_cards) if (card.TowerId != 0) return false;
                return true;
            }
        }

        public void Roll()
        {
            for (int i = 0; i < m_cards.Length; i++) m_cards[i] = RollCard();
            Changed?.Invoke();
        }

        public void OnPlayerTurnStarted()
        {
            if (!Locked || IsEmpty) Roll();
        }

        public void SetLocked(bool locked)
        {
            if (Locked == locked) return;
            Locked = locked;
            Changed?.Invoke();
        }

        public void MarkSold(int index)
        {
            if (index < 0 || index >= m_cards.Length) return;
            m_cards[index].Sold = true;
            Changed?.Invoke();
        }

        private Card RollCard()
        {
            IReadOnlyList<TowerEmblem> emblems = m_emblems();
            if (emblems == null || emblems.Count == 0) return default;

            int tier = BalanceTables.RollTier(m_level(), m_random());
            TowerEmblem emblem = emblems[Pick(emblems.Count)];
            TowerColor color = TowerColor.Red + Pick(SimulationRunner.AttackOnlyBuilds ? 2 : 4);
            int id = TowerId.Make(tier, color, emblem);
            if (m_database != null && !m_database.TryGet(id, out _)) id = TowerId.Make(1, color, emblem);
            return new Card { TowerId = id, Price = BalanceTables.GetShopPrice(TowerId.GetTier(id)) };
        }

        private int Pick(int count) => Mathf.Clamp((int)(m_random() * count), 0, count - 1);
    }
}
