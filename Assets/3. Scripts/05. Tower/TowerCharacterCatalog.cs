using System;
using UnityEngine;

namespace SquareTD
{
    [CreateAssetMenu(menuName = "SquareTD/Tower Character Catalog")]
    public class TowerCharacterCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int emblemId;
            public string emblemName;
            public Sprite[] characters;
        }

        [SerializeField] private Entry[] m_entries;

        public Sprite GetCharacter(int emblemId, int attackClass)
        {
            if (m_entries == null || attackClass < 1 || attackClass > 4) return null;
            foreach (Entry entry in m_entries)
            {
                if (entry.emblemId != emblemId) continue;
                int index = attackClass - 1;
                return entry.characters != null && index < entry.characters.Length
                    ? entry.characters[index] : null;
            }
            return null;
        }
    }
}
