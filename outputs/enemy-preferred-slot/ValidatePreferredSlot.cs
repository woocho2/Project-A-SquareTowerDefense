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
        Func<EnemyHealthController> spawn=()=>{var e=pool.Spawn(Vector3.zero,1,1);Check(e!=null,"Unexpected spawn rejection");Set(e.Movement,"m_moveSpeed",30f);return e;};
        yield return PreferredSlotChecks.Verify(pool,manager);RandomCubeChecks.Verify(prefab,pool,manager);yield return RandomCubeChecks.VerifyRolls(pool);yield return TripleRollChecks.Verify(pool,manager);TripleRollChecks.RenderPreview(prefab);yield return HopRightingChecks.Verify(pool,manager);HopRightingChecks.RenderPreview(prefab);
        var start=new List<EnemyHealthController>();for(int i=0;i<9;i++)start.Add(spawn());
        Check(pool.Spawn(Vector3.zero,1,1)==null,"Tenth spawn must be rejected");Check(manager.GetAllEnemiesOnPath().Count==9,"Rejected spawn leaked into path registry");UniquePositions(manager,0);
        var center=manager.GetPathWorldPosition(0);
        foreach(var e in start){var bounds=e.GetComponentInChildren<MeshRenderer>().bounds;Check(Mathf.Abs(bounds.size.x-4f/9f)<.001f,"Scaled cube side mismatch");Check(Mathf.Abs(bounds.center.x-center.x)+bounds.extents.x<=.75f&&Mathf.Abs(bounds.center.y-center.y)+bounds.extents.y<=.75f,"Cube falls outside tile");Check(e.GetComponent<BoxCollider2D>().bounds.size.x<.34f,"Collider did not shrink with cube");}
        RenderNine(manager);Debug.Log("PASS nine non-overlapping 3x3 cubes inside one tile, scaled collider/UI hierarchy, tenth pool spawn rejected");
        var keep=start[1];Vector3 keepPosition=keep.transform.position,freed=start[0].transform.position;start[0].ReturnToPool();var replacement=spawn();Check(replacement.transform.position==freed&&keep.transform.position==keepPosition,"Vacancy reuse moved other enemies");start[0]=replacement;
        for(int i=0;i<8;i++){Check(manager.SetEnemyAtPathIndex(start[i],1,out _),"Could not prepare destination occupants");manager.TryGetEnemyFormationPosition(start[i],out Vector3 position);start[i].transform.position=position;Set(start[i].Movement,"m_hasFormationTarget",false);}
        var sources=new List<EnemyHealthController>{start[8]};for(int i=0;i<8;i++)sources.Add(spawn());
        var incoming=sources[0];var waiting=sources[1];
        Check(manager.TryReserveEnemyArrivalPosition(incoming,1,out Vector3 reserved),"Ninth destination reservation failed");
        Check(manager.TryReserveEnemyArrivalPosition(incoming,1,out Vector3 reservedAgain)&&reservedAgain==reserved,"Repeated reservation must be idempotent");
        Check(!manager.TryReserveEnemyArrivalPosition(waiting,1,out _),"Tenth arrival reservation accepted");
        Check(!manager.SetEnemyAtPathIndex(waiting,1,out _)&&manager.GetEnemyOccupantsAtPathIndex(0).Contains(waiting),"Failed direct registration must preserve source occupancy");
        yield return waiting.Movement.ProcessTurnMoveRoutine();Check(waiting.Movement.CurrentTileIndex==0&&!waiting.Movement.IsMoving&&!Get<bool>(waiting.Movement,"m_hasMadeFirstMove"),"Blocked first move must remain pending without a coroutine hang");
        var canvas=incoming.GetComponentInChildren<Canvas>(true);var uiRotation=canvas.transform.rotation;
        yield return incoming.Movement.ProcessTurnMoveRoutine();Check(incoming.Movement.CurrentTileIndex==1&&manager.GetEnemyOccupantsAtPathIndex(1).Count==9,"Reserved ninth arrival failed");
        Check(canvas.transform.rotation==uiRotation&&incoming.transform.rotation==Quaternion.identity,"3D roll rotated root or HP UI");
        start[0].ReturnToPool();yield return waiting.Movement.ProcessTurnMoveRoutine();Check(waiting.Movement.CurrentTileIndex==1&&Get<bool>(waiting.Movement,"m_hasMadeFirstMove"),"Blocked enemy did not retry first movement after vacancy");
        UniquePositions(manager,1);Debug.Log("PASS arrival reservations enforce nine, failed registration preserves source, blocked movement stops and retries after vacancy");
        spawn();spawn();Check(manager.GetEnemyOccupantsAtPathIndex(0).Count==9,"Reverse test setup needs full start");
        incoming.Movement.ReverseNextDiceMove();yield return incoming.Movement.ProcessTurnMoveRoutine();Check(incoming.Movement.CurrentTileIndex==1,"Reverse movement entered a full tile");
        manager.GetEnemyOccupantsAtPathIndex(0)[0].ReturnToPool();incoming.Movement.ReverseNextDiceMove();yield return incoming.Movement.ProcessTurnMoveRoutine();Check(incoming.Movement.CurrentTileIndex==0,"Reverse retry did not enter vacancy");
        var all=manager.GetAllEnemiesOnPath();for(int i=0;i<9;i++)Check(manager.TryReserveEnemyArrivalPosition(all[i],2,out _),"Concurrent reservation rejected before nine");Check(!manager.TryReserveEnemyArrivalPosition(all[9],2,out _),"Tenth concurrent reservation accepted");
        Check(manager.GetLivingEnemiesAtPathIndex(2).Count==0,"Reservations became attack targets before arrival");all[0].ReturnToPool();Check(manager.TryReserveEnemyArrivalPosition(all[9],2,out _),"Pool return did not release reservation");
        foreach(var e in manager.GetAllEnemiesOnPath())e.ReturnToPool();Check(Get<Dictionary<EnemyHealthController,int>>(manager,"m_arrivalPathIndexByEnemy").Count==0&&Get<Dictionary<EnemyHealthController,int>>(manager,"m_formationSlotByEnemy").Count==0,"Pool return leaked capacity/slot state");
        Debug.Log("PASS reverse blocking, simultaneous reservation cap, attack-target isolation and reservation release on pool return");
        var moving=new List<EnemyHealthController>();for(int i=0;i<9;i++){var e=spawn();moving.Add(e);StartCoroutine(e.Movement.ProcessTurnMoveRoutine());}
        float deadline=Time.realtimeSinceStartup+5;while(moving.Any(e=>e.Movement.IsMoving)){Check(Time.realtimeSinceStartup<deadline,"Concurrent moves deadlocked");yield return null;}
        Check(manager.GetEnemyOccupantsAtPathIndex(1).Count==9&&manager.GetEnemyOccupantsAtPathIndex(0).Count==0,"Concurrent nine arrivals lost occupancy");UniquePositions(manager,1);
        foreach(var e in manager.GetAllEnemiesOnPath())e.ReturnToPool();
        // Interrupted rolls must release both the source slot and destination reservation.
        var interrupted=spawn();Set(interrupted.Movement,"m_moveSpeed",.1f);StartCoroutine(interrupted.Movement.ProcessTurnMoveRoutine());yield return null;interrupted.ReturnToPool();
        Check(manager.GetAllEnemiesOnPath().Count==0&&Get<Dictionary<EnemyHealthController,int>>(manager,"m_arrivalPathIndexByEnemy").Count==0,"Interrupted roll leaked a reservation");
        var reused=spawn();Check(reused.CurrentHP==100&&Quaternion.Angle(reused.GetComponentInChildren<EnemyCubeMeshVisual>().transform.localRotation,EnemyCubeMeshVisual.RotationFor(EnemyCubeOrientation.FromFace(reused.Movement.VisibleCubeFace,reused.Movement.CubeFaceQuarterTurns)))<.1f,"Reuse failed to reset cube or health");reused.ReturnToPool();
        Debug.Log("PASS nine actual concurrent 3D rolls, unique landings, interrupted movement and pool reuse");
        cells.Add(Vector3Int.zero);BuildPath(manager,cells);for(int i=0;i<9;i++)spawn();
        var outsider=Instantiate(prefab).GetComponent<EnemyHealthController>();outsider.InitHealth(100,10);Check(manager.SetEnemyAtPathIndex(outsider,1,out _),"Duplicate-cell outsider setup failed");
        Check(!manager.TryReserveEnemyArrivalPosition(outsider,4,out _)&&!manager.SetEnemyAtPathIndex(outsider,4,out _),"Repeated path visits bypassed physical tile capacity");manager.GetEnemyOccupantsAtPathIndex(0)[0].ReturnToPool();
        Check(manager.TryReserveEnemyArrivalPosition(outsider,4,out _)&&manager.SetEnemyAtPathIndex(outsider,4,out _),"Repeated path visit could not reuse vacancy");Check(manager.GetEnemyOccupantsAtPathCell(Vector3Int.zero).Count==9,"Shared physical-cell cap mismatch");
        outsider.ReturnToPool();spawn();
        var waveObject=new GameObject("WaveCapacityTest");var wave=waveObject.AddComponent<WaveManager>();Set(wave,"m_enemyPool",new[]{pool,pool,pool,pool,pool});
        Check(wave.SpawnWaveEnemiesForTurn()==0&&Get<int>(wave,"m_waveSpawnedCount")==0,"Full start incorrectly consumed wave spawn quota");manager.GetEnemyOccupantsAtPathIndex(0)[0].ReturnToPool();
        Check(wave.SpawnWaveEnemiesForTurn()==1&&Get<int>(wave,"m_waveSpawnedCount")==1,"Wave retry did not use freed start slot");
        foreach(var e in manager.GetAllEnemiesOnPath())e.ReturnToPool();Destroy(waveObject);Destroy(poolObject);Destroy(gridObject);Destroy(data);
        Debug.Log("PASS shared capacity across repeated path cells and wave spawn quota preservation; all random-face and nine-slot checks passed");EditorApplication.Exit(0);
    }
}
