using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class OrbMotionBuild
{
    const string Pending = "OrbMotionV1.Pending";
    const string Folder = "Assets/OrbMotion_v1/";
    const float Duration = 8f / 12f;
    [Serializable] public class Manifest { public Entry[] assets; }
    [Serializable] public class Entry { public int id; public string key; public string texture; public float[] color; }
    static readonly float[] IdleScale = { 1f, 1.023f, 1.039f, 1.026f, 1.002f, .977f, .962f, .974f };
    static readonly float[] FlightX = { 1.1f, 1.122f, 1.14f, 1.125f, 1.104f, 1.083f, 1.067f, 1.081f };
    static readonly float[] FlightY = { .96f, .947f, .936f, .945f, .958f, .973f, .985f, .976f };
    static readonly float[] IdleAlpha = { 1f, .98f, .96f, .975f, .995f, .98f, .95f, .97f };
    static readonly float[] FlightAlpha = { 1f, .99f, .97f, .98f, .995f, .98f, .96f, .99f };
    static readonly float[] IdleAngle = { 0f, 1.7f, 2.4f, 1.6f, .1f, -1.7f, -2.4f, -1.6f };
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static OrbMotionBuild() { EditorApplication.playModeStateChanged += OnPlayState; }
    static Manifest Read() => JsonUtility.FromJson<Manifest>(File.ReadAllText("orb-motion-manifest.json"));
    static void OnPlayState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        { SessionState.SetBool(Pending, false); ValidateRuntime(); }
    }
    static void Curve(AnimationClip clip, Type type, string property, float[] values)
    {
        // Eight held poses, plus an identical boundary key to close the 8/12-second loop.
        var keys = Enumerable.Range(0, 9).Select(i => new Keyframe(i / 12f, values[i % 8], float.PositiveInfinity, float.PositiveInfinity)).ToArray();
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++)
        { AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant); AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant); }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("OrbVisual", type, property), curve);
    }
    static AnimationClip Clip(string state)
    {
        var clip = new AnimationClip { name = "Orb_" + state, frameRate = 12 };
        bool idle = state == "Idle";
        Curve(clip, typeof(Transform), "m_LocalScale.x", idle ? IdleScale : FlightX);
        Curve(clip, typeof(Transform), "m_LocalScale.y", idle ? IdleScale : FlightY);
        Curve(clip, typeof(Transform), "m_LocalScale.z", Enumerable.Repeat(1f, 8).ToArray());
        Curve(clip, typeof(Transform), "localEulerAnglesRaw.z", idle ? IdleAngle : new float[8]);
        Curve(clip, typeof(SpriteRenderer), "m_Color.a", idle ? IdleAlpha : FlightAlpha);
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, Folder + "Orb_" + state + ".anim"); return clip;
    }
    public static void BuildAndTest()
    {
        GameObject root = null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = Read(); Check(manifest.assets.Length == 14, "14 orb entries required");
            var idleClip = Clip("Idle"); var flightClip = Clip("Flight");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "Orb_Motion.controller");
            controller.AddParameter("IsFlying", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle"); idle.motion = idleClip;
            var flight = machine.AddState("Flight"); flight.motion = flightClip; machine.defaultState = idle;
            var launch = idle.AddTransition(flight); launch.hasExitTime = false; launch.duration = 0; launch.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");
            var reset = flight.AddTransition(idle); reset.hasExitTime = false; reset.duration = 0; reset.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");
            foreach (var entry in manifest.assets)
            {
                var sprite = AssetDatabase.LoadAllAssetsAtPath(entry.texture).OfType<Sprite>().Single();
                Check(sprite != null, entry.key + ": orb sprite missing");
                string prefabPath = "Assets/Resources/Projectiles/" + (100 + entry.id) + ".prefab";
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                var original = root.GetComponent<SpriteRenderer>(); Check(original != null, "Original root sprite required");
                var visual = new GameObject("OrbVisual"); visual.transform.SetParent(root.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>(); EditorUtility.CopySerialized(original, renderer);
                renderer.sprite = sprite; renderer.color = Color.white;
                UnityEngine.Object.DestroyImmediate(original);
                var animator = root.GetComponent<Animator>(); if (animator == null) animator = root.AddComponent<Animator>();
                animator.enabled = true; animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var projectile = root.GetComponent<ProjectileHit2D>(); Check(projectile != null && projectile.IsSplash, "Splash projectile script required");
                var serialized = new SerializedObject(projectile);
                serialized.FindProperty("m_usePrefabVisual").boolValue = true;
                serialized.FindProperty("m_prefabEffectColor").colorValue = new Color(entry.color[0], entry.color[1], entry.color[2], 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath); PrefabUtility.UnloadPrefabContents(root); root = null;
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Debug.LogException(error); if (root != null) PrefabUtility.UnloadPrefabContents(root); EditorApplication.Exit(1); }
    }
    static void ValidateRuntime()
    {
        GameObject poolRoot = null;
        try
        {
            foreach (string state in new[] { "Idle", "Flight" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "Orb_" + state + ".anim");
                Check(clip.frameRate == 12 && Math.Abs(clip.length - Duration) < .0001f && AnimationUtility.GetAnimationClipSettings(clip).loopTime && clip.events.Length == 0, state + ": invalid loop");
                Check(AnimationUtility.GetCurveBindings(clip).All(b => b.path == "OrbVisual"), state + ": root movement must not be animated");
            }
            foreach (var entry in Read().assets)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Projectiles/" + (100 + entry.id) + ".prefab");
                var expectedSprite = AssetDatabase.LoadAllAssetsAtPath(entry.texture).OfType<Sprite>().Single();
                poolRoot = new GameObject(entry.key + "_OrbPoolCheck"); var pool = poolRoot.AddComponent<ProjectileObjectPool2D>();
                pool.InitPool(prefab.GetComponent<ProjectileHit2D>(), 1, 2, true, poolRoot.transform);
                var projectile = pool.Spawn(Vector3.zero); var animator = projectile.GetComponent<Animator>();
                var renderer = projectile.GetComponentInChildren<SpriteRenderer>(); var visual = renderer.transform;
                Check(projectile.IsSplash && renderer.sprite == expectedSprite && renderer.color == Color.white && visual.localScale == Vector3.one && !animator.GetBool("IsFlying"), entry.key + ": initial Idle visual incorrect");
                projectile.Init(new ProjectileStats { projectileID = 1100 + entry.id, hitEffectID = 100 + entry.id, speed = 1, damage = 1, SplashRadius = 2 });
                projectile.SetVisualScale(.5f); projectile.SetTargetPosition(new Vector2(0, 100)); projectile.PrepareAsSatellite(100);
                Check(renderer.sprite == expectedSprite && !projectile.GetComponent<TrailRenderer>().emitting, entry.key + ": Idle must retain orb without trail");
                for (int i = 0; i < 8; i++)
                {
                    animator.Play("Idle", 0, i / 8f + .001f); animator.Update(0);
                    Check(Math.Abs(visual.localScale.x - IdleScale[i]) < .0001f && Math.Abs(renderer.color.a - IdleAlpha[i]) < .0001f, entry.key + ": Idle pose " + i);
                }
                projectile.Launch(Vector3.up, 1, true, 100);
                Check(animator.GetBool("IsFlying") && animator.GetCurrentAnimatorStateInfo(0).IsName("Flight") && Math.Abs(visual.localScale.x - FlightX[0]) < .0001f, entry.key + ": immediate Flight failed");
                Check(projectile.GetComponent<TrailRenderer>().emitting && projectile.GetComponent<TrailRenderer>().sortingLayerID == renderer.sortingLayerID, entry.key + ": child visual trail broken");
                for (int i = 0; i < 8; i++)
                {
                    animator.Play("Flight", 0, i / 8f + .001f); animator.Update(0);
                    Check(Math.Abs(visual.localScale.x - FlightX[i]) < .0001f && Math.Abs(visual.localScale.y - FlightY[i]) < .0001f && Math.Abs(renderer.color.a - FlightAlpha[i]) < .0001f, entry.key + ": Flight pose " + i);
                }
                animator.Play("Flight", 0, 1.251f); animator.Update(0); Check(Math.Abs(visual.localScale.x - FlightX[2]) < .0001f, entry.key + ": loop boundary failed");
                Check(projectile.transform.localScale == Vector3.one * .5f && projectile.transform.position == Vector3.zero && Mathf.Abs(Mathf.DeltaAngle(projectile.transform.eulerAngles.z, 90)) < .01f, entry.key + ": animation affected travel transform");
                projectile.CancelPreparedProjectile(); Check(!projectile.gameObject.activeSelf, entry.key + ": return failed");
                var reused = pool.Spawn(Vector3.zero);
                Check(reused == projectile && visual.localScale == Vector3.one && renderer.color == Color.white && renderer.sprite == expectedSprite && !animator.GetBool("IsFlying"), entry.key + ": reuse must reset Idle");
                reused.CancelPreparedProjectile(); UnityEngine.Object.DestroyImmediate(poolRoot); poolRoot = null;
                Debug.Log("PASS " + entry.key + ": actual PlayMode orb Idle 8 -> Flight 8, loop, child visual, trail, unchanged travel transform, pool reset");
            }
            foreach (var entry in Read().assets)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Projectiles/" + (200 + entry.id) + ".prefab");
                poolRoot = new GameObject(entry.key + "_SingleRegression"); var pool = poolRoot.AddComponent<ProjectileObjectPool2D>();
                pool.InitPool(prefab.GetComponent<ProjectileHit2D>(), 1, 2, true, poolRoot.transform);
                var projectile = pool.Spawn(Vector3.zero); var renderer = projectile.GetComponent<SpriteRenderer>(); var animator = projectile.GetComponent<Animator>();
                projectile.Init(new ProjectileStats { hitEffectID = 200 + entry.id, speed = 1 }); projectile.SetTargetPosition(new Vector2(100, 0)); projectile.PrepareAsSatellite(100);
                Check(renderer.sprite.name.EndsWith("_Idle_01") && !animator.GetBool("IsFlying"), entry.key + ": single Idle regression");
                projectile.Launch(Vector3.right, 1, true, 100); Check(renderer.sprite.name.EndsWith("_Flight_01") && animator.GetBool("IsFlying"), entry.key + ": single Flight regression");
                projectile.CancelPreparedProjectile(); UnityEngine.Object.DestroyImmediate(poolRoot); poolRoot = null;
            }
            Debug.Log("PASS all 14 splash orb PlayMode pools and all 14 single projectile regressions"); EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); if (poolRoot != null) UnityEngine.Object.DestroyImmediate(poolRoot); EditorApplication.Exit(1); }
    }
}
