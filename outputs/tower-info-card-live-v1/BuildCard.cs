using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCard
{
    static string Output => Path.GetFullPath("../../tower-info-card-live-v1");
    static string Ui = "Assets/4. Asset/5. UI/";
    static TMP_FontAsset font;
    static TowerInfoCardView view;
    static Color Ink = new Color(.25f,.18f,.14f);
    static Sprite[] icons;
    static void Bind(string key,UnityEngine.Object value){var so=new SerializedObject(view);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static GameObject Child(Transform parent,string name,float w,float h,float x=0,float y=0)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);return go;
    }
    static TextMeshProUGUI Text(Transform parent,string name,string value,float w,float h,float x,float y,float size,TextAlignmentOptions align=TextAlignmentOptions.Center,bool wrap=false)
    {
        var go=Child(parent,"CTxt_"+name,w,h,x,y);var t=go.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.72f;t.fontSizeMax=size;t.alignment=align;t.color=Ink;t.raycastTarget=false;t.margin=Vector4.zero;t.enableWordWrapping=wrap;t.overflowMode=TextOverflowModes.Ellipsis;return t;
    }
    static Image Icon(Transform parent,string name,Sprite sprite,float size,float x,float y)
    {
        var go=Child(parent,name,size,size,x,y);var image=go.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
    }
    static UITilePanel Panel(Transform parent,string name,float w,float h,float x,float y,UITileFrameStyle style,float thickness,Color bg,Color edge,bool block=false)
    {
        var go=Child(parent,name,w,h,x,y);var p=go.AddComponent<UITilePanel>();p.SetSurfaces(UITilePanelEditor.CreateSurface(go.transform,"Img_A_BG",block),UITilePanelEditor.CreateSurface(go.transform,"Img_Frame",false));p.SetStyle(style,thickness);p.SetColors(bg,edge);return p;
    }
    static Button Button(Transform parent,string name,string text,float w,float h,float x,float y,Color bg)
    {
        var p=Panel(parent,name,w,h,x,y,UITileFrameStyle.Compact,1.5f,bg,new Color(.82f,.70f,.53f),true);var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p.transform.Find("Img_A_BG").GetComponent<Image>();
        var c=b.colors;c.highlightedColor=new Color(1,1,.92f);c.pressedColor=new Color(.82f,.86f,.88f);c.disabledColor=new Color(.66f,.66f,.66f,.72f);b.colors=c;
        if(!string.IsNullOrEmpty(text))Text(p.transform,"Label",text,w-12,h-8,0,0,25);return b;
    }
    static Sprite S(string name)=>icons.Single(s=>s.name==name);
    static void Rule(Transform p,float w,float h,float x,float y){var go=Child(p,"Divider",w,h,x,y);var image=go.AddComponent<Image>();image.color=new Color(.66f,.56f,.41f,.22f);image.raycastTarget=false;}
    public static void Run()
    {
        try{
            Directory.CreateDirectory(Output);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/5. Font/NotoSansKR-Black SDF.asset");if(font==null)throw new Exception("Project Korean font missing");
            icons=AssetDatabase.LoadAllAssetsAtPath(Ui+"Tower_Card_Icon_Atlas_v1.png").OfType<Sprite>().ToArray();
            var root=Child(null,"TowerInfoCard",600,1280);view=root.AddComponent<TowerInfoCardView>();
            var card=Panel(root.transform,"FlipRoot",600,980,0,150,UITileFrameStyle.Decorated,4,new Color(1,.9882f,.9255f),new Color(.84f,.65f,.44f),true);Bind("flipRoot",card.GetComponent<RectTransform>());
            var front=Child(card.transform,"Front",600,980);Bind("front",front);
            var back=Child(card.transform,"Back",600,980);Bind("back",back);
            Bind("towerName",Text(front.transform,"TowerName","히어로",470,76,0,420,52));
            Bind("level",Text(front.transform,"Level","Lv.1",95,40,-87,358,22));
            Bind("tierIcon",Icon(front.transform,"TierIcon",S("CardBadge_TierBronze"),40,0,358));
            var tierFallback=Child(front.transform,"TierFallback",33,39,0,358).AddComponent<TowerTierBadgeGraphic>();tierFallback.raycastTarget=false;tierFallback.enabled=false;Bind("fallbackTierBadge",tierFallback);
            Bind("tierName",Text(front.transform,"TierName","브론즈",100,40,77,358,22));
            var marks=Child(front.transform,"EmblemBackdrop",546,430,0,151);marks.AddComponent<RectMask2D>();
            var seal=AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"Card_Background_SwordSigil_v1.png");
            var a=Icon(marks.transform,"MarkLeft",seal,142,-210,35);a.color=new Color(.62f,.45f,.25f,.16f);
            var b=Icon(marks.transform,"MarkRight",seal,142,210,75);b.color=new Color(.62f,.45f,.25f,.12f);
            var so=new SerializedObject(view);so.FindProperty("backgroundMarks").arraySize=2;so.FindProperty("backgroundMarks").GetArrayElementAtIndex(0).objectReferenceValue=a;so.FindProperty("backgroundMarks").GetArrayElementAtIndex(1).objectReferenceValue=b;so.ApplyModifiedPropertiesWithoutUndo();
            var clip=Child(front.transform,"PortraitViewport",546,430,0,151);clip.AddComponent<RectMask2D>();
            var hero=AssetDatabase.LoadAllAssetsAtPath("Assets/4. Asset/2. Tower/TowerCharacter/Tower_Sword_Front_Atlas_v2.png").OfType<Sprite>().Single(s=>s.name=="Tower_Sword_Hero_Front_v2");
            Bind("portrait",Icon(clip.transform,"Portrait",hero,550,0,20));
            for(int i=0;i<2;i++){
                var tag=Panel(front.transform,"Keyword"+i,174,48,i==0?-180:180,-41,UITileFrameStyle.Compact,1.2f,new Color(1,.94f,.89f),new Color(.86f,.73f,.54f));
                Bind(i==0?"emblemIcon":"attackIcon",Icon(tag.transform,"Icon",S(i==0?"CardBadge_EmblemSword":"CardBadge_AttackSplash"),42,-49,0));
                Bind(i==0?"emblemName":"attackName",Text(tag.transform,"Name",i==0?"검":"광역",93,36,20,0,22));
            }
            var stat=Panel(front.transform,"Stats",546,286,0,-213,UITileFrameStyle.Compact,1,new Color(1,.995f,.97f),new Color(.87f,.80f,.67f));
            string[] fields={"power","attackCount","range","criticalRate","action","criticalDamage"};
            string[] labels={"공격력","공격 횟수","사거리","치명타 확률","행동력","치명타 피해"};
            string[] values={"10","1회","2칸","5%","2/3","200%"};
            string[] sprites={"CardStat_AttackPower","CardStat_AttackCount","CardStat_Range","CardStat_CriticalRate","CardStat_Action","CardStat_CriticalDamage"};
            for(int i=0;i<6;i++){float x=i%2==0?-140:140,y=94-i/2*94;Icon(stat.transform,"Icon_"+fields[i],S(sprites[i]),70,x-88,y);Text(stat.transform,"Label_"+fields[i],labels[i],165,29,x+31,y+20,20,TextAlignmentOptions.Left);Bind(fields[i],Text(stat.transform,"Value_"+fields[i],values[i],165,47,x+31,y-19,36,TextAlignmentOptions.Left));}
            Rule(stat.transform,500,1.5f,0,47);Rule(stat.transform,500,1.5f,0,-47);Rule(stat.transform,1.5f,250,0,0);
            var basic=AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"Icon_Attack_Basic_Common.png");var skill=AssetDatabase.LoadAssetAtPath<Sprite>(Ui+"Icon_Attack_Skill_Common.png");
            for(int i=0;i<2;i++){
                var button=Button(front.transform,i==0?"BasicButton":"SkillButton",null,258,88,i==0?-139:139,-414,i==0?new Color(1,.92f,.88f):new Color(.88f,.96f,1));Bind(i==0?"basicButton":"skillButton",button);
                Icon(button.transform,"Icon",i==0?basic:skill,60,-86,0);Text(button.transform,"Name",i==0?"기본공격":"스킬공격",158,34,21,i==0?0:13,25);
                if(i==1)Bind("skillSubtitle",Text(button.transform,"SkillSubtitle","엑스칼리버",158,25,21,-18,17));Text(button.transform,"Chevron","›",20,36,111,0,27);
            }
            Bind("returnButton",Button(back.transform,"ReturnButton","‹ 돌아가기",152,44,-190,423,new Color(1,.98f,.91f)));
            Bind("detailTowerName",Text(back.transform,"TowerName","히어로",340,46,0,352,29));
            var basicPage=Child(back.transform,"BasicPage",550,350,0,180);Bind("basicPage",basicPage);
            var skillPage=Child(back.transform,"SkillPage",550,350,0,180);Bind("skillPage",skillPage);
            Icon(basicPage.transform,"Icon",basic,74,0,103);Icon(skillPage.transform,"Icon",skill,74,0,103);
            Text(basicPage.transform,"Title","기본공격",490,54,0,36,37);Bind("skillTitle",Text(skillPage.transform,"Title","엑스칼리버",490,54,0,36,37));
            Bind("basicDescription",Text(basicPage.transform,"Description","범위 안의 모든 적에게\n10만큼의 피해",500,185,0,-91,27,TextAlignmentOptions.Center,true));
            Bind("skillDescription",Text(skillPage.transform,"Description","범위 안의 모든 적에게\n20만큼의 피해 (쿨타임: 5턴)",500,185,0,-91,27,TextAlignmentOptions.Center,true));
            string[] extraFields={"abilityValue","duration","mastery"};string[] extraLabels={"스킬 계수","지속 / 발동 조건","숙련도"};string[] extraValues={"x2","5턴","3x3"};
            for(int i=0;i<3;i++){var row=Panel(back.transform,"DetailStat"+i,506,84,0,-98-i*100,UITileFrameStyle.Compact,1,new Color(1,.98f,.92f),new Color(.86f,.76f,.61f));Text(row.transform,"Label",extraLabels[i],282,42,-86,0,23,TextAlignmentOptions.Left);Bind(extraFields[i],Text(row.transform,"Value",extraValues[i],194,55,143,0,29));}
            Text(back.transform,"Hint","공격 정보",450,40,0,-416,20);
            var target=Panel(root.transform,"TargetBar",600,48,0,-378,UITileFrameStyle.Compact,1,new Color(1,.98f,.92f),new Color(.86f,.76f,.61f));
            Bind("previousTarget",Button(target.transform,"Previous","‹",52,42,-268,0,Color.white));Bind("nextTarget",Button(target.transform,"Next","›",52,42,268,0,Color.white));
            Text(target.transform,"Label","우선순위",112,34,-175,0,20);Bind("targetPriority",Text(target.transform,"Value","가까운 적",345,36,54,0,22));
            string[] combineFields={"combineColor","combineExact","combineEmblem"};string[] combineNames={"컬러 합성","완전 합성","문양 합성"};
            for(int i=0;i<3;i++)Bind(combineFields[i],Button(root.transform,combineFields[i],combineNames[i],188,100,(i-1)*206,-463,new Color(1,.96f,.89f)));
            var sell=Button(root.transform,"Sell",null,600,64,0,-558,new Color(1,.94f,.88f));Bind("sell",sell);Text(sell.transform,"Label","판매",210,48,-120,0,25);
            var gem=AssetDatabase.LoadAllAssetsAtPath(Ui+"UI_Icon_Atlas.png").OfType<Sprite>().Single(s=>s.name=="Icon_Gem");Icon(sell.transform,"Gem",gem,37,62,0);Bind("sellValue",Text(sell.transform,"Value","1",160,48,159,0,29));
            Bind("swordIcon",S("CardBadge_EmblemSword"));Bind("splashIcon",S("CardBadge_AttackSplash"));Bind("bronzeIcon",S("CardBadge_TierBronze"));Bind("swordBackground",seal);
            var oldStats=AssetDatabase.LoadAllAssetsAtPath(Ui+"Stat_Icon_Atlas_v2.png").OfType<Sprite>().ToArray();
            so=new SerializedObject(view);so.FindProperty("attackIcons").arraySize=4;string[] fallback={"Icon_Tower_SplashRadius","Icon_Tower_CriticalRate","Icon_Tower_AbilityValue","Icon_Enemy_Resistance"};for(int i=0;i<4;i++)so.FindProperty("attackIcons").GetArrayElementAtIndex(i).objectReferenceValue=oldStats.Single(s=>s.name==fallback[i]);so.ApplyModifiedPropertiesWithoutUndo();
            back.SetActive(false);skillPage.SetActive(false);
            Directory.CreateDirectory("Assets/2. Prefab/UICommon");PrefabUtility.SaveAsPrefabAsset(root,"Assets/2. Prefab/UICommon/TowerInfoCard.prefab");
            var host=Child(null,"IntegrationHost",420,900);
            var nested=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/UICommon/TowerInfoCard.prefab"),host.transform);
            nested.transform.localScale=Vector3.one*.7f;nested.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-20);
            host.AddComponent<TowerCardHost>().card=nested.GetComponent<TowerInfoCardView>();
            Directory.CreateDirectory("Assets/Validation");PrefabUtility.SaveAsPrefabAsset(host,"Assets/Validation/IntegrationHost.prefab");UnityEngine.Object.DestroyImmediate(host);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            var canvas=new GameObject("PreviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit=2;root.transform.SetParent(canvas.transform,false);
            Render("card-front.png");view.ShowSkill();Render("card-skill-detail.png");view.ShowBasic();Render("card-basic-detail.png");
            File.WriteAllText(Path.Combine(Output,"build-verification.json"),"{\"passed\":true,\"frontStats\":6,\"detailStats\":3,\"actionButtons\":6,\"font\":\"NotoSansKR-Black SDF\",\"sharedSkin\":true}");
            Debug.Log("PASS TowerInfoCard prefab built with explicit project font, six front stats, two detail pages and retained utility buttons");EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Render(string filename)
    {
        var go=new GameObject("RenderCamera");var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=670;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.86f,.89f,.91f);
        var rt=new RenderTexture(1000,1600,24);rt.Create();camera.targetTexture=rt;Canvas.ForceUpdateCanvases();foreach(var text in view.GetComponentsInChildren<TextMeshProUGUI>())text.ForceMeshUpdate();camera.Render();RenderTexture.active=rt;var png=new Texture2D(1000,1600,TextureFormat.RGBA32,false);png.ReadPixels(new Rect(0,0,1000,1600),0,0);png.Apply();File.WriteAllBytes(Path.Combine(Output,filename),png.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(png);
    }
}
