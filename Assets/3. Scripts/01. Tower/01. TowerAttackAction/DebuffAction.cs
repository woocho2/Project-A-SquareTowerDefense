using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class DebuffAction : TowerAttackAction
{
    private DebuffZone m_activeZone;

    public DebuffAction(TowerData data) : base(data) { }

    public void BindZone(DebuffZone zone, Vector3 towerPosition, TowerStats currentStats)
    {
        if (zone == null) return;

        m_activeZone = zone;
        m_activeZone.gameObject.SetActive(true);

        // 타워 위치 및 사거리 정보 전달 (드래그 제한용)
        m_activeZone.SetupBoundary(towerPosition, currentStats.Range);

        // 1. 타일맵에서 가장 가까운 길목 셀의 월드 좌표 계산
        Vector3 targetRoadPos = FindClosestTilemapRoadPosition(towerPosition, currentStats.Range);

        // 2. 장판 월드 분리 및 해당 타일 정중앙으로 스냅
        m_activeZone.transform.SetParent(null);
        m_activeZone.transform.position = targetRoadPos;
        m_activeZone.transform.localScale = Vector3.one;

        // 3. 이벤트 등록
        m_activeZone.OnEnemyEnterEvent -= HandleEnemyEnter;
        m_activeZone.OnEnemyEnterEvent += HandleEnemyEnter;

        m_activeZone.OnEnemyExitEvent -= HandleEnemyExit;
        m_activeZone.OnEnemyExitEvent += HandleEnemyExit;
    }

    // Tilemap_Path에서 타워 주변 유효한 길목 타일의 정중앙 좌표를 찾는 함수
    private Vector3 FindClosestTilemapRoadPosition(Vector3 towerPos, float range)
    {
        // 씬 내의 Tilemap_Path 오브젝트 탐색
        Tilemap pathTilemap = null;
        GameObject pathObj = GameObject.Find("Tilemap_Path");

        if (pathObj != null)
        {
            pathTilemap = pathObj.GetComponent<Tilemap>();
        }

        if (pathTilemap == null)
        {
            // 이름으로 못 찾을 경우 씬 내 첫 번째 Tilemap 탐색
            pathTilemap = Object.FindAnyObjectByType<Tilemap>();
        }

        if (pathTilemap == null)
        {
            Debug.LogError("[DebuffZone] Tilemap_Path를 찾을 수 없습니다!");
            return towerPos;
        }

        // 타워 위치를 타일맵 셀 그리드 좌표로 변환
        Vector3Int centerCell = pathTilemap.WorldToCell(towerPos);
        int cellRadius = Mathf.CeilToInt(Mathf.Max(range, 1.5f));

        Vector3 bestWorldPos = towerPos;
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
            Debug.Log($"[DebuffZone] 길목 타일맵 셀 감지 성공: {bestWorldPos}");
            return bestWorldPos;
        }

        Debug.LogWarning($"[DebuffZone] 사거리({range}) 내 길목 타일을 찾지 못해 기본 위치를 반환합니다.");
        return towerPos;
    }

    private void HandleEnemyEnter(EnemyHealthController health)
    {
        if (health == null || health.CurrentHP <= 0f) return;

        int patternID = m_data.towerID % 100;

        // 최신 업그레이드 스탯 가져오기 (매니저가 없을 경우 기본 데이터 fallback)
        TowerStats currentStats = TowerManager.Instance != null
            ? TowerManager.Instance.GetGlobalStats(m_data.towerID)
            : m_data.ToTowerStats();

        if (health.TryGetComponent<EnemyDebuffController>(out var debuff))
        {
            switch (patternID)
            {
                case TowerPattern.SWORD:
                    debuff.AddDebuff(new SwordDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.BOW:
                    debuff.AddDebuff(new BowDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.SPEAR:
                    debuff.AddDebuff(new SpearDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.AXE:
                    debuff.AddDebuff(new AxeDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.HAMMER:
                    debuff.AddDebuff(new HammerDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.FIRE:
                    debuff.AddDebuff(new FireDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.ICE:
                    debuff.AddDebuff(new IceDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.ELECTRICITY:
                    debuff.AddDebuff(new ElectricityDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
                case TowerPattern.LIGHT:
                    debuff.AddDebuff(new LightDebuff(currentStats.Duration, currentStats.AbilityValue));
                    break;
            }
        }
    }

    private void HandleEnemyExit(EnemyHealthController health)
    {
        if (health == null) return;
    }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        if (m_activeZone == null) return false;

        int patternID = m_data.towerID % 100;

        if (m_activeZone.EnemiesInZone.Count == 0 && patternID != TowerPattern.SHIELD)
            return false;

        switch (patternID)
        {
            case TowerPattern.SHIELD:
                towerTransform.GetComponent<MonoBehaviour>().StartCoroutine(ShieldBlockRoutine(currentStats.AbilityValue));
                break;

            case TowerPattern.WIND:
                for (int i = 0; i < m_activeZone.EnemiesInZone.Count; i++)
                {
                    if (m_activeZone.EnemiesInZone[i].TryGetComponent<EnemyDebuffController>(out var debuff))
                    {
                        debuff.AddDebuff(new WindDebuff(0.2f, currentStats.AbilityValue));
                    }
                }
                break;

            case TowerPattern.EARTH:
                for (int i = 0; i < m_activeZone.EnemiesInZone.Count; i++)
                {
                    if (m_activeZone.EnemiesInZone[i].TryGetComponent<EnemyDebuffController>(out var debuff))
                    {
                        debuff.AddDebuff(new EarthDebuff(currentStats.Duration, currentStats.AbilityValue));
                    }
                }
                break;

            case TowerPattern.DARKNESS:
                for (int i = 0; i < m_activeZone.EnemiesInZone.Count; i++)
                {
                    if (m_activeZone.EnemiesInZone[i].TryGetComponent<EnemyDebuffController>(out var debuff))
                    {
                        debuff.AddDebuff(new DarknessDebuff(currentStats.Duration, m_activeZone.transform.position));
                    }
                }
                break;

            case TowerPattern.FIRE:
                for (int i = 0; i < m_activeZone.EnemiesInZone.Count; i++)
                {
                    m_activeZone.EnemiesInZone[i].ApplyDamage(currentStats.AttackPower);
                }
                break;
        }

        return true;
    }

    private IEnumerator ShieldBlockRoutine(float duration)
    {
        m_activeZone.ToggleObstacle(true);
        yield return new WaitForSeconds(duration);
        m_activeZone.ToggleObstacle(false);
    }
}