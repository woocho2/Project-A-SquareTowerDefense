using UnityEngine;
using UnityEngine.Tilemaps;

public class DebuffAction : TowerAttackAction
{
    private DebuffZone m_activeZone;

    public DebuffAction(TowerData data) : base(data) { }

    public void BindZone(DebuffZone zone, Transform towerTransform, TowerStats currentStats)
    {
        if (zone == null || towerTransform == null) return;

        m_activeZone = zone;
        // 프리팹에 포함된 자식 존을 타워의 직접 자식으로 보장합니다.
        // 따라서 타워의 이동·삭제 시 존도 반드시 함께 이동·삭제됩니다.
        m_activeZone.transform.SetParent(towerTransform, true);

        // 타워 위치 및 사거리 정보 전달 (드래그 제한용)
        m_activeZone.SetupBoundary(towerTransform.position, currentStats.Range);

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
        int cellRadius = Mathf.CeilToInt(Mathf.Max(range, 1.5f));

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
        var enemiesInZone = m_activeZone.GetEnemiesOnPlacedPathTile();
        if (enemiesInZone.Count == 0) return false;

        int tier = Mathf.Clamp(m_data.towerID / 1000, 1, 5);
        for (int i = 0; i < enemiesInZone.Count; i++)
        {
            EnemyHealthController enemy = enemiesInZone[i];
            if (enemy != null && enemy.TryGetComponent(out EnemyDebuffController debuff))
            {
                debuff.ApplyZoneStack(m_data.debuffTarget, tier, currentStats.AbilityValue, m_activeZone.transform.position);
            }
        }
        return true;
    }

}
