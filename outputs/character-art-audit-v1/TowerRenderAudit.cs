using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TowerRenderAudit
{
    [Serializable] public class Report {public string unityVersion,scope,graphicsDevice;public int regularCombinations,synergy,renderedImages;public bool passed;public List<string> portraits=new List<string>();}
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static SpriteRenderer Layer(Transform parent,string name,int order){var go=new GameObject(name);go.transform.SetParent(parent,false);var r=go.AddComponent<SpriteRenderer>();r.sortingOrder=order;return r;}
    static void SetLayer(SpriteRenderer r,Sprite s){r.sprite=s;r.transform.localScale=new Vector3((256f/192f)/s.bounds.size.x,(256f/192f)/s.bounds.size.y,1);}
    public static void Run()
    {
        try {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);PlayerSettings.colorSpace=ColorSpace.Linear;
            var regular=Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog");var synergy=Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");var palette=SharedBoardVisuals.Default;
            Check(regular!=null&&synergy!=null&&palette!=null,"Missing catalogs/palette");
            var report=new Report{unityVersion=Application.unityVersion,graphicsDevice=SystemInfo.graphicsDeviceName,scope="Isolated Unity sprite-render fixture, production catalogs/atlases/shared palette; source TowerVisual layer placement and canvas size. Verifies 280 regular combinations and 14 synergies. Not a full stage/combat play test."};
            var root=new GameObject("TowerAssetRender");var bg=Layer(root.transform,"Background",0);var portrait=Layer(root.transform,"Character",1);var decoration=Layer(root.transform,"Decoration",2);var emblem=Layer(root.transform,"Emblem",3);emblem.transform.localPosition=new Vector3(0,.475f,0);emblem.transform.localScale=new Vector3(.25f,.25f,1);
            var icons=AssetDatabase.LoadAllAssetsAtPath("Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png").OfType<Sprite>().ToDictionary(s=>s.name);
            string[] iconNames={"Emblem_01_Sword","Emblem_02_Axe","Emblem_03_Bow","Emblem_04_Spear","Emblem_05_Shield","Emblem_06_Hammer","Emblem_07_Fire","Emblem_08_Water","Emblem_11_Earth","Emblem_10_Wind","Emblem_08_Ice","Emblem_09_Electricity","Emblem_12_Light","Emblem_13_Dark"};
            SetLayer(bg,palette.Background);SetLayer(decoration,palette.Decoration);
            var cameraObject=new GameObject("TowerAuditCamera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-6);camera.orthographic=true;camera.orthographicSize=.73f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.24f,1);camera.nearClipPlane=.1f;camera.farClipPlane=20;
            var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();camera.targetTexture=target;var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
            const string output="../../character-art-audit-v1/unity-rendered-towers";Directory.CreateDirectory(output);
            for(int tier=1;tier<=5;tier++)for(int e=1;e<=14;e++)for(int a=1;a<=4;a++){
                int id=tier*1000+a*100+e;var s=regular.GetCharacter(e,a);Check(s!=null,"Missing portrait "+id);Check(s.rect.size==new Vector2(256,256)&&s.pivot==new Vector2(128,128)&&Mathf.Abs(s.pixelsPerUnit-192)<.001f,"Sprite layout mismatch "+id);SetLayer(portrait,s);
                var colors=palette.GetTower(id);bg.color=colors.background;decoration.color=colors.decoration;emblem.sprite=icons[iconNames[e-1]];emblem.color=a==1?(Color)new Color32(226,93,98,255):a==2?(Color)new Color32(83,138,205,255):a==3?(Color)new Color32(213,173,40,255):(Color)new Color32(32,32,39,255);emblem.enabled=true;
                Check(Mathf.Abs(portrait.bounds.size.x-256f/192f)<.0001f&&Mathf.Abs(portrait.bounds.size.y-256f/192f)<.0001f,"Canvas size mismatch "+id);
                Capture(camera,target,texture,output+"/Tower_"+id+".png");report.regularCombinations++;report.renderedImages++;if(tier==1)report.portraits.Add(s.name);
            }
            emblem.enabled=false;
            for(int id=6001;id<=6014;id++){var s=synergy.GetFront(id);Check(s!=null&&s.rect.size==new Vector2(256,256)&&s.pivot==new Vector2(128,128),"Missing/invalid synergy "+id);SetLayer(portrait,s);var colors=palette.GetTower(id);bg.color=colors.background;decoration.color=colors.decoration;Capture(camera,target,texture,output+"/Tower_"+id+".png");report.synergy++;report.renderedImages++;report.portraits.Add(s.name);}
            Check(report.regularCombinations==280&&report.synergy==14&&report.renderedImages==294,"Coverage mismatch");report.passed=true;
            File.WriteAllText("../../character-art-audit-v1/unity-tower-render-verification.json",JsonUtility.ToJson(report,true));Debug.Log("TOWER_RENDER_AUDIT_PASS");EditorApplication.Exit(0);
        }catch(Exception ex){File.WriteAllText("../../character-art-audit-v1/unity-tower-render-failure.txt",ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);}
    }
    static void Capture(Camera camera,RenderTexture rt,Texture2D texture,string file){camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();RenderTexture.active=null;var pixels=texture.GetPixels32();Check(pixels.Select(p=>(p.r<<16)|(p.g<<8)|p.b).Distinct().Take(128).Count()==128,"Flat/empty render "+file);Check(pixels.Count(p=>p.r>180&&p.g<100&&p.b>180)<pixels.Length/4,"Missing shader "+file);File.WriteAllBytes(file,texture.EncodeToPNG());}
}
