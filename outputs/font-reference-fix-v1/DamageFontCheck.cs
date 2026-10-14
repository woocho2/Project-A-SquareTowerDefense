using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class DamageFontCheck
{
    // The isolated fixture uses the project's compiled TMP DLL instead of the
    // UGUI package sources. Resolve package MonoScript references to the exact
    // same types in that DLL in fixture copies only; main assets are untouched.
    static void ResolveFixturePackageScript(string assetPath, Type type, string packageGuid)
    {
        var script = AssetDatabase.LoadAllAssetsAtPath("Assets/Plugins/Unity.TextMeshPro.dll").OfType<MonoScript>().Single(x => x.GetClass() == type);
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long fileId)) throw new Exception("TMP DLL script reference unavailable");
        string text = File.ReadAllText(assetPath);
        text = text.Replace("{fileID: 11500000, guid: " + packageGuid + ", type: 3}", "{fileID: " + fileId + ", guid: " + guid + ", type: 3}");
        File.WriteAllText(assetPath, text, new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }
    public static void Run()
    {
        GameObject instance = null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ResolveFixturePackageScript("Assets/5. Font/NotoSansKR-Black SDF.asset", typeof(TMP_FontAsset), "71c1514a6bd24e1e882cebbe1904ce04");
            ResolveFixturePackageScript("Assets/8. TextMesh Pro/Resources/TMP Settings.asset", typeof(TMP_Settings), "2705215ac5b84b70bacc50632be6e391");
            ResolveFixturePackageScript("Assets/2. Prefab/Txt_Damage.prefab", typeof(TextMeshPro), "9541d86e2fd84c1d9990edf0852d74ab");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/5. Font/NotoSansKR-Black SDF.asset");
            if (font == null || font.material == null || font.atlasTexture == null) throw new Exception("Replacement font, material, or atlas missing: " + (font == null ? "font" : font.material == null ? "material" : "atlas"));
            foreach (char character in "0123456789.-") if (!font.HasCharacter(character)) throw new Exception("Damage glyph missing: " + character);
            if (TMP_Settings.defaultFontAsset != font) throw new Exception("Default TMP font not repaired");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/Txt_Damage.prefab");
            if (prefab == null) throw new Exception("Damage prefab missing");
            instance = UnityEngine.Object.Instantiate(prefab);
            var text = instance.GetComponent<TextMeshPro>();
            if (text == null || text.font != font || text.fontSharedMaterial != font.material) throw new Exception("Damage text references not repaired");
            text.text = "1234.56";
            text.ForceMeshUpdate(true, true);
            if (text.textInfo.characterCount != 7 || text.textInfo.meshInfo.Length == 0 || text.textInfo.meshInfo[0].vertexCount == 0) throw new Exception("Damage number mesh not generated");
            UnityEngine.Object.DestroyImmediate(instance); instance = null;
            Debug.Log("PASS damage font: prefab font and material, TMP default font, numeric glyphs, cloned damage number mesh generation");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); if (instance != null) UnityEngine.Object.DestroyImmediate(instance); EditorApplication.Exit(1); }
    }
}
