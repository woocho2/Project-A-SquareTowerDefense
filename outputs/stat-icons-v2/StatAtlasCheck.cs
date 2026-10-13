using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StatAtlasCheck
{
    [Serializable] sealed class Entry { public string name; public string fileID; public int x; public int y; }
    [Serializable] sealed class Manifest { public Entry[] entries; }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            const string path = "Assets/Stat_Icon_Atlas_v2.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Check(texture != null && texture.width == 1280 && texture.height == 1024, "Texture dimensions changed");
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Check(importer.spriteImportMode == SpriteImportMode.Multiple && !importer.mipmapEnabled && importer.alphaIsTransparency, "Wrong sprite importer settings");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            Check(sprites.Length == 20 && sprites.Select(s => s.name).Distinct().Count() == 20, "Expected 20 unique sprites");
            Check(sprites.Count(s => s.name.StartsWith("Icon_Tower_")) == 15 && sprites.Count(s => s.name.StartsWith("Icon_Enemy_")) == 5, "Wrong tower/enemy split");
            Check(!sprites.Any(s => s.name.Contains("HPBar")), "Health bars occupy new icon slots");
            var json = "{\"entries\":" + File.ReadAllText("Assets/manifest.json") + "}";
            foreach (var entry in JsonUtility.FromJson<Manifest>(json).entries)
            {
                var sprite = sprites.Single(s => s.name == entry.name);
                Check(sprite.rect == new Rect(entry.x, entry.y, 256, 256) && sprite.border == Vector4.zero, "Wrong sprite rect: " + entry.name);
                Check(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id) && guid == "a87ced92ee9146f7a674922b255125d0" && id.ToString() == entry.fileID, "Wrong serialized sprite identity: " + entry.name);
            }
            var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Check(readable.LoadImage(File.ReadAllBytes(path)), "Cannot decode PNG");
            var allPixels = readable.GetPixels32();
            foreach(var sprite in sprites)
            {
                var pixels = new Color32[256 * 256];
                for (int row = 0; row < 256; row++)
                    Array.Copy(allPixels, ((int)sprite.rect.y + row) * readable.width + (int)sprite.rect.x, pixels, row * 256, 256);
                Check(pixels.Count(p => p.a == 0) > 20000 && pixels.Count(p => p.a > 200) > 2000, "Missing transparent background or icon: " + sprite.name);
                for (int i = 0; i < 256; i++)
                    Check(pixels[i].a == 0 && pixels[255 * 256 + i].a == 0 && pixels[i * 256].a == 0 && pixels[i * 256 + 255].a == 0, "Icon bleeding across sprite edge: " + sprite.name);
            }
            Debug.Log("PASS: 20 native imported sprites, exact serialized IDs, 15 tower/5 enemy, 256-square rects, original PNG alpha and clear cell boundaries");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
