#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AsgardAtlasGenerator
{
    private const string AtlasPath = "Assets/4. DotAsset/5. UI/Themes/Asgard/Asgard_UI_Atlas_2048.png";
    private const string AsgardDir = "Assets/4. DotAsset/5. UI/Themes/Asgard";

    [MenuItem("Tools/👑 아스가르드 2048 마스터 UI 아틀라스 생성")]
    public static void GenerateAtlas()
    {
        Debug.Log("<color=#FFD700>[AsgardAtlasGenerator]</color> 2048x2048 마스터 UI 아틀라스 제작 시작...");

        int atlasSize = 2048;
        Texture2D atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);

        // Clear with transparent
        Color[] clearColors = new Color[atlasSize * atlasSize];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
        atlas.SetPixels(clearColors);

        // Load existing base textures
        Texture2D pFrame = LoadTexture(Path.Combine(AsgardDir, "Asgard_Panel_Main_Frame.png"));
        Texture2D pBG = LoadTexture(Path.Combine(AsgardDir, "Asgard_Panel_Main_BG.png"));
        Texture2D bFrame = LoadTexture(Path.Combine(AsgardDir, "Asgard_Btn_High_Frame.png"));
        Texture2D bBG = LoadTexture(Path.Combine(AsgardDir, "Asgard_Btn_High_BG.png"));
        Texture2D wFrame = LoadTexture(Path.Combine(AsgardDir, "Asgard_Wave_Banner_Frame.png"));
        Texture2D wBG = LoadTexture(Path.Combine(AsgardDir, "Asgard_Wave_Banner_BG.png"));
        Texture2D cFrame = LoadTexture(Path.Combine(AsgardDir, "Asgard_Resource_Capsule_Frame.png"));
        Texture2D cBG = LoadTexture(Path.Combine(AsgardDir, "Asgard_Resource_Capsule_BG.png"));

        // 1. Tier 1: 1024x1024
        // Top-Left (x: 0, y: 1024, w: 1024, h: 1024) -> Frame
        // Top-Right (x: 1024, y: 1024, w: 1024, h: 1024) -> BG
        if (pFrame != null)
        {
            Blit9Slice(pFrame, atlas, 0, 1024, 1024, 1024, new Vector4(96, 96, 96, 96), 128);
            AddJewelAccents(atlas, 0, 1024, 1024, 1024);
        }
        if (pBG != null)
        {
            Blit9Slice(pBG, atlas, 1024, 1024, 1024, 1024, new Vector4(32, 32, 32, 32), 64);
        }

        // 2. Tier 2: 512x512
        // (x: 0, y: 512, w: 512, h: 512) -> Frame
        // (x: 512, y: 512, w: 512, h: 512) -> BG
        if (pFrame != null) BlitDirect(pFrame, atlas, 0, 512);
        if (pBG != null) BlitDirect(pBG, atlas, 512, 512);

        // 3. Tier 3: 256x256
        // (x: 0, y: 256, w: 256, h: 256) -> Frame
        // (x: 256, y: 256, w: 256, h: 256) -> BG
        if (bFrame != null) BlitDirect(bFrame, atlas, 0, 256);
        if (bBG != null) BlitDirect(bBG, atlas, 256, 256);

        // 4. Tier 4: 128x128
        // (x: 0, y: 128, w: 128, h: 128) -> Frame
        // (x: 128, y: 128, w: 128, h: 128) -> BG
        if (bFrame != null) Blit9Slice(bFrame, atlas, 0, 128, 128, 128, new Vector4(64, 64, 64, 64), 24);
        if (bBG != null) Blit9Slice(bBG, atlas, 128, 128, 128, 128, new Vector4(64, 64, 64, 64), 16);

        // 5. Tier 5: 64x64
        // (x: 0, y: 64, w: 64, h: 64) -> Frame
        // (x: 64, y: 64, w: 64, h: 64) -> BG
        if (bFrame != null) Blit9Slice(bFrame, atlas, 0, 64, 64, 64, new Vector4(64, 64, 64, 64), 12);
        if (bBG != null) Blit9Slice(bBG, atlas, 64, 64, 64, 64, new Vector4(64, 64, 64, 64), 8);

        // 6. Bonus Top HUD: Wave Banner & Resource Capsule
        // Banner: (x: 1024, y: 896, w: 512, h: 128) -> Frame
        // Banner: (x: 1536, y: 896, w: 512, h: 128) -> BG
        if (wFrame != null) BlitDirect(wFrame, atlas, 1024, 896);
        if (wBG != null) BlitDirect(wBG, atlas, 1536, 896);

        // Capsule: (x: 1024, y: 768, w: 256, h: 128) -> Frame
        // Capsule: (x: 1280, y: 768, w: 256, h: 128) -> BG
        if (cFrame != null) BlitDirect(cFrame, atlas, 1024, 768);
        if (cBG != null) BlitDirect(cBG, atlas, 1280, 768);

        atlas.Apply();

        // Save PNG
        byte[] pngData = atlas.EncodeToPNG();
        File.WriteAllBytes(AtlasPath, pngData);
        Object.DestroyImmediate(atlas);

        AssetDatabase.Refresh();

        // Configure TextureImporter with SpriteMode: Multiple & SpriteSheet metadata
        ConfigureImporter();

        Debug.Log("<color=#40E0D0>[AsgardAtlasGenerator]</color> 2048x2048 마스터 UI 아틀라스 생성 및 14개 서브 스프라이트 슬라이싱 완료!");
    }

    private static void ConfigureImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;

        // Define 14 sub-sprites with exact rect and 9-slice borders
        SpriteMetaData[] sheet = new SpriteMetaData[]
        {
            // 1. Tier 1 (1024x1024)
            CreateSpriteMeta("Asgard_Frame_1024", 0, 1024, 1024, 1024, new Vector4(128, 128, 128, 128)),
            CreateSpriteMeta("Asgard_BG_1024", 1024, 1024, 1024, 1024, new Vector4(64, 64, 64, 64)),

            // 2. Tier 2 (512x512)
            CreateSpriteMeta("Asgard_Frame_512", 0, 512, 512, 512, new Vector4(64, 64, 64, 64)),
            CreateSpriteMeta("Asgard_BG_512", 512, 512, 512, 512, new Vector4(32, 32, 32, 32)),

            // 3. Tier 3 (256x256)
            CreateSpriteMeta("Asgard_Frame_256", 0, 256, 256, 256, new Vector4(48, 48, 48, 48)),
            CreateSpriteMeta("Asgard_BG_256", 256, 256, 256, 256, new Vector4(32, 32, 32, 32)),

            // 4. Tier 4 (128x128)
            CreateSpriteMeta("Asgard_Frame_128", 0, 128, 128, 128, new Vector4(24, 24, 24, 24)),
            CreateSpriteMeta("Asgard_BG_128", 128, 128, 128, 128, new Vector4(16, 16, 16, 16)),

            // 5. Tier 5 (64x64)
            CreateSpriteMeta("Asgard_Frame_64", 0, 64, 64, 64, new Vector4(12, 12, 12, 12)),
            CreateSpriteMeta("Asgard_BG_64", 64, 64, 64, 64, new Vector4(8, 8, 8, 8)),

            // 6. Bonus Top HUD
            CreateSpriteMeta("Asgard_Wave_Banner_Frame", 1024, 896, 512, 128, new Vector4(128, 32, 128, 32)),
            CreateSpriteMeta("Asgard_Wave_Banner_BG", 1536, 896, 512, 128, new Vector4(64, 32, 64, 32)),
            CreateSpriteMeta("Asgard_Resource_Capsule_Frame", 1024, 768, 256, 128, new Vector4(48, 48, 48, 48)),
            CreateSpriteMeta("Asgard_Resource_Capsule_BG", 1280, 768, 256, 128, new Vector4(48, 48, 48, 48))
        };

        importer.spritesheet = sheet;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static SpriteMetaData CreateSpriteMeta(string name, int x, int y, int w, int h, Vector4 border)
    {
        SpriteMetaData meta = new SpriteMetaData();
        meta.name = name;
        meta.rect = new Rect(x, y, w, h);
        meta.border = border;
        meta.alignment = (int)SpriteAlignment.Center;
        meta.pivot = new Vector2(0.5f, 0.5f);
        return meta;
    }

    private static Texture2D LoadTexture(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return tex;
    }

    private static void BlitDirect(Texture2D src, Texture2D dst, int dstX, int dstY)
    {
        Color[] pixels = src.GetPixels();
        dst.SetPixels(dstX, dstY, src.width, src.height, pixels);
    }

    private static void Blit9Slice(Texture2D src, Texture2D dst, int dstX, int dstY, int dstW, int dstH, Vector4 srcBorder, int dstBorder)
    {
        int sw = src.width;
        int sh = src.height;
        int sbl = (int)srcBorder.x;
        int sbb = (int)srcBorder.y;
        int sbr = (int)srcBorder.z;
        int sbt = (int)srcBorder.w;

        int dbl = dstBorder;
        int dbb = dstBorder;
        int dbr = dstBorder;
        int dbt = dstBorder;

        Color[] outPixels = new Color[dstW * dstH];

        for (int y = 0; y < dstH; y++)
        {
            float syNorm;
            if (y < dbb) syNorm = (float)y / dbb * sbb;
            else if (y >= dstH - dbt) syNorm = sh - sbt + (float)(y - (dstH - dbt)) / dbt * sbt;
            else syNorm = sbb + (float)(y - dbb) / (dstH - dbb - dbt) * (sh - sbb - sbt);

            int sy = Mathf.Clamp((int)syNorm, 0, sh - 1);

            for (int x = 0; x < dstW; x++)
            {
                float sxNorm;
                if (x < dbl) sxNorm = (float)x / dbl * sbl;
                else if (x >= dstW - dbr) sxNorm = sw - sbr + (float)(x - (dstW - dbr)) / dbr * sbr;
                else sxNorm = sbl + (float)(x - dbl) / (dstW - dbl - dbr) * (sw - sbl - sbr);

                int sx = Mathf.Clamp((int)sxNorm, 0, sw - 1);

                outPixels[y * dstW + x] = src.GetPixel(sx, sy);
            }
        }

        dst.SetPixels(dstX, dstY, dstW, dstH, outPixels);
    }

    private static void AddJewelAccents(Texture2D atlas, int dstX, int dstY, int w, int h)
    {
        int[] jx = new int[] { dstX + 56, dstX + w - 56, dstX + 56, dstX + w - 56 };
        int[] jy = new int[] { dstY + 56, dstY + 56, dstY + h - 56, dstY + h - 56 };

        Color cyanCore = new Color(0.25f, 0.88f, 0.95f, 1f);
        Color cyanBorder = new Color(0.1f, 0.5f, 0.6f, 1f);
        Color goldRing = new Color(1f, 0.85f, 0.35f, 1f);

        for (int i = 0; i < 4; i++)
        {
            int cx = jx[i];
            int cy = jy[i];
            int r = 16;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 8) atlas.SetPixel(cx + dx, cy + dy, cyanCore);
                    else if (dist <= 11) atlas.SetPixel(cx + dx, cy + dy, cyanBorder);
                    else if (dist <= 14) atlas.SetPixel(cx + dx, cy + dy, goldRing);
                }
            }
        }
    }

    [MenuItem("Tools/👑 아스가르드 4대 테마별 아틀라스(2048x1024) 생성")]
    public static void GenerateThemeAtlases()
    {
        Debug.Log("<color=#FFD700>[AsgardAtlasGenerator]</color> 4대 테마별 아틀라스(Gold, Ivory, Night, Frame) 제작 시작...");

        string[] themes = new string[] { "Gold", "Ivory", "Night", "Frame" };
        foreach (string theme in themes)
        {
            GenerateSingleThemeAtlas(theme);
        }

        AssetDatabase.Refresh();
        Debug.Log("<color=#40E0D0>[AsgardAtlasGenerator]</color> 4대 테마별 아틀라스 및 24개 서브 스프라이트 슬라이싱 생성 완료!");
    }

    private static void GenerateSingleThemeAtlas(string theme)
    {
        int atlasW = 2048;
        int atlasH = 1024;
        Texture2D atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);

        Color[] clearColors = new Color[atlasW * atlasH];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
        atlas.SetPixels(clearColors);

        string p1024 = theme == "Frame" ? "Asgard_Btn_Frame_Gold_1024.png" : $"Asgard_Btn_{theme}_BG_1024.png";
        string p512  = theme == "Frame" ? "Asgard_Btn_Frame_Gold_512.png"  : $"Asgard_Btn_{theme}_BG_512.png";
        string p128  = theme == "Frame" ? "Asgard_Btn_Frame_Gold_128.png"  : $"Asgard_Btn_{theme}_BG_128.png";
        string c512  = theme == "Frame" ? "Asgard_Capsule_Frame_Gold_512x128.png" : $"Asgard_Capsule_{theme}_BG_512x128.png";
        string c384  = theme == "Frame" ? "Asgard_Capsule_Frame_Gold_384x128.png" : $"Asgard_Capsule_{theme}_BG_384x128.png";
        string c256  = theme == "Frame" ? "Asgard_Capsule_Frame_Gold_256x128.png" : $"Asgard_Capsule_{theme}_BG_256x128.png";

        Texture2D t1024 = LoadTexture(Path.Combine(AsgardDir, p1024));
        Texture2D t512  = LoadTexture(Path.Combine(AsgardDir, p512));
        Texture2D t128  = LoadTexture(Path.Combine(AsgardDir, p128));
        Texture2D tc512 = LoadTexture(Path.Combine(AsgardDir, c512));
        Texture2D tc384 = LoadTexture(Path.Combine(AsgardDir, c384));
        Texture2D tc256 = LoadTexture(Path.Combine(AsgardDir, c256));

        if (t1024 != null) BlitDirect(t1024, atlas, 0, 0);
        if (t512 != null)  BlitDirect(t512, atlas, 1024, 512);
        if (tc512 != null) BlitDirect(tc512, atlas, 1024, 384);
        if (tc384 != null) BlitDirect(tc384, atlas, 1024, 256);
        if (tc256 != null) BlitDirect(tc256, atlas, 1024, 128);
        if (t128 != null)  BlitDirect(t128, atlas, 1024, 0);

        atlas.Apply();

        string outPath = Path.Combine(AsgardDir, $"Asgard_Atlas_{theme}.png");
        byte[] pngData = atlas.EncodeToPNG();
        File.WriteAllBytes(outPath, pngData);
        Object.DestroyImmediate(atlas);

        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;

            Vector4 capBorder = theme == "Frame" ? new Vector4(48, 48, 48, 48) : new Vector4(36, 36, 36, 36);

            SpriteMetaData[] sheet = new SpriteMetaData[]
            {
                CreateSpriteMeta($"Asgard_{theme}_Btn_1024", 0, 0, 1024, 1024, new Vector4(152, 152, 152, 152)),
                CreateSpriteMeta($"Asgard_{theme}_Btn_512", 1024, 512, 512, 512, new Vector4(76, 76, 76, 76)),
                CreateSpriteMeta($"Asgard_{theme}_Capsule_512x128", 1024, 384, 512, 128, capBorder),
                CreateSpriteMeta($"Asgard_{theme}_Capsule_384x128", 1024, 256, 384, 128, capBorder),
                CreateSpriteMeta($"Asgard_{theme}_Capsule_256x128", 1024, 128, 256, 128, capBorder),
                CreateSpriteMeta($"Asgard_{theme}_Btn_128", 1024, 0, 128, 128, new Vector4(24, 24, 24, 24))
            };

            importer.spritesheet = sheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
#endif
