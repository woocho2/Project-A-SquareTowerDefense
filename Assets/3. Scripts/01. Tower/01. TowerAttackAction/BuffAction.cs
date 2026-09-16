using UnityEngine;
using UnityEngine.Tilemaps;

public class BuffAction : TowerAttackAction
{
    public BuffAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        int sourceTier = Mathf.Clamp(m_data.towerID / 1000, 1, 5);
        if (m_data.buffTarget == BuffTarget.Shield)
        {
            // 쉴드 타워는 행동할 때마다 자신의 티어를 최대치로 하여 쉴드 1을 충전합니다.
            GameManager.Instance?.AddShield(
                towerTransform.GetInstanceID(),
                sourceTier);
            return true;
        }

        // Earth는 TowerController의 100초 타이머에서 전역 강화하므로, 주변 타워에 일반 버프를 적용하지 않습니다.
        if (m_data.buffTarget == BuffTarget.Earth) return true;

        if (TileManager.Instance == null || TowerManager.Instance == null) return false;

        Tilemap spawnTilemap = TowerManager.Instance.GetSpawnPointTilemap();
        if (spawnTilemap == null) return false;

        // 버프의 대상은 콜라이더나 현재 설치된 타워가 아니라, 사거리 안의 모든 타워 스폰 타일입니다.
        // 타워가 나중에 그 타일로 이동/소환되어도 그 즉시 같은 효과를 읽습니다.
        int sourceID = towerTransform.GetInstanceID();
        TileManager.Instance.RemoveTowerBuffEffectsBySource(sourceID);
        BoundsInt bounds = spawnTilemap.cellBounds;
        bool appliedAny = false;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!spawnTilemap.HasTile(cell)) continue;

                Vector3 cellCenter = spawnTilemap.GetCellCenterWorld(cell);
                if (Vector2.Distance(towerTransform.position, cellCenter) > currentStats.Range) continue;

                TileManager.Instance.AddTowerBuffEffect(
                    cell,
                    sourceID,
                    m_data.buffTarget,
                    sourceTier,
                    currentStats.AbilityValue,
                    currentStats.Duration);
                appliedAny = true;
            }
        }

        return appliedAny;
    }
}
