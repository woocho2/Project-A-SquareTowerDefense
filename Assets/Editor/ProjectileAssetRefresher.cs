#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectileAssetRefresher
{
    static ProjectileAssetRefresher()
    {
        EditorApplication.delayCall += () =>
        {
            AssetDatabase.Refresh();
        };
    }

    [MenuItem("Tools/Refresh Projectiles")]
    public static void Refresh()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[ProjectileAssetRefresher] AssetDatabase refreshed!");
    }

    [MenuItem("Tools/Refresh Effects")]
    [MenuItem("Tools/Refresh All (Projectiles & Effects)")]
    public static void RefreshAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("[AssetRefresher] All Projectiles and Effects refreshed!");
    }
}
#endif
