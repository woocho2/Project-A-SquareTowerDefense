#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Temporary migration: synchronize the open prefab stage before its auto-save.
[InitializeOnLoad]
public static class StatIconConnectionOnce
{
    const string CanvasPath = "Assets/2. Prefab/Canvas.prefab";
    const string AtlasPath = "Assets/4. Asset/5. UI/Stat_Icon_Atlas_v2.png";
    const string DonePath = "outputs/stat-icons-v2/editor-connection.txt";
    static readonly Dictionary<string, string> Names = new Dictionary<string, string>
    {
        {"Img_Power_Symbol", "Icon_Tower_AttackPower"}, {"Img_Range_Symbol", "Icon_Tower_Range"},
        {"Img_Action_Symbol", "Icon_Tower_ActionInterval"}, {"Img_AttackCount_Symbol", "Icon_Tower_AttackCount"},
        {"Img_CriticalRate_Symbol", "Icon_Tower_CriticalRate"}, {"Img_CriticalDamage_Symbol", "Icon_Tower_CriticalDamage"},
        {"Img_AbilityValue_Symbol", "Icon_Tower_AbilityValue"}, {"Img_Mastery_Symbol", "Icon_Tower_Mastery"},
        {"Img_Stack_Symbol", "Icon_Tower_Stack"}, {"Img_AttackType_Symbol", "Icon_Tower_AttackType"},
        {"Img_Skill_Symbol", "Icon_Tower_Skill"}, {"Img_Duration_Symbol", "Icon_Tower_Duration"}
    };
    static StatIconConnectionOnce() { if (!File.Exists(DonePath)) EditorApplication.update += Run; }
    static bool UnderPanel(Transform transform)
    {
        for (var current = transform; current != null; current = current.parent)
            if (current.name == "TowerInfoPanel") return true;
        return false;
    }
    static int Connect(GameObject root, Dictionary<string, Sprite> sprites)
    {
        int count = 0;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (!UnderPanel(image.transform) || !Names.TryGetValue(image.name, out var name)) continue;
            var sprite = sprites[name];
            if (image.sprite != sprite)
            {
                Undo.RecordObject(image, "Connect stat icon");
                image.sprite = sprite;
                EditorUtility.SetDirty(image);
                if (PrefabUtility.IsPartOfPrefabInstance(image)) PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }
            count++;
        }
        return count;
    }
    static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Run;
        try
        {
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s => s.name);
            if (sprites.Count != 20) throw new Exception("Expected twenty stat icons");
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            int count;
            if (stage != null && stage.assetPath == CanvasPath)
            {
                count = Connect(stage.prefabContentsRoot, sprites);
                if (count != 12) throw new Exception("Expected twelve open prefab stat Images");
                EditorSceneManager.MarkSceneDirty(stage.scene);
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, CanvasPath);
            }
            else
            {
                var root = PrefabUtility.LoadPrefabContents(CanvasPath);
                try
                {
                    count = Connect(root, sprites);
                    if (count != 12) throw new Exception("Expected twelve prefab stat Images");
                    PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(DonePath, "PASS: twelve TowerInfoPanel stat icons connected and saved through Unity. Open prefab stage synchronized; sizes, colors, labels preserved.\n");
            Debug.Log("Stat icons: twenty atlas sprites, twelve panel connections saved.");
        }
        catch (Exception error)
        {
            File.WriteAllText("outputs/stat-icons-v2/editor-connection-error.txt", error.ToString());
            Debug.LogException(error);
        }
    }
}
#endif
