using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SharedUICheck
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static Image CreateImage(Transform parent,Sprite sprite,string name){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.rectTransform.sizeDelta=new Vector2(220,80);return image;}
    public static void Run()
    {
        try
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/Shared_UI_Atlas.png").OfType<Sprite>().ToArray();
            Check(sprites.Length==60,"Expected 60 shared atlas sprites");
            Check(sprites.Select(s=>s.texture).Distinct().Count()==1,"Sprites do not share one texture");
            Check(sprites.Count(s=>s.border.sqrMagnitude>0)==4,"Expected A/B/C/Frame sliced sprites");
            foreach(var entry in sprites.Where(s=>s.border.sqrMagnitude>0))Check(entry.name.EndsWith("_Btn_128_B")&&entry.rect.width==128&&entry.rect.height==128&&entry.border==new Vector4(48,48,48,48),"Wrong sliced sprite");
            var sliced=sprites.First(s=>s.name=="Shared_Frame_Btn_128_B");
            Check(sliced.rect.width==128&&sliced.rect.height==128&&sliced.border==new Vector4(48,48,48,48),"Sliced frame size or border changed");
            var root=new GameObject("ColorPersistenceCheck",typeof(Canvas));var theme=root.AddComponent<UIStageThemeController>();
            var a=CreateImage(root.transform,sprites.First(s=>s.name=="Shared_A_Capsule_256x128"),"Button_BG");
            var b=CreateImage(root.transform,sprites.First(s=>s.name=="Shared_B_Capsule_256x128"),"Accent_BG");
            var c=CreateImage(root.transform,sprites.First(s=>s.name=="Shared_C_Capsule_256x128"),"Dark_BG");
            var frame=CreateImage(root.transform,sprites.First(s=>s.name=="Shared_Frame_Capsule_256x128"),"Button_Frame");
            a.type=Image.Type.Filled;a.fillAmount=.4f;b.type=Image.Type.Simple;c.type=Image.Type.Sliced;
            frame.color=new Color(1,1,1,0);
            foreach(StageRealm realm in Enum.GetValues(typeof(StageRealm)))
            {
                theme.SetRealm(realm);theme.ApplyTheme();Undo.FlushUndoRecordObjects();
                Check(a.color==theme.GetLayerColor("A"),"A palette mismatch");Check(b.color==theme.GetLayerColor("B"),"B palette mismatch");Check(c.color==theme.GetLayerColor("C"),"C palette mismatch");
                Check(frame.color.a==0,"Transparent frame became visible");
                Check(theme.ManualColorCount==0,"Automatic colors registered as manual edits");
                Check(a.type==Image.Type.Filled&&a.fillAmount==.4f&&b.type==Image.Type.Simple&&c.type==Image.Type.Simple,"Simple rendering mode or functional fill changed");
            }
            frame.sprite=sliced;theme.ApplyTheme();Check(frame.type==Image.Type.Sliced,"Single sliced frame did not use Sliced mode");
            Debug.Log("PASS 60 shared sprites, four 128_B sliced sprites, remaining Simple sprites, functional fills and zero alpha preservation");
            Undo.IncrementCurrentGroup();Undo.RecordObject(a,"User Image Color edit");var chosen=new Color(.17f,.41f,.76f,.6f);a.color=chosen;Undo.FlushUndoRecordObjects();
            Check(theme.ManualColorCount==1,"Inspector/Undo color edit was not tracked");theme.ApplyTheme();Undo.FlushUndoRecordObjects();
            Check(a.color==chosen,"Manual Image Color was overwritten");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/ColorPersistence.prefab");UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            var loaded=PrefabUtility.LoadPrefabContents("Assets/ColorPersistence.prefab");var loadedTheme=loaded.GetComponent<UIStageThemeController>();loadedTheme.SetRealm(StageRealm.Asgard);loadedTheme.ApplyTheme();
            Check(loaded.transform.Find("Button_BG").GetComponent<Image>().color==chosen,"Saved/reopened color changed");
            Check(loadedTheme.ManualColorCount==1,"Saved manual override disappeared");
            loadedTheme.ClearManualColors();Undo.FlushUndoRecordObjects();
            Check(loadedTheme.ManualColorCount==0,"Reset theme colors failed");Check(loaded.transform.Find("Button_BG").GetComponent<Image>().color.r==1,"Reset did not restore A color");
            PrefabUtility.UnloadPrefabContents(loaded);
            Debug.Log("PASS direct Image Color edit tracking, save/reopen, theme switching and restoring theme defaults");
            var gallery=new GameObject("SharedPaletteGallery",typeof(Canvas));gallery.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            for(int row=0;row<10;row++)
            {
                var rowObject=new GameObject("PaletteRow",typeof(Canvas));rowObject.transform.SetParent(gallery.transform,false);var rowTheme=rowObject.AddComponent<UIStageThemeController>();
                foreach(string layer in new[]{"A","B","C","Frame"})
                {
                    int col=Array.IndexOf(new[]{"A","B","C","Frame"},layer);var image=CreateImage(rowObject.transform,sprites.First(s=>s.name=="Shared_"+layer+"_Capsule_256x128"),"Preview_"+layer);
                    image.rectTransform.anchoredPosition=new Vector2((col-1.5f)*250,(4.5f-row)*100);image.rectTransform.sizeDelta=new Vector2(220,80);
                }
                rowTheme.SetRealm((StageRealm)row);rowTheme.ApplyTheme();
            }
            var cameraObject=new GameObject("Camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=550;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.18f,1);
            var target=new RenderTexture(1100,1100,24);target.Create();camera.targetTexture=target;camera.Render();RenderTexture.active=target;var output=new Texture2D(1100,1100,TextureFormat.RGBA32,false);output.ReadPixels(new Rect(0,0,1100,1100),0,0);output.Apply();File.WriteAllBytes(Path.GetFullPath("../shared-ui-preview.png"),output.EncodeToPNG());
            Debug.Log("PASS shared atlas UI GPU preview render");EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
