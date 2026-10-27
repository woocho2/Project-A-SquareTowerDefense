using System;
using UnityEngine;

namespace SquareTD
{
    [CreateAssetMenu(menuName = "SquareTD/Synergy Character Catalog")]
    public class SynergyCharacterCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int towerID;
            public string characterName;
            public Sprite front;
        }

        [SerializeField] private Entry[] m_entries;

        public Sprite GetFront(int towerID)
        {
            if (m_entries == null) return null;
            foreach (Entry entry in m_entries)
                if (entry.towerID == towerID) return entry.front;
            return null;
        }
    }
}
