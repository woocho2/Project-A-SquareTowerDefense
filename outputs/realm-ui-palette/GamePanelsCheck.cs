using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GamePanelsCheck
{
    [Serializable] public class Assignment { public string path,sprite,guid,fileID,role,ratio; public float width,height; public int type; public float[] color; }
    [Serializable] public class Assignments { public Assignment[] entries; }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            var expected=JsonUtility.FromJson<Assignments>(File.ReadAllText("Assets/assignments.json")).entries;
            var atlas=AssetDatabase.LoadAllAssetsAtPath("Assets/Shared_UI_Atlas.png").OfType<Sprite>().ToArray();
            Check(atlas.Length==60,"Atlas has been extended");Check(atlas[0].texture.width==4096&&atlas[0].texture.height==2048,"Atlas dimensions changed");
            Check(atlas.Count(s=>s.border.sqrMagnitude>0)==4,"Expected exactly four sliced 128_B sprites");
            foreach(string layer in new[]{"A","B","C","Frame"}){
                var sliced=atlas.First(s=>s.name=="Shared_"+layer+"_Btn_128_B");
                Check(sliced.rect.size==new Vector2(128,128)&&sliced.border==new Vector4(48,48,48,48),"Layer is missing its 128_B sliced sprite");
            }
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Canvas.prefab");Check(root!=null,"Canvas prefab did not import");
            Check(expected.Length==30,"Wrong panel assignment count");
            foreach(var entry in expected)
            {
                var child=root.transform.Find(entry.path.Substring("Canvas/".Length));Check(child!=null,"Missing hierarchy object: "+entry.path);
                var image=child.GetComponent<Image>();Check(image!=null&&image.sprite!=null,"Missing image sprite: "+entry.path);
                Check(image.sprite.name==entry.sprite,"Wrong sprite: "+entry.path);
                Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(image.sprite,out string guid,out long id)&&guid==entry.guid&&id.ToString()==entry.fileID,"Wrong atlas GUID or sprite ID");
                var ratio=entry.ratio.Split(':');float desired=float.Parse(ratio[0])/float.Parse(ratio[1]);
                Check(Mathf.Abs(image.sprite.rect.width/image.sprite.rect.height-desired)<.0001f,"Sprite ratio mismatch");
                Check(Mathf.Abs(image.rectTransform.rect.width-entry.width)<.01f&&Mathf.Abs(image.rectTransform.rect.height-entry.height)<.01f,"UI rectangle changed unexpectedly");
                Check((int)image.type==entry.type,"Wrong Simple/Sliced mode");
                Check(Mathf.Abs(image.color.r-entry.color[0])+Mathf.Abs(image.color.g-entry.color[1])+Mathf.Abs(image.color.b-entry.color[2])<.0001f,"Wrong role color");
            }
            foreach(string name in new[]{"Group_TurnEnd","Group_TurnEnd/Btn_TurnEnd","Group_TurnEnd/Img_C_TurnEnd_BG 4:1","Group_TurnEnd/Img_TurnEnd_Frame 4:1"})
            {
                var rect=root.transform.Find("PlayerTurnPanel/"+name).GetComponent<RectTransform>();Check(rect.rect.size==new Vector2(400,100),"TurnEnd hit area is not aligned");
            }
            Check(root.transform.Find("GameInfoPanel/Group_GameSpeed/Btn_GameSpeed").GetComponent<Image>().color.a==0,"Transparent button hit area changed");
            Debug.Log("PASS actual Canvas prefab: 30 atlas references, exact ratios, role colors, Simple/Sliced modes and 400x100 TurnEnd group/button/background/frame");
            Debug.Log("PASS unchanged atlas: 60 sprites on 4096x2048 texture; transparent button hit areas preserved");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
