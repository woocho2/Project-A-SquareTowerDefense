#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class SatelliteTrailSetupEditor
{
    [MenuItem("Tools/Setup Satellite Trails (MapBuff & DebuffZone)")]
    public static void SetupAllSatelliteTrails()
    {
        string[] targetPrefabs = new[]
        {
            "Assets/2. Prefab/MapBuff.prefab",
            "Assets/2. Prefab/DebuffZone.prefab"
        };

        int count = 0;
        foreach (string path in targetPrefabs)
        {
            if (!File.Exists(path)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            TileSatelliteOrbiter orbiter = root.GetComponent<TileSatelliteOrbiter>();
            if (orbiter != null)
            {
                orbiter.SetupSatelliteVisualEffects();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SatelliteTrailSetupEditor] Successfully configured satellite trails & particles on {count} prefabs!");
    }
}
#endif
