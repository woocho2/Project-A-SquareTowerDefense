using UnityEngine;
using UnityEngine.Tilemaps;

public class DebuffAction : TowerAttackAction
{
    private DebuffZone m_activeZone;
    private int m_sourceID;

    public DebuffAction(TowerData data) : base(data) { }

    public void BindZone(DebuffZone zone, Transform towerTransform, TowerStats currentStats)
    {
        if (zone == null || towerTransform == null) return;

        if (m_activeZone != null) m_activeZone.OnPlacedPathCellChanged -= HandleZoneCellChanged;
        m_activeZone = zone;
        m_sourceID = towerTransform.GetInstanceID();
        m_activeZone.OnPlacedPathCellChanged += HandleZoneCellChanged;
        // 존은 최초 배치 위치에 고정합니다. 타워를 드래그해도 장판은 따라가지 않습니다.
        m_activeZone.transform.SetParent(null, true);

        // 타워 위치 및 사거리 정보 전달 (드래그 제한용)
        m_activeZone.SetupBoundary(towerTransform.position, currentStats.Range);

        // 장판의 시각 연출은 TileSatelliteOrbiter가 전담합니다.
        // DebuffZone은 오비터를 직접 보관하지 않고, 배치/드래그만 담당합니다.
        TileSatelliteOrbiter orbiter = m_activeZone.GetComponent<TileSatelliteOrbiter>();
        if (orbiter != null)
        {
            orbiter.SetEffectType(TileSatelliteEffectType.Debuff);
            orbiter.SetDebuffType(m_data.debuffTarget);
        }

        // 길 타일을 찾지 못한 경우에는 타워 위치에 존을 만들지 않습니다.
        if (!TryFindClosestPathTilePosition(towerTransform.position, currentStats.Range, out Vector3 targetRoadPos))
        {
            m_activeZone.gameObject.SetActive(false);
            Debug.LogWarning("[DebuffZone] 사거리 안의 패스 타일을 찾지 못했습니다. 디버프 존을 비활성화합니다.");
            return;
        }

        // 월드 좌표는 패스 타일 정중앙으로만 설정합니다.
        m_activeZone.transform.position = targetRoadPos;
        m_activeZone.gameObject.SetActive(true);

        // 장판을 놓는 즉시 타일 정보 패널에는 디버프가 보이게 등록합니다.
        // 실제 적 스택은 ExecuteAction에서 ApplicationVersion이 1 이상이 된 뒤부터 적용됩니다.
        if (TileManager.Instance != null && m_activeZone.TryGetPlacedPathCell(out Vector3Int pathCell))
        {
            int tier = Mathf.Clamp(m_data.towerID / 1000, 1, 5);
            TileManager.Instance.RegisterPathDebuffZone(
                pathCell,
                m_sourceID,
                m_data.debuffTarget,
                tier,
                currentStats.AbilityValue,
                currentStats.Duration,
                m_activeZone.transform.position);
        }
    }

    private void HandleZoneCellChanged(Vector3Int cell, Vector3 worldPosition)
    {
        // 아직 한 번도 행동하지 않은 장판은 데이터가 없습니다. 행동 후에는 드래그 즉시 기존 효과를 함께 이동합니다.
        TileManager.Instance?.MovePathDebuffEffect(m_sourceID, cell, worldPosition);
    }

    // Tilemap_Path에서 타워 주변 유효한 길목 타일의 정중앙 좌표를 찾는 함수
    private bool TryFindClosestPathTilePosition(Vector3 towerPos, float range, out Vector3 bestWorldPos)
    {
        bestWorldPos = towerPos;
        // 씬 내의 Tilemap_Path 오브젝트 탐색
        Tilemap pathTilemap = null;
        GameObject pathObj = GameObject.Find("Tilemap_Path");

        if (pathObj != null)
        {
            pathTilemap = pathObj.GetComponent<Tilemap>();
        }

        // 씬 오브젝트 이름이 달라도 실제 적 이동 경로(TilePath)의 타일맵을 우선 사용합니다.
        if (pathTilemap == null)
        {
            TilePath tilePath = Object.FindFirstObjectByType<TilePath>();
            if (tilePath != null)
            {
                pathTilemap = tilePath.GetComponent<Tilemap>();
            }
        }

        if (pathTilemap == null)
        {
            Debug.LogError("[DebuffZone] Tilemap_Path를 찾을 수 없습니다!");
            return false;
        }

        // 타워 위치를 타일맵 셀 그리드 좌표로 변환
        Vector3Int centerCell = pathTilemap.WorldToCell(towerPos);
        int cellRadius = TowerAttackAction.ToTileRange(range);

        float minDistance = float.MaxValue;
        bool found = false;

        // 타워 주변 반경(cellRadius) 내 모든 그리드 셀 검사
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                Vector3Int checkCell = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);

                // 해당 셀에 길목 타일이 존재하는지 확인
                if (pathTilemap.HasTile(checkCell))
                {
                    // 해당 타일 셀의 정확한 정중앙 월드 좌표 취득
                    Vector3 cellWorldPos = pathTilemap.GetCellCenterWorld(checkCell);
                    float dist = Vector2.Distance(towerPos, cellWorldPos);

                    if (dist <= range && dist < minDistance)
                    {
                        minDistance = dist;
                        bestWorldPos = cellWorldPos;
                        found = true;
                    }
                }
            }
        }

        if (found)
        {
            return true;
        }

        return false;
    }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        if (m_activeZone == null) return false;
        if (TileManager.Instance == null || !m_activeZone.TryGetPlacedPathCell(out Vector3Int pathCell)) return false;

        int tier = Mathf.Clamp(m_data.towerID / 1000, 1, 5);
        // 실제 적 탐색은 하지 않습니다. 장판의 패스 타일에 효과 데이터만 기록하고,
        // 적은 이동을 마친 뒤 자신이 서 있는 타일의 데이터를 읽습니다.
        TileManager.Instance.SetPathDebuffEffect(
            pathCell,
            towerTransform.GetInstanceID(),
            m_data.debuffTarget,
            tier,
            currentStats.AbilityValue,
            currentStats.Duration,
            m_activeZone.transform.position);
        return true;
    }
}
