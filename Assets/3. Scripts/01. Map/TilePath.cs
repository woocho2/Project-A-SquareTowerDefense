using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilePath : MonoBehaviour
{
    [Tooltip("에디터에서 등록된 경로 타일 그리드 좌표 리스트")]
    public List<Vector3Int> pathGridPositions = new List<Vector3Int>();

    private Tilemap tilemap;

    // 경로의 마지막 인덱스 (자동 계산: 등록된 타일 개수 - 1)
    public int LastIndex => pathGridPositions.Count > 0 ? pathGridPositions.Count - 1 : 0;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    // 인덱스 번호에 해당하는 타일의 월드 중앙 좌표 반환
    public Vector3 GetWorldPosition(int index)
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (pathGridPositions.Count == 0) return transform.position;

        // 인덱스가 범위를 벗어나지 않도록 클램프 처리
        int clampedIndex = Mathf.Clamp(index, 0, LastIndex);
        return tilemap.GetCellCenterWorld(pathGridPositions[clampedIndex]);
    }

    // 특정 인덱스의 그리드 좌표 반환 (타일 속성 체크용)
    public Vector3Int GetGridPosition(int index)
    {
        if (pathGridPositions.Count == 0) return Vector3Int.zero;
        int clampedIndex = Mathf.Clamp(index, 0, LastIndex);
        return pathGridPositions[clampedIndex];
    }

    // 씬 뷰 시각화
    private void OnDrawGizmos()
    {
        if (pathGridPositions == null || pathGridPositions.Count == 0) return;

        Tilemap currentTilemap = GetComponent<Tilemap>();
        if (currentTilemap == null) return;

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        for (int i = 0; i < pathGridPositions.Count; i++)
        {
            Vector3 worldPos = currentTilemap.GetCellCenterWorld(pathGridPositions[i]);
            UnityEditor.Handles.Label(worldPos + new Vector3(-0.1f, 0.1f, 0), i.ToString(), style);

            if (i < pathGridPositions.Count - 1)
            {
                Vector3 nextWorldPos = currentTilemap.GetCellCenterWorld(pathGridPositions[i + 1]);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(worldPos, nextWorldPos);
            }
        }
#endif
    }
}