#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(SharedBoardVisuals))]
public sealed class SharedBoardVisualsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("배경·장식 이미지는 공용입니다. 색상은 아래 설정 에셋에 저장되며 생성 시 자동 적용됩니다.", MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("현재 씬에 적용")) SharedBoardVisualPreview.Apply();
        if (GUILayout.Button("색상 편집 창 열기")) SharedBoardVisualsWindow.Open();
    }
}

public sealed class SharedBoardVisualsWindow : EditorWindow
{
    private SerializedObject m_serialized;
    private SharedBoardVisuals m_settings;
    private Vector2 m_scroll;
    private int m_tab;
    private static readonly string[] Tabs = { "공용 이미지", "스테이지", "타워", "몬스터" };

    [MenuItem("Tools/Visuals/공용 배경·장식 색상")]
    public static void Open() => GetWindow<SharedBoardVisualsWindow>("공용 비주얼 색상");

    private void OnEnable() => SetSettings(SharedBoardVisuals.Default);
    private void SetSettings(SharedBoardVisuals settings)
    {
        m_settings = settings;
        m_serialized = settings != null ? new SerializedObject(settings) : null;
    }
    private void OnGUI()
    {
        SharedBoardVisuals selected = (SharedBoardVisuals)EditorGUILayout.ObjectField("색상 설정", m_settings, typeof(SharedBoardVisuals), false);
        if (selected != m_settings) SetSettings(selected);
        if (m_serialized == null) return;
        EditorGUILayout.HelpBox("색상 변경은 자동 저장됩니다. 타일맵은 TileManager의 Stage Realm을 기준으로 적용합니다. 개별 타워 ID / 몬스터 캐릭터 설정은 기본 색상보다 우선합니다.", MessageType.Info);
        m_tab = GUILayout.Toolbar(m_tab, Tabs);
        m_serialized.Update();
        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
        switch (m_tab)
        {
            case 0:
                Field("m_background", "공용 배경"); Field("m_decoration", "공용 장식"); Field("m_arrow", "공용 화살표");
                Field("m_backgroundTile", "배경 타일"); Field("m_decorationTile", "장식 타일"); Field("m_arrowTile", "화살표 타일");
                break;
            case 1: Field("m_stages", "스테이지별 색상"); break;
            case 2:
                Field("m_tiers", "티어별 기본 색상"); Field("m_towers", "타워 ID별 덮어쓰기");
                break;
            case 3:
                Field("m_enemyStates", "면 유형별 기본 색상"); Field("m_enemies", "캐릭터별 면 색상");
                break;
        }
        EditorGUILayout.EndScrollView();
        if (m_serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(m_settings);
            AssetDatabase.SaveAssetIfDirty(m_settings);
            SharedBoardVisualPreview.Queue();
        }
        if (GUILayout.Button("현재 씬에 적용")) SharedBoardVisualPreview.Apply();
        if (GUILayout.Button("설정 에셋 선택")) Selection.activeObject = m_settings;
    }
    private void Field(string property, string label) =>
        EditorGUILayout.PropertyField(m_serialized.FindProperty(property), new GUIContent(label), true);
}

[InitializeOnLoad]
public static class SharedBoardVisualPreview
{
    private static bool s_queued;
    static SharedBoardVisualPreview()
    {
        SharedBoardVisuals.Changed += Queue;
        Undo.undoRedoPerformed += Queue;
    }
    public static void Queue()
    {
        if (s_queued) return;
        s_queued = true;
        EditorApplication.delayCall += () => { s_queued = false; Apply(); };
    }
    public static void Apply()
    {
        foreach (TileManager manager in Resources.FindObjectsOfTypeAll<TileManager>())
        {
            if (!manager.gameObject.scene.IsValid() || EditorUtility.IsPersistent(manager)) continue;
            manager.ApplySharedVisuals();
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
        foreach (TowerVisual visual in Resources.FindObjectsOfTypeAll<TowerVisual>())
            if (visual.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(visual)) visual.RefreshSharedVisuals();
        foreach (EnemyCubeMeshVisual visual in Resources.FindObjectsOfTypeAll<EnemyCubeMeshVisual>())
            if (visual.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(visual)) visual.RefreshSharedVisuals();
        SceneView.RepaintAll();
    }
}
#endif
