using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SwordAtlasCheck
{
    static void Check(bool condition,string message) { if(!condition)throw new Exception(message); }
    public static void Run()
    {
        try
        {
            const string path="Assets/Tower_Sword_Characters_And_Frames_Atlas_v1.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Check(importer!=null&&importer.spriteImportMode==SpriteImportMode.Multiple,"Expected multiple sprite import");
            Check(importer.spritePixelsPerUnit==192&&!importer.mipmapEnabled&&importer.alphaIsTransparency,"Wrong PPU/alpha/mipmap settings");
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Check(sprites.Length==9&&sprites.Select(s=>s.texture).Distinct().Count()==1,"Expected nine sprites sharing one texture");
            string[] names={"Tower_Sword_Hero","Tower_Sword_Swordsman","Tower_Sword_Paladin","Tower_Sword_DeathKnight","Tower_Frame_Bronze","Tower_Frame_Silver","Tower_Frame_Gold","Tower_Frame_Mithril","Tower_Frame_Diamond"};
            long[] ids={42001,42002,42003,42004,42011,42012,42013,42014,42015};
            for(int i=0;i<9;i++)
            {
                var sprite=sprites.Single(s=>s.name==names[i]);
                Check(sprite.rect==new Rect((i%3)*256,(2-i/3)*256,256,256)&&sprite.border==Vector4.zero,"Wrong sprite slice");
                Check(Vector2.Distance(sprite.pivot,new Vector2(128,128))<.001f,"Wrong pivot");
                Check(Mathf.Abs(sprite.bounds.size.x-256f/192)<.001f&&Mathf.Abs(sprite.bounds.size.y-256f/192)<.001f,"Wrong world size");
                Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long fileId)&&guid=="3dc0d1c0881d4668a3cf42a5710e7bdf"&&fileId==ids[i],"Serialized sprite identity mismatch");
            }
            var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);
            string fullPath=Path.Combine(Application.dataPath,"Tower_Sword_Characters_And_Frames_Atlas_v1.png");
            Check(readable.LoadImage(File.ReadAllBytes(fullPath))&&readable.width==768&&readable.height==768,"Wrong texture dimensions");
            var pixels=readable.GetPixels32();
            for(int i=4;i<9;i++)
            {
                int bx=(i%3)*256,by=(2-i/3)*256,rx=256,ry=256;
                Check(pixels[(by+128)*768+bx+128].a==0,"Frame center must be transparent");
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                    Check(pixels[(by+y)*768+bx+x].a==pixels[(ry+y)*768+rx+x].a,"Frame transparency differs by tier");
            }
            Debug.Log("PASS: Unity imported nine shared-texture sprites with stable file IDs, 256x256 slices, 192 PPU, centered pivots, correct world bounds, transparent frame centers and matching frame alpha across five tiers.");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
