using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SwordVisualCheck
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Bind(SerializedObject target,string property,string prefab)
    {
        var match=Regex.Match(prefab,@"(?m)^  "+property+@":\r?\n(?<rows>(?:  - \{[^\r\n]+\}\r?\n)+)");
        Check(match.Success,"Missing prefab sprite table: "+property);
        var refs=Regex.Matches(match.Groups["rows"].Value,@"fileID: (\d+), guid: ([a-f0-9]+)");
        var field=target.FindProperty(property);field.arraySize=refs.Count;
        for(int i=0;i<refs.Count;i++)
        {
            long expected=long.Parse(refs[i].Groups[1].Value);string guid=refs[i].Groups[2].Value;
            var sprite=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>()
                .Single(s=>AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string actualGuid,out long id)&&actualGuid==guid&&id==expected);
            field.GetArrayElementAtIndex(i).objectReferenceValue=sprite;
        }
    }
    static SpriteRenderer Renderer(Transform root,string name,int order)
    {
        var child=new GameObject(name);child.transform.SetParent(root,false);
        var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=order;return renderer;
    }
    static void AssertSword(TowerVisual visual,SpriteRenderer face,SpriteRenderer frame,SpriteRenderer background,int tier,int attack)
    {
        string[] names={"Hero","Swordsman","Paladin","DeathKnight"};string[] tiers={"Bronze","Silver","Gold","Mithril","Diamond"};
        Check(visual.UsesCharacterVisual&&visual.EmblemSprite.name=="Tower_Sword_"+names[attack-1],"Wrong sword character");
        Check(visual.TierSprite.name=="Tower_Frame_"+tiers[tier-1],"Wrong sword tier frame");
        Check(face.enabled&&frame.enabled&&!background.enabled&&visual.ColorSprite==null,"Legacy background still visible");
        Check(face.color==Color.white&&frame.color==Color.white,"Character/frame was tinted");
        Check(visual.ProjectileEmblemSprite.name=="Legacy"&&visual.ProjectileEmblemColor!=Color.white,"Character replaced legacy projectile art/tint");
        Check(frame.sortingLayerID==face.sortingLayerID&&frame.sortingOrder<face.sortingOrder,"Frame covers the character");
        Check(Vector3.Distance(frame.transform.localPosition,face.transform.localPosition)<.0001f,"Frame offset differs");
        float size=256f/192;
        Check(Mathf.Abs(face.bounds.size.x-size)<.0001f&&Mathf.Abs(face.bounds.size.y-size)<.0001f,"Character canvas size differs from spawn tile");
        Check(Mathf.Abs(frame.bounds.size.x-size)<.0001f&&Mathf.Abs(frame.bounds.size.y-size)<.0001f,"Frame canvas size differs from character");
    }
    public static void Run()
    {
        GameObject root=null;
        try
        {
            string prefab=File.ReadAllText(Path.Combine(Application.dataPath,"TowerBase.prefab.txt"));
            root=new GameObject("VisualCheck");var face=Renderer(root.transform,"Emblem",3);var frame=Renderer(root.transform,"Tier",4);var background=Renderer(root.transform,"Color",2);
            var oldScale=new Vector3(1.06f,1.045f,1);var oldPosition=new Vector3(0,-.0128125f,0);frame.transform.localScale=oldScale;frame.transform.localPosition=oldPosition;
            var originalMaterial=new Material(Shader.Find("Sprites/Default"));face.sharedMaterial=originalMaterial;
            var visual=root.AddComponent<TowerVisual>();var config=new SerializedObject(visual);
            Bind(config,"m_swordCharacterSprites",prefab);Bind(config,"m_characterTierSprites",prefab);
            var texture=new Texture2D(16,16);var legacy=Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16);legacy.name="Legacy";
            foreach(var table in new[]{"m_tierSprites","m_emblemSprites"}){var field=config.FindProperty(table);field.arraySize=table=="m_tierSprites"?5:13;for(int i=0;i<field.arraySize;i++)field.GetArrayElementAtIndex(i).objectReferenceValue=legacy;}
            config.FindProperty("m_backgroundSprite").objectReferenceValue=legacy;config.ApplyModifiedPropertiesWithoutUndo();
            for(int tier=1;tier<=5;tier++)for(int attack=1;attack<=4;attack++)
            {
                int id=tier*1000+attack*100+1;visual.Apply(id);AssertSword(visual,face,frame,background,tier,attack);
                var data=ScriptableObject.CreateInstance<TowerData>();data.towerID=id;data.InitializeIdentity();visual.Apply(data);AssertSword(visual,face,frame,background,tier,attack);UnityEngine.Object.DestroyImmediate(data);
            }
            var overrideData=ScriptableObject.CreateInstance<TowerData>();overrideData.towerID=1101;overrideData.visualTowerID=5201;overrideData.InitializeIdentity();visual.Apply(overrideData);AssertSword(visual,face,frame,background,5,2);UnityEngine.Object.DestroyImmediate(overrideData);
            visual.Apply(1202);
            Check(!visual.UsesCharacterVisual&&visual.EmblemSprite==legacy&&visual.ColorSprite==legacy,"Non-sword fallback broken");
            Check(frame.transform.localScale==oldScale&&frame.transform.localPosition==oldPosition&&frame.sortingOrder==4,"Legacy frame transform/order not restored");
            Check(face.sharedMaterial==originalMaterial&&background.enabled&&visual.EmblemColor!=Color.white,"Legacy material/color not restored");
            var catalog=ScriptableObject.CreateInstance<SynergyCharacterCatalog>();var catalogConfig=new SerializedObject(catalog);var entries=catalogConfig.FindProperty("m_entries");entries.arraySize=1;entries.GetArrayElementAtIndex(0).FindPropertyRelative("towerID").intValue=6001;entries.GetArrayElementAtIndex(0).FindPropertyRelative("front").objectReferenceValue=legacy;catalogConfig.ApplyModifiedPropertiesWithoutUndo();
            config.Update();config.FindProperty("m_synergyCharacters").objectReferenceValue=catalog;config.ApplyModifiedPropertiesWithoutUndo();
            visual.Apply(6001);Check(visual.UsesCharacterVisual&&!frame.enabled&&!background.enabled,"Synergy display broken");
            visual.Apply(1201);AssertSword(visual,face,frame,background,1,2);
            var block=new MaterialPropertyBlock();face.GetPropertyBlock(block);Check(block.GetVector("_SpriteRect")==Vector4.zero&&block.GetVector("_HeadRect")==Vector4.zero&&block.GetFloat("_CornerRadius")==0&&face.sharedMaterial==originalMaterial,"Synergy mask leaked into sword character");
            config.Update();config.FindProperty("m_swordCharacterSprites").GetArrayElementAtIndex(1).objectReferenceValue=null;config.ApplyModifiedPropertiesWithoutUndo();visual.Apply(1201);Check(!visual.UsesCharacterVisual&&visual.EmblemSprite==legacy&&background.enabled,"Missing-art fallback broken");
            Debug.Log("PASS: actual prefab sprite bindings, all 20 sword IDs through integer and real TowerData APIs, visual override, original-color artwork, spawn-size alignment, frame ordering, non-sword and missing-art fallback, synergy-to-sword material/mask reset.");
            UnityEngine.Object.DestroyImmediate(root);root=null;EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
    }
}
