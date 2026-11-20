using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    internal static class PathTintVerification
    {
        private const string Folder = "ArtPreviews/RemovePathTint_20261120";
        private const string Key = "SquareTD.PathTintVerification";
        private const string StageOne = "Assets/1. Scenes/3. Stage1.unity";

        static PathTintVerification() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            bool started = SessionState.GetBool(Key, false);
            try
            {
                if (started)
                {
                    if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0))
                        throw new InvalidOperationException("Stage1 Play Mode did not start in time.");
                    if (!EditorApplication.isPlaying || TurnManager.Instance?.Current is not PlayerTurnState) return;
                    int count = Check(SceneManager.GetActiveScene());
                    File.AppendAllText(Folder + "/result.txt", $"Stage1 Play Mode: {count} path cells on the original board tint.\n");
                    ExitPlay();
                    return;
                }

                string request = Folder + "/request.txt";
                if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(request);
                var results = Directory.GetFiles("Assets/1. Scenes", "*Stage*.unity").OrderBy(path => path).Select(path =>
                {
                    Scene preview = EditorSceneManager.OpenPreviewScene(path.Replace('\\', '/'));
                    try { return $"{Path.GetFileName(path)}: {Check(preview)} path cells on the original board tint."; }
                    finally { EditorSceneManager.ClosePreviewScene(preview); }
                }).ToArray();
                if (results.Length != 10) throw new InvalidOperationException("Expected ten stage scenes.");
                File.WriteAllLines(Folder + "/result.txt", results);
                Scene active = SceneManager.GetActiveScene();
                if (active.path != StageOne || PrefabStageUtility.GetCurrentPrefabStage() != null
                    || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                {
                    File.AppendAllText(Folder + "/result.txt", "Play Mode skipped to preserve the editor's open work.\n");
                    return;
                }
                if (!PathCellsAreWhite(active, out _)) EditorSceneManager.OpenScene(StageOne);
                SessionState.SetBool(Key, true);
                SessionState.SetBool(Key + ".background", Application.runInBackground);
                SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 25);
                Application.runInBackground = true;
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                File.WriteAllText(Folder + "/error.txt", exception.ToString());
                if (started) ExitPlay();
            }
        }

        private static void ExitPlay()
        {
            SessionState.SetBool(Key, false);
            Application.runInBackground = SessionState.GetBool(Key + ".background", false);
            EditorApplication.isPlaying = false;
        }

        private static int Check(Scene scene)
        {
            if (!PathCellsAreWhite(scene, out int count) || count != 32)
                throw new InvalidOperationException($"{scene.name}: expected 32 path cells with a neutral background.");
            return count;
        }

        private static bool PathCellsAreWhite(Scene scene, out int count)
        {
            var maps = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Tilemap>(true)).ToArray();
            Tilemap path = maps.Single(map => map.name == "Path");
            Tilemap surface = maps.Single(map => map.name == "BoardTile");
            var all = new System.Collections.Generic.List<Vector3Int>();
            foreach (Vector3Int cell in path.cellBounds.allPositionsWithin) all.Add(cell);
            var cells = all.Where(path.HasTile).ToArray();
            count = cells.Length;
            return cells.Select(cell => surface.WorldToCell(path.GetCellCenterWorld(cell)))
                .All(cell => surface.HasTile(cell) && surface.GetColor(cell) == Color.white);
        }
    }
}
