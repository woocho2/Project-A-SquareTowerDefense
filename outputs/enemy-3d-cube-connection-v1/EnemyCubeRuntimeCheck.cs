using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class EnemyCubeRuntimeCheck : MonoBehaviour
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object target,string field,object value){target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Render(GameObject instance,EnemyCubeMeshVisual visual,EnemyCubeOrientation orientation,string name,float progress=-1)
    {
        visual.ShowOrientation(orientation);if(progress>=0)visual.AnimateRoll(orientation,EnemyCubeRollDirection.Right,progress);
        var cameraObject=new GameObject("CubeRenderCamera");var camera=cameraObject.AddComponent<Camera>();cameraObject.transform.position=new Vector3(0,0,-8);
        camera.orthographic=true;camera.orthographicSize=1.15f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(36,48,58,255);camera.cullingMask=1<<8;
        var target=new RenderTexture(512,512,24);camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();
        int visible=0;foreach(var p in texture.GetPixels32())if(Mathf.Abs(p.r-36)+Mathf.Abs(p.g-48)+Mathf.Abs(p.b-58)>35)visible++;
        Check(visible>30000,"GPU cube render empty: "+name);Directory.CreateDirectory("cube-previews");File.WriteAllBytes("cube-previews/"+name+".png",texture.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;Destroy(target);Destroy(texture);Destroy(cameraObject);
    }
    IEnumerator Start()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/3. Enemy/Enemy.prefab");
        Check(prefab.GetComponents<EnemyHealthController>().Length==1&&prefab.GetComponents<EnemyMoveController>().Length==1,"Enemy requires exactly one health and movement controller");
        Check(prefab.GetComponent<EnemyMoveController>()&&prefab.GetComponent<EnemyHealthController>()&&prefab.GetComponent<EnemyDebuffController>()&&prefab.GetComponent<EnemyUIController>(),"Legacy enemy scripts missing");
        Check(prefab.GetComponent<BoxCollider2D>()&&prefab.GetComponent<Rigidbody2D>()&&!prefab.GetComponent<Collider>(),"2D combat collider required");
        var instance=Instantiate(prefab);var visual=instance.GetComponentInChildren<EnemyCubeMeshVisual>();var movement=instance.GetComponent<EnemyMoveController>();
        Check(visual.HasAllFaces,"Actual six-submesh cube with six materials required");
        Check(instance.transform.localScale==Vector3.one&&Mathf.Abs(visual.transform.localScale.x-256f/192f)<.0001f,"3D cube scale must match path tile and have nonzero Z");
        var seen=new HashSet<EnemyCubeOrientation>();var queue=new Queue<EnemyCubeOrientation>();queue.Enqueue(EnemyCubeOrientation.Identity);int transitions=0;
        while(queue.Count>0)
        {
            var before=queue.Dequeue();if(!seen.Add(before))continue;
            foreach(EnemyCubeRollDirection direction in Enum.GetValues(typeof(EnemyCubeRollDirection)))
            {
                var after=before.Rolled(direction);var expected=EnemyCubeMeshVisual.RotationFor(after);
                visual.ShowOrientation(before);visual.AnimateRoll(before,direction,1);
                Check(Quaternion.Angle(visual.transform.localRotation,expected)<.1f,"3D/integer orientation mismatch: "+before.VisibleFace+" "+direction);
                visual.AnimateRoll(before,direction,.5f);Check(visual.transform.localPosition.z<-(256f/192f)*.5f,"Rolling cube must lift toward camera");
                Check(instance.transform.rotation==Quaternion.identity,"Enemy root must not rotate");transitions++;queue.Enqueue(after);
            }
        }
        Check(seen.Count==24&&transitions==96,"All 24 orientations and 96 transitions required");Debug.Log("PASS 24 actual mesh orientations and 96 physical quarter-turn transitions");
        foreach(EnemyCubeFace face in Enum.GetValues(typeof(EnemyCubeFace)))foreach(var pose in seen)if(pose.VisibleFace==face&&pose.CounterClockwiseQuarterTurns==0){Render(instance,visual,pose,face.ToString());break;}
        Render(instance,visual,EnemyCubeOrientation.Identity,"Roll_25",.25f);Render(instance,visual,EnemyCubeOrientation.Identity,"Roll_50",.5f);
        Debug.Log("PASS GPU rendering all six face textures and two intermediate real-mesh roll poses");Destroy(instance);yield return null;
        var gridObject=new GameObject("TestBoard",typeof(Grid));gridObject.GetComponent<Grid>().cellSize=new Vector3(1.5f,1.5f,1);
        var tileObject=new GameObject("TestPath",typeof(Tilemap));tileObject.transform.SetParent(gridObject.transform,false);tileObject.SetActive(false);
        var manager=tileObject.AddComponent<TileManager>();manager.enabled=false;Set(manager,"tilemap",tileObject.GetComponent<Tilemap>());
        var cells=new List<Vector3Int>{new Vector3Int(0,0),new Vector3Int(1,0),new Vector3Int(1,1),new Vector3Int(0,1)};
        Set(manager,"m_pathGridPositions",cells);Set(manager,"m_enablePathArrowPulse",false);tileObject.SetActive(true);
        typeof(TileManager).GetMethod("InitializePathTileIndices",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
        var data=ScriptableObject.CreateInstance<EnemyData>();data.MaxHP=100;data.Defend=10;data.Speed=1;data.Action=1;
        var poolObject=new GameObject("ActualEnemyPool");poolObject.SetActive(false);var pool=poolObject.AddComponent<EnemyObjectPool2D>();
        Set(pool,"m_enemyData",data);Set(pool,"enemyPrefab",prefab.GetComponent<EnemyHealthController>());Set(pool,"initialSize",1);Set(pool,"maxSize",3);Set(pool,"expandable",true);poolObject.SetActive(true);
        var health=pool.Spawn(Vector3.zero,1,1);Check(health!=null&&health.CurrentHP==100,"Actual pool spawn failed");movement=health.Movement;visual=health.GetComponentInChildren<EnemyCubeMeshVisual>();
        var canvas=health.GetComponentInChildren<Canvas>(true);var uiPosition=canvas.transform.localPosition;var uiRotation=canvas.transform.localRotation;
        Set(movement,"m_moveSpeed",30f);var orientation=EnemyCubeOrientation.Identity;
        foreach(var direction in new[]{EnemyCubeRollDirection.Right,EnemyCubeRollDirection.Up,EnemyCubeRollDirection.Left,EnemyCubeRollDirection.Down})
        {
            yield return movement.ProcessTurnMoveRoutine();orientation=orientation.Rolled(direction);
            Check(movement.VisibleCubeFace==orientation.VisibleFace&&movement.CubeFaceQuarterTurns==orientation.CounterClockwiseQuarterTurns,"Actual path turn orientation mismatch");
            Check(Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(orientation))<.1f,"Actual path cube pose wrong");
            Check(canvas.transform.localPosition==uiPosition&&canvas.transform.localRotation==uiRotation&&health.transform.rotation==Quaternion.identity,"Rolling changed HP UI or root rotation");
        }
        Check(movement.CurrentTileIndex==0,"Looped board must return to Start");
        movement.ReverseNextDiceMove();yield return movement.ProcessTurnMoveRoutine();orientation=orientation.Rolled(EnemyCubeRollDirection.Up);
        Check(movement.CurrentTileIndex==3&&movement.VisibleCubeFace==orientation.VisibleFace,"Reverse movement must roll correctly");
        Physics2D.SyncTransforms();bool detected=false;foreach(var hit in Physics2D.OverlapPointAll(health.transform.position))if(hit.GetComponent<EnemyHealthController>()==health)detected=true;
        Check(detected,"3D enemy must remain targetable by 2D tower queries");
        health.ApplyDamage(20);Check(health.CurrentHP<100&&canvas.gameObject.activeSelf,"Damage/HP bar wiring failed");
        var debuff=health.GetComponent<EnemyDebuffController>();debuff.ApplyZoneStack(DebuffTarget.Sword,1,0,1,100);
        Check(Get<Dictionary<DebuffTarget,EnemyDebuffState>>(debuff,"m_states").Count==1,"Existing debuff script not functional");
        Debug.Log("PASS actual pooled path moves, corners, reverse movement, 2D targeting, damage, HP UI and debuffs");
        health.ReturnToPool();Check(!health.gameObject.activeSelf&&manager.GetAllEnemiesOnPath().Count==0,"Pool return must release path occupancy");
        var reused=pool.Spawn(Vector3.zero,1,1);Check(reused==health&&reused.CurrentHP==100&&reused.Movement.VisibleCubeFace==EnemyCubeFace.Front,"Reuse must reset health and visible cube face");
        Check(Quaternion.Angle(visual.transform.localRotation,Quaternion.identity)<.1f&&Get<Dictionary<DebuffTarget,EnemyDebuffState>>(debuff,"m_states").Count==0,"Reuse must reset 3D pose and debuffs");
        Set(movement,"m_moveSpeed",1f);movement.StartCoroutine(movement.ProcessTurnMoveRoutine());yield return null;movement.StopMovement();
        Check(!movement.IsMoving&&Quaternion.Angle(visual.transform.localRotation,Quaternion.identity)<.1f,"Interrupted movement must restore committed pose");
        reused.ReturnToPool();Destroy(poolObject);Destroy(gridObject);Destroy(data);
        Debug.Log("PASS pool reuse/reset and interrupted roll recovery");
        var legacy=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/3. Enemy/1. Enemy_normal.prefab"));legacy.GetComponent<EnemyMoveController>().InitMovement(null);
        Check(legacy.GetComponentInChildren<EnemyCubeVisual>()!=null&&legacy.GetComponentInChildren<EnemyCubeMeshVisual>()==null,"Legacy sprite cube compatibility lost");Destroy(legacy);
        Debug.Log("PASS legacy sprite cube compatibility; all enemy 3D cube checks passed");UnityEditor.EditorApplication.Exit(0);
    }
}
