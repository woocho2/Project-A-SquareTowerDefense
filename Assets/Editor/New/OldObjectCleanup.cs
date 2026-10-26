using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 옛 스크립트를 지운 뒤 한 번만 돌리는 정리 도구. 돌리고 나면 이 파일은 지움
    public static class OldObjectCleanup
    {
        private const string ScenePath = "Assets/1. Scenes/3. Stage1.unity";
        private const string CanvasPath = "Assets/2. Prefab/Canvas.prefab";
        private const string TowerPath = "Assets/2. Prefab/2. Tower/Tower.prefab";
        private const string OldZoneChild = "DebuffZone";

        [MenuItem("Tools/옛 오브젝트 정리 (한 번만)")]
        private static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CleanPrefab(CanvasPath, null);
            CleanPrefab(TowerPath, OldZoneChild);
            CleanScene();

            AssetDatabase.SaveAssets();
            Debug.LogWarning("[Cleanup] 끝. Stage1을 플레이해서 확인한 뒤 Claude에게 알려 주세요 (이 도구를 지움)");
        }

        private static void CleanScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (IsOnlyOldStuff(root))
                {
                    Debug.Log($"[Cleanup] 씬: 옛 오브젝트 삭제 — {root.name}");
                    Object.DestroyImmediate(root);
                }
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = t.gameObject;
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (missing == 0) continue;

                    if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsAddedGameObjectOverride(go))
                    {
                        Debug.LogWarning($"[Cleanup] 씬: {go.name}의 빈 스크립트 {missing}개는 프리팹 안에 있음 ({PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)})");
                        continue;
                    }

                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    Debug.Log($"[Cleanup] 씬: {go.name}에서 빈 스크립트 {missing}개 제거");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // 빈(지워진) 스크립트가 있고, 살아 있는 스크립트는 하나도 없는 오브젝트 = 옛 게임 것
        private static bool IsOnlyOldStuff(GameObject root)
        {
            int missing = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            }
            if (missing == 0) return false;

            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null) return false;
            }
            return true;
        }

        private static void CleanPrefab(string path, string removeChildName)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (removeChildName != null)
                {
                    Transform child = contents.transform.Find(removeChildName);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                        Debug.Log($"[Cleanup] {path}: 옛 자식 {removeChildName} 삭제");
                    }
                }

                var targets = new List<GameObject>();
                foreach (Transform t in contents.GetComponentsInChildren<Transform>(true)) targets.Add(t.gameObject);
                foreach (GameObject go in targets)
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (missing == 0) continue;

                    if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsAddedGameObjectOverride(go))
                    {
                        Debug.LogWarning($"[Cleanup] {path}: {go.name}의 빈 스크립트 {missing}개는 안쪽 프리팹에 있음 ({PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)})");
                        continue;
                    }

                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    Debug.Log($"[Cleanup] {path}: {go.name}에서 빈 스크립트 {missing}개 제거");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
