using System.Collections.Generic;

namespace SquareTD
{
    public class EnemyHitCounter
    {
        private readonly Dictionary<TowerEmblem, int> m_counts = new Dictionary<TowerEmblem, int>();

        public int Get(TowerEmblem emblem)
        {
            if (m_counts.TryGetValue(emblem, out int count)) return count;
            return 0;
        }

        public int Add(TowerEmblem emblem)
        {
            int value = Get(emblem) + 1;            
            m_counts[emblem] = value;
            return value;
        }

        public void Reset(TowerEmblem emblem)
        {
            m_counts.Remove(emblem);
        }

        public void Clear()
        {
            m_counts.Clear();
        }
    }
}
