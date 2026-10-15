using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class TripleRollChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    public static IEnumerator Verify(EnemyObjectPool2D pool,TileManager manager)
    {
        var enemy=pool.Spawn(Vector3.zero,1,1);Check(enemy!=null,"Triple roll spawn failed");var move=enemy.Movement;var visual=enemy.GetComponentInChildren<EnemyCubeMeshVisual>();
        Transform shadow=enemy.transform.Find("CubeGroundShadow");Check(shadow!=null&&shadow.GetComponent<MeshRenderer>().sharedMaterial.shader.isSupported,"Ground shadow/shader missing");
        Vector3 shadowPosition=shadow.localPosition,shadowScale=shadow.localScale,basePosition=visual.transform.localPosition,baseScale=visual.transform.localScale;int samples=0;
        for(int face=0;face<6;face++)for(int spin=0;spin<4;spin++)foreach(EnemyCubeRollDirection direction in Enum.GetValues(typeof(EnemyCubeRollDirection)))
        {
            var before=EnemyCubeOrientation.FromFace((EnemyCubeFace)face,spin);var final=before;for(int i=0;i<3;i++)final=final.Rolled(direction);
            float previous=-1;for(int sample=0;sample<=120;sample++){
                visual.AnimateTileRoll(before,direction,sample/120f,out float travel,out var committed);Check(travel>=previous-.00001f&&travel<=1.00001f,"Triple roll travel is not monotonic/bounded");previous=travel;samples++;
                Check(shadow.localPosition==shadowPosition,"Shadow must remain on ground while cube rolls");
                if(sample==120)Check(committed.Equals(final)&&Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(final))<.1f&&Mathf.Abs(travel-1)<.0001f,"Three real quarter-turns must determine final physical/logical pose");
            }
        }
        var initial=EnemyCubeOrientation.Identity;visual.ShowOrientation(initial);visual.AnimateWindUp(initial,EnemyCubeRollDirection.Right,.5f);
        float angle=Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(initial));Check(angle>7&&angle<9,"Launch anticipation must lean backward");
        visual.AnimateTileRoll(initial,EnemyCubeRollDirection.Right,.43f/3f,out _,out _);Check(shadow.localScale.x<shadowScale.x*.9f,"Airborne shadow must shrink");
        visual.AnimateUprightSquash(initial,.16f);Check(shadow.localScale.x>shadowScale.x,"Landing shadow must expand");visual.ShowOrientation(initial);
        Check(shadow.localScale==shadowScale&&visual.transform.localScale==baseScale&&visual.transform.localPosition==basePosition,"Restoring pose must also restore shadow/mesh scale");
        Debug.Log("PASS "+samples+" triple-roll samples across 24 orientations/four directions, 270-degree final pose, anticipation, fixed ground shadow, airborne shrink and impact expansion");
        Set(move,"m_cubeOrientation",initial);Set(move,"m_moveSpeed",1f);float started=Time.time;
        yield return move.ProcessTurnMoveRoutine();float elapsed=Time.time-started;
        var expected=initial;for(int i=0;i<3;i++)expected=expected.Rolled(EnemyCubeRollDirection.Right);
        Check(move.CurrentTileIndex==1&&move.VisibleCubeFace==expected.VisibleFace&&move.CubeFaceQuarterTurns==0,"Actual one-tile move must apply three rolls and finish upright");
        Check(elapsed>=.35f&&elapsed<.6f,"Base-speed one-tile move outside expected 0.4-second frame tolerance: "+elapsed);
        Check(shadow.localScale==shadowScale&&shadow.localPosition==shadowPosition&&visual.transform.localScale==baseScale,"Actual move left mesh or shadow deformation");
        Check(enemy.GetComponentsInChildren<MeshRenderer>(true).Length==2,"Pooled cube must have one body and one shared-material ground shadow");enemy.ReturnToPool();
        Check(manager.GetAllEnemiesOnPath().Count==0,"Triple-roll return leaked occupancy");
        Debug.Log("PASS actual base-speed one-tile triple roll in "+elapsed.ToString("F3")+" seconds, exact destination, upright recovery and clean shadow/pool reset");
    }

    public static void RenderPreview(GameObject prefab)
    {
        var instance=UnityEngine.Object.Instantiate(prefab);var visual=instance.GetComponentInChildren<EnemyCubeMeshVisual>();
        var floorTexture=new Texture2D(1,1);floorTexture.SetPixel(0,0,Color.white);floorTexture.Apply();var sprite=Sprite.Create(floorTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
        var floors=new List<GameObject>();for(int i=0;i<2;i++){
            var floor=new GameObject("PreviewPathTile",typeof(SpriteRenderer));floor.layer=8;floor.transform.position=new Vector3(i*1.5f,0,1);floor.transform.localScale=new Vector3(1.46f,1.46f,1);
            var renderer=floor.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingLayerName="MapTile";renderer.color=i==0?new Color(.37f,.43f,.47f):new Color(.42f,.48f,.51f);floors.Add(floor);
        }
        var cameraObject=new GameObject("TripleRollPreviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();cameraObject.transform.position=new Vector3(.75f,.05f,-8);
        camera.orthographic=true;camera.orthographicSize=1.03f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.14f,.18f);camera.cullingMask=1<<8;
        var target=new RenderTexture(640,360,24);camera.targetTexture=target;var pixels=new Texture2D(640,360,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        Directory.CreateDirectory("triple-roll-preview");var before=EnemyCubeOrientation.Identity;var after=before;for(int i=0;i<3;i++)after=after.Rolled(EnemyCubeRollDirection.Right);
        for(int frame=0;frame<60;frame++)
        {
            float time=frame/59f*.9f-.15f;instance.transform.position=Vector3.zero;
            if(time<0)visual.ShowOrientation(before);
            else if(time<.04f)visual.AnimateWindUp(before,EnemyCubeRollDirection.Right,time/.04f);
            else if(time<.33f){visual.AnimateTileRoll(before,EnemyCubeRollDirection.Right,(time-.04f)/.29f,out float travel,out _);instance.transform.position=Vector3.right*(1.5f*travel);}
            else{instance.transform.position=Vector3.right*1.5f;if(time<.4f)visual.AnimateUprightSquash(after,.16f*Mathf.Sin((time-.33f)/.07f*Mathf.PI));else visual.ShowOrientation(after);}
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,640,360),0,0);pixels.Apply();File.WriteAllBytes("triple-roll-preview/"+frame.ToString("D3")+".png",pixels.EncodeToPNG());
        }
        RenderTexture.active=previous;camera.targetTexture=null;instance.SetActive(false);cameraObject.SetActive(false);foreach(var floor in floors){floor.SetActive(false);UnityEngine.Object.Destroy(floor);}
        UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(instance);UnityEngine.Object.Destroy(sprite);UnityEngine.Object.Destroy(floorTexture);
        Debug.Log("PASS actual Unity GPU render of three-roll tile movement with ground shadow, 60 preview frames");
    }
}
