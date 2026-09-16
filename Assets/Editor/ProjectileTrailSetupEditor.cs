#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ProjectileTrailSetupEditor
{
    [MenuItem("Tools/Setup Projectile Trails & Particles")]
    public static void SetupAllProjectileTrails()
    {
        string folderPath = "Assets/Resources/Projectiles";
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        int count = 0;
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!int.TryParse(fileName, out int projId)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            ProjectileHit2D projHit = root.GetComponent<ProjectileHit2D>();
            if (projHit != null)
            {
                int effectId = projId;
                projHit.SetupVisualEffects(effectId);
                projHit.ResetVisualEffects();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ProjectileTrailSetupEditor] Successfully configured TrailRenderer & ParticleSystem on {count} projectiles!");
    }
}
#endif
