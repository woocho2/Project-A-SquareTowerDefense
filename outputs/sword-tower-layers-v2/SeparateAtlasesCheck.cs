using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SeparateAtlasesCheck
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Group(string path,string expectedGuid,string[] names,long[] ids,int columns,int rows)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        Check(importer!=null&&importer.spriteImportMode==SpriteImportMode.Multiple&&importer.spritePixelsPerUnit==192&&!importer.mipmapEnabled&&importer.alphaIsTransparency,"Wrong importer settings");
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        Check(sprites.Length==names.Length&&sprites.Select(s=>s.texture).Distinct().Count()==1,"Wrong sprite count/shared texture");
        Check(sprites[0].texture.width==columns*256&&sprites[0].texture.height==rows*256,"Wrong atlas dimensions");
        for(int i=0;i<names.Length;i++)
        {
            var sprite=sprites.Single(s=>s.name==names[i]);
            Check(sprite.rect==new Rect((i%columns)*256,(rows-1-i/columns)*256,256,256)&&sprite.border==Vector4.zero,"Wrong slice rect");
            Check(Vector2.Distance(sprite.pivot,new Vector2(128,128))<.001f&&Mathf.Abs(sprite.bounds.size.x-256f/192)<.001f&&Mathf.Abs(sprite.bounds.size.y-256f/192)<.001f,"Wrong pivot/bounds");
            Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id)&&guid==expectedGuid&&id==ids[i],"Sprite identity mismatch");
        }
        Debug.Log("PASS: "+path+"; "+names.Length+" sprites; stable IDs; 256x256 slices, 192 PPU, centered pivots, correct bounds.");
    }
    public static void Run()
    {
        try
        {
            Group("Assets/Tower_Sword_Characters_Atlas_v2.png","f5d4f9f3e6b14a2aac4e263ba24fb29b",new[]{"Tower_Sword_Hero","Tower_Sword_Swordsman","Tower_Sword_Paladin","Tower_Sword_DeathKnight"},new long[]{42001,42002,42003,42004},2,2);
            Group("Assets/Tower_Frames_Atlas_v1.png","d25b136f115b43538e33a31e1c69d13e",new[]{"Tower_Frame_Bronze","Tower_Frame_Silver","Tower_Frame_Gold","Tower_Frame_Mithril","Tower_Frame_Diamond","Tower_Frame_Synergy"},new long[]{42011,42012,42013,42014,42015,42016},3,2);
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
