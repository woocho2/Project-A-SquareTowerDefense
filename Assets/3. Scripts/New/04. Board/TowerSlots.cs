namespace SquareTD
{
    public class TowerSlots
    {
        private readonly TowerController[] m_slots;

        public TowerSlots(int slotCount)
        {
            m_slots = new TowerController[slotCount];
        }

        public int Count => m_slots.Length;

        public TowerController Get(int index) => m_slots[index];

        public bool IsEmpty(int index) => m_slots[index] == null;

        public bool TryGetFirstEmpty(out int index)
        {
            for (int i = 0; i < m_slots.Length; i++)
            {
                if (IsEmpty(i))
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public bool TryPlace(TowerController tower, int index)
        {
            if (!IsEmpty(index)) return false;
            m_slots[index] = tower;
            return true;
        }

        public void Remove(int index)
        {
            m_slots[index] = null;
        }
    }
}
