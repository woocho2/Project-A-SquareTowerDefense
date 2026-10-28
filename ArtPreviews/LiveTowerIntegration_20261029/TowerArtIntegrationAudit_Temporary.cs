using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class TowerArtIntegrationAudit_Temporary
{
    [Serializable] public class FileEntry
    {
        public int emblem;
        public string group;
        public string source;
        public string target;
        public string guid;
        public string sourceSha256;
    }
    [Serializable] public class Plan { public FileEntry[] files; }
    [Serializable] public class Row
    {
        public string group;
        public string assetPath;
        public int importedSpriteCount;
        public int catalogRoles;
        public bool sourceHashMatches;
    }
    [Serializable] public class Result
    {
        public bool success;
        public string unityVersion;
        public int importedAtlases;
        public int fullCharacterReferences;
        public int boardCharacterReferences;
        public int prefabTierVariants;
        public bool runtimeLogicChanged;
        public bool enteredPlayMode;
        public string error;
        public List<Row> files = new List<Row>();
        public List<string> warnings = new List<string>();
    }

    public static void Run()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string output = Path.Combine(root, "ArtPreviews/LiveTowerIntegration_20261029/Unity_Audit.json");
        var result = new Result { unityVersion = Application.unityVersion };
        GameObject instance = null;
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Warning && result.warnings.Count < 40) result.warnings.Add(message);
        };
        Application.logMessageReceived += capture;
        try
        {
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(root,
                "ArtPreviews/LiveTowerIntegration_20261029/Preflight.json")));
            Require(plan != null && plan.files != null && plan.files.Length == 14, "Expected 14 live atlases.");
            foreach (var entry in plan.files)
                AssetDatabase.ImportAsset(entry.target, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<SquareTD.TowerCharacterCatalog>("TowerCharacterCatalog");
            Require(catalog != null, "Resources TowerCharacterCatalog missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2. Prefab/1. Tower.prefab");
            Require(prefab != null, "Production tower prefab missing.");
            instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            var visual = instance.GetComponentInChildren<SquareTD.TowerVisual>(true);
            Require(visual != null, "Production prefab has no TowerVisual.");
            var character = instance.transform.Find("Character").GetComponent<SpriteRenderer>();
            var background = instance.transform.Find("Background").GetComponent<SpriteRenderer>();
            var decoration = instance.transform.Find("Decoration").GetComponent<SpriteRenderer>();
            Require(background.sprite != null && decoration.sprite != null, "Production tower tile sprites missing.");

            foreach (var entry in plan.files)
            {
                Require(AssetDatabase.AssetPathToGUID(entry.target) == entry.guid, "GUID changed: " + entry.group);
                var importer = AssetImporter.GetAtPath(entry.target) as TextureImporter;
                Require(importer != null && importer.spriteImportMode == SpriteImportMode.Multiple,
                    "Sprite Multiple importer missing: " + entry.group);
                Require(Mathf.Approximately(importer.spritePixelsPerUnit, 512f), "Wrong PPU: " + entry.group);
                Require(importer.filterMode == FilterMode.Bilinear, "Wrong filter: " + entry.group);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.target);
                Require(texture != null && texture.width == 1024 && texture.height == 1024,
                    "Wrong imported atlas size: " + entry.group);
                string hash;
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(Path.Combine(root, entry.target)))
                    hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                Require(hash == entry.sourceSha256, "Production image differs from selected artwork: " + entry.group);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(entry.target).OfType<Sprite>().ToArray();
                Require(sprites.Length == (entry.emblem == 1 ? 8 : 4), "Unexpected sprite count: " + entry.group);
                var row = new Row { group = entry.group, assetPath = entry.target,
                    importedSpriteCount = sprites.Length, sourceHashMatches = true };
                for (int role = 1; role <= 4; role++)
                {
                    var full = catalog.GetCharacter(entry.emblem, role);
                    Require(full != null, "Missing character: " + entry.group + "/" + role);
                    CheckSprite(full, entry.guid, 52000 + entry.emblem * 10 + role - 1);
                    Require(full.rect.width == 512 && full.rect.height == 512,
                        "Wrong full-body slice: " + full.name);
                    result.fullCharacterReferences++;
                    var board = catalog.GetTowerCharacter(entry.emblem, role);
                    Require(board != null, "Missing board sprite: " + entry.group + "/" + role);
                    CheckSprite(board, entry.guid, 52000 + entry.emblem * 10 + role - 1 + (entry.emblem == 1 ? 4 : 0));
                    result.boardCharacterReferences++;
                    for (int tier = 1; tier <= 5; tier++)
                    {
                        int id = tier * 1000 + role * 100 + entry.emblem;
                        visual.Apply(id);
                        Require(visual.UsesCharacterVisual && visual.CharacterSprite == board
                            && visual.IllustrationSprite == full, "Prefab uses wrong artwork for tower " + id);
                        Require(character.enabled && character.sprite == board && character.color == Color.white,
                            "Character renderer invalid for tower " + id);
                        var scale = character.transform.localScale;
                        Require(scale.x > 0 && scale.y > 0 && !float.IsNaN(scale.x) && !float.IsNaN(scale.y),
                            "Character scale invalid for tower " + id);
                        result.prefabTierVariants++;
                    }
                    row.catalogRoles++;
                }
                result.files.Add(row);
                result.importedAtlases++;
            }
            Require(result.fullCharacterReferences == 56 && result.boardCharacterReferences == 56
                && result.prefabTierVariants == 280, "Coverage incomplete.");
            result.success = true;
        }
        catch (Exception exception)
        {
            result.error = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            Application.logMessageReceived -= capture;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
        }
        Debug.Log("[TowerArtIntegration] " + (result.success ? "PASS" : "FAIL")
            + " atlases=" + result.importedAtlases + " roles=" + result.fullCharacterReferences
            + " prefab variants=" + result.prefabTierVariants);
        EditorApplication.Exit(result.success ? 0 : 1);
    }

    private static void CheckSprite(Sprite sprite, string expectedGuid, long expectedId)
    {
        string guid;
        long id;
        Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out id),
            "Sprite ID lookup failed: " + sprite.name);
        Require(guid == expectedGuid && id == expectedId,
            "Wrong sprite reference: " + sprite.name + " GUID=" + guid + " ID=" + id);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
