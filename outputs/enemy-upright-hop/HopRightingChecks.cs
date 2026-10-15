using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class HopRightingChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static IEnumerator Righting(EnemyMoveController movement)=>(IEnumerator)typeof(EnemyMoveController).GetMethod("CorrectCubeUprightRoutine",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(movement,null);
    static IEnumerator Done(IEnumerator routine,Action complete){yield return routine;complete();}
    public static IEnumerator Verify(EnemyObjectPool2D pool,TileManager manager)
    {
        int hops=0;
        for(int face=0;face<6;face++)for(int turns=0;turns<4;turns++)
        {
            var enemy=pool.Spawn(Vector3.zero,1,1);Check(enemy!=null,"Hop test spawn failed");
            var move=enemy.Movement;var visual=enemy.GetComponentInChildren<EnemyCubeMeshVisual>();var pose=EnemyCubeOrientation.FromFace((EnemyCubeFace)face,turns);
            Set(move,"m_cubeOrientation",pose);Set(move,"m_isMoving",true);visual.ShowOrientation(pose);
            Vector3 root=enemy.transform.position,basePosition=visual.transform.localPosition,baseScale=visual.transform.localScale;
            var canvas=enemy.GetComponentInChildren<Canvas>(true);Vector3 uiPosition=canvas.transform.localPosition;Quaternion uiRotation=canvas.transform.localRotation;
            int asset=move.VisibleCubeAssetIndex,previousTurn=turns,commits=0;bool finished=false,sawJump=false,sawSquash=false;
            move.StartCoroutine(Done(Righting(move),()=>finished=true));float deadline=Time.realtimeSinceStartup+3;
            while(!finished)
            {
                Check(Time.realtimeSinceStartup<deadline,"Righting jump failed to finish");
                Check(move.IsMoving,"Movement ended before righting animation finished");
                Check(enemy.transform.position==root&&enemy.transform.rotation==Quaternion.identity,"Righting changed combat root position/rotation");
                Check(move.VisibleCubeFaceIndex==face&&move.VisibleCubeAssetIndex==asset,"Righting changed visible face or assigned asset");
                Check(canvas.transform.localPosition==uiPosition&&canvas.transform.localRotation==uiRotation,"Righting moved HP UI");
                sawJump|=visual.transform.localPosition.y>basePosition.y+.01f&&visual.transform.localPosition.z<basePosition.z-.01f;
                sawSquash|=Vector3.Distance(visual.transform.localScale,baseScale)>.01f;
                if(move.CubeFaceQuarterTurns!=previousTurn)
                {
                    int delta=(move.CubeFaceQuarterTurns-previousTurn+4)%4;Check(delta==1||delta==3,"Righting skipped a 90-degree landing");commits++;previousTurn=move.CubeFaceQuarterTurns;
                }
                yield return null;
            }
            if(previousTurn!=move.CubeFaceQuarterTurns)commits++;
            int expectedHops=turns==0?0:turns==2?2:1;Check(commits==expectedHops,"Wrong number of 90-degree hops");hops+=commits;
            Check(move.CubeFaceQuarterTurns==0,"Righting did not finish upright");Check(turns==0||sawJump&&sawSquash,"Missing visible jump or impact squash");
            Check(visual.transform.localScale==baseScale&&visual.transform.localPosition==basePosition,"Hop left residual scale/position");
            Check(Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(EnemyCubeOrientation.FromFace((EnemyCubeFace)face,0)))<.1f,"Physical mesh did not finish upright");enemy.ReturnToPool();
        }
        Check(hops==24,"Expected 24 total hops across all 24 initial poses");
        Debug.Log("PASS all 24 arrival orientations: 0/1/2 shortest 90-degree hops, visible lift and squash, same face/asset, stable root/HP UI and exact pose/scale restoration");
        var interrupted=pool.Spawn(Vector3.zero,1,1);var movement=interrupted.Movement;var cube=interrupted.GetComponentInChildren<EnemyCubeMeshVisual>();
        var upsideDown=EnemyCubeOrientation.FromFace(EnemyCubeFace.Front,2);Set(movement,"m_cubeOrientation",upsideDown);Set(movement,"m_isMoving",true);cube.ShowOrientation(upsideDown);
        var rest=cube.transform.localPosition;var scale=cube.transform.localScale;movement.StartCoroutine(Righting(movement));float cutoff=Time.realtimeSinceStartup+2;
        while(cube.transform.localPosition.y<=rest.y+.01f){Check(Time.realtimeSinceStartup<cutoff,"Interrupted test never became airborne");yield return null;}
        interrupted.ApplyDamage(10000);Check(!interrupted.gameObject.activeSelf&&manager.GetAllEnemiesOnPath().Count==0,"Mid-hop death did not return to pool/release occupancy");
        Check(cube.transform.localPosition==rest&&cube.transform.localScale==scale,"Mid-hop death left deformed mesh");
        var reused=pool.Spawn(Vector3.zero,1,1);Check(reused==interrupted&&reused.Movement.CubeFaceQuarterTurns==0&&reused.CurrentHP==100,"Mid-hop pool reuse did not reset upright pose/health");reused.ReturnToPool();
        Debug.Log("PASS mid-hop damage/death, coroutine cancellation, occupancy release and clean upright pool reuse");
    }

    public static void RenderPreview(GameObject prefab)
    {
        var instance=UnityEngine.Object.Instantiate(prefab);instance.transform.position=Vector3.zero;
        var visual=instance.GetComponentInChildren<EnemyCubeMeshVisual>();
        var cameraObject=new GameObject("HopPreviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();cameraObject.transform.position=new Vector3(0,.04f,-8);
        camera.orthographic=true;camera.orthographicSize=.55f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.15f,.19f);camera.cullingMask=1<<8;
        var target=new RenderTexture(384,384,24);camera.targetTexture=target;var pixels=new Texture2D(384,384,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        Directory.CreateDirectory("hop-preview");float prep=visual.UprightImpactDuration*.6f,flight=visual.UprightHopDuration,impact=visual.UprightImpactDuration,step=prep+flight+impact;
        for(int frame=0;frame<48;frame++)
        {
            // Include a pause before and after the two-hop upside-down correction.
            float time=(frame/47f)*(2*step+.4f)-.2f;
            if(time<0)visual.ShowOrientation(EnemyCubeOrientation.FromFace(EnemyCubeFace.Front,2));
            else if(time>=2*step)visual.ShowOrientation(EnemyCubeOrientation.FromFace(EnemyCubeFace.Front,0));
            else
            {
                int turn=(int)(time/step);float phase=time-turn*step;var before=EnemyCubeOrientation.FromFace(EnemyCubeFace.Front,2-turn);
                if(phase<prep)visual.AnimateUprightSquash(before,.1f*phase/prep);
                else if(phase<prep+flight)visual.AnimateUprightHop(before,-1,(phase-prep)/flight);
                else visual.AnimateUprightSquash(EnemyCubeOrientation.FromFace(EnemyCubeFace.Front,1-turn),.16f*Mathf.Sin((phase-prep-flight)/impact*Mathf.PI));
            }
            camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,384,384),0,0);pixels.Apply();File.WriteAllBytes("hop-preview/"+frame.ToString("D3")+".png",pixels.EncodeToPNG());
        }
        RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(pixels);UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(instance);
        Debug.Log("PASS actual Unity GPU render of two-hop arrival correction, 48 preview frames");
    }
}
