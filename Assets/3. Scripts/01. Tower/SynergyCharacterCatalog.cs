using System;
using UnityEngine;

/// <summary>시너지 타워의 정적 정면 그림입니다. 보스의 굴림 면은 적 외형에서 별도로 관리합니다.</summary>
[CreateAssetMenu(menuName = "Tower Defense/Synergy Character Catalog")]
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
