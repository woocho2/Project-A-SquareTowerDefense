using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomCubeChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static EnemyCubeMeshVisual Visual(EnemyHealthController e)=>e.GetComponentInChildren<EnemyCubeMeshVisual>();
    static int[] Assets(EnemyCubeMeshVisual visual){var result=new int[6];for(int i=0;i<6;i++)result[i]=visual.GetFaceAssetIndex(i);return result;}
    static void SameAssets(EnemyCubeMeshVisual visual,int[] expected){for(int i=0;i<6;i++)Check(visual.GetFaceAssetIndex(i)==expected[i],"Rolling changed a face's asset assignment");}
    public static void Verify(GameObject prefab,EnemyObjectPool2D pool,TileManager manager)
    {
        var originalRandom=UnityEngine.Random.state;UnityEngine.Random.InitState(160026);
        var originalMaterials=prefab.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        var instance=UnityEngine.Object.Instantiate(prefab);var visual=instance.GetComponentInChildren<EnemyCubeMeshVisual>();
        var orientations=new HashSet<EnemyCubeOrientation>();int transitions=0;
        for(int face=0;face<6;face++)for(int turn=0;turn<4;turn++){
            var pose=EnemyCubeOrientation.FromFace((EnemyCubeFace)face,turn);
            Check((int)pose.VisibleFace==face&&pose.CounterClockwiseQuarterTurns==turn,"Face/spin factory returned wrong orientation");Check(orientations.Add(pose),"Duplicate of the 24 poses");
            foreach(EnemyCubeRollDirection direction in Enum.GetValues(typeof(EnemyCubeRollDirection))){visual.ShowOrientation(pose);visual.AnimateRoll(pose,direction,1);Check(Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(pose.Rolled(direction)))<.1f,"Random initial pose broke physical rolling");transitions++;}
        }
        Check(orientations.Count==24&&transitions==96,"Missing 24 poses/96 transitions");UnityEngine.Object.Destroy(instance);
        Debug.Log("PASS indexed face/spin factory covers 24 distinct poses and all 96 real-mesh roll transitions");
        var appearances=new int[6,6];var initialFaces=new int[6];var initialPoses=new HashSet<string>();bool duplicates=false,redrawn=false;int[] previous=null;EnemyHealthController reused=null;
        for(int sample=0;sample<480;sample++){
            var e=pool.Spawn(Vector3.zero,1,1);Check(e!=null,"Random sample failed to spawn");if(reused!=null)Check(e==reused,"Sample must exercise actual pooled reuse");reused=e;
            visual=Visual(e);var movement=e.Movement;var materials=visual.GetComponent<MeshRenderer>().sharedMaterials;var indices=Assets(visual);var distinct=new HashSet<int>();
            for(int face=0;face<6;face++){
                int asset=indices[face];Check(asset>=0&&asset<6,"Invalid asset index");Check(materials[face]==originalMaterials[asset],"Face index does not match rendered shared material");appearances[face,asset]++;distinct.Add(asset);
                if(previous!=null&&indices[face]!=previous[face])redrawn=true;
            }
            duplicates|=distinct.Count<6;initialFaces[movement.VisibleCubeFaceIndex]++;initialPoses.Add(movement.VisibleCubeFaceIndex+":"+movement.CubeFaceQuarterTurns);
            Check(movement.CubeFaceQuarterTurns==0,"Spawn must show the initial artwork upright");
            Check(Vector3.Dot(visual.transform.TransformDirection(EnemyCubeMeshVisual.FaceImageUp(movement.VisibleCubeFace)),Vector3.up)>.999f,"Spawned artwork head must point up on the actual mesh");
            Check(movement.VisibleCubeAssetIndex==indices[movement.VisibleCubeFaceIndex],"Visible asset lookup returned wrong index");
            var expected=EnemyCubeOrientation.FromFace(movement.VisibleCubeFace,movement.CubeFaceQuarterTurns);Check(Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(expected))<.1f,"Visible logical face and physical spawn pose disagree");
            previous=indices;e.ReturnToPool();
        }
        for(int face=0;face<6;face++){Check(initialFaces[face]>40&&initialFaces[face]<120,"Spawn face distribution unexpectedly biased");for(int asset=0;asset<6;asset++)Check(appearances[face,asset]>40&&appearances[face,asset]<120,"Per-face asset draw unexpectedly biased or incomplete");}
        Check(duplicates&&redrawn&&initialPoses.Count==6,"Expected duplicate assets, redraws on reuse and all six upright starting faces");
        var after=prefab.GetComponentInChildren<MeshRenderer>().sharedMaterials;for(int i=0;i<6;i++)Check(after[i]==originalMaterials[i],"Randomization mutated the prefab");
        Check(manager.GetAllEnemiesOnPath().Count==0,"Sample pool returns leaked path occupancy");UnityEngine.Random.state=originalRandom;
        Debug.Log("PASS 480 real pooled spawns: all six random initial faces are physically upright, independent duplicate-allowed face assets, render/index agreement and unchanged shared prefab materials");
    }
    public static IEnumerator VerifyRolls(EnemyObjectPool2D pool)
    {
        var e=pool.Spawn(Vector3.zero,1,1);Check(e!=null,"Movement randomization setup failed");
        typeof(EnemyMoveController).GetField("m_moveSpeed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(e.Movement,30f);
        var visual=Visual(e);int[] assets=Assets(visual);var expected=EnemyCubeOrientation.FromFace(e.Movement.VisibleCubeFace,e.Movement.CubeFaceQuarterTurns);
        bool naturallyRotated=false;
        foreach(var direction in new[]{EnemyCubeRollDirection.Right,EnemyCubeRollDirection.Up,EnemyCubeRollDirection.Left,EnemyCubeRollDirection.Down}){
            yield return e.Movement.ProcessTurnMoveRoutine();for(int roll=0;roll<EnemyCubeMeshVisual.RollsPerTile;roll++)expected=expected.Rolled(direction);SameAssets(visual,assets);
            naturallyRotated|=expected.CounterClockwiseQuarterTurns!=0;
            expected=EnemyCubeOrientation.FromFace(expected.VisibleFace,0);
            Check(e.Movement.VisibleCubeFace==expected.VisibleFace&&e.Movement.CubeFaceQuarterTurns==expected.CounterClockwiseQuarterTurns,"Actual path corner discarded random starting orientation");
            Check(Quaternion.Angle(visual.transform.localRotation,EnemyCubeMeshVisual.RotationFor(expected))<.1f,"Actual mesh roll from random pose is wrong");
        }
        e.Movement.ReverseNextDiceMove();yield return e.Movement.ProcessTurnMoveRoutine();for(int roll=0;roll<EnemyCubeMeshVisual.RollsPerTile;roll++)expected=expected.Rolled(EnemyCubeRollDirection.Up);expected=EnemyCubeOrientation.FromFace(expected.VisibleFace,0);SameAssets(visual,assets);
        Check(e.Movement.VisibleCubeFace==expected.VisibleFace&&e.Movement.CubeFaceQuarterTurns==expected.CounterClockwiseQuarterTurns,"Reverse roll discarded random orientation");
        Check(naturallyRotated,"Movement must naturally rotate artwork after the upright-only spawn");e.ReturnToPool();
        Debug.Log("PASS actual path corners and reverse rolls preserve random initial orientation and indexed face assets");
    }
}
