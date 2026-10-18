using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SharedCardPreview
{
    static string Output=>Path.GetFullPath("../../tower-info-shared-ui-v5");
    static string Ui="Assets/4. Asset/5. UI/";
    static Font font;
    static Color Ink=new Color(.25f,.18f,.14f);
    static GameObject Child(Transform parent,string name,float w,float h,float x,float y)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(w,h);rect.anchoredPosition=new Vector2(x,y);return go;
    }
    static Image Icon(Transform parent,Sprite sprite,float size,float x,float y)
    {
        var go=Child(parent,sprite.name,size,size,x,y);var image=go.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    static Text Label(Transform parent,string text,float w,float h,float x,float y,int size,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        var go=Child(parent,text,w,h,x,y);var label=go.AddComponent<Text>();label.font=font;label.fontSize=size;label.alignment=alignment;label.color=Ink;label.text=text;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;return label;
    }
    static UITilePanel Panel(Transform parent,string name,float w,float h,float x,float y,UITileFrameStyle style,float thickness,Color bg,Color edge)
    {
        var go=Child(parent,name,w,h,x,y);var p=go.AddComponent<UITilePanel>();p.SetSurfaces(UITilePanelEditor.CreateSurface(go.transform,"Img_A_BG",false),UITilePanelEditor.CreateSurface(go.transform,"Img_Frame",false));p.SetStyle(style,thickness);p.SetColors(bg,edge);return p;
    }
    static void Rule(Transform parent,float w,float x,float y)
    {
        var go=Child(parent,"Divider",w,1.5f,x,y);go.AddComponent<Image>().color=new Color(.66f,.56f,.41f,.25f);
    }
    static void Render(Camera camera,int width,int height,string name)
    {
        var rt=new RenderTexture(width,height,24);rt.Create();camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        var png=new Texture2D(width,height,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(Path.Combine(Output,name),png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(png);
    }
    public static void Run()
    {
        try{
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/PreviewOnlyFont.ttf");if(font==null)throw new Exception("Preview font missing");
            var skin=UITileSkin.Default;
            var stats=AssetDatabase.LoadAllAssetsAtPath(Ui+"Stat_Icon_Atlas_v2.png").OfType<Sprite>().ToArray();
            var hero=AssetDatabase.LoadAllAssetsAtPath("Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png").OfType<Sprite>().Single(s=>s.name=="Tower_Sword_Hero_Front_v2");
            var basic=AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"Icon_Attack_Basic_Common.png");var skill=AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"Icon_Attack_Skill_Common.png");
            foreach(var sprite in new[]{basic,skill})if(sprite==null||sprite.rect.size!=new Vector2(512,512)||sprite.texture.filterMode!=FilterMode.Bilinear)throw new Exception("Attack icon import invalid");
            var go=new GameObject("SharedCardConcept",typeof(RectTransform),typeof(Canvas));var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit=2;
            var card=Panel(go.transform,"Card_Shared_Decorated",600,980,0,0,UITileFrameStyle.Decorated,4,new Color(1,.9882f,.9255f),new Color(.84f,.65f,.44f));
            Label(card.transform,"히어로",470,76,0,420,52);
            Label(card.transform,"Lv.1  ·  브론즈",400,40,0,358,22);
            // Crop transparent outer padding for this portrait display only; the source atlas stays unchanged.
            var portrait=Sprite.Create(hero.texture,new Rect(hero.rect.x+32,hero.rect.y+12,192,200),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);portrait.name="ExistingHero_PreviewCrop";
            Icon(card.transform,portrait,450,0,137);
            var tags=Panel(card.transform,"Keywords_Shared_Compact",220,38,0,-45,UITileFrameStyle.Compact,1,new Color(1,.98f,.90f),new Color(.86f,.73f,.54f));
            Label(tags.transform,"검  ·  광역",210,32,0,0,19);
            var statArea=Panel(card.transform,"Stats_Shared_Compact",546,286,0,-213,UITileFrameStyle.Compact,1,new Color(1,.995f,.97f),new Color(.87f,.80f,.67f));
            string[] names={"공격력","공격 횟수","사거리","치명타 확률","행동력","치명타 피해"};
            string[] values={"10","1회","2칸","5%","2/3","200%"};
            string[] spriteNames={"Icon_Tower_AttackPower","Icon_Tower_AttackCount","Icon_Tower_Range","Icon_Tower_CriticalRate","Icon_Tower_ActionInterval","Icon_Tower_CriticalDamage"};
            for(int i=0;i<6;i++){
                float x=i%2==0?-140:140,y=94-i/2*94;
                Icon(statArea.transform,stats.Single(s=>s.name==spriteNames[i]),56,x-88,y);
                Label(statArea.transform,names[i],165,29,x+31,y+20,20,TextAnchor.MiddleLeft);
                Label(statArea.transform,values[i],165,47,x+31,y-19,36,TextAnchor.MiddleLeft);
            }
            Rule(statArea.transform,500,0,47);Rule(statArea.transform,500,0,-47);
            var divider=Child(statArea.transform,"ColumnDivider",1.5f,250,0,0);divider.AddComponent<Image>().color=new Color(.66f,.56f,.41f,.18f);
            for(int i=0;i<2;i++){
                var p=Panel(card.transform,i==0?"BasicAttack_Shared":"SkillAttack_Shared",258,88,i==0?-139:139,-414,UITileFrameStyle.Compact,1.5f,i==0?new Color(1,.92f,.88f):new Color(.88f,.96f,1),i==0?new Color(.88f,.60f,.47f):new Color(.52f,.72f,.81f));
                Icon(p.transform,i==0?basic:skill,60,-86,0);
                Label(p.transform,i==0?"기본공격":"스킬공격",158,34,21,i==0?0:13,25);
                if(i==1)Label(p.transform,"엑스칼리버",158,25,21,-18,17);
                Label(p.transform,"›",20,36,111,0,27);
            }
            var cameraObject=new GameObject("PreviewCamera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=520;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.86f,.89f,.91f);
            Canvas.ForceUpdateCanvases();
            foreach(var image in card.GetComponentsInChildren<Image>())if(image.GetComponent<UITileSurface>()!=null&&image.sprite.texture!=skin.background.texture)throw new Exception("Panel did not reuse common atlas");
            Render(camera,1024,1536,"card-shared-ui-preview.png");
            File.WriteAllText(Path.Combine(Output,"unity-verification.json"),"{\"passed\":true,\"renderer\":\"Unity uGUI\",\"size\":[1024,1536],\"stats\":6,\"sharedSkinPanels\":5,\"newIcons\":2,\"existingHeroSprite\":true,\"previewOnly\":true}");
            Debug.Log("PASS Shared card preview: actual common UI atlas, existing portrait/stat icons, six stats and two new shared attack sprites");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
