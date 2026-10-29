using System.Collections.Generic;

namespace SquareTD
{
    // 보스 제한 턴
    public class BossTimer
    {
        public const int TurnLimit = 20;

        private readonly Dictionary<EnemyController, int> m_remaining = new Dictionary<EnemyController, int>();

        public int Count => m_remaining.Count;

        public void Add(EnemyController boss)
        {
            m_remaining[boss] = TurnLimit;
        }

        public void Remove(EnemyController boss)
        {
            m_remaining.Remove(boss);
        }

        public int GetRemaining(EnemyController boss)
        {
            return m_remaining.TryGetValue(boss, out int turns) ? turns : 0;
        }

        public bool Tick()
        {
            bool timeUp = false;
            foreach (EnemyController boss in new List<EnemyController>(m_remaining.Keys))
            {
                m_remaining[boss]--;
                if (m_remaining[boss] <= 0) timeUp = true;
            }
            return timeUp;
        }
    }
}
