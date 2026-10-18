using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UICommonKitCheck
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static UITilePanel Create(Transform parent,string name,Vector2 size,UITileFrameStyle style,float width)
    {
        var root=new GameObject(name,typeof(RectTransform),typeof(UITilePanel));root.layer=5;
        root.transform.SetParent(parent,false);root.GetComponent<RectTransform>().sizeDelta=size;
        var bg=UITilePanelEditor.CreateSurface(root.transform,"Img_A_BG",true);
        var frame=UITilePanelEditor.CreateSurface(root.transform,"Img_Frame",false);
        var panel=root.GetComponent<UITilePanel>();panel.SetSurfaces(bg,frame);panel.SetStyle(style,width);
        return panel;
    }
    static Image Bg(UITilePanel p)=>p.transform.Find("Img_A_BG").GetComponent<Image>();
    static UITileSurface Edge(UITilePanel p)=>p.transform.Find("Img_Frame").GetComponent<UITileSurface>();
    static void WritePrefab(string name,Vector2 size,UITileFrameStyle style,float thickness,bool button)
    {
        var panel=Create(null,name,size,style,thickness);
        if(button){var b=panel.gameObject.AddComponent<Button>();b.targetGraphic=Bg(panel);}
        var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(panel.transform,false);
        var rect=content.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;float inset=button?8:24;rect.offsetMin=new Vector2(inset,inset);rect.offsetMax=new Vector2(-inset,-inset);
        PrefabUtility.SaveAsPrefabAsset(panel.gameObject,"Assets/2. Prefab/UICommon/"+name+".prefab");
        UnityEngine.Object.DestroyImmediate(panel.gameObject);
    }
    public static void Run()
    {
        try
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/4. Asset/5. UI/Shared_UI_Atlas.png").OfType<Sprite>().ToArray();
            Check(sprites.Length==63,"Expected 3 canonical sprites and 60 aliases");
            Check(sprites.Select(s=>s.rect).Distinct().Count()==3,"Atlas should contain only three unique regions");
            var texture=sprites[0].texture;Check(texture.width==1024&&texture.height==256,"Wrong atlas dimensions");
            for(int layer=0;layer<4;layer++)for(int i=1;i<=15;i++)
            {
                long expected=2000+layer*100+i;
                Check(sprites.Any(s=>AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string guid,out long id)&&id==expected),"Lost old sprite fileID "+expected);
            }
            var skin=UITileSkin.Default;Check(skin!=null&&skin.background!=null&&skin.decoratedFrame!=null&&skin.compactFrame!=null,"Skin references missing");
            Directory.CreateDirectory("Assets/2. Prefab/UICommon");
            WritePrefab("UITilePanel",new Vector2(320,180),UITileFrameStyle.Auto,2,false);
            WritePrefab("UITileButton",new Vector2(180,56),UITileFrameStyle.Compact,1.5f,true);
            WritePrefab("UITileCard",new Vector2(256,384),UITileFrameStyle.Decorated,2.5f,false);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();

            var canvasObject=new GameObject("ValidationCanvas",typeof(RectTransform),typeof(Canvas));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.referencePixelsPerUnit=100;
            var theme=canvasObject.AddComponent<UIStageThemeController>();
            var sizes=new[]{new Vector2(320,180),new Vector2(180,56),new Vector2(256,384),new Vector2(600,40),new Vector2(40,300),new Vector2(24,24),new Vector2(800,600),new Vector2(160,160)};
            foreach(var size in sizes)
            {
                var panel=Create(canvas.transform,"ResizeCheck",size,UITileFrameStyle.Auto,2);
                Canvas.ForceUpdateCanvases();
                Check(Edge(panel).ResolvedStyle==(Mathf.Min(size.x,size.y)<96?UITileFrameStyle.Compact:UITileFrameStyle.Decorated),"Wrong automatic frame at "+size);
                var images=panel.GetComponentsInChildren<Image>();
                Check(images.All(i=>i.type==Image.Type.Sliced&&i.pixelsPerUnitMultiplier>0),"Invalid sliced geometry at "+size);
                Check(Mathf.Abs(images[0].pixelsPerUnitMultiplier-images[1].pixelsPerUnitMultiplier)<.001f,"Background/frame corner scale mismatch");
                var mesh=images[1].canvasRenderer.GetMesh();Check(mesh!=null&&mesh.vertexCount>0,"Frame has no UI mesh");
                var rect=panel.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(70,70);Canvas.ForceUpdateCanvases();
                Check(Edge(panel).ResolvedStyle==UITileFrameStyle.Compact&&Edge(panel).GetComponent<Image>().sprite==skin.compactFrame,"Resize did not switch source automatically");
                UnityEngine.Object.DestroyImmediate(panel.gameObject);
            }
            var test=Create(canvas.transform,"ColorCheck",new Vector2(220,110),UITileFrameStyle.Auto,2);
            var bg=Bg(test);var edge=Edge(test);var surface=bg.GetComponent<UITileSurface>();
            Canvas.ForceUpdateCanvases();float oldDensity=bg.pixelsPerUnit*bg.pixelsPerUnitMultiplier;
            canvas.referencePixelsPerUnit=200;Canvas.ForceUpdateCanvases();
            Check(Mathf.Abs(oldDensity-bg.pixelsPerUnit*bg.pixelsPerUnitMultiplier)<.001f,"Canvas reference PPU changed UI border width");
            canvas.referencePixelsPerUnit=100;Canvas.ForceUpdateCanvases();
            foreach(StageRealm realm in Enum.GetValues(typeof(StageRealm)))foreach(var role in new[]{UITileColorRole.A,UITileColorRole.B,UITileColorRole.C})
            {
                surface.Configure(skin,role,UITileFrameStyle.Auto,2,true,Color.white);theme.SetRealm(realm);theme.ApplyTheme();Undo.FlushUndoRecordObjects();
                Check(bg.sprite==skin.background&&bg.color==theme.GetLayerColor(role.ToString()),"Shared background lost its color role");
                Check(edge.GetComponent<Image>().color==theme.GetLayerColor("Frame"),"Frame theme mismatch");
            }
            var manual=new Color(.22f,.55f,.88f,.4f);
            Undo.RecordObject(bg,"User color");bg.color=manual;Undo.FlushUndoRecordObjects();theme.ApplyTheme();
            Check(bg.color==manual&&theme.HasImageColorOverride(bg),"Manual color edit was overwritten");
            test.SetColors(new Color(.3f,.4f,.5f,.6f),Color.red);theme.SetRealm(StageRealm.Asgard);theme.ApplyTheme();
            Check(bg.color==new Color(.3f,.4f,.5f,.6f)&&edge.GetComponent<Image>().color==Color.red,"Explicit panel colors were overwritten by theme");
            bg.type=Image.Type.Filled;bg.fillAmount=.37f;surface.Refresh();theme.ApplyTheme();
            Check(bg.type==Image.Type.Filled&&Mathf.Approximately(bg.fillAmount,.37f),"Filled gauge changed");
            var free=new GameObject("Img_B_Free_BG",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));free.transform.SetParent(canvas.transform,false);free.GetComponent<RectTransform>().sizeDelta=new Vector2(217,83);
            var plan=UIAtlasNameConnector.BuildPlan(new[]{free},texture,false,UITileFrameStyle.Auto,1.5f,false);
            Check(plan.Count==1&&plan[0].CanApply,"Connector still requires a ratio");
            var originalColor=new Color(.1f,.2f,.3f,.7f);free.GetComponent<Image>().color=originalColor;
            UIAtlasNameConnector.Apply(plan);Check(free.GetComponent<Image>().color==originalColor,"Connector changed existing color");
            Check(free.GetComponent<UITileSurface>().ThemeLayer=="B","Connector lost color role");
            free.name="Img_A_BG";
            var existingPlan=UIAtlasNameConnector.BuildPlan(new[]{free},texture,false,UITileFrameStyle.Auto);
            Check(existingPlan[0].layer=="B","Object name overrode explicit surface color role");
            foreach(var name in new[]{"UITilePanel","UITileButton","UITileCard"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/UICommon/"+name+".prefab");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform);Canvas.ForceUpdateCanvases();
                Check(instance.GetComponentsInChildren<UITileSurface>().Length==2,"Saved prefab missing surfaces: "+name);
                if(name=="UITileButton")Check(instance.GetComponent<Button>().targetGraphic!=null,"Button target missing");
                UnityEngine.Object.DestroyImmediate(instance);
            }
            UnityEngine.Object.DestroyImmediate(canvasObject);

            // Render actual uGUI geometry at several aspect ratios, not an image-composited approximation.
            var gallery=new GameObject("Gallery",typeof(RectTransform),typeof(Canvas));gallery.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var positions=new[]{new Vector2(-390,215),new Vector2(0,215),new Vector2(390,215),new Vector2(-300,-210),new Vector2(300,-210)};
            var renderSizes=new[]{new Vector2(256,384),new Vector2(256,384),new Vector2(256,384),new Vector2(480,56),new Vector2(180,56)};
            for(int i=0;i<5;i++)
            {
                var panel=Create(gallery.transform,"Preview",renderSizes[i],i==2?UITileFrameStyle.Compact:UITileFrameStyle.Auto,i==1?4:2);
                panel.GetComponent<RectTransform>().anchoredPosition=positions[i];
                panel.SetColors(i==1?new Color(.9569f,.9725f,.9216f):new Color(1,.9882f,.9255f),i==1?new Color(.5765f,.3647f,.1804f):new Color(.8941f,.6706f,.1765f));
            }
            var cameraObject=new GameObject("RenderCamera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=500;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.157f,.204f,.259f);
            var rt=new RenderTexture(1400,1000,24);rt.Create();camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();
            RenderTexture.active=rt;var png=new Texture2D(1400,1000,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1400,1000),0,0);png.Apply();
            File.WriteAllBytes(Path.GetFullPath("../unity-preview.png"),png.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;rt.Release();
            File.WriteAllText(Path.GetFullPath("../unity-verification.json"),"{\"passed\":true,\"checks\":"+checks+",\"sprites\":63,\"uniqueShapes\":3,\"legacyFileIds\":60,\"testedSizes\":8,\"themes\":10,\"prefabs\":3}");
            Debug.Log("PASS UI common kit: "+checks+" checks, 3 prefabs, 60 legacy IDs, resize/roles/manual colors/Filled and GPU render");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
