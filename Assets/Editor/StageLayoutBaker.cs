using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD.EditorTools
{
    // 열린 스테이지 씬의 판 배치(길·타워 칸)를 그 스테이지 에셋에 저장 — 메뉴 씬 카드의 판 미리보기용 (색은 에셋의 월드 색)
    public static class StageLayoutBaker
    {
        [MenuItem("Tools/SquareTD/판 배치를 스테이지 에셋에 저장")]
        private static void Bake()
        {
            BoardManager board = Object.FindAnyObjectByType<BoardManager>();
            GameSession session = Object.FindAnyObjectByType<GameSession>();
            if (board == null || session == null)
            {
                Debug.LogError("[StageLayoutBaker] 스테이지 씬을 열고 누르세요 (BoardManager·GameSession이 없음)");
                return;
            }

            var sessionObject = new SerializedObject(session);
            var stage = sessionObject.FindProperty("m_stage").objectReferenceValue as StageDefinition;
            if (stage == null) stage = Selection.activeObject as StageDefinition;
            if (stage == null)
            {
                Debug.LogError("[StageLayoutBaker] GameSession의 Stage 칸을 채우거나, 프로젝트 창에서 스테이지 에셋을 고른 뒤 누르세요");
                return;
            }

            var boardObject = new SerializedObject(board);
            var pathTilemap = boardObject.FindProperty("m_pathTilemap").objectReferenceValue as Tilemap;
            var spawnTilemap = boardObject.FindProperty("m_spawnTilemap").objectReferenceValue as Tilemap;
            SerializedProperty pathProperty = boardObject.FindProperty("m_pathCells");

            var path = new List<Vector2Int>();
            for (int i = 0; i < pathProperty.arraySize; i++)
            {
                Vector3Int cell = pathProperty.GetArrayElementAtIndex(i).vector3IntValue;
                path.Add(new Vector2Int(cell.x, cell.y));
            }

            var spawn = new List<Vector2Int>();
            if (spawnTilemap != null)
            {
                BoundsInt bounds = spawnTilemap.cellBounds;
                foreach (Vector3Int cell in bounds.allPositionsWithin)
                {
                    if (spawnTilemap.HasTile(cell)) spawn.Add(new Vector2Int(cell.x, cell.y));
                }
            }

            if (path.Count == 0)
            {
                Debug.LogError("[StageLayoutBaker] BoardManager의 길 칸이 비어 있음");
                return;
            }

            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (Vector2Int cell in path) Grow(ref min, ref max, cell);
            foreach (Vector2Int cell in spawn) Grow(ref min, ref max, cell);

            Undo.RecordObject(stage, "판 배치 저장");
            stage.SetLayout(min, max - min + Vector2Int.one, path, spawn);
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageLayoutBaker] {stage.name}: 길 {path.Count}칸, 타워 칸 {spawn.Count}칸, 판 {max.x - min.x + 1}×{max.y - min.y + 1}");
        }

        private static void Grow(ref Vector2Int min, ref Vector2Int max, Vector2Int cell)
        {
            min = Vector2Int.Min(min, cell);
            max = Vector2Int.Max(max, cell);
        }
    }
}
