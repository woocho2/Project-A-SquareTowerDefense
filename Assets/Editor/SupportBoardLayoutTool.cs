using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    public static class SupportBoardLayoutTool
    {
        private const string Output = "ArtPreviews/SupportBoards_20261117";
        private const string Tiles = "Assets/4. Asset/1. BackGround/Tiles/BoardFrame/";

        private static Tilemap Find(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true)).Single(map => map.name == name);

        [MenuItem("Tools/SquareTD/대기석·시너지 받침판 맞추기")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply the support boards in Edit mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/1. Scenes/3. Stage1.unity") throw new InvalidOperationException("Open 3. Stage1 before applying this layout.");
            Tilemap seat = Find(scene, "Wating Seat");
            Tilemap synergy = Find(scene, "Synergy");
            Tilemap seatBoard = Find(scene, "SeatBoard");
            Tilemap synergyBoard = Find(scene, "SynergyBoard");
            string seatBefore = Fingerprint(seat), synergyBefore = Fingerprint(synergy);
            string backup = "ArtBackups/SupportBoards_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupScene = Path.Combine(backup, scene.path);
            Directory.CreateDirectory(Path.GetDirectoryName(backupScene));
            File.Copy(scene.path, backupScene);
            File.Copy(scene.path + ".meta", backupScene + ".meta");
            Capture(scene, "before.png");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Fit seat and synergy support boards");
            Undo.RegisterFullObjectHierarchyUndo(seatBoard.gameObject, "Seat support board");
            Undo.RegisterFullObjectHierarchyUndo(synergyBoard.gameObject, "Synergy support board");
            Rect seatRect = SlotBounds(seat, new Vector2(0.85f, 0.65f));
            Rect synergyRect = SlotBounds(synergy, new Vector2(0.75f, 0.75f));
            synergyRect.yMin -= 0.35f;
            Fit(seatBoard, seat, seatRect);
            Fit(synergyBoard, synergy, synergyRect);
            if (seatBefore != Fingerprint(seat) || synergyBefore != Fingerprint(synergy))
                throw new InvalidOperationException("Slot tiles or positions changed.");
            Undo.CollapseUndoOperations(undo);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
            Capture(scene, "after.png");
            Write("status.json", JsonUtility.ToJson(new Status
            {
                state = "applied", scene = scene.path, backup = backup,
                slotTilesUnchanged = true, sceneSaved = true, playModeVerified = false,
                seatBounds = seatRect, synergyBounds = synergyRect,
                maps = new[] { seat, synergy, seatBoard, synergyBoard }.Select(map =>
                    new MapInfo { name = map.name, cells = Count(map), position = map.transform.position }).ToArray()
            }, true));
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
            Debug.Log("[SupportBoards] Existing nine frame tiles fitted behind the bench and synergy slots. Gameplay and UI controls unchanged.");
        }

        private static IEnumerable<Vector3Int> Cells(Tilemap map)
        {
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                if (map.HasTile(cell)) yield return cell;
        }

        private static int Count(Tilemap map) => Cells(map).Count();

        private static string Fingerprint(Tilemap map) => map.transform.localToWorldMatrix.ToString("R") + "|" +
            string.Join(";", Cells(map)
                .Select(cell => cell + ":" + AssetDatabase.GetAssetPath(map.GetTile(cell)) + ":" + map.GetTransformMatrix(cell).ToString("R")));

        internal static Rect SlotBounds(Tilemap map, Vector2 padding)
        {
            Vector3[] centers = Cells(map).Select(map.GetCellCenterWorld).ToArray();
            if (centers.Length == 0) throw new InvalidOperationException(map.name + " has no slots.");
            return Rect.MinMaxRect(centers.Min(p => p.x) - padding.x, centers.Min(p => p.y) - padding.y,
                centers.Max(p => p.x) + padding.x, centers.Max(p => p.y) + padding.y);
        }

        internal static void Fit(Tilemap map, Tilemap slots, Rect visible)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/4. Asset/1. BackGround/BoardFrameWhiteNeon.mat");
            if (material == null) throw new InvalidOperationException("Board frame material missing.");
            foreach (var component in map.GetComponents<SpecialTileGlowEffect>()) Undo.DestroyObjectImmediate(component);
            foreach (var component in map.GetComponents<ZoneGlowEffect>()) Undo.DestroyObjectImmediate(component);
            map.ClearAllTiles();
            map.color = Color.white;
            map.tileAnchor = new Vector3(0.5f, 0.5f, 0);
            var renderer = map.GetComponent<TilemapRenderer>();
            var slotRenderer = slots.GetComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = slotRenderer.sortingLayerID;
            renderer.sortingOrder = slotRenderer.sortingOrder - 2;
            const float cap = 0.6f;
            Vector4 uv = material.GetVector("_PlateUvBounds");
            float left = visible.xMin - uv.x * 3f * cap;
            float bottom = visible.yMin - uv.y * 3f * cap;
            float right = visible.xMax + (1f - uv.z) * 3f * cap;
            float top = visible.yMax + (1f - uv.w) * 3f * cap;
            float[] widths = { cap, right - left - 2f * cap, cap };
            float[] heights = { cap, top - bottom - 2f * cap, cap };
            string[,] names = { { "BL", "L", "TL" }, { "B", "C", "T" }, { "BR", "R", "TR" } };
            float x = left;
            for (int column = 0; column < 3; column++)
            {
                float y = bottom;
                for (int row = 0; row < 3; row++)
                {
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(Tiles + "Tile_Frame_" + names[column, row] + ".asset");
                    if (tile == null || tile.sprite == null) throw new InvalidOperationException("Missing frame piece: " + names[column, row]);
                    var cell = new Vector3Int(column, row, 0);
                    map.SetTile(cell, tile);
                    map.SetTileFlags(cell, TileFlags.None);
                    Vector3 center = map.transform.InverseTransformPoint(new Vector3(x + widths[column] * 0.5f, y + heights[row] * 0.5f, map.transform.position.z));
                    Vector3 size = map.transform.InverseTransformVector(new Vector3(widths[column], heights[row], 1));
                    Vector3 spriteSize = tile.sprite.bounds.size;
                    map.SetTransformMatrix(cell, Matrix4x4.TRS(center - map.GetCellCenterLocal(cell), Quaternion.identity,
                        new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1)));
                    y += heights[row];
                }
                x += widths[column];
            }
            map.CompressBounds();
            map.RefreshAllTiles();
            EditorUtility.SetDirty(map);
            EditorUtility.SetDirty(renderer);
        }

        private static void Capture(Scene source, string filename)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            RenderTexture target = null;
            Texture2D pixels = null;
            RenderTexture previous = RenderTexture.active;
            Camera camera = null;
            GameObject[] roots = source.GetRootGameObjects();
            var globalLights = roots.SelectMany(root => root.GetComponentsInChildren<Light2D>(true))
                .Where(light => light.lightType == Light2D.LightType.Global && light.enabled).ToArray();
            var enabledGlobals = new HashSet<Light2D>(globalLights);
            try
            {
                foreach (Light2D light in globalLights) light.enabled = false;
                foreach (GameObject root in roots)
                {
                    GameObject clone = Object.Instantiate(root);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                    Light2D[] originalLights = root.GetComponentsInChildren<Light2D>(true);
                    Light2D[] cloneLights = clone.GetComponentsInChildren<Light2D>(true);
                    for (int i = 0; i < originalLights.Length; i++)
                        if (enabledGlobals.Contains(originalLights[i])) cloneLights[i].enabled = true;
                }
                camera = preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
                camera.scene = preview;
                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                camera.aspect = 16f / 9f;
                foreach (Canvas canvas in preview.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>()))
                {
                    if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1;
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory(Output);
                File.WriteAllBytes(Path.Combine(Output, filename), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (camera != null) camera.targetTexture = null;
                if (pixels != null) Object.DestroyImmediate(pixels);
                if (target != null) Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(preview);
                foreach (Light2D light in globalLights) if (light != null) light.enabled = true;
            }
        }

        private static void Write(string file, string content)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, file), content);
        }

        [Serializable] private class MapInfo { public string name; public int cells; public Vector3 position; }
        [Serializable] private class Status
        {
            public string state, scene, backup;
            public bool slotTilesUnchanged, sceneSaved, playModeVerified;
            public Rect seatBounds, synergyBounds;
            public MapInfo[] maps;
        }
    }
}
