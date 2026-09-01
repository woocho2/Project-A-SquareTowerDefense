#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TilePath))]
public class TilePathEditor : Editor
{
    private void OnSceneGUI()
    {
        TilePath path = (TilePath)target;
        Tilemap tilemap = path.GetComponent<Tilemap>();
        if (tilemap == null) return;

        Event currentEvent = Event.current;

        // 1. [Shift + 좌클릭]: 경로에 타일 추가
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.shift)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(ray.origin);

            // 실제 타일이 존재하는 셀인지 확인
            if (tilemap.HasTile(gridPos))
            {
                // 중복 등록 방지
                if (!path.pathGridPositions.Contains(gridPos))
                {
                    Undo.RecordObject(path, "Add Path Tile");
                    path.pathGridPositions.Add(gridPos);
                    EditorUtility.SetDirty(path);
                }
            }
            currentEvent.Use(); // 이벤트 소비 (타 오브젝트 선택 방지)
        }

        // 2. [Ctrl + 좌클릭]: 클릭한 타일을 경로에서 제거
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && currentEvent.control)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(ray.origin);

            if (path.pathGridPositions.Contains(gridPos))
            {
                Undo.RecordObject(path, "Remove Path Tile");
                path.pathGridPositions.Remove(gridPos);
                EditorUtility.SetDirty(path);
            }
            currentEvent.Use();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TilePath path = (TilePath)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("단축키 안내:\n- Shift + 좌클릭: 경로 타일 순서대로 추가\n- Ctrl + 좌클릭: 선택 타일 경로에서 삭제", MessageType.Info);

        // 마지막으로 추가된 타일 1개 취소 버튼
        if (GUILayout.Button("마지막 타일 삭제 (Undo)"))
        {
            if (path.pathGridPositions.Count > 0)
            {
                Undo.RecordObject(path, "Remove Last Tile");
                path.pathGridPositions.RemoveAt(path.pathGridPositions.Count - 1);
                EditorUtility.SetDirty(path);
            }
        }

        // 전체 초기화 버튼
        if (GUILayout.Button("경로 전체 초기화"))
        {
            if (EditorUtility.DisplayDialog("경로 초기화", "등록된 모든 경로를 지우시겠습니까?", "예", "아니오"))
            {
                Undo.RecordObject(path, "Clear All Path");
                path.pathGridPositions.Clear();
                EditorUtility.SetDirty(path);
            }
        }
    }
}
#endif