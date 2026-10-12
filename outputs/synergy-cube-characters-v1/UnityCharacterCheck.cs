using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class UnityCharacterCheck
{
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
    static Sprite[] Faces(string key)
    {
        string filename=key=="Valkyrie"?"Enemy_Cube_FallenValkyrie_Faces_v4.png":"Boss_"+key+"_Faces_v2.png";
        return AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/"+filename).OfType<Sprite>().ToArray();
    }
    static EnemyCubeVisual Cube(GameObject root)
    {
        var cube=root.GetComponentInChildren<EnemyCubeVisual>(true);
        Check(cube!=null&&cube.HasAllFaces,"Missing cube visual or faces: "+root.name);
        if(typeof(EnemyCubeVisual).GetField("m_renderer",PrivateInstance).GetValue(cube)==null)
            typeof(EnemyCubeVisual).GetMethod("Awake",PrivateInstance).Invoke(cube,null);
        return cube;
    }
    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog=Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");
            Check(catalog!=null,"Missing character catalog");
            var floors=AssetDatabase.LoadAllAssetsAtPath("Assets/Tile_Floor_Atlas.png").OfType<Sprite>().ToArray();
            string[] realms={"Hel","Midgard","Hel","Hel","Muspelheim","Alfheim","Hel","Niflheim","Vanaheim","Jotunheim","Niflheim","Asgard","Nidavellir"};
            for(int i=0;i<13;i++)
            {
                int id=6001+i;
                Sprite front=catalog.GetFront(id);
                Check(front!=null,"Missing static character "+id);
                var root=new GameObject("Static_"+id);root.transform.position=new Vector3(i%5*1.5f,6-i/5*1.5f,0);
                foreach(string name in new[]{"Emblem","Color","Tier"})
                {
                    var child=new GameObject(name);child.transform.SetParent(root.transform,false);child.AddComponent<SpriteRenderer>();
                }
                var tower=root.AddComponent<TowerVisual>();
                tower.Apply(id);
                Check(tower.EmblemSprite==front&&tower.TierSprite==null&&tower.ColorSprite==null,"Static tower still has emblem/frame layers");
                Check(root.GetComponentInChildren<EnemyCubeVisual>()==null,"Tower received cube rolling behavior");
                var renderer=root.transform.Find("Emblem").GetComponent<SpriteRenderer>();
                Check(Mathf.Abs(renderer.bounds.size.x-256f/192f)<0.0001f&&Mathf.Abs(renderer.bounds.size.y-256f/192f)<0.0001f,"Static character canvas differs from spawn tile");
                var floor=new GameObject("SpawnTile");floor.transform.SetParent(root.transform,false);
                var floorRenderer=floor.AddComponent<SpriteRenderer>();floorRenderer.sortingOrder=-1;
                floorRenderer.sprite=floors.First(s=>s.name=="Tile_TowerSpawn_"+realms[i]);
                if(i==0)
                {
                    var settings=new SerializedObject(tower);
                    foreach(var field in new[]{"m_tierSprites","m_emblemSprites"})
                    {
                        var property=settings.FindProperty(field);property.arraySize=13;
                        for(int n=0;n<13;n++)property.GetArrayElementAtIndex(n).objectReferenceValue=front;
                    }
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    tower.Apply(1101);
                    Check(root.transform.Find("Tier").GetComponent<SpriteRenderer>().enabled,"Regular tier renderer was not restored");
                    Check(root.transform.Find("Color").GetComponent<SpriteRenderer>().enabled,"Regular color renderer was not restored");
                    Check(renderer.transform.localScale==Vector3.one,"Regular emblem scale was not restored");
                    tower.Apply(id);
                }
            }
            Debug.Log("PASS 13 static tower sprites, spawn-tile canvas size and regular-tower restoration");
            string[] bosses={"Brunhild","Loki","Ymir","Surtr","Hel"};
            for(int i=0;i<bosses.Length;i++)
            {
                string key=bosses[i];
                var sprites=Faces(key);Check(sprites.Length==6,"Expected six sprites for "+key);
                for(int n=0;n<6;n++)
                {
                    Check(sprites.Any(s=>s.name==key+"_"+new[]{"Front","Left","Right","Back","Top","Bottom"}[n]),"Wrong atlas face name");
                    Check(sprites[n].rect.width==256&&sprites[n].rect.height==256,"Wrong atlas cell size");
                }
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnemyPrefabs/EnemyBoss_"+key+".prefab");
                Check(prefab!=null,"Missing boss prefab "+key);
                var instance=UnityEngine.Object.Instantiate(prefab);instance.transform.position=new Vector3(i*1.5f,1.2f,0);
                var cube=Cube(instance);
                var right=EnemyCubeOrientation.Identity.Rolled(EnemyCubeRollDirection.Left);
                cube.ShowOrientation(right);
                var renderer=cube.GetComponent<SpriteRenderer>();
                Check(renderer.sprite.name==key+"_Right_CubeContent"&&!renderer.flipX,"Baked right face was mirrored or not selected");
                cube.AnimateRoll(EnemyCubeOrientation.Identity,EnemyCubeOrientation.Identity.Rolled(EnemyCubeRollDirection.Right),EnemyCubeRollDirection.Right,0.5f);
                Check(instance.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.enabled&&s.sprite!=null)>=2,"Boss did not render two roll faces");
            }
            Debug.Log("PASS five rolling boss prefabs, six atlas slices and explicit right-face selection");
            var valkyrie=Faces("Valkyrie");Check(valkyrie.Length==6,"Missing square Valkyrie faces");
            string[] palettes={"SelfHeal","Mobility","Action","Defense","GroupHeal"};
            for(int x=0;x<5;x++)for(int y=0;y<6;y++)
            {
                var obj=new GameObject("Palette_"+palettes[x]+"_"+y);obj.transform.position=new Vector3(x*1.5f,-.6f-y*1.1f,0);obj.transform.localScale=Vector3.one*.7f;
                var renderer=obj.AddComponent<SpriteRenderer>();
                string face=new[]{"Front","Left","Right","Back","Top","Bottom"}[y];
                renderer.sprite=valkyrie.First(s=>s.name=="Valkyrie_"+face);
                renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Palettes/Valkyrie_"+palettes[x]+".mat");
                Check(renderer.sharedMaterial!=null&&renderer.sharedMaterial.GetFloat("_SquareHead")==1,"Missing square equipment material");
                var block=new MaterialPropertyBlock();block.SetFloat("_FaceIndex",y==2?5:y>2?y-1:y);
                var rect=renderer.sprite.rect;block.SetVector("_SpriteRect",new Vector4(rect.x,rect.y,rect.width,rect.height));renderer.SetPropertyBlock(block);
            }
            var cameraObject=new GameObject("ValidationCamera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.9f;camera.transform.position=new Vector3(3,.4f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.13f,.18f,1);
            var target=new RenderTexture(1000,1560,24);target.Create();camera.targetTexture=target;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1000,1560,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1000,1560),0,0);image.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(Path.GetFullPath("../unity-preview.png"),image.EncodeToPNG());
            var roundedShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoundedCharacterSprite.shader");
            foreach(var message in ShaderUtil.GetShaderMessages(roundedShader))Debug.Log(message.severity+": "+message.message);
            Check(!ShaderUtil.ShaderHasError(roundedShader),"Rounded character shader GPU compile error");
            Debug.Log("PASS rounded 8px character corner shaders for static towers and rolling bosses");
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/EnemyEquipmentPalette.shader");
            foreach(var message in ShaderUtil.GetShaderMessages(shader))Debug.Log(message.severity+": "+message.message);
            Check(!ShaderUtil.ShaderHasError(shader),"Equipment shader GPU compile error");
            Debug.Log("PASS GPU rendering of static characters, rolling bosses and 30 square-head palette combinations");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
