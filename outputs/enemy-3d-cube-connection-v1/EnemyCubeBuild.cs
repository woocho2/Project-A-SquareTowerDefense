using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class EnemyCubeBuild
{
    const string Pending = "EnemyCube3D.Pending";
    const string Folder = "Assets/EnemyCubeGenerated/";
    const string PrefabPath = "Assets/2. Prefab/3. Enemy/Enemy.prefab";
    static readonly string[] Keys = { "SelfHeal", "Defense", "Action", "Mobility", "Locked", "Hourglass" };
    static EnemyCubeBuild() { EditorApplication.playModeStateChanged += State; Application.logMessageReceived += Log; }
    static void Log(string condition,string trace,LogType type)
    {
        // This isolated fixture uses DLLs; unrelated Editor Search/Burst startup diagnostics
        // must not abort the gameplay assertions. All game/runtime errors remain fatal.
        if(trace.Contains("UnityEditor.Search.")||trace.Contains("Unity.Burst."))return;
        if(SessionState.GetBool(Pending,false)&&(type==LogType.Exception||type==LogType.Error))
        {File.AppendAllText("enemy-cube-failure.txt",condition+"\n"+trace);EditorApplication.Exit(1);}
    }
    static void State(PlayModeStateChange state) { if (state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) new GameObject("CubeRuntimeValidation").AddComponent<EnemyCubeRuntimeCheck>(); }
    static void Set(UnityEngine.Object target,string property,UnityEngine.Object value) { var serialized=new SerializedObject(target);serialized.FindProperty(property).objectReferenceValue=value;serialized.ApplyModifiedPropertiesWithoutUndo(); }
    static void ResolveFixtureUI(Type type,string sourceGuid)
    {
        var script=AssetDatabase.LoadAllAssetsAtPath("Assets/Plugins/UnityEngine.UI.dll").OfType<MonoScript>().Single(s=>s.GetClass()==type);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script,out string guid,out long fileId);
        string path="Assets/2. Prefab/HPBar.prefab",text=File.ReadAllText(path);
        File.WriteAllText(path,text.Replace("{fileID: 11500000, guid: "+sourceGuid+", type: 3}","{fileID: "+fileId+", guid: "+guid+", type: 3}"));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
    }
    public static void BuildAndTest()
    {
        GameObject target=null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SessionState.SetBool(Pending,false);
            ResolveFixtureUI(typeof(UnityEngine.UI.Image),"fe87c0e1cc204ed48ad3b37840f39efc");
            ResolveFixtureUI(typeof(UnityEngine.UI.CanvasScaler),"0cd44c1031e13a943bb63640046fad76");
            ResolveFixtureUI(typeof(UnityEngine.UI.GraphicRaycaster),"dc42784cf147c0c48a680349fa168899");
            var mesh=new Mesh { name="Enemy_StateCube_6Faces" };
            var vertices=new Vector3[24];var normals=new Vector3[24];var uv=new Vector2[24];mesh.subMeshCount=6;
            for(int f=0;f<6;f++)
            {
                var face=(EnemyCubeFace)f;Vector3 normal=EnemyCubeMeshVisual.FaceNormal(face),up=EnemyCubeMeshVisual.FaceImageUp(face),right=Vector3.Cross(normal,up);
                int j=f*4;vertices[j]=normal*.5f-right*.5f-up*.5f;vertices[j+1]=normal*.5f+right*.5f-up*.5f;vertices[j+2]=normal*.5f+right*.5f+up*.5f;vertices[j+3]=normal*.5f-right*.5f+up*.5f;
                for(int i=0;i<4;i++)normals[j+i]=normal;
                uv[j]=Vector2.zero;uv[j+1]=Vector2.right;uv[j+2]=Vector2.one;uv[j+3]=Vector2.up;
            }
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;
            for(int f=0;f<6;f++){int j=f*4;mesh.SetTriangles(new[]{j,j+2,j+1,j,j+3,j+2},f);}
            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"Enemy_StateCube.asset");
            Shader shader=Shader.Find("SquareTowerDefense/EnemyCubeUnlit");if(shader==null)throw new Exception("Cube shader missing");
            var materials=new Material[6];
            for(int f=0;f<6;f++)
            {
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CubeFaces/Enemy_Valkyrie_Face_"+Keys[f]+"_v1.png");
                if(texture==null)throw new Exception(Keys[f]+": texture missing");
                materials[f]=new Material(shader){name="EnemyCube_"+Keys[f],mainTexture=texture,color=Color.white};AssetDatabase.CreateAsset(materials[f],Folder+"EnemyCube_"+Keys[f]+".mat");
            }
            target=PrefabUtility.LoadPrefabContents(PrefabPath);
            for(int i=target.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(target.transform.GetChild(i).gameObject);
            // Remove dependents before their RequireComponent dependencies on rebuild.
            foreach(var component in target.GetComponents<Component>().Reverse())if(!(component is Transform))UnityEngine.Object.DestroyImmediate(component);
            target.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);target.transform.localScale=Vector3.one;target.layer=8;
            var normalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/3. Enemy/1. Enemy_normal.prefab");
            var box=target.AddComponent<BoxCollider2D>();EditorUtility.CopySerialized(normalPrefab.GetComponent<BoxCollider2D>(),box);
            var body=target.AddComponent<Rigidbody2D>();EditorUtility.CopySerialized(normalPrefab.GetComponent<Rigidbody2D>(),body);body.bodyType=RigidbodyType2D.Kinematic;body.constraints=RigidbodyConstraints2D.FreezeRotation;
            var movement=target.AddComponent<EnemyMoveController>();EditorUtility.CopySerialized(normalPrefab.GetComponent<EnemyMoveController>(),movement);
            var health=target.AddComponent<EnemyHealthController>();EditorUtility.CopySerialized(normalPrefab.GetComponent<EnemyHealthController>(),health);
            target.AddComponent<EnemyDebuffController>();var ui=target.AddComponent<EnemyUIController>();
            var visual=new GameObject("CubeVisual3D");visual.layer=8;visual.transform.SetParent(target.transform,false);float side=256f/192f;
            visual.transform.localScale=Vector3.one*side;visual.transform.localPosition=Vector3.back*side*.5f;
            visual.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortingLayerName="Enemy";renderer.sortingOrder=0;
            var cube=visual.AddComponent<EnemyCubeMeshVisual>();Set(movement,"m_cubeVisual",null);Set(movement,"m_cubeMeshVisual",cube);
            var bar=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/HPBar.prefab"),target.transform) as GameObject;bar.name="HPBar";
            var oldBar=normalPrefab.GetComponentInChildren<HPBarSlider>(true).GetComponentInParent<Canvas>(true).transform;
            bar.transform.localPosition=oldBar.localPosition;bar.transform.localRotation=oldBar.localRotation;bar.transform.localScale=oldBar.localScale;
            // Keep the UI in front of the cube; only CubeVisual3D rolls.
            var pos=bar.transform.localPosition;pos.z=-1.5f;bar.transform.localPosition=pos;
            Set(ui,"m_HPBarSlider",bar.GetComponentInChildren<HPBarSlider>(true));Set(health,"m_uiController",ui);Set(health,"m_hpBar",bar);bar.SetActive(false);
            if(PrefabUtility.SaveAsPrefabAsset(target,PrefabPath)==null)throw new Exception("Enemy prefab save failed");PrefabUtility.UnloadPrefabContents(target);target=null;
            AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception error){Debug.LogException(error);if(target!=null)PrefabUtility.UnloadPrefabContents(target);EditorApplication.Exit(1);}
    }
}
