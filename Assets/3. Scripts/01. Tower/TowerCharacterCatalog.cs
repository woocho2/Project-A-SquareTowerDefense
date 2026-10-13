using System;
using UnityEngine;

/// <summary>기획의 14문양 번호에 맞춘 일반 타워의 완성 초상입니다.</summary>
[CreateAssetMenu(menuName = "Tower Defense/Tower Character Catalog")]
public class TowerCharacterCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public int emblemId;
        public string emblemName;
        // 광역, 단일, 버프, 디버프 순서. 티어 테두리는 TowerVisual에서 별도로 표시합니다.
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
