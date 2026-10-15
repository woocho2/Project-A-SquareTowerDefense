using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class SpawnDropChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static EnemyCubeMeshVisual Visual(EnemyHealthController enemy)=>enemy.GetComponentInChildren<EnemyCubeMeshVisual>();
    static EnemyCubeOrientation Pose(EnemyHealthController enemy)=>EnemyCubeOrientation.FromFace(enemy.Movement.VisibleCubeFace,enemy.Movement.CubeFaceQuarterTurns);
    static IEnumerator Land(List<EnemyHealthController> enemies)
    {
        float deadline=Time.realtimeSinceStartup+4;
        while(enemies.Any(e=>e!=null&&e.Movement.IsSpawning)){
            Check(Time.realtimeSinceStartup<deadline,"Spawn animation failed to finish");yield return null;
        }
    }
    public static IEnumerator Verify(EnemyObjectPool2D pool,TileManager manager)
    {
        var enemies=new List<EnemyHealthController>();
        var roots=new List<Vector3>();var faces=new List<int[]>();var rotations=new List<Quaternion>();var restingPositions=new List<Vector3>();var restingScales=new List<Vector3>();
        float started=Time.time;
        for(int i=0;i<9;i++){
            var enemy=pool.Spawn(Vector3.zero,1,1);Check(enemy!=null,"Spawn unexpectedly rejected");enemies.Add(enemy);
            var visual=Visual(enemy);roots.Add(enemy.transform.position);rotations.Add(visual.transform.localRotation);
            visual.ShowOrientation(Pose(enemy));restingPositions.Add(visual.transform.localPosition);restingScales.Add(visual.transform.localScale);visual.AnimateSpawn(Pose(enemy),0);
            faces.Add(Enumerable.Range(0,6).Select(visual.GetFaceAssetIndex).ToArray());
            Check(enemy.Movement.IsSpawning&&enemy.Movement.IsMoving,"Spawn must report busy immediately");
            Check(visual.transform.localPosition.y>6,"Cube did not start above its tile");
            Check(enemy.Movement.CubeFaceQuarterTurns==0,"Spawn face must stay upright");
        }
        Check(pool.Spawn(Vector3.zero,1,1)==null&&manager.GetAllEnemiesOnPath().Count==9,"Airborne cubes must reserve all nine tile slots");
        var shadow=enemies[0].transform.Find("CubeGroundShadow");Vector3 airborneShadowScale=shadow.localScale;
        Vector3 airbornePosition=Visual(enemies[0]).transform.localPosition;
        Time.timeScale=0;yield return null;yield return null;yield return null;
        Check(Visual(enemies[0]).transform.localPosition==airbornePosition&&enemies[0].Movement.IsSpawning,"Pause must freeze drop");Time.timeScale=1;
        bool sawFall=false,sawImpact=false,sawBounce=false;
        while(enemies.Any(e=>e.Movement.IsSpawning)){
            float elapsed=Time.time-started;var first=Visual(enemies[0]);
            if(first.transform.localPosition.y<airbornePosition.y-.1f)sawFall=true;
            if(first.transform.localScale!=restingScales[0])sawImpact=true;
            if(elapsed>.43f&&first.transform.localPosition.y>.02f)sawBounce=true;
            for(int i=0;i<9;i++){
                var visual=Visual(enemies[i]);Check(enemies[i].transform.position==roots[i],"Drop moved gameplay root/slot");
                Check(enemies[i].Movement.CurrentTileIndex==0,"Drop advanced logical path");
                Check(Quaternion.Angle(visual.transform.localRotation,rotations[i])<.01f,"Drop rotated the selected face");
                Check(Mathf.Abs(visual.transform.localPosition.y-first.transform.localPosition.y)<.0001f,"Same-frame batch dropped out of sync");
                for(int f=0;f<6;f++)Check(visual.GetFaceAssetIndex(f)==faces[i][f],"Drop rerolled face assets");
            }
            Check(Time.time-started<1,"Drop took too long");yield return null;
        }
        Check(sawFall&&sawImpact&&sawBounce,"Missing fall, deformation or rebound phase");
        Check(Time.time-started>=.67f&&Time.time-started<.85f,"Drop duration outside expected 0.68s");
        for(int i=0;i<9;i++)Check(!enemies[i].Movement.IsMoving&&Visual(enemies[i]).transform.localPosition==restingPositions[i]&&Visual(enemies[i]).transform.localScale==restingScales[i],"Landing left transform/busy state");
        Check(shadow.localScale.x>airborneShadowScale.x&&shadow.localPosition.y<0,"Ground shadow failed to grow on landing");
        foreach(var enemy in enemies)enemy.ReturnToPool();
        Debug.Log("PASS nine synchronized pool spawns, acceleration/impact/bounce, stable upright faces, fixed slots, airborne capacity, scaled-time pause and 0.68s restoration");

        var moving=pool.Spawn(Vector3.zero,1,1);var move=moving.Movement;Set(move,"m_moveSpeed",1f);
        var pendingMove=move.ProcessTurnMoveRoutine();Check(pendingMove.MoveNext(),"Move should wait while spawning");
        Check(move.CurrentTileIndex==0,"Move advanced before landing");
        yield return Land(new List<EnemyHealthController>{moving});float moveStart=Time.time;
        yield return move.ProcessTurnMoveRoutine();
        Check(move.CurrentTileIndex==1&&move.CubeFaceQuarterTurns==0&&Time.time-moveStart<.65f,"Landing broke triple-roll or upright correction");moving.ReturnToPool();
        var cancelled=pool.Spawn(Vector3.zero,1,1);yield return null;cancelled.ReturnToPool();
        Check(!cancelled.Movement.IsSpawning&&!cancelled.Movement.IsMoving&&manager.GetAllEnemiesOnPath().Count==0,"Pool return during drop leaked state/occupancy");
        var reused=pool.Spawn(Vector3.zero,1,1);Check(reused==cancelled&&reused.Movement.IsSpawning&&Visual(reused).transform.localPosition.y>6,"Pool reuse did not restart drop from full height");
        yield return Land(new List<EnemyHealthController>{reused});reused.ReturnToPool();
        var disabled=pool.Spawn(Vector3.zero,1,1);disabled.gameObject.SetActive(false);
        Check(!disabled.Movement.IsSpawning&&manager.GetAllEnemiesOnPath().Count==0,"Direct disable during drop leaked state");disabled.ReturnToPool();
        Debug.Log("PASS first move waits for landing, actual triple-roll after drop, interrupted drop cleanup, pool reuse and direct disable");

        var existing=pool.Spawn(Vector3.zero,1,1);yield return Land(new List<EnemyHealthController>{existing});
        var fresh=pool.Spawn(Vector3.zero,1,1);var objectManager=new GameObject("SpawnSyncEnemyManager");var enemyManager=objectManager.AddComponent<EnemyManager>();
        var turn=enemyManager.ProcessEnemyTurnRoutine(false);
        while(fresh.Movement.IsSpawning){Check(turn.MoveNext(),"Enemy phase ended during drop");Check(existing.Movement.CurrentTileIndex==0&&existing.transform.position==roots[0],"Existing enemy moved before new cube landed");yield return null;}
        yield return turn;
        Check(existing.Movement.CurrentTileIndex==1&&fresh.Movement.CurrentTileIndex==1,"Enemy phase did not resume movement after landing");
        existing.ReturnToPool();fresh.ReturnToPool();UnityEngine.Object.Destroy(objectManager);
        Debug.Log("PASS EnemyManager waits for fresh landing before moving both existing and new enemies");
    }

    public static void RenderPreview(GameObject prefab)
    {
        var instances=new List<GameObject>();var floors=new List<GameObject>();
        var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.white);texture.Apply();var sprite=Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1);
        for(int tile=0;tile<3;tile++){
            var floor=new GameObject("DropPreviewTile",typeof(SpriteRenderer));floor.layer=8;floor.transform.position=new Vector3((tile-1)*1.5f,0,1);floor.transform.localScale=new Vector3(1.46f,1.46f,1);
            var renderer=floor.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingLayerName="MapTile";renderer.color=new Color(.33f+tile*.025f,.4f+tile*.025f,.45f+tile*.025f);floors.Add(floor);
            for(int slot=0;slot<3;slot++){
                var enemy=UnityEngine.Object.Instantiate(prefab);enemy.transform.position=new Vector3((tile-1)*1.5f+(slot-1)*.46f,0,0);instances.Add(enemy);
                enemy.GetComponentInChildren<EnemyCubeMeshVisual>().RandomizeForSpawn();
            }
        }
        var cameraObject=new GameObject("SpawnDropPreviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();cameraObject.transform.position=new Vector3(0,.9f,-8);
        camera.orthographic=true;camera.orthographicSize=1.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.18f);camera.cullingMask=1<<8;
        var target=new RenderTexture(800,480,24);camera.targetTexture=target;var pixels=new Texture2D(800,480,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        Directory.CreateDirectory("spawn-drop-preview");
        for(int frame=0;frame<60;frame++){
            float time=frame/59f*1.05f;
            foreach(var instance in instances){var visual=instance.GetComponentInChildren<EnemyCubeMeshVisual>();visual.AnimateSpawn(Pose(instance.GetComponent<EnemyHealthController>()),time);}
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,800,480),0,0);pixels.Apply();File.WriteAllBytes("spawn-drop-preview/"+frame.ToString("D3")+".png",pixels.EncodeToPNG());
        }
        RenderTexture.active=previous;camera.targetTexture=null;cameraObject.SetActive(false);
        foreach(var instance in instances){instance.SetActive(false);UnityEngine.Object.Destroy(instance);}foreach(var floor in floors)UnityEngine.Object.Destroy(floor);
        UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(sprite);UnityEngine.Object.Destroy(texture);
        Debug.Log("PASS 60 actual Unity GPU frames of synchronized nine-cube drop");
    }
}
