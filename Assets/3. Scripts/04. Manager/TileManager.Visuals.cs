using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public partial class TileManager
{
    /// <summary>배치 좌표와 경로 인덱스는 유지하고 공용 타일과 색상만 적용합니다.</summary>
    public void ApplySharedVisuals()
    {
        SharedBoardVisuals visuals = m_sharedVisuals != null ? m_sharedVisuals : SharedBoardVisuals.Default;
        if (visuals == null) return;
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tilemap == null || m_towerSpawnTilemap == null) return;
        StageBoardPalette colors = visuals.GetStage(m_stageRealm);
        if (colors == null) return;

        RestorePathArrowColors();
        Transform board = m_towerSpawnTilemap.transform.parent;
        if (m_pathDirectionTilemap == null) m_pathDirectionTilemap = FindBoardTilemap(board, "Path Direction");
        if (m_towerDecorationTilemap == null) m_towerDecorationTilemap = FindBoardTilemap(board, "TowerSpawn Decoration");
        if (m_pathArrowTilemap == null) m_pathArrowTilemap = FindBoardTilemap(board, "Path Arrows");
        if (m_pathArrowTilemap == null && m_pathDirectionTilemap != null)
            m_pathArrowTilemap = CreateArrowTilemap(m_pathDirectionTilemap);

        ReplaceOccupiedTiles(m_towerSpawnTilemap, visuals.BackgroundTile, colors.towerSpawn.background);
        ReplaceOccupiedTiles(tilemap, visuals.BackgroundTile, colors.path.background);
        if (m_towerDecorationTilemap != null)
        {
            m_towerDecorationTilemap.ClearAllTiles();
            foreach (Vector3Int cell in m_towerSpawnTilemap.cellBounds.allPositionsWithin)
                if (m_towerSpawnTilemap.HasTile(cell))
                    SetVisualTile(m_towerDecorationTilemap, m_towerDecorationTilemap.WorldToCell(
                        m_towerSpawnTilemap.GetCellCenterWorld(cell)), visuals.DecorationTile, colors.towerSpawn.decoration);
        }
        if (m_pathDirectionTilemap != null)
        {
            m_pathDirectionTilemap.ClearAllTiles();
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(cell))
                    SetVisualTile(m_pathDirectionTilemap, m_pathDirectionTilemap.WorldToCell(
                        tilemap.GetCellCenterWorld(cell)), visuals.DecorationTile, colors.path.decoration);
        }
        if (m_pathArrowTilemap != null)
        {
            m_pathArrowTilemap.ClearAllTiles();
            HashSet<Vector3Int> shown = new HashSet<Vector3Int>();
            for (int index = 0; index < m_pathGridPositions.Count; index++)
            {
                Vector3Int cell = m_pathArrowTilemap.WorldToCell(tilemap.GetCellCenterWorld(m_pathGridPositions[index]));
                if (!TryGetPathArrowRotation(index, out Quaternion rotation) || !shown.Add(cell)) continue;
                SetVisualTile(m_pathArrowTilemap, cell, visuals.ArrowTile, colors.arrow);
                m_pathArrowTilemap.SetTransformMatrix(cell, Matrix4x4.Rotate(rotation));
            }
        }
        m_pathArrowElapsed = 0f;
        InitializePathArrowPulse();
    }

    public void SetStageRealm(StageRealm realm)
    {
        m_stageRealm = realm;
        ApplySharedVisuals();
    }

    private bool TryGetPathArrowRotation(int index, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        // 마지막 방문에는 화살표를 두지 않습니다. 실제 적 이동의 순환 규칙은 변경하지 않습니다.
        if (index < 0 || index + 1 >= m_pathGridPositions.Count || tilemap == null) return false;
        Vector3 direction = tilemap.GetCellCenterWorld(m_pathGridPositions[index + 1]) -
                            tilemap.GetCellCenterWorld(m_pathGridPositions[index]);
        if (m_pathArrowTilemap != null) direction = m_pathArrowTilemap.transform.InverseTransformVector(direction);
        if (direction.sqrMagnitude < 0.0001f) return false;
        rotation = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward);
        return true;
    }

    private static void ReplaceOccupiedTiles(Tilemap map, Tile tile, Color color)
    {
        if (map == null || tile == null) return;
        // Cache occupied coordinates before replacing tiles; sparse/hand-painted layouts stay intact.
        var occupied = new List<Vector3Int>();
        foreach (Vector3Int cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) occupied.Add(cell);
        map.color = Color.white;
        foreach (Vector3Int cell in occupied) SetVisualTile(map, cell, tile, color);
    }

    private static void SetVisualTile(Tilemap map, Vector3Int cell, Tile tile, Color color)
    {
        if (tile == null) return;
        map.color = Color.white;
        map.SetTile(cell, tile);
        map.SetTileFlags(cell, TileFlags.None);
        map.SetTransformMatrix(cell, Matrix4x4.identity);
        map.SetColor(cell, color);
    }

    private static Tilemap FindBoardTilemap(Transform board, string name)
    {
        Transform child = board != null ? board.Find(name) : null;
        return child != null ? child.GetComponent<Tilemap>() : null;
    }

    private static Tilemap CreateArrowTilemap(Tilemap source)
    {
        GameObject arrows = new GameObject("Path Arrows", typeof(Tilemap), typeof(TilemapRenderer));
        arrows.layer = source.gameObject.layer;
        arrows.transform.SetParent(source.transform.parent, false);
        arrows.transform.localPosition = source.transform.localPosition;
        arrows.transform.localRotation = source.transform.localRotation;
        arrows.transform.localScale = source.transform.localScale;
        Tilemap result = arrows.GetComponent<Tilemap>();
        result.tileAnchor = source.tileAnchor;
        result.orientation = source.orientation;
        result.orientationMatrix = source.orientationMatrix;
        TilemapRenderer originalRenderer = source.GetComponent<TilemapRenderer>();
        TilemapRenderer renderer = arrows.GetComponent<TilemapRenderer>();
        if (originalRenderer != null)
        {
            renderer.sharedMaterial = originalRenderer.sharedMaterial;
            renderer.sortingLayerID = originalRenderer.sortingLayerID;
            renderer.sortingOrder = originalRenderer.sortingOrder + 1;
        }
        return result;
    }
}
