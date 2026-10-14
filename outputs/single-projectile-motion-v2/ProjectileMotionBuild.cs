using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectileMotionBuild
{
    const string Pending="ProjectileMotionV2.Pending";
    [Serializable] public class Manifest { public bool complete; public Entry[] assets; }
    [Serializable] public class Entry { public int id; public int projectileID; public string key; public string name; public string @base; public float[] color; }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static ProjectileMotionBuild(){EditorApplication.playModeStateChanged+=OnPlayState;}
    static void OnPlayState(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);ValidateRuntime();}}
    static string Base(Entry e)=>"Assets/BulletMotion_v2/"+e.name;
    public static void BuildAndTest()
    {
        GameObject root=null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("projectile-motion-manifest.json"));
            Check(manifest.complete&&manifest.assets.Length==14,"14 completed motion atlases required");
            foreach(var entry in manifest.assets)
            {
                var sprites=AssetDatabase.LoadAllAssetsAtPath(Base(entry)+".png").OfType<Sprite>().ToArray();
                Check(sprites.Length==16,entry.key+": needs 16 sprites");
                var clips=new AnimationClip[2];
                for(int state=0;state<2;state++)
                {
                    string stateName=state==0?"Idle":"Flight",clipPath=Base(entry)+"_"+stateName+".anim";
                    var sequence=sprites.Where(s=>s.name.Contains("_"+stateName+"_")).OrderBy(s=>s.name).ToArray();
                    Check(sequence.Length==8,entry.key+": needs 8 "+stateName+" images");
                    var clip=new AnimationClip{name=entry.key+"_"+stateName,frameRate=12};
                    AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),sequence.Select((sprite,i)=>new ObjectReferenceKeyframe{time=i/12f,value=sprite}).ToArray());
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                    AssetDatabase.CreateAsset(clip,clipPath);clips[state]=clip;
                }
                string controllerPath=Base(entry)+".controller";
                var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                controller.AddParameter("IsFlying",AnimatorControllerParameterType.Bool);
                var machine=controller.layers[0].stateMachine;
                var idle=machine.AddState("Idle");idle.motion=clips[0];idle.speed=1;
                var flight=machine.AddState("Flight");flight.motion=clips[1];flight.speed=1;
                machine.defaultState=idle;
                var launch=idle.AddTransition(flight);launch.hasExitTime=false;launch.duration=0;launch.AddCondition(AnimatorConditionMode.If,0,"IsFlying");
                var reset=flight.AddTransition(idle);reset.hasExitTime=false;reset.duration=0;reset.AddCondition(AnimatorConditionMode.IfNot,0,"IsFlying");
                EditorUtility.SetDirty(controller);
                string prefabPath="Assets/Resources/Projectiles/"+entry.projectileID+".prefab";
                root=PrefabUtility.LoadPrefabContents(prefabPath);
                var animator=root.GetComponent<Animator>();if(animator==null)animator=root.AddComponent<Animator>();
                animator.enabled=true;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=sprites.Single(s=>s.name.EndsWith("_Idle_01"));renderer.color=Color.white;
                var settingsObject=new SerializedObject(root.GetComponent<ProjectileHit2D>());
                settingsObject.FindProperty("m_usePrefabVisual").boolValue=true;
                settingsObject.FindProperty("m_prefabEffectColor").colorValue=new Color(entry.color[0],entry.color[1],entry.color[2],1);
                settingsObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);PrefabUtility.UnloadPrefabContents(root);root=null;
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception error){Debug.LogException(error);if(root!=null)PrefabUtility.UnloadPrefabContents(root);EditorApplication.Exit(1);}
    }
    static void ValidateRuntime()
    {
        GameObject poolRoot=null;
        try
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("projectile-motion-manifest.json"));
            foreach(var entry in manifest.assets)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Projectiles/"+entry.projectileID+".prefab");
                Check(prefab!=null&&prefab.GetComponent<ProjectileHit2D>()!=null,entry.key+": prefab script missing");
                poolRoot=new GameObject(entry.key+"_MotionPoolCheck");var pool=poolRoot.AddComponent<ProjectileObjectPool2D>();
                pool.InitPool(prefab.GetComponent<ProjectileHit2D>(),1,2,true,poolRoot.transform);
                var projectile=pool.Spawn(Vector3.zero);Check(projectile!=null,entry.key+": spawn failed");
                var animator=projectile.GetComponent<Animator>();var renderer=projectile.GetComponent<SpriteRenderer>();
                var sprites=AssetDatabase.LoadAllAssetsAtPath(Base(entry)+".png").OfType<Sprite>().ToArray();
                var idle=sprites.Where(s=>s.name.Contains("_Idle_")).OrderBy(s=>s.name).ToArray();
                var flight=sprites.Where(s=>s.name.Contains("_Flight_")).OrderBy(s=>s.name).ToArray();
                Check(animator!=null&&animator.enabled&&renderer.sprite==idle[0]&&!animator.GetBool("IsFlying"),entry.key+": pooled spawn must start Idle");
                projectile.Init(new ProjectileStats{projectileID=1000+entry.projectileID,hitEffectID=entry.projectileID,speed=1,damage=1});
                projectile.SetTargetPosition(new Vector2(100,0));projectile.SetVisualScale(.5f);projectile.PrepareAsSatellite(100);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")&&!animator.GetBool("IsFlying"),entry.key+": preparation must be Idle");
                foreach(string stateName in new[]{"Idle","Flight"})
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Base(entry)+"_"+stateName+".anim");
                    Check(clip!=null&&clip.frameRate==12&&Math.Abs(clip.length-8/12f)<.0001f&&AnimationUtility.GetAnimationClipSettings(clip).loopTime&&clip.events.Length==0,entry.key+": clip must loop 8 frames without return event");
                }
                for(int i=0;i<8;i++){animator.Play("Idle",0,(i/12f+.001f)/(8/12f));animator.Update(0);Check(renderer.sprite==idle[i],entry.key+": Idle frame "+i);}
                projectile.Launch(Vector3.right,1,true,100);
                Check(animator.GetBool("IsFlying")&&animator.GetCurrentAnimatorStateInfo(0).IsName("Flight")&&renderer.sprite==flight[0],entry.key+": Launch must immediately select Flight");
                for(int i=0;i<8;i++){animator.Play("Flight",0,(i/12f+.001f)/(8/12f));animator.Update(0);Check(renderer.sprite==flight[i],entry.key+": Flight frame "+i);}
                animator.Play("Flight",0,1+2/8f+.001f);animator.Update(0);Check(renderer.sprite==flight[2],entry.key+": Flight must loop across cycle");
                Check(projectile.transform.localScale==Vector3.one*.5f,entry.key+": visual scale changed");
                projectile.CancelPreparedProjectile();Check(!projectile.gameObject.activeSelf,entry.key+": pool return failed");
                var reused=pool.Spawn(Vector3.zero);Check(reused==projectile&&reused.GetComponent<SpriteRenderer>().sprite==idle[0]&&!animator.GetBool("IsFlying"),entry.key+": reuse must reset to Idle first frame");
                reused.CancelPreparedProjectile();UnityEngine.Object.DestroyImmediate(poolRoot);poolRoot=null;
                Debug.Log("PASS "+entry.key+": actual PlayMode pooled Idle -> Prepare -> Flight, 16 frames, 12 FPS, both loops, first Idle frame on reuse");
            }
            Debug.Log("PASS all 14 single projectile motion state machines and PlayMode pool lifecycle");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);if(poolRoot!=null)UnityEngine.Object.DestroyImmediate(poolRoot);EditorApplication.Exit(1);}
    }
}
