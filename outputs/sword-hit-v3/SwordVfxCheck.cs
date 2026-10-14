using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SwordVfxCheck
{
    static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }
    static void ValidateConnection()
    {
        var library = AssetDatabase.LoadAssetAtPath<EffectLibrary>("Assets/2. Prefab/4. Effect/EffectLibrary.asset");
        Check(library != null, "Effect library missing");
        Check(library.effectEntries.Select(x => x.effectID).Distinct().Count() == library.effectEntries.Count, "Duplicate effect ID");
        Check(library.GetEffectPrefab(101) == AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SwordHit_v3/Sword_Splash_30_v3.prefab"), "Sword splash ID 101 not connected");
        Check(library.GetEffectPrefab(201) == AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SwordHit_v3/Sword_Target_30_v3.prefab"), "Sword target ID 201 not connected");
        var manager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/1. Manager/EffectManager.prefab");
        Check(manager != null, "Effect manager prefab missing");
        var settings = new SerializedObject(manager.GetComponent<EffectManager>());
        Check(settings.FindProperty("m_effectLibrary").objectReferenceValue == library, "Game manager uses a different effect library");
        var rows = File.ReadAllLines("Assets/Resources/TowerDataCSV.csv").Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Split(',')).Where(x => int.Parse(x[0]) % 100 == 1 && (x[12] == "Splash" || x[12] == "Target")).ToArray();
        Check(rows.Length == 10 && rows.Count(x => x[12] == "Splash") == 5 && rows.Count(x => x[12] == "Target") == 5, "Sword tier rows missing");
        foreach (var row in rows) Check(int.Parse(row[16]) == (row[12] == "Splash" ? 101 : 201), "Sword CSV points to wrong effect: " + row[0]);
        Debug.Log("PASS Sword connection: game manager library, splash ID 101, target ID 201, all 10 sword tower tier rows");
    }
    public static void BuildAndRun()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateConnection();
            foreach (string kind in new[] { "Target", "Splash" })
            {
                string basePath = "Assets/SwordHit_v3/Sword_" + kind + "_30_v3";
                var sprites = AssetDatabase.LoadAllAssetsAtPath(basePath + ".png").OfType<Sprite>().OrderBy(x => x.name).ToArray();
                Check(sprites.Length == 30, kind + ": imported frame count");
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(basePath + ".anim");
                existing.ClearCurves();
                existing.frameRate = 12;
                var keys = sprites.Select((sprite, i) => new ObjectReferenceKeyframe { time = i / 12f, value = sprite }).ToArray();
                AnimationUtility.SetObjectReferenceCurve(existing, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var settings = AnimationUtility.GetAnimationClipSettings(existing);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(existing, settings);
                AnimationUtility.SetAnimationEvents(existing, new[] { new AnimationEvent { time = 2.5f, functionName = "DestroyEffect" } });
                EditorUtility.SetDirty(existing);
            }
            AssetDatabase.SaveAssets();
            foreach (string kind in new[] { "Target", "Splash" })
                AssetDatabase.ImportAsset("Assets/SwordHit_v3/Sword_" + kind + "_30_v3.anim", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Run();
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    public static void Run()
    {
        GameObject sample = null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateConnection();
            foreach (string kind in new[] { "Target", "Splash" })
            {
                string basePath = "Assets/SwordHit_v3/Sword_" + kind + "_30_v3";
                var sprites = AssetDatabase.LoadAllAssetsAtPath(basePath + ".png").OfType<Sprite>().OrderBy(x => x.name).ToArray();
                Check(sprites.Length == 30, kind + ": must import 30 sprites");
                for (int i = 0; i < sprites.Length; i++)
                {
                    Check(sprites[i].rect == new Rect(i % 6 * 256, (4 - i / 6) * 256, 256, 256), kind + ": wrong frame rect " + i);
                    Check(sprites[i].pixelsPerUnit == 256, kind + ": wrong world size");
                    Check(sprites[i].pivot == new Vector2(128, kind == "Splash" ? 76 : 128), kind + ": incorrect impact pivot");
                }
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(basePath + ".anim");
                Check(clip != null && Math.Abs(clip.length - 2.5f) < .0001f && clip.frameRate == 12, kind + ": wrong length or FPS: " + (clip == null ? "missing" : clip.length + " sec, " + clip.frameRate + " FPS"));
                Check(!AnimationUtility.GetAnimationClipSettings(clip).loopTime, kind + ": should play once");
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                Check(keys.Length == 30, kind + ": exactly 30 image keys required");
                for (int i = 0; i < 30; i++) Check(keys[i].value == sprites[i] && Math.Abs(keys[i].time - i / 12f) < .0001f, kind + ": wrong key " + i);
                Check(clip.events.Length == 1 && clip.events[0].functionName == "DestroyEffect" && clip.events[0].time == 2.5f, kind + ": pool return timing missing");
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(basePath + ".controller");
                Check(controller != null, kind + ": controller missing");
                var state = controller.layers[0].stateMachine.defaultState;
                Check(state != null && state.motion == clip && state.speed == 1, kind + ": state must run at normal speed");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + ".prefab");
                Check(prefab != null && prefab.GetComponent<Animator>().runtimeAnimatorController == controller, kind + ": broken prefab controller reference");
                Check(prefab.GetComponent<SpriteRenderer>().sprite == sprites[0], kind + ": broken first sprite reference");
                Check(prefab.GetComponent<EffectController>() != null && prefab.GetComponent<EffectPool2D>() != null, kind + ": pool components missing");
                sample = new GameObject("SwordVfxFrameSample", typeof(SpriteRenderer), typeof(Animator));
                var renderer = sample.GetComponent<SpriteRenderer>();
                var animator = sample.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
                for (int i = 0; i < 30; i++) { animator.Play(state.name, 0, (i / 12f + .001f) / clip.length); animator.Update(0); Check(renderer.sprite == sprites[i], kind + ": actual Animator frame wrong " + i + ", got " + (renderer.sprite == null ? "null" : renderer.sprite.name)); }
                animator.Play(state.name, 0, 2.499f / clip.length); animator.Update(0); Check(renderer.sprite == sprites[29], kind + ": final image not held for its full frame");
                UnityEngine.Object.DestroyImmediate(sample); sample = null;
                Debug.Log("PASS Sword " + kind + ": 30 imported sprites, 12 FPS, 2.5 seconds, all Animator frames, normal speed, prefab links, full final frame, pool return event");
            }
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); if (sample != null) UnityEngine.Object.DestroyImmediate(sample); EditorApplication.Exit(1); }
    }
}
