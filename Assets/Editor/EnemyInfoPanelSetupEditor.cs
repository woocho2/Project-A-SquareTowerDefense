#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyInfoPanelSetupEditor
{
    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying)
            {
                SetupEnemyInfoPanel();
            }
        };
    }

    [MenuItem("Tools/Setup EnemyInfoPanel in Stage1")]
    public static void SetupEnemyInfoPanel()
    {
        string targetScenePath = "Assets/1. Scenes/3. Stage1.unity";
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.path != targetScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            currentScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
        }

        GameObject enemyPanel = GameObject.Find("EnemyInfoPanel");
        if (enemyPanel == null)
        {
            Debug.LogError("[EnemyInfoPanelSetup] EnemyInfoPanel을 찾을 수 없습니다.");
            return;
        }

        if (!enemyPanel.TryGetComponent(out EnemyInfoPanelUI enemyUI))
        {
            enemyUI = enemyPanel.AddComponent<EnemyInfoPanelUI>();
            Debug.Log("[EnemyInfoPanelSetup] EnemyInfoPanel에 EnemyInfoPanelUI 컴포넌트 추가 완료.");
        }

        enemyUI.InitializeComponents();

        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            SerializedObject so = new SerializedObject(uiManager);
            SerializedProperty enemyPanelProp = so.FindProperty("m_enemyInfoPanel");
            SerializedProperty enemyUIProp = so.FindProperty("m_enemyInfoPanelUI");

            if (enemyPanelProp != null) enemyPanelProp.objectReferenceValue = enemyPanel;
            if (enemyUIProp != null) enemyUIProp.objectReferenceValue = enemyUI;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiManager);
            Debug.Log("[EnemyInfoPanelSetup] UIManager에 m_enemyInfoPanel 및 m_enemyInfoPanelUI 연결 완료.");
        }

        EditorUtility.SetDirty(enemyPanel);
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);
        Debug.Log("[EnemyInfoPanelSetup] Stage1 씬 저장 완료!");
    }
}
#endif
