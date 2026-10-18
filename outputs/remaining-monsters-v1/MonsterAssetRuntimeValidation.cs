using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class MonsterAssetRuntimeValidation : MonoBehaviour
{
    [Serializable] public class Result { public string unityVersion,graphicsDevice,scope;public bool importPassed,playModePassed; public int appearances,textureBindings,paletteBindings,randomSpawns,rollSamples,spawnSamples,renderedFaces;public List<string> checkedAppearances=new List<string>();public List<string> runtimeErrors=new List<string>(); }
    Result result=new Result();
    void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    void CaptureLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)result.runtimeErrors.Add(message+"\n"+trace);}
    IEnumerator Start()
    {
        Application.logMessageReceived+=CaptureLog;
        yield return null;
        try { Validate(); } catch(Exception ex) { MonsterAssetValidation.Fail(ex); }
    }
    void Validate()
    {
        result.unityVersion=Application.unityVersion;result.graphicsDevice=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
        result.scope="Isolated Unity play-mode fixture using exact production cube visual code, mesh, shader, shared materials/palette and 27 appearances. Stage scene, combat and wave spawning are not part of this fixture.";
        Require(Application.isPlaying,"Validation did not enter play mode");
        Require(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null,"Rendering requires a real graphics device");
        result.importPassed=true;
        var paths=AssetDatabase.FindAssets("t:EnemyCubeAppearance",new[]{"Assets/4. Asset/4. Enemy"}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
        var visuals=SharedBoardVisuals.Default;Require(visuals!=null&&visuals.Background!=null&&visuals.Decoration!=null,"Shared palette did not load");
        string[] keys={"Heal","Defense","Action","Mobility","Locked","Hourglass"};
        var materials=keys.Select(k=>AssetDatabase.LoadAssetAtPath<Material>("Assets/4. Asset/0. Common/Enemy_State_"+k+".mat")).ToArray();
        Require(materials.All(m=>m!=null),"Missing state material");
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/4. Asset/4. Enemy/Enemy_StateCube.asset");Require(mesh!=null&&mesh.subMeshCount==6,"Cube mesh does not have six submeshes");
        var root=new GameObject("CubeValidationRoot");root.SetActive(false);
        var cubeObject=new GameObject("CubeVisual3D");cubeObject.transform.SetParent(root.transform,false);cubeObject.transform.localScale=Vector3.one*(256f/192f);
        cubeObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=cubeObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;
        var cube=cubeObject.AddComponent<EnemyCubeMeshVisual>();Set(cube,"m_sharedVisuals",visuals);Set(cube,"m_faceAssetPool",materials);Set(cube,"m_shadowMaterial",AssetDatabase.LoadAssetAtPath<Material>("Assets/4. Asset/4. Enemy/EnemyCube_Shadow.mat"));
        root.SetActive(true);Require(cube.HasAllFaces,"Cube Awake rejected mesh/material setup");
        var cameraObject=new GameObject("ValidationCamera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-6);camera.orthographic=true;camera.orthographicSize=.73f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.24f,1);camera.nearClipPlane=.1f;camera.farClipPlane=20;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();camera.targetTexture=target;
        var readback=new Texture2D(256,256,TextureFormat.RGB24,false);Directory.CreateDirectory("../unity-rendered");
        foreach(string path in paths) {
            var appearance=AssetDatabase.LoadAssetAtPath<EnemyCubeAppearance>(path);cube.SetAppearance(appearance);result.appearances++;result.checkedAppearances.Add(path);
            for(int trial=0;trial<12;trial++) {
                var orientation=cube.RandomizeForSpawn();result.randomSpawns++;
                Require(orientation.CounterClockwiseQuarterTurns==0,"Spawn portrait is not upright");
                Require(Vector3.Dot(cube.transform.localRotation*EnemyCubeMeshVisual.FaceImageUp(orientation.VisibleFace),Vector3.up)>.999f,"Spawn rotation is not upright");
                CheckBindings(cube,renderer,appearance,visuals);
            }
            foreach(EnemyCubeRollDirection direction in Enum.GetValues(typeof(EnemyCubeRollDirection))) {
                float previous=-.001f;
                for(int sample=0;sample<=24;sample++) {
                    cube.AnimateTileRoll(EnemyCubeOrientation.Identity,direction,sample/24f,out float travel,out var committed);result.rollSamples++;
                    Require(!float.IsNaN(travel)&&travel>=previous-.001f&&travel<=1.001f,"Non-monotonic roll travel");previous=travel;
                    Require(float.IsFinite(cube.transform.localPosition.sqrMagnitude)&&float.IsFinite(cube.transform.localScale.sqrMagnitude),"Invalid roll transform");
                    if(sample==24){var expected=EnemyCubeOrientation.Identity;for(int n=0;n<3;n++)expected=expected.Rolled(direction);Require(committed.Equals(expected)&&Mathf.Abs(travel-1)<.001f,"Roll did not land after three turns");}
                }
            }
            for(int sample=0;sample<=20;sample++){cube.AnimateSpawn(EnemyCubeOrientation.Identity,cube.SpawnDuration*sample/20f);result.spawnSamples++;Require(float.IsFinite(cube.transform.localPosition.sqrMagnitude),"Invalid spawn transform");}
            cube.ShowOrientation(EnemyCubeOrientation.Identity);Require(cube.transform.localPosition.sqrMagnitude<.00001f,"Spawn did not settle at base");
            for(int state=0;state<6;state++) {
                renderer.sharedMaterials=Enumerable.Repeat(materials[state],6).ToArray();Set(cube,"m_faceAssetIndices",Enumerable.Repeat(state,6).ToArray());cube.RefreshSharedVisuals();cube.ShowOrientation(EnemyCubeOrientation.Identity);
                CheckBindings(cube,renderer,appearance,visuals);
                camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,256,256),0,0);readback.Apply();RenderTexture.active=null;
                var pixels=readback.GetPixels32();Require(pixels.Count(p=>p.r>180&&p.g<100&&p.b>180)<pixels.Length/4,"Pink/missing shader rendering");
                Require(pixels.Select(p=>(p.r<<16)|(p.g<<8)|p.b).Distinct().Take(128).Count()>=128,"Empty/flat rendering");
                File.WriteAllBytes("../unity-rendered/"+appearance.name+"_"+state+".png",readback.EncodeToPNG());result.renderedFaces++;
            }
            renderer.sharedMaterials=materials;Set(cube,"m_faceAssetIndices",new[]{0,1,2,3,4,5});cube.RefreshSharedVisuals();
        }
        Require(result.appearances==27&&result.renderedFaces==162,"Incomplete appearance coverage");
        Require(result.runtimeErrors.Count==0,"Runtime error: "+string.Join("; ",result.runtimeErrors));
        result.playModePassed=true;camera.targetTexture=null;target.Release();Destroy(readback);Destroy(target);Destroy(root);Destroy(cameraObject);
        MonsterAssetValidation.Complete(JsonUtility.ToJson(result,true));
    }
    void CheckBindings(EnemyCubeMeshVisual cube,MeshRenderer renderer,EnemyCubeAppearance appearance,SharedBoardVisuals visuals)
    {
        for(int face=0;face<6;face++) {
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,face);
            Require(block.GetTexture("_MainTex")==appearance.CharacterTexture&&block.GetTexture("_EquipmentMask")==appearance.EquipmentMask,"Appearance did not bind to face "+face);result.textureBindings++;
            var palette=visuals.GetEnemy(appearance,(EnemyCubeFaceAsset)cube.GetFaceAssetIndex(face));Require(palette!=null,"Missing fallback palette");
            Require(block.GetColor("_EquipmentColor")==palette.equipment&&block.GetColor("_DecorationColor")==palette.decoration&&block.GetColor("_BackgroundColor")==palette.background,"Incorrect face palette");result.paletteBindings++;
        }
    }
}
