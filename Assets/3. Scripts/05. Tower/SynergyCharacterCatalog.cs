using System;
using UnityEngine;

namespace SquareTD
{
    // 시너지 타워 ID → 캐릭터 그림 표
    [CreateAssetMenu(menuName = "SquareTD/Synergy Character Catalog")]
    public class SynergyCharacterCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int towerID;
            public string characterName;
            public Sprite front;
            public Sprite face;
        }

        [SerializeField] private Entry[] m_entries;

        public Sprite GetFront(int towerID)
        {
            if (m_entries == null) return null;
            foreach (Entry entry in m_entries)
                if (entry.towerID == towerID) return entry.front;
            return null;
        }

        public Sprite GetTowerCharacter(int towerID)
        {
            if (m_entries == null) return null;
            foreach (Entry entry in m_entries)
                if (entry.towerID == towerID) return entry.face != null ? entry.face : entry.front;
            return null;
        }
    }
}
