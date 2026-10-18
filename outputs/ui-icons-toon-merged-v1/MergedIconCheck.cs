using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MergedIconCheck
{
    [Serializable] public class Manifest {public string atlasGuid;public int[] size;public Entry[] sprites;}
    [Serializable] public class Entry {public string name,fileID;public int x,y,width,height;}
    static int checks;
    static string Output=>Path.GetFullPath("../../ui-icons-toon-merged-v1");
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static GameObject Child(Transform parent,string name,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;return go;
    }
    static Image Icon(Transform parent,Sprite sprite,Vector2 position,int size)
    {
        var go=Child(parent,sprite.name,new Vector2(size,size),position);var image=go.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;
    }
    static void Label(Transform parent,string text,Vector2 pos,int fontSize)
    {
        var go=Child(parent,text,new Vector2(1080,40),pos);var label=go.AddComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.text=text;label.color=new Color(.45f,.34f,.28f);label.raycastTarget=false;
    }
    public static void Run()
    {
        try
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(Output,"verification.json")));
            string assetPath="Assets/4. Asset/5. UI/UI_Icon_Atlas.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(assetPath);importer.isReadable=true;importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();Check(sprites.Length==8,"Expected exactly eight merged icons");
            Check(sprites.Select(s=>s.texture).Distinct().Count()==1,"Icons do not share one texture");
            foreach(var entry in manifest.sprites)
            {
                var sprite=sprites.Single(s=>s.name==entry.name);Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id)&&guid==manifest.atlasGuid&&id.ToString()==entry.fileID,"Wrong sprite reference "+entry.name);
                Check(sprite.rect==new Rect(entry.x,entry.y,entry.width,entry.height)&&sprite.pivot==new Vector2(entry.width/2f,entry.height/2f),"Wrong rectangle or pivot");
            }
            foreach(var retired in new[]{"Icon_Heart_Frame","Icon_Heart_Black","Icon_Deco_Star4","Icon_Deco_Star8"})Check(!sprites.Any(s=>s.name==retired),"Retired sprite remains");
            Sprite red=sprites.Single(s=>s.name=="Icon_Heart"),white=sprites.Single(s=>s.name=="Icon_Heart_White");
            var texture=red.texture;Check(texture.width==manifest.size[0]&&texture.height==manifest.size[1],"Imported texture was downsized");
            Check(texture.filterMode==FilterMode.Bilinear,"Toon icon downscaling filter changed");
            Check(red.rect.width>=512&&white.rect.size==red.rect.size,"Insufficient icon resolution");
            var pixels=texture.GetPixels32();int mismatch=0;
            for(int y=0;y<(int)red.rect.height;y++)for(int x=0;x<(int)red.rect.width;x++)if(pixels[((int)red.rect.y+y)*texture.width+(int)red.rect.x+x].a!=pixels[((int)white.rect.y+y)*texture.width+(int)white.rect.x+x].a)mismatch++;
            Check(mismatch==0,"Heart overlay alpha differs in Unity");
            Check(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath("a59a7d01000100010001000100010099")),"Retired button atlas still imported");
            var canvasObject=new GameObject("MergedIcons",typeof(RectTransform),typeof(Canvas));canvasObject.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var panelObject=Child(canvasObject.transform,"Panel",new Vector2(1160,760),Vector2.zero);var panel=panelObject.AddComponent<UITilePanel>();panel.SetSurfaces(UITilePanelEditor.CreateSurface(panelObject.transform,"Img_A_BG",false),UITilePanelEditor.CreateSurface(panelObject.transform,"Img_Frame",false));panel.SetStyle(UITileFrameStyle.Decorated,3);panel.SetColors(new Color(.98f,.95f,.90f),new Color(.847f,.612f,.451f));
            Label(canvasObject.transform,"TOON UI - MERGED ATLAS",new Vector2(0,335),30);
            for(int i=0;i<8;i++)Icon(canvasObject.transform,sprites.Single(s=>s.name==manifest.sprites[i].name),new Vector2(-420+i%4*280,190-i/4*245),160);
            Label(canvasObject.transform,"WHITE HEART OVER RED: 0 / 25 / 50 / 75 / 100 %",new Vector2(0,-230),22);
            for(int i=0;i<5;i++)
            {
                var pos=new Vector2(-400+i*200,-300);Icon(canvasObject.transform,red,pos,64);var overlay=Icon(canvasObject.transform,white,pos,64);overlay.type=Image.Type.Filled;overlay.fillMethod=Image.FillMethod.Horizontal;overlay.fillOrigin=0;overlay.fillAmount=i*.25f;
            }
            var cameraObject=new GameObject("Camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=400;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.157f,.204f,.259f);
            var rt=new RenderTexture(1200,800,24);rt.Create();camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var png=new Texture2D(1200,800,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1200,800),0,0);png.Apply();File.WriteAllBytes(Path.Combine(Output,"unity-preview.png"),png.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;rt.Release();
            var zoom=new RenderTexture(2400,1600,24);zoom.Create();camera.targetTexture=zoom;camera.Render();RenderTexture.active=zoom;var zoomPng=new Texture2D(2400,1600,TextureFormat.RGBA32,false);zoomPng.ReadPixels(new Rect(0,0,2400,1600),0,0);zoomPng.Apply();File.WriteAllBytes(Path.Combine(Output,"unity-zoom-preview.png"),zoomPng.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;zoom.Release();
            File.WriteAllText(Path.Combine(Output,"unity-verification.json"),"{\"passed\":true,\"checks\":"+checks+",\"icons\":8,\"cellSize\":512,\"textureSize\":[2048,1024],\"filter\":\"Bilinear\",\"zoomPreview\":[2400,1600],\"heartAlphaMismatch\":0,\"overlaySteps\":5,\"oldAtlasRemoved\":true}");
            Debug.Log("PASS merged toon icons: "+checks+" checks, eight shared sprites, exact heart overlay and GPU render");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
