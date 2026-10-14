using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AllHitCheck
{
    [Serializable] public class Manifest { public bool complete; public Entry[] assets; }
    [Serializable] public class Pivot { public float x; public float y; }
    [Serializable] public class Entry { public int id; public int effectID; public string key; public string kind; public string name; public string @base; public bool reused; public Pivot pivot; }
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);}
    public static void Run()
    {
        GameObject sample=null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("all-hit-manifest.json"));
            Check(manifest.complete && manifest.assets.Length==28,"All 28 effects required");
            var library=AssetDatabase.LoadAssetAtPath<EffectLibrary>("Assets/2. Prefab/4. Effect/EffectLibrary.asset");
            Check(library!=null && library.effectEntries.Select(x=>x.effectID).Distinct().Count()==library.effectEntries.Count,"Effect library missing or duplicate IDs");
            foreach(var entry in manifest.assets)
            {
                string basePath=entry.reused?"Assets/SwordHit_v3/"+entry.name:"Assets/TowerHits_v4/"+entry.name;
                var sprites=AssetDatabase.LoadAllAssetsAtPath(basePath+".png").OfType<Sprite>().OrderBy(x=>x.name).ToArray();
                Check(sprites.Length==30,entry.name+": sprite count");
                for(int i=0;i<30;i++)
                {
                    Check(sprites[i].rect==new Rect(i%6*256,(4-i/6)*256,256,256),entry.name+": sprite order/rect "+i);
                    Check(sprites[i].pixelsPerUnit==256,entry.name+": world scale");
                    Check(entry.pivot!=null&&sprites[i].pivot==new Vector2(entry.pivot.x*256,entry.pivot.y*256),entry.name+": impact pivot");
                }
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(basePath+".anim");
                Check(clip!=null&&clip.frameRate==12&&Math.Abs(clip.length-2.5f)<.0001f,entry.name+": length/FPS");
                Check(!AnimationUtility.GetAnimationClipSettings(clip).loopTime,entry.name+": should play once");
                var bindings=AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Check(bindings.Length==1&&bindings[0].path==""&&bindings[0].type==typeof(SpriteRenderer)&&bindings[0].propertyName=="m_Sprite",entry.name+": binding");
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,bindings[0]);
                Check(keys.Length==30,entry.name+": key count");
                for(int i=0;i<30;i++)Check(keys[i].value==sprites[i]&&Math.Abs(keys[i].time-i/12f)<.0001f,entry.name+": key "+i);
                Check(clip.events.Length==1&&clip.events[0].time==2.5f&&clip.events[0].functionName=="DestroyEffect",entry.name+": return event");
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(basePath+".controller");
                Check(controller!=null,entry.name+": controller missing");
                var state=controller.layers[0].stateMachine.defaultState;
                Check(state!=null&&state.speed==1&&state.motion==clip,entry.name+": normal speed or clip reference");
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(basePath+".prefab");
                Check(prefab!=null&&library.GetEffectPrefab(entry.effectID)==prefab,entry.name+": live effect library connection");
                Check(prefab.GetComponent<SpriteRenderer>().sprite==sprites[0]&&prefab.GetComponent<Animator>().runtimeAnimatorController==controller,entry.name+": prefab reference");
                Check(prefab.GetComponent<EffectPool2D>()!=null&&prefab.GetComponent<EffectController>()!=null,entry.name+": pool components");
                sample=new GameObject("AllHitFrameCheck",typeof(SpriteRenderer),typeof(Animator));
                var renderer=sample.GetComponent<SpriteRenderer>();var animator=sample.GetComponent<Animator>();
                animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
                for(int i=0;i<30;i++){animator.Play(state.name,0,(i/12f+.001f)/clip.length);animator.Update(0);Check(renderer.sprite==sprites[i],entry.name+": actual Animator frame "+i);}
                animator.Play(state.name,0,2.499f/clip.length);animator.Update(0);Check(renderer.sprite==sprites[29],entry.name+": final full frame");
                UnityEngine.Object.DestroyImmediate(sample);sample=null;
                Debug.Log("PASS "+entry.key+" "+entry.kind+" ID "+entry.effectID+": 30 sprites, all Animator frames, 12 FPS, 2.5 seconds, live prefab and pool event");
            }
            var rows=File.ReadAllLines("Assets/Resources/TowerDataCSV.csv").Skip(1).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Split(',')).Where(x=>x[12]=="Target"||x[12]=="Splash").ToArray();
            Check(rows.Length==140,"Attack tower CSV row count");
            foreach(var row in rows){int id=int.Parse(row[0]);int expected=(row[12]=="Splash"?100:200)+id%100;Check(int.Parse(row[16])==expected&&library.GetEffectPrefab(expected)!=null,"CSV effect mapping "+id);}
            var manager=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/1. Manager/EffectManager.prefab");
            Check(manager!=null&&new SerializedObject(manager.GetComponent<EffectManager>()).FindProperty("m_effectLibrary").objectReferenceValue==library,"Live manager library reference");
            Debug.Log("PASS all 28 attack effect bindings, 140 tower tier rows, actual EffectManager library, 840 Animator frame samples");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);if(sample!=null)UnityEngine.Object.DestroyImmediate(sample);EditorApplication.Exit(1);}
    }
}
