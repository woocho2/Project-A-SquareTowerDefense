using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TierAtlasCheck
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            const string path="Assets/Tower_Sword_Splash_Tiers_Layer_Atlas_v1.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Check(importer.spriteImportMode==SpriteImportMode.Multiple&&importer.spritePixelsPerUnit==192&&!importer.mipmapEnabled&&importer.alphaIsTransparency,"Wrong importer settings");
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Check(sprites.Length==6&&sprites.Select(s=>s.texture).Distinct().Count()==1,"Expected six layers sharing one texture");
            string[] names={"Face_Common","Helmet_Bronze","Helmet_Silver","Helmet_Gold","Helmet_Mithril","Helmet_Diamond"};
            long[] ids={31001,31011,31012,31013,31014,31015};
            var layers=new Color32[6][];
            var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);
            Check(readable.LoadImage(File.ReadAllBytes(path))&&readable.width==768&&readable.height==512,"Wrong atlas dimensions");
            var pixels=readable.GetPixels32();
            for(int i=0;i<6;i++)
            {
                var sprite=sprites.Single(s=>s.name=="Tower_Sword_Splash_"+names[i]);
                int x=(i%3)*256,y=(1-i/3)*256;
                Check(sprite.rect==new Rect(x,y,256,256)&&sprite.border==Vector4.zero,"Wrong sprite rect");
                Check(Vector2.Distance(sprite.pivot,new Vector2(128,128))<.001f&&Mathf.Abs(sprite.bounds.size.x-256f/192)<.001f&&Mathf.Abs(sprite.bounds.size.y-256f/192)<.001f,"Layer placement/world size mismatch");
                Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id)&&guid=="fa27898b561e452aadd31073590b3a88"&&id==ids[i],"Serialized sprite identity mismatch");
                layers[i]=new Color32[256*256];
                for(int row=0;row<256;row++)Array.Copy(pixels,(y+row)*768+x,layers[i],row*256,256);
            }
            var original=new Texture2D(2,2,TextureFormat.RGBA32,false);
            Check(original.LoadImage(File.ReadAllBytes("Assets/OriginalFace.png")),"Original decode failed");
            var old=original.GetPixels32();
            Check(layers[0].Count(p=>p.a>0)>14000,"Missing common face");
            for(int tier=1;tier<=5;tier++)for(int p=0;p<old.Length;p++)
            {
                var face=layers[0][p];var helmet=layers[tier][p];
                Check(!(face.a>0&&helmet.a>0),"Equipment covers the shared face");
                Check(helmet.a==layers[1][p].a,"Equipment silhouettes differ between tiers");
                var combined=face.a>0?face:helmet;
                Check(combined.a==old[p].a,"Combined silhouette differs from spawn-sized original");
                if(face.a>0)Check(combined.Equals(old[p]),"Face color/eyes/mouth changed");
                if(tier==1&&combined.a>0)Check(combined.Equals(old[p]),"Bronze reconstruction changed visible pixels");
            }
            Debug.Log("PASS: Unity imported six shared-texture layers, serialized IDs, 256x256/192 PPU, centered pivots, transparent equipment window, identical common face and all tier silhouettes, original bronze reconstruction.");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
