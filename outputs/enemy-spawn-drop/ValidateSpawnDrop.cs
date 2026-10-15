using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[InitializeOnLoad]
public static class RandomCubeValidationEntry
{
    const string Pending="EnemyRandomFaces.Pending";
    static RandomCubeValidationEntry()
    {
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))new GameObject("RandomCubeRuntimeChecks").AddComponent<RandomCubeRuntimeChecks>();};
        Application.logMessageReceived+=(message,trace,type)=>{
            if(trace.Contains("UnityEditor.Search.")||trace.Contains("Unity.Burst."))return;
            if(SessionState.GetBool(Pending,false)&&(type==LogType.Exception||type==LogType.Error)){File.WriteAllText("nine-slots-failure.txt",message+"\n"+trace);EditorApplication.Exit(1);}
        };
    }
    public static void Run(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
}

public sealed class RandomCubeRuntimeChecks : MonoBehaviour
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void BuildPath(TileManager manager,List<Vector3Int> cells)
    {
        Set(manager,"m_pathGridPositions",cells);
        typeof(TileManager).GetMethod("InitializePathTileIndices",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
    }
    static void UniquePositions(TileManager manager,int index)
    {
        var seen=new HashSet<Vector3>();foreach(var e in manager.GetEnemyOccupantsAtPathIndex(index)){
            Check(manager.TryGetEnemyFormationPosition(e,out Vector3 position),"Missing assigned slot");Check(seen.Add(position),"Duplicate assigned slot");
            Check(Vector3.Distance(e.transform.position,position)<.001f,"Enemy did not land on its assigned slot");
        }
    }
    static void RenderNine(TileManager manager)
    {
        var center=manager.GetPathWorldPosition(0);
        var back=new GameObject("TilePreview",typeof(SpriteRenderer));back.layer=8;back.transform.position=center+Vector3.forward;
        var texture=new Texture2D(1,1);texture.SetPixel(0,0,new Color(.16f,.21f,.25f));texture.Apply();
        back.GetComponent<SpriteRenderer>().sprite=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);back.transform.localScale=Vector3.one*1.5f;
        var cameraObject=new GameObject("NineCubePreviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();cameraObject.transform.position=new Vector3(center.x,center.y,-8);
        camera.orthographic=true;camera.orthographicSize=.95f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.1f,.13f);camera.cullingMask=1<<8;
        var target=new RenderTexture(768,768,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
        var result=new Texture2D(768,768,TextureFormat.RGBA32,false);result.ReadPixels(new Rect(0,0,768,768),0,0);result.Apply();File.WriteAllBytes("nine-cubes-on-tile.png",result.EncodeToPNG());RenderTexture.active=old;
        camera.targetTexture=null;Destroy(target);Destroy(result);Destroy(cameraObject);Destroy(back);Destroy(texture);
    }
    IEnumerator Start()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/3. Enemy/Enemy.prefab");Check(prefab!=null,"Enemy prefab missing");
        Check(Mathf.Abs(prefab.transform.localScale.x-1f/3f)<.00001f,"Cube must have one-third linear scale");
        var gridObject=new GameObject("TestBoard",typeof(Grid));gridObject.GetComponent<Grid>().cellSize=new Vector3(1.5f,1.5f,1);
        var pathObject=new GameObject("TestPath",typeof(Tilemap));pathObject.transform.SetParent(gridObject.transform,false);pathObject.SetActive(false);
        var manager=pathObject.AddComponent<TileManager>();manager.enabled=false;Set(manager,"tilemap",pathObject.GetComponent<Tilemap>());Set(manager,"m_enablePathArrowPulse",false);
        var cells=new List<Vector3Int>{Vector3Int.zero,Vector3Int.right,new Vector3Int(1,1),Vector3Int.up};Set(manager,"m_pathGridPositions",cells);pathObject.SetActive(true);BuildPath(manager,cells);
        var data=ScriptableObject.CreateInstance<EnemyData>();data.MaxHP=100;data.Defend=10;data.Speed=1;data.Action=1;
        var poolObject=new GameObject("TestPool");poolObject.SetActive(false);var pool=poolObject.AddComponent<EnemyObjectPool2D>();Set(pool,"m_enemyData",data);Set(pool,"enemyPrefab",prefab.GetComponent<EnemyHealthController>());Set(pool,"initialSize",1);Set(pool,"maxSize",40);poolObject.SetActive(true);
        yield return SpawnDropChecks.Verify(pool,manager);
        SpawnDropChecks.RenderPreview(prefab);
        foreach(var e in manager.GetAllEnemiesOnPath())e.ReturnToPool();
        Destroy(poolObject);Destroy(gridObject);Destroy(data);
        Debug.Log("PASS all spawn-drop runtime checks and GPU preview");EditorApplication.Exit(0);
    }
}
