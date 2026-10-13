using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AllTowerVisualCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void BindTable(SerializedObject target, string field, string prefab)
    {
        var rows = Regex.Match(prefab, @"(?m)^  " + field + @":\r?\n(?<rows>(?:  - \{[^\r\n]+\}\r?\n)+)");
        Check(rows.Success, "Missing prefab table: " + field);
        var refs = Regex.Matches(rows.Groups["rows"].Value, @"fileID: (\d+), guid: ([a-f0-9]+)");
        var property = target.FindProperty(field); property.arraySize = refs.Count;
        for (int i = 0; i < refs.Count; i++)
        {
            long fileId = long.Parse(refs[i].Groups[1].Value); string guid = refs[i].Groups[2].Value;
            var sprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>()
                .Single(s => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string actualGuid, out long id) && id == fileId && actualGuid == guid);
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
        }
    }
    static void BindSingle(SerializedObject target, string field, string prefab)
    {
        var match = Regex.Match(prefab, @"(?m)^  " + field + @": \{fileID: (\d+), guid: ([a-f0-9]+), type: \d+\}");
        Check(match.Success, "Missing prefab binding: " + field);
        string guid = match.Groups[2].Value;
        long fileId = long.Parse(match.Groups[1].Value);
        target.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))
            .Single(asset => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string actualGuid, out long actualId) && actualGuid == guid && actualId == fileId);
    }
    static SpriteRenderer Renderer(Transform root, string name, int order)
    {
        var child = new GameObject(name); child.transform.SetParent(root, false);
        var renderer = child.AddComponent<SpriteRenderer>(); renderer.sortingOrder = order; return renderer;
    }
    static void Regular(TowerVisual visual, SpriteRenderer face, SpriteRenderer frame, SpriteRenderer background,
        TowerCharacterCatalog catalog, int tier, int attack, int emblem, Material material)
    {
        Check(visual.UsesCharacterVisual && face.enabled && frame.enabled && !background.enabled, "Missing regular layers");
        Check(face.color == Color.white && frame.color == Color.white && face.sharedMaterial == material, "Tint/material leak");
        Check(Mathf.Abs(face.bounds.size.x - 1.2666667f) < .0001f && Mathf.Abs(frame.bounds.size.x - 1.2666667f) < .0001f, "Canvas alignment");
        Check(frame.sortingOrder < face.sortingOrder && frame.transform.localPosition == face.transform.localPosition, "Frame alignment/order");
        string[] tiers = { "Bronze", "Silver", "Gold", "Mithril", "Diamond" };
        Check(visual.TierSprite.name == "Tower_Frame_" + tiers[tier - 1], "Wrong tier frame");
        if (emblem == 1 || emblem == 2)
        {
            Check(visual.FrameRimSprite != null && visual.HeadgearSprite != null, "Missing Sword/Axe layers");
            Check(face.sprite.name.StartsWith(emblem == 1 ? "Tower_Sword_" : "Tower_Axe_"), "Wrong layered family");
        }
        else
        {
            Check(face.sprite == catalog.GetCharacter(emblem, attack), "Wrong complete portrait");
            Check(visual.FrameRimSprite == null && visual.HeadgearSprite == null, "Stale layered overlays");
        }
        var block = new MaterialPropertyBlock(); face.GetPropertyBlock(block);
        Check(block.GetVector("_HeadRect") == Vector4.zero && block.GetFloat("_CornerRadius") == 0, "Synergy mask leaked");
        Check(visual.ProjectileEmblemSprite != null && visual.ProjectileEmblemSprite != face.sprite, "Portrait leaked into projectiles");
    }
    public static void Run()
    {
        GameObject root = null;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string prefab = File.ReadAllText(Path.Combine(Application.dataPath, "TowerBase.prefab.txt"));
            root = new GameObject("TowerVisualCheck");
            var face = Renderer(root.transform, "Emblem", 10); var frame = Renderer(root.transform, "Tier", 9);
            var background = Renderer(root.transform, "Color", 8); Renderer(root.transform, "FrameRim", 11); Renderer(root.transform, "Headgear", 12);
            var defaultScale = new Vector3(1.04f, 1.03f, 1); frame.transform.localScale = defaultScale;
            var material = new Material(Shader.Find("Sprites/Default")); face.sharedMaterial = material;
            var visual = root.AddComponent<TowerVisual>(); var settings = new SerializedObject(visual);
            foreach (string field in new[] { "m_tierSprites", "m_emblemSprites", "m_swordCharacterSprites", "m_swordHeadgearSprites", "m_axeCharacterSprites", "m_axeHeadgearSprites", "m_characterTierSprites", "m_characterRimSprites" }) BindTable(settings, field, prefab);
            foreach (string field in new[] { "m_regularCharacters", "m_synergyCharacters", "m_backgroundSprite" }) BindSingle(settings, field, prefab);
            settings.ApplyModifiedPropertiesWithoutUndo();
            var catalog = Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog");
            var synergy = Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");
            Check(catalog != null && synergy != null, "Missing Resources catalogs");
            int checkedRegular = 0;
            for (int emblem = 1; emblem <= 14; emblem++) for (int tier = 1; tier <= 5; tier++) for (int attack = 1; attack <= 4; attack++)
            {
                int id = tier * 1000 + attack * 100 + emblem;
                visual.Apply(id); Regular(visual, face, frame, background, catalog, tier, attack, emblem, material);
                var data = ScriptableObject.CreateInstance<TowerData>(); data.towerID = id; data.InitializeIdentity();
                visual.Apply(data); Regular(visual, face, frame, background, catalog, tier, attack, emblem, material);
                UnityEngine.Object.DestroyImmediate(data); checkedRegular++;
            }
            Debug.Log("PASS " + checkedRegular + " regular visuals through integer and TowerData APIs, all frames and portrait/layer modes");
            string[] synergyNames = { "Odin", "Thor", "Loki", "Hel", "Surtr", "Freyr", "Baldr", "Skadi", "Freyja", "Ymir", "Ullr", "Brunhild", "BrokEitri", "Njord" };
            for (int i = 0; i < 14; i++)
            {
                int id = 6001 + i; visual.Apply(id);
                Check(visual.EmblemSprite == synergy.GetFront(id) && face.sprite.name == "Synergy_" + synergyNames[i] + "_Front_v3", "Wrong synergy identity " + id);
                Check(visual.UsesCharacterVisual && !frame.enabled && !background.enabled && visual.FrameRimSprite == null && visual.HeadgearSprite == null, "Synergy retained regular layers");
                Check(Mathf.Abs(face.bounds.size.x - 256f / 192f) < .0001f, "Wrong synergy bounds");
                var data = ScriptableObject.CreateInstance<TowerData>(); data.towerID = id; data.visualTowerID = 5105; data.InitializeIdentity();
                visual.Apply(data); Check(visual.EmblemSprite == synergy.GetFront(id), "Logical synergy ID lost to visual override");
                UnityEngine.Object.DestroyImmediate(data);
                visual.Apply(1103); Regular(visual, face, frame, background, catalog, 1, 1, 3, material);
                visual.Apply(1202); Regular(visual, face, frame, background, catalog, 1, 2, 2, material);
            }
            var overrideData = ScriptableObject.CreateInstance<TowerData>(); overrideData.towerID = 1103; overrideData.visualTowerID = 5411; overrideData.InitializeIdentity();
            visual.Apply(overrideData); Regular(visual, face, frame, background, catalog, 5, 4, 11, material); UnityEngine.Object.DestroyImmediate(overrideData);
            var emptyCatalog = ScriptableObject.CreateInstance<TowerCharacterCatalog>(); settings.Update(); settings.FindProperty("m_regularCharacters").objectReferenceValue = emptyCatalog; settings.ApplyModifiedPropertiesWithoutUndo();
            visual.Apply(1103); Check(!visual.UsesCharacterVisual && background.enabled && visual.ColorSprite != null && frame.enabled && visual.FrameRimSprite == null && visual.HeadgearSprite == null, "Missing art fallback broken");
            Check(frame.transform.localScale == defaultScale && face.sharedMaterial == material, "Legacy state not restored");
            UnityEngine.Object.DestroyImmediate(emptyCatalog);
            Debug.Log("PASS 14 v3 synergies including Njord, logical/visual IDs, synergy-to-normal/Sword/Axe material and mask restoration, missing-art fallback, projectile identity");
            TowerRuntimeCheck.Validate();
            UnityEngine.Object.DestroyImmediate(root); root = null; EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); if (root != null) UnityEngine.Object.DestroyImmediate(root); EditorApplication.Exit(1); }
    }
}
