using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NameConnectorCheck
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static Image MakeImage(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.rectTransform.sizeDelta=new Vector2(243,117);image.color=new Color(.13f,.39f,.7f,.4f);return image;}
    public static void Run()
    {
        try
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shared_UI_Atlas.png");Check(texture!=null,"Atlas import failed");
            var root=new GameObject("SelectedPanel",typeof(Canvas));var theme=root.AddComponent<UIStageThemeController>();
            theme.SetRealm(StageRealm.Asgard);
            Check(theme.GetLayerColor("B")==new Color(1,223f/255,125f/255,1),"Asgard B gold was not brightened");
            Check(theme.GetLayerColor("Frame")==new Color(236f/255,187f/255,65f/255,1),"Frame color changed");
            var card=MakeImage(root.transform,"Img_A_Card_BG 3:2");var bar=MakeImage(root.transform,"Img_B_Bar_BG 3:1");bar.gameObject.SetActive(false);
            var square=MakeImage(root.transform,"Img_C_Square_BG 1:1");var filled=MakeImage(root.transform,"Img_A_Gauge_BG 2:1");filled.type=Image.Type.Filled;filled.fillAmount=.35f;
            var container=new GameObject("Parent 2:1");container.transform.SetParent(root.transform,false);var frame=MakeImage(container.transform,"Img_Button_Frame");
            var reduced=MakeImage(root.transform,"Img_A_Reduced_BG 6 : 4");var unsupported=MakeImage(root.transform,"Img_A_Unknown_BG 5:1");
            var ambiguous=MakeImage(root.transform,"Img_A_Conflict_BG 3:2 3:1");var zero=MakeImage(root.transform,"Img_A_Zero_BG 0:1");
            var icon=MakeImage(root.transform,"Img_Icon 3:2");var outside=MakeImage(null,"Img_A_Outside_BG 3:1");
            var plan=UIAtlasNameConnector.BuildPlan(new[]{root,card.gameObject},texture,true,true,UIAtlasNameConnector.SquareStyle.Sliced128B);
            Check(plan.Count==10,"Overlapping selection was not deduplicated or inactive children were missed");
            Check(plan.Single(row=>row.image==card).sprite.name=="Shared_A_SkillCard_384x256","3:2 mapping failed");
            Check(plan.Single(row=>row.image==bar).sprite.name=="Shared_B_Capsule_384x128","Inactive 3:1 mapping failed");
            Check(plan.Single(row=>row.image==frame).sprite.name=="Shared_Frame_Capsule_256x128","Frame / inherited ratio mapping failed");
            Check(plan.Single(row=>row.image==square).sprite.name=="Shared_C_Btn_128_B"&&plan.Single(row=>row.image==square).type==Image.Type.Sliced,"Sliced square mapping failed");
            Check(plan.Single(row=>row.image==reduced).sprite==plan.Single(row=>row.image==card).sprite,"Equivalent ratio reduction failed");
            Check(!plan.Single(row=>row.image==unsupported).CanApply&&!plan.Single(row=>row.image==ambiguous).CanApply&&!plan.Single(row=>row.image==zero).CanApply&&!plan.Single(row=>row.image==icon).CanApply,"Invalid names were not skipped");
            Check(card.sprite==null&&square.sprite==null,"Preview mutated UI");
            int changed=UIAtlasNameConnector.Apply(plan);Check(changed==6,"Wrong number of applied rows");
            Check(card.rectTransform.sizeDelta==new Vector2(243,117)&&card.color==new Color(.13f,.39f,.7f,.4f),"User size or color changed");
            Check(filled.type==Image.Type.Filled&&filled.fillAmount==.35f,"Functional gauge changed");
            Check(outside.sprite==null&&unsupported.sprite==null&&icon.sprite==null,"Unselected or unsupported image was mutated");
            theme.ApplyTheme();Check(card.color==new Color(.13f,.39f,.7f,.4f),"Saved custom color was lost during theme reapplication");
            Undo.PerformUndo();Check(card.sprite==null&&square.sprite==null&&filled.sprite==null,"Undo failed to restore selected sprites");
            plan=UIAtlasNameConnector.BuildPlan(new[]{root},texture,true,true,UIAtlasNameConnector.SquareStyle.Sliced128B);
            Check(UIAtlasNameConnector.Apply(plan)==6,"Redo application failed");
            Check(UIAtlasNameConnector.Apply(plan)==0,"Repeated application is not idempotent");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/ConnectedPanel.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
            var loaded=PrefabUtility.LoadPrefabContents("Assets/ConnectedPanel.prefab");
            Check(loaded.transform.Find("Img_A_Card_BG 3:2").GetComponent<Image>().sprite.name=="Shared_A_SkillCard_384x256","Prefab connection was not saved");
            Check(loaded.transform.Find("Img_C_Square_BG 1:1").GetComponent<Image>().type==Image.Type.Sliced,"Prefab mode was not saved");
            var loadedTheme=loaded.GetComponent<UIStageThemeController>();loadedTheme.ApplyTheme();Check(loaded.transform.Find("Img_A_Card_BG 3:2").GetComponent<Image>().color==new Color(.13f,.39f,.7f,.4f),"Reopened prefab lost user color");
            var regular=UIAtlasNameConnector.BuildPlan(new[]{loaded.transform.Find("Img_C_Square_BG 1:1").gameObject},texture,false,false,UIAtlasNameConnector.SquareStyle.Simple128);
            Check(regular.Single().sprite.name=="Shared_C_Btn_128"&&regular.Single().type==Image.Type.Simple,"Optional Simple square mapping failed");
            PrefabUtility.UnloadPrefabContents(loaded);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ConnectedPanel.prefab");Check(UIAtlasNameConnector.BuildPlan(new[]{asset},texture,true,true,UIAtlasNameConnector.SquareStyle.Sliced128B).Count==0,"Project prefab asset was edited directly");
            Debug.Log("PASS name-based A/B/C/Frame ratio mapping, inactive/deduplicated selection, inherited/equivalent ratios, invalid-name skips and preview without mutation");
            Debug.Log("PASS scope, color/size preservation, functional fill, Undo, repeated apply, prefab save/reopen and both square styles");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
