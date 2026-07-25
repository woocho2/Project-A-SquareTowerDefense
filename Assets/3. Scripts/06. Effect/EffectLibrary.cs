using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "EffectLibrary", menuName = "Custom/EffectLibrary", order = 1)]

public class EffectLibrary : ScriptableObject
{    
    [Serializable]
    public class EffectEntry
    {
        public int effectID;
        public GameObject effectPrefab;
    }

    public List<EffectEntry> effectEntries = new List<EffectEntry>();

    private Dictionary<int, GameObject> effectDictionary;

    private void OnEnable()
    {
        BuildMap();
    }

    void BuildMap()
    {
        effectDictionary = new Dictionary<int, GameObject>();

        foreach (var entry in effectEntries)
        {
            if (!effectDictionary.ContainsKey(entry.effectID))
            {
                effectDictionary.Add(entry.effectID, entry.effectPrefab);
            }
            else
            {
                Debug.LogWarning($"EffectLibrary: Duplicate key '{entry.effectID}' found. Skipping entry.");
            }
        }
    }

    public GameObject GetEffectPrefab(int effectID)
    {
        if (effectDictionary == null)
        {
            BuildMap();
        }

        if (effectDictionary.TryGetValue(effectID, out GameObject prefab))
        {
            return prefab;
        }
        else
        {
            Debug.LogWarning($"EffectLibrary: Effect with key '{effectID}' not found.");
            return null;
        }
    }
}
