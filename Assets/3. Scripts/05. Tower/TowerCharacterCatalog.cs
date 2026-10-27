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
            public Sprite[] towerCharacters;
        }

        [SerializeField] private Entry[] m_entries;

        public Sprite GetTowerCharacter(int emblemId, int attackClass)
        {
            if (m_entries != null && attackClass >= 1 && attackClass <= 4)
            {
                foreach (Entry entry in m_entries)
                {
                    if (entry.emblemId != emblemId) continue;
                    int index = attackClass - 1;
                    if (entry.towerCharacters != null && index < entry.towerCharacters.Length
                        && entry.towerCharacters[index] != null) return entry.towerCharacters[index];
                    break;
                }
            }
            return GetCharacter(emblemId, attackClass);
        }

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
