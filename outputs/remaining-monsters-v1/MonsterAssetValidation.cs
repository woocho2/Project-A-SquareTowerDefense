using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MonsterAssetValidation
{
    const string Pending = "MonsterAssets.Validation.Pending";
    static MonsterAssetValidation()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                new GameObject("MonsterAssetRuntimeValidation").AddComponent<MonsterAssetRuntimeValidation>();
        };
    }
    public static void Run()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var paths = AssetDatabase.FindAssets("t:EnemyCubeAppearance", new[]{"Assets/4. Asset/4. Enemy"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
            if(paths.Length != 27) throw new Exception("Expected 27 appearances, imported " + paths.Length);
            foreach(var path in paths) {
                var appearance = AssetDatabase.LoadAssetAtPath<EnemyCubeAppearance>(path);
                if(appearance == null || appearance.CharacterTexture == null || appearance.EquipmentMask == null) throw new Exception("Broken imported appearance " + path);
                foreach(var texture in new[]{appearance.CharacterTexture,appearance.EquipmentMask}) {
                    if(texture.width != 512 || texture.height != 512) throw new Exception("Wrong imported dimensions " + path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                    if(importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed) throw new Exception("Incorrect imported compression/mips " + path);
                    if(importer.sRGBTexture != (texture == appearance.CharacterTexture)) throw new Exception("Incorrect imported color space " + path);
                }
            }
            var shader = Shader.Find("SquareTowerDefense/EnemyCubeUnlit");
            if(shader == null || !shader.isSupported) throw new Exception("Cube shader missing or unsupported");
            var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
            if(errors.Length>0)throw new Exception("Shader errors: "+string.Join("; ",errors.Select(m=>m.message)));
            SessionState.SetBool(Pending,true);
            EditorApplication.EnterPlaymode();
        } catch(Exception ex) { Fail(ex); }
    }
    public static void Complete(string json)
    {
        File.WriteAllText("../unity-runtime-verification.json",json);
        SessionState.SetBool(Pending,false);
        Debug.Log("MONSTER_ASSET_VALIDATION_PASS");
        EditorApplication.Exit(0);
    }
    public static void Fail(Exception ex)
    {
        File.WriteAllText("../unity-validation-failure.txt",ex.ToString());
        Debug.LogException(ex);
        SessionState.SetBool(Pending,false);
        EditorApplication.Exit(1);
    }
}
