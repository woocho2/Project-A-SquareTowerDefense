#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Temporary integration test. The source is retained in output, not shipped with the game.
public static class EmblemAtlasIntegrationCheck
{
    private const string Atlas = "Assets/4. Asset/2. Tower/3. Emblem/Tower_Emblem_White_Atlas.png";
    private const string Background = "Assets/4. Asset/2. Tower/2. Color/Tower_White_Background.png";
    private const string Prefab = "Assets/2. Prefab/2. Tower/TowerBase.prefab";
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += Run;

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static bool SameColor(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.00001f && Mathf.Abs(a.g - b.g) < 0.00001f &&
        Mathf.Abs(a.b - b.b) < 0.00001f && Mathf.Abs(a.a - b.a) < 0.00001f;

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("[EmblemAtlasCheck] Deferred until edit mode.");
            return;
        }

        GameObject root = null;
        GameObject panelObject = null;
        TowerData data = null;
        string result;
        try
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            Check(sprites.Length == 13, "Expected 13 imported atlas sprites, got " + sprites.Length);
            for (int i = 0; i < sprites.Length; i++)
            {
                Check(sprites[i].rect == new Rect((i % 4) * 256, 768 - (i / 4) * 256, 256, 256), "Sprite rect mismatch " + i);
                Check(Mathf.Approximately(sprites[i].pixelsPerUnit, 256), "Emblem PPU mismatch");
            }

            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(Background);
            Check(background != null && Mathf.Approximately(background.pixelsPerUnit, 192), "Background missing or PPU mismatch");

            root = PrefabUtility.LoadPrefabContents(Prefab);
            TowerVisual visual = root.GetComponent<TowerVisual>();
            var emblem = root.transform.Find("Emblem").GetComponent<SpriteRenderer>();
            var face = root.transform.Find("Color").GetComponent<SpriteRenderer>();
            var tier = root.transform.Find("Tier").GetComponent<SpriteRenderer>();
            Check(face.sortingOrder < emblem.sortingOrder && emblem.sortingOrder < tier.sortingOrder, "Layer order mismatch");

            panelObject = new GameObject("Emblem atlas test panel");
            panelObject.SetActive(false);
            TowerInfoPanel panel = panelObject.AddComponent<TowerInfoPanel>();
            var tierUI = NewImage(panelObject.transform);
            var faceUI = NewImage(panelObject.transform);
            var emblemUI = NewImage(panelObject.transform);
            typeof(TowerInfoPanel).GetField("m_selectedTower", Fields).SetValue(panel, root.GetComponent<TowerController>());
            typeof(TowerInfoPanel).GetField("img_tier", Fields).SetValue(panel, tierUI);
            typeof(TowerInfoPanel).GetField("img_color", Fields).SetValue(panel, faceUI);
            typeof(TowerInfoPanel).GetField("img_emblem", Fields).SetValue(panel, emblemUI);
            MethodInfo refreshUI = typeof(TowerInfoPanel).GetMethod("UpdateIdentityImage", Fields);
            data = ScriptableObject.CreateInstance<TowerData>();
            Color[] colors = { new Color32(226,93,98,255), new Color32(83,138,205,255),
                               new Color32(213,173,40,255), new Color32(32,32,39,255) };
            int cases = 0;
            for (int t = 1; t <= 5; t++)
            for (int c = 1; c <= 4; c++)
            for (int e = 1; e <= 13; e++)
            {
                int id = t * 1000 + c * 100 + e;
                visual.Apply(id);
                Verify(visual, face, background, sprites[e-1], colors[c-1], id);
                refreshUI.Invoke(panel, null);
                Check(emblemUI.sprite == sprites[e-1] && SameColor(emblemUI.color, colors[c-1]), "UI emblem/tint mismatch " + id);
                Check(faceUI.sprite == background && SameColor(faceUI.color, Color.white), "UI white face mismatch " + id);

                data.towerID = id;
                data.visualTowerID = 0;
                data.InitializeIdentity();
                visual.Apply(data);
                Verify(visual, face, background, sprites[e-1], colors[c-1], id);
                cases += 2;
            }

            // Visual identity override used by synergy towers must still work.
            data.towerID = 999999;
            data.visualTowerID = 5313;
            data.InitializeIdentity();
            visual.Apply(data);
            Verify(visual, face, background, sprites[12], colors[2], 5313);
            result = "PASS\nImported sprites: 13; 256x256; PPU256\nWhite background: PPU192\n" +
                     "Visual int/data cases: " + cases + "\nUI sprite/tint cases: 260\n" +
                     "Visual identity override: PASS\nLayer order: background < emblem < tier\n" +
                     "Tested in an isolated prefab scene; user's active scene was not saved or modified.\n";
            Debug.Log("[EmblemAtlasCheck] PASS: 520 visual cases, 260 UI cases, 13 atlas sprites.");
        }
        catch (Exception ex)
        {
            result = "FAIL\n" + ex;
            Debug.LogError("[EmblemAtlasCheck] " + ex);
        }
        finally
        {
            if (data != null) UnityEngine.Object.DestroyImmediate(data);
            if (panelObject != null) UnityEngine.Object.DestroyImmediate(panelObject);
            if (root != null) PrefabUtility.UnloadPrefabContents(root);
        }
        string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "output/tower-emblem-atlas-v1/Unity_Integration_Validation.txt");
        File.WriteAllText(path, result);
    }

    private static Image NewImage(Transform parent)
    {
        var go = new GameObject("Test image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static void Verify(TowerVisual visual, SpriteRenderer face, Sprite background, Sprite emblem, Color tint, int id)
    {
        Check(visual.TierSprite != null, "Tier missing " + id);
        Check(visual.EmblemSprite == emblem, "Emblem reference mismatch " + id);
        Check(SameColor(visual.EmblemColor, tint), "Tint mismatch " + id);
        Check(face.sprite == background && SameColor(face.color, Color.white), "Face mismatch " + id);
    }
}
#endif
