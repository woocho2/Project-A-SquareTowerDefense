using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD
{
    // 길 칸의 화살표 타일을 다음 인덱스 칸 쪽으로 돌림
    [RequireComponent(typeof(Tilemap))]
    public class PathDirectionArrows : MonoBehaviour
    {
        private void Start()
        {
            Tilemap tilemap = GetComponent<Tilemap>();
            BoardGrid grid = BoardManager.Instance.Grid;

            for (int i = 0; i < grid.PathCount; i++)
            {
                Vector3Int cell = grid.GetPathCell(i);
                if (!tilemap.HasTile(cell)) continue;
                
                Vector3Int next = grid.GetPathCell(i + 1);

                Vector3Int dir = next - cell;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Matrix4x4 rotation = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle));

                tilemap.SetTileFlags(cell, TileFlags.None);                
                tilemap.SetTransformMatrix(cell, rotation);
            }
        }
    }
}
