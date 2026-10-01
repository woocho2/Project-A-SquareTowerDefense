#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TileManager))]
public class TileManagerEditor : Editor
{
    private const string PathPropertyName = "m_pathGridPositions";

    private void OnSceneGUI()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 ||
            (!currentEvent.shift && !currentEvent.control)) return;

        TileManager manager = (TileManager)target;
        Tilemap tilemap = manager.GetComponent<Tilemap>();
        if (tilemap == null) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        Plane tilePlane = new Plane(tilemap.transform.forward, tilemap.transform.position);
        if (!tilePlane.Raycast(ray, out float distance)) return;

        Vector3Int cell = tilemap.WorldToCell(ray.GetPoint(distance));
        if (!tilemap.HasTile(cell)) return;

        serializedObject.Update();
        SerializedProperty path = serializedObject.FindProperty(PathPropertyName);
        if (path == null) return;

        int existingIndex = FindCellIndex(path, cell);
        if (currentEvent.shift && existingIndex < 0)
        {
            Undo.RecordObject(manager, "Add Path Tile");
            int newIndex = path.arraySize;
            path.InsertArrayElementAtIndex(newIndex);
            path.GetArrayElementAtIndex(newIndex).vector3IntValue = cell;
            serializedObject.ApplyModifiedProperties();
        }
        else if (currentEvent.control && !currentEvent.shift && existingIndex >= 0)
        {
            Undo.RecordObject(manager, "Remove Path Tile");
            path.DeleteArrayElementAtIndex(existingIndex);
            serializedObject.ApplyModifiedProperties();
        }

        currentEvent.Use();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "경로 편집: Shift + 좌클릭으로 경로 끝에 타일 추가, Ctrl + 좌클릭으로 경로에서 타일 제거",
            MessageType.Info);

        serializedObject.Update();
        SerializedProperty path = serializedObject.FindProperty(PathPropertyName);
        if (path == null) return;

        if (GUILayout.Button("마지막 경로 타일 제거") && path.arraySize > 0)
        {
            Undo.RecordObject(target, "Remove Last Path Tile");
            path.DeleteArrayElementAtIndex(path.arraySize - 1);
            serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("경로 전체 초기화") && path.arraySize > 0 &&
            EditorUtility.DisplayDialog("경로 초기화", "등록된 경로 타일을 모두 지우시겠습니까?", "예", "아니요"))
        {
            Undo.RecordObject(target, "Clear Path Tiles");
            path.ClearArray();
            serializedObject.ApplyModifiedProperties();
        }
    }

    private static int FindCellIndex(SerializedProperty path, Vector3Int cell)
    {
        for (int i = 0; i < path.arraySize; i++)
        {
            if (path.GetArrayElementAtIndex(i).vector3IntValue == cell) return i;
        }

        return -1;
    }
}
#endif
