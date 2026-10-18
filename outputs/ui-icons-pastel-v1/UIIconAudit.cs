using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIIconAudit
{
    [Serializable] public class Report { public int spriteCount; public Item[] assets; }
    [Serializable] public class Item { public string file,guid; public int width,height; public Entry[] sprites; }
    [Serializable] public class Entry { public string name,fileID; public int x,y,width,height; }
    static int checks;
    static string Output => Path.GetFullPath("../../ui-icons-pastel-v1");
    static void Check(bool condition,string text){checks++;if(!condition)throw new Exception(text);}
    static GameObject Child(Transform parent,string name){var g=new GameObject(name,typeof(RectTransform));g.layer=5;g.transform.SetParent(parent,false);return g;}
    static void Icon(Transform parent,Sprite sprite,float x,float y,float size)
    {
        var g=Child(parent,sprite.name);var rect=g.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(size,size);rect.anchoredPosition=new Vector2(x,y);
        var image=g.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
    }
    static void Label(Transform parent,string value,float y)
    {
        var g=Child(parent,value);var rect=g.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(1080,50);rect.anchoredPosition=new Vector2(0,y);
        var text=g.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=28;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleLeft;text.text=value;text.color=new Color(.45f,.34f,.28f);text.raycastTarget=false;
    }
    public static void Run()
    {
        try
        {
            var report=JsonUtility.FromJson<Report>(File.ReadAllText(Path.Combine(Output,"verification.json")));
            var before=new List<Sprite>();var after=new List<Sprite>();
            foreach(var item in report.assets)
            {
                string assetPath="Assets/4. Asset/5. UI/"+item.file;
                var sprites=AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
                Check(sprites.Length==item.sprites.Length,"Wrong sprite count: "+item.file);
                Check(sprites[0].texture.width==item.width&&sprites[0].texture.height==item.height,"Texture dimensions changed");
                var oldTexture=new Texture2D(2,2,TextureFormat.RGBA32,false);oldTexture.LoadImage(File.ReadAllBytes(Path.Combine(Output,"before",item.file)));oldTexture.filterMode=FilterMode.Bilinear;
                foreach(var entry in item.sprites)
                {
                    var sprite=sprites.Single(s=>s.name==entry.name);
                    Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id)&&guid==item.guid&&id.ToString()==entry.fileID,"Lost sprite reference: "+entry.name);
                    Check(sprite.rect==new Rect(entry.x,item.height-entry.y-entry.height,entry.width,entry.height),"Sprite rect changed");
                    Check(sprite.pivot==new Vector2(entry.width/2f,entry.height/2f),"Pivot changed");
                    after.Add(sprite);var oldSprite=Sprite.Create(oldTexture,sprite.rect,new Vector2(.5f,.5f),sprite.pixelsPerUnit);oldSprite.name=sprite.name;before.Add(oldSprite);
                }
            }
            Check(after.Count==33&&report.spriteCount==33,"Expected 33 icons");
            var root=new GameObject("UIIconGallery",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            var panelObject=Child(root.transform,"PastelPanel");panelObject.GetComponent<RectTransform>().sizeDelta=new Vector2(1180,960);
            var panel=panelObject.AddComponent<UITilePanel>();
            var bg=UITilePanelEditor.CreateSurface(panelObject.transform,"Img_A_BG",false);var edge=UITilePanelEditor.CreateSurface(panelObject.transform,"Img_Frame",false);panel.SetSurfaces(bg,edge);panel.SetStyle(UITileFrameStyle.Decorated,3);
            panel.SetColors(new Color(.98f,.95f,.90f),new Color(.847f,.612f,.451f));
            Label(root.transform,"BEFORE",420);Label(root.transform,"AFTER - PASTEL",10);
            for(int i=0;i<33;i++)
            {
                float x=-500+(i%11)*100,y=325-(i/11)*105;Icon(root.transform,before[i],x,y,64);Icon(root.transform,after[i],x,y-410,64);
            }
            for(int i=0;i<8;i++)Icon(root.transform,after[i],-500+i*65,-410,32);
            var cameraObject=new GameObject("Camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=500;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.157f,.204f,.259f);
            var target=new RenderTexture(1240,1000,24);target.Create();camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(1240,1000,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,1240,1000),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Output,"unity-preview.png"),pixels.EncodeToPNG());
            Check(after.All(s=>s!=null),"Missing displayed icon");
            File.WriteAllText(Path.Combine(Output,"unity-verification.json"),"{\"passed\":true,\"checks\":"+checks+",\"icons\":33,\"files\":4,\"referenceIdsPreserved\":true,\"renderedSizes\":[64,32]}");
            RenderTexture.active=null;camera.targetTexture=null;target.Release();
            Debug.Log("PASS pastel UI icons: "+checks+" checks, 33 original GUID/fileIDs/rects/pivots and real UI render");EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
