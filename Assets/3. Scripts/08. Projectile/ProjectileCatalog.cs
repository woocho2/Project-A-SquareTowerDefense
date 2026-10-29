using System;
using UnityEngine;

namespace SquareTD
{
    // 타워별 탄 모양 표
    [CreateAssetMenu(menuName = "SquareTD/Projectile Catalog")]
    public class ProjectileCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public int Id;
            public Sprite Sprite;
            public Color EffectColor;
            public RuntimeAnimatorController Animation;
            public HitEffect HitEffect;
            public float HitEffectScale;
        }

        [SerializeField] private Entry[] m_entries;

        public static int GetLookId(int towerId) => towerId % 1000;

        public bool TryGet(int id, out Entry entry)
        {
            if (m_entries != null)
            {
                foreach (Entry e in m_entries)
                {
                    if (e.Id == id)
                    {
                        entry = e;
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }
    }
}
