using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
/// <summary>
/// 보드 위 적 경로의 단일 진실 원본입니다.
///
/// <para>이 컴포넌트는 서로 다른 세 가지 정보를 분리해서 관리합니다.</para>
/// <list type="number">
/// <item><description><b>논리 경로</b>: <see cref="pathGridPositions"/>의 인덱스입니다. 적의 실제 진행도,
/// 타겟 우선순위, 특수 타일 판정은 이 인덱스를 기준으로 합니다.</description></item>
/// <item><description><b>전투 점유</b>: <c>m_enemiesByPathIndex</c>입니다. 물리 Collider 검색 대신
/// 공격·스플래시·타일 효과가 이 목록을 읽습니다.</description></item>
/// <item><description><b>표현 위치</b>: <c>m_enemyVisualTargets</c>입니다. 같은 타일에 여러 적이 있어도
/// 겹쳐 보이지 않게 할 뿐이며, 논리적 타일 위치를 바꾸지 않습니다.</description></item>
/// </list>
/// 따라서 적 이동 코드는 반드시 <see cref="RegisterEnemyAtIndex"/>로 논리 위치를 확정하고,
/// 공격 코드는 적 Transform 위치가 아니라 이 클래스의 조회 API를 사용해야 합니다.
/// </summary>
public class TilePath : MonoBehaviour
{
    // 다른 시스템(에너미 이동, 타워 타겟팅, 디버프 등)이 현재 스테이지의 경로를 공통으로 참조할 때 사용합니다.
    /// <summary>현재 스테이지의 경로입니다. 경로 기반 전투 계산의 공용 진입점입니다.</summary>
    public static TilePath Instance { get; private set; }
    [Tooltip("에디터에서 등록된 경로 타일 그리드 좌표 리스트")]
    // 에너미가 이동할 패스 타일을 순서대로 저장합니다.
    // 리스트의 인덱스 자체가 "경로 진행도"가 되므로, 숫자가 클수록 도착 지점에 가깝습니다.
    /// <summary>
    /// 적이 밟는 타일을 시작점부터 도착점까지 순서대로 저장합니다.
    /// 리스트의 인덱스가 곧 적의 진행도입니다. 0은 시작 타일, <see cref="LastIndex"/>는 도착 타일입니다.
    /// 같은 좌표를 중복 등록하지 않는 것을 원칙으로 하며, 지름길/순환 경로를 만들 때에도
    /// "현재 인덱스에서 다음 인덱스로 어디로 갈지"를 이 순서 또는 별도 분기 데이터로 결정해야 합니다.
    /// </summary>
    public List<Vector3Int> pathGridPositions = new List<Vector3Int>();

    [Header("Enemy Visual Formation")]
    // 한 타일에 적이 여럿 있을 때, 서로 겹치지 않도록 벌릴 간격입니다. (타일 크기에 비례)
    [SerializeField, Range(0.05f, 0.45f)] private float m_enemySlotSpacing = 0.28f;
    // 한 타일 안에서 적을 몇 열까지 배치할지 결정합니다. 초과한 적은 다음 행으로 내려갑니다.
    [SerializeField, Range(1, 5)] private int m_enemyFormationMaxColumns = 3;
    // 적 수가 바뀌어 진형을 재정렬할 때, 새 자리까지 부드럽게 이동하는 시각 연출 속도입니다.
    [SerializeField, Min(0.1f)] private float m_enemyFormationMoveSpeed = 1.5f;

    // pathGridPositions의 그리드 좌표를 실제 월드 좌표로 변환하기 위해 사용하는 패스 Tilemap입니다.
    /// <summary>경로 셀과 월드 좌표를 상호 변환하는 Tilemap입니다.</summary>
    private Tilemap tilemap;
    // 핵심 전투 판정 데이터입니다.
    // "패스 인덱스 -> 그 타일 위에 실제로 서 있는 적 목록"으로 관리합니다.
    // 타겟 선택, 스플래시 피해, 디버프 장판 판정은 콜라이더 대신 이 목록을 기준으로 계산합니다.
    /// <summary>
    /// 논리 경로 인덱스별 현재 점유 적 목록입니다.
    /// 적이 이동을 완료한 뒤에만 이 목록을 갱신합니다. 공격과 스플래시는 이 자료를 사용하므로,
    /// 이동 연출 중인 적이 다음 타일의 공격 대상으로 미리 잡히지 않습니다.
    /// </summary>
    private readonly Dictionary<int, List<EnemyHealthController>> m_enemiesByPathIndex =
        new Dictionary<int, List<EnemyHealthController>>();
    // 이동 중인 적이 도착할 타일의 진형 자리를 미리 예약하는 목록입니다.
    // 아직 도착하지 않은 적을 위 전투 목록에 넣으면 공격 대상이 되어버리므로 따로 보관합니다.
    // 덕분에 이동 시작부터 목표 진형 위치로 자연스럽게 이동할 수 있습니다.
    /// <summary>
    /// 이동 중인 적이 도착 예정인 타일의 화면상 자리만 예약하는 목록입니다.
    /// 전투 대상 목록에는 포함하지 않습니다. 즉, 예약은 표현 전용이고 점유는 아닙니다.
    /// </summary>
    private readonly Dictionary<int, List<EnemyHealthController>> m_pendingArrivalsByPathIndex =
        new Dictionary<int, List<EnemyHealthController>>();
    // 이미 타일에 도착한 적의 "화면상 목표 위치"입니다.
    // 진형 인원이 바뀌면 목표 좌표만 갱신하고, LateUpdate에서 해당 위치까지 천천히 이동시킵니다.
    /// <summary>
    /// 적마다 최종적으로 보간되어야 할 화면상 위치입니다.
    /// 적의 <c>CurrentTileIndex</c>와 독립적이므로 이 값으로 게임 규칙을 계산하면 안 됩니다.
    /// </summary>
    private readonly Dictionary<EnemyHealthController, Vector3> m_enemyVisualTargets =
        new Dictionary<EnemyHealthController, Vector3>();

    // 경로의 마지막 인덱스 (자동 계산: 등록된 타일 개수 - 1)
    // 유효한 마지막 패스 인덱스입니다. 경로가 비어 있으면 0으로 처리해 범위 오류를 막습니다.
    /// <summary>도착 타일의 논리 인덱스입니다. 경로가 비어 있으면 안전하게 0을 반환합니다.</summary>
    public int LastIndex => pathGridPositions.Count > 0 ? pathGridPositions.Count - 1 : 0;

    void Awake()
    {
        Instance = this;
        tilemap = GetComponent<Tilemap>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 전투 이동 코루틴이 끝난 후, 예약된 진형 위치까지 Sprite만 부드럽게 보간합니다.
    /// <see cref="EnemyMoveController.IsMoving"/>이 true인 동안은 이동 코루틴이 Transform을 제어하므로
    /// 여기서는 건드리지 않습니다. 풀로 반환된 적은 즉시 추적 목록에서 제거합니다.
    /// </summary>
    private void LateUpdate()
    {
        List<EnemyHealthController> invalidEnemies = null;

        foreach (KeyValuePair<EnemyHealthController, Vector3> pair in m_enemyVisualTargets)
        {
            EnemyHealthController enemy = pair.Key;
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                invalidEnemies ??= new List<EnemyHealthController>();
                invalidEnemies.Add(enemy);
                continue;
            }

            if (enemy.TryGetComponent(out EnemyMoveController movement) && movement.IsMoving)
            {
                continue;
            }

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position,
                pair.Value,
                m_enemyFormationMoveSpeed * Time.deltaTime);
        }

        if (invalidEnemies == null) return;
        foreach (EnemyHealthController enemy in invalidEnemies)
        {
            m_enemyVisualTargets.Remove(enemy);
        }
    }

    // 인덱스 번호에 해당하는 타일의 월드 중앙 좌표 반환
    /// <summary>
    /// 논리 경로 인덱스의 타일 중심 월드 좌표를 반환합니다.
    /// 범위를 벗어난 인덱스는 가장 가까운 유효 인덱스로 보정합니다. 이는 복귀·풀링 중 예외를 막기 위한
    /// 안전장치이며, 정상 이동 규칙이 범위를 벗어난 인덱스를 의도적으로 사용한다는 뜻은 아닙니다.
    /// </summary>
    public Vector3 GetWorldPosition(int index)
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (pathGridPositions.Count == 0) return transform.position;

        // 인덱스가 범위를 벗어나지 않도록 클램프 처리
        int clampedIndex = Mathf.Clamp(index, 0, LastIndex);
        return tilemap.GetCellCenterWorld(pathGridPositions[clampedIndex]);
    }
    
    // 특정 인덱스의 그리드 좌표 반환 (타일 속성 체크용)
    /// <summary>
    /// 논리 경로 인덱스를 Tilemap 셀 좌표로 변환합니다.
    /// 특수 타일, 경로 디버프, 타일 반경 스플래시 판정은 월드 거리 대신 이 좌표를 기준으로 합니다.
    /// </summary>
    public Vector3Int GetGridPosition(int index)
    {
        if (pathGridPositions.Count == 0) return Vector3Int.zero;
        int clampedIndex = Mathf.Clamp(index, 0, LastIndex);
        return pathGridPositions[clampedIndex];
    }

    /// <summary>적을 현재 서 있는 패스 타일의 목록에 등록합니다.</summary>
    /// <summary>
    /// 적의 이동이 완료된 순간 호출하여 실제 전투 점유 타일을 확정합니다.
    /// 이전 타일 점유와 도착 예약을 먼저 제거한 뒤 새 인덱스에 등록하고, 해당 타일의 모든 적 진형을 다시
    /// 배치합니다. 적 한 명은 언제나 최대 한 개의 경로 인덱스에만 등록되어야 합니다.
    /// </summary>
    public void RegisterEnemyAtIndex(EnemyHealthController enemy, int index)
    {
        if (enemy == null || index < 0 || index > LastIndex) return;
        RemovePendingArrival(enemy);
        UnregisterEnemy(enemy);

        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> enemies))
        {
            enemies = new List<EnemyHealthController>();
            m_enemiesByPathIndex.Add(index, enemies);
        }
        enemies.Add(enemy);
        ArrangeEnemiesOnTile(index);
    }

    /// <summary>
    /// 적이 타일에 도착하기 전에, 도착 순서에 맞는 시각 슬롯을 예약하고 좌표를 반환합니다.
    /// 예약 적은 아직 타일 판정 목록에 넣지 않으므로 이동 중 공격/디버프 대상이 되지 않습니다.
    /// </summary>
    /// <summary>
    /// 이동을 시작하기 전에 목적 타일에서 사용할 화면상 자리만 예약합니다.
    /// 이 메서드는 <b>전투상 이동 완료</b>가 아니므로 <c>m_enemiesByPathIndex</c>를 변경하지 않습니다.
    /// 도착 애니메이션 도중 적이 다음 타일의 공격·도트 대상으로 취급되는 문제를 막는 역할입니다.
    /// </summary>
    public Vector3 ReserveArrivalPosition(EnemyHealthController enemy, int index)
    {
        if (enemy == null || index < 0 || index > LastIndex) return GetWorldPosition(index);

        RemovePendingArrival(enemy);

        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> enemies))
        {
            enemies = new List<EnemyHealthController>();
        }

        if (!m_pendingArrivalsByPathIndex.TryGetValue(index, out List<EnemyHealthController> pendingEnemies))
        {
            pendingEnemies = new List<EnemyHealthController>();
            m_pendingArrivalsByPathIndex.Add(index, pendingEnemies);
        }

        pendingEnemies.RemoveAll(pendingEnemy => pendingEnemy == null || !pendingEnemy.gameObject.activeInHierarchy);
        int totalEnemyCount = enemies.Count + pendingEnemies.Count + 1;
        int arrivalOrder = enemies.Count + pendingEnemies.Count;
        pendingEnemies.Add(enemy);

        return GetFormationPosition(index, arrivalOrder, totalEnemyCount);
    }

    /// <summary>풀 반환·사망·이동 직전에 이전 패스 타일 목록에서 제거합니다.</summary>
    /// <summary>
    /// 사망, 풀 반환, 경로 재초기화 또는 새 타일 등록 전에 호출합니다.
    /// 점유 목록·도착 예약·화면상 보간 목표를 함께 제거하므로, 풀에 들어간 적이 공격 대상에 남지 않습니다.
    /// </summary>
    public void UnregisterEnemy(EnemyHealthController enemy)
    {
        if (enemy == null) return;
        RemovePendingArrival(enemy);

        foreach (KeyValuePair<int, List<EnemyHealthController>> pair in m_enemiesByPathIndex)
        {
            if (pair.Value.Remove(enemy))
            {
                ArrangeEnemiesOnTile(pair.Key);
            }
        }

        m_enemyVisualTargets.Remove(enemy);
    }

    /// <summary>
    /// 같은 패스 타일에 도착한 순서대로 적을 격자 형태로 배치합니다.
    /// 적 목록의 순서는 유지하므로 0번째 적이 좌상단부터 차례로 놓입니다.
    /// </summary>
    /// <summary>
    /// 한 타일의 실제 점유 목록을 기준으로 모든 적의 화면상 목표 위치를 다시 계산합니다.
    /// 여기서 Transform을 즉시 이동하지 않고 목표만 갱신하는 이유는, 새 적의 진입이나 기존 적의 사망에도
    /// 전투 논리와 표현 보간을 분리하기 위해서입니다.
    /// </summary>
    private void ArrangeEnemiesOnTile(int pathIndex)
    {
        if (!m_enemiesByPathIndex.TryGetValue(pathIndex, out List<EnemyHealthController> enemies)) return;

        enemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
        if (enemies.Count == 0) return;

        for (int i = 0; i < enemies.Count; i++)
        {
            m_enemyVisualTargets[enemies[i]] = GetFormationPosition(pathIndex, i, enemies.Count);
        }
    }

    /// <summary>
    /// 같은 경로 타일 안에서 사용할 격자형 화면 위치를 계산합니다.
    /// 열 수는 <c>m_enemyFormationMaxColumns</c>를 넘지 않으며, 행·열 중앙을 기준으로 오프셋을 잡아
    /// 적 수가 홀수·짝수여도 진형 전체가 타일 중심에 유지됩니다.
    /// </summary>
    private Vector3 GetFormationPosition(int pathIndex, int slotIndex, int totalEnemyCount)
    {
        int columns = Mathf.Min(m_enemyFormationMaxColumns, totalEnemyCount);
        int rows = Mathf.CeilToInt(totalEnemyCount / (float)columns);
        float tileSize = tilemap != null ? Mathf.Min(tilemap.cellSize.x, tilemap.cellSize.y) : 1f;
        float spacing = tileSize * m_enemySlotSpacing;
        int column = slotIndex % columns;
        int row = slotIndex / columns;
        float xOffset = (column - ((columns - 1) * 0.5f)) * spacing;
        float yOffset = (((rows - 1) * 0.5f) - row) * spacing;
        return GetWorldPosition(pathIndex) + new Vector3(xOffset, yOffset, 0f);
    }

    /// <summary>모든 목적 타일의 예약 목록에서 해당 적을 제거합니다. 실제 점유 목록은 건드리지 않습니다.</summary>
    private void RemovePendingArrival(EnemyHealthController enemy)
    {
        foreach (List<EnemyHealthController> pendingEnemies in m_pendingArrivalsByPathIndex.Values)
        {
            pendingEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// 투사체 착탄 지점을 중심으로 범위 안의 패스 타일 목록만 합쳐 반환합니다.
    /// 물리 콜라이더 검색과 무관한 실제 스플래시 피해 대상입니다.
    /// </summary>
    /// <summary>
    /// 충돌 지점을 중심으로 정사각 타일 반경 안에 있는 살아 있는 적만 반환합니다.
    /// <paramref name="tileRadius"/>가 0이면 충돌 타일 하나, 1이면 3x3 타일입니다.
    /// 물리 Collider가 아닌 경로 점유 자료를 사용하므로 같은 타일의 모든 적이 안정적으로 포함됩니다.
    /// </summary>
    public List<EnemyHealthController> GetEnemiesInSquare(Vector3 worldPosition, int tileRadius)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tilemap == null) return result;

        Vector3Int impactCell = tilemap.WorldToCell(worldPosition);
        foreach (KeyValuePair<int, List<EnemyHealthController>> pair in m_enemiesByPathIndex)
        {
            Vector3Int pathCell = GetGridPosition(pair.Key);
            if (Mathf.Abs(pathCell.x - impactCell.x) > tileRadius || Mathf.Abs(pathCell.y - impactCell.y) > tileRadius) continue;

            foreach (EnemyHealthController enemy in pair.Value)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f)
                {
                    result.Add(enemy);
                }
            }
        }
        return result;
    }

    /// <summary>모든 패스 타일에 등록된 살아 있는 적을 반환합니다.</summary>
    /// <summary>
    /// 모든 경로 타일에서 살아 있고 활성화된 적을 평탄화하여 반환합니다.
    /// 타겟 우선순위, 체인 공격처럼 경로 전체 후보가 필요한 경우에만 사용합니다.
    /// 호출자는 반환 리스트를 장기 보관하지 않아야 합니다.
    /// </summary>
    public List<EnemyHealthController> GetAllActiveEnemies()
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        foreach (List<EnemyHealthController> enemies in m_enemiesByPathIndex.Values)
        {
            foreach (EnemyHealthController enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f)
                {
                    result.Add(enemy);
                }
            }
        }
        return result;
    }

    /// <summary>특정 패스 인덱스에 위치한 살아 있는 적 목록을 반환합니다.</summary>
    /// <summary>특정 논리 경로 타일을 현재 점유한 살아 있는 적 목록을 반환합니다.</summary>
    public List<EnemyHealthController> GetEnemiesAtIndex(int index)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                EnemyHealthController enemy = list[i];
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f)
                {
                    result.Add(enemy);
                }
            }
        }
        return result;
    }

    /// <summary>그리드 좌표에 해당하는 패스 인덱스를 반환합니다. (없을 경우 -1)</summary>
    /// <summary>
    /// 경로 셀 좌표의 첫 번째 논리 인덱스를 반환합니다. 경로가 같은 셀을 여러 번 지난다면 이 API만으로는
    /// 어느 방문인지 구분할 수 없으므로, 순환/분기 경로에서는 적의 현재 인덱스를 우선 사용해야 합니다.
    /// </summary>
    public int GetPathIndexAtGridPosition(Vector3Int gridPos)
    {
        return pathGridPositions.IndexOf(gridPos);
    }

    /// <summary>특정 그리드 좌표의 패스 타일 위에 서 있는 적 목록을 반환합니다.</summary>
    /// <summary>셀 좌표를 경로 인덱스로 바꾼 뒤 해당 타일 점유 적을 반환하는 편의 API입니다.</summary>
    public List<EnemyHealthController> GetEnemiesAtGridPosition(Vector3Int gridPos)
    {
        int index = GetPathIndexAtGridPosition(gridPos);
        if (index < 0) return new List<EnemyHealthController>();
        return GetEnemiesAtIndex(index);
    }

    /// <summary>
    /// 중심 셀에서 정사각 타일 반경 안에 있는 고유한 경로 셀을 반환합니다.
    /// 불 디버프존처럼 "경로 전체가 아닌 특정 구역의 모든 패스타일"에 효과를 기록할 때 사용합니다.
    /// 경로가 순환하여 같은 셀을 여러 번 포함하더라도 <see cref="HashSet{T}"/>으로 한 번만 반환합니다.
    /// </summary>
    public List<Vector3Int> GetPathCellsInSquare(Vector3Int centerCell, int tileRadius)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        HashSet<Vector3Int> uniqueCells = new HashSet<Vector3Int>();
        int radius = Mathf.Max(0, tileRadius);

        for (int i = 0; i < pathGridPositions.Count; i++)
        {
            Vector3Int cell = pathGridPositions[i];
            if (Mathf.Abs(cell.x - centerCell.x) > radius || Mathf.Abs(cell.y - centerCell.y) > radius) continue;
            if (uniqueCells.Add(cell)) result.Add(cell);
        }

        return result;
    }

    /// <summary>
    /// 타워의 정사각 사거리 안에 있는 각 경로 타일의 중심 월드 좌표를 반환합니다.
    /// 불 스플래시의 메테오처럼 "적이 있는 타일"이 아니라 "사거리 안의 모든 패스타일"을 대상으로 하는
    /// 연출에 사용합니다. 반환된 좌표에 적이 없어도 투사체는 정상적으로 도착할 수 있습니다.
    /// </summary>
    public List<Vector3> GetPathWorldPositionsInRange(Vector3 origin, float worldRange)
    {
        List<Vector3> result = new List<Vector3>();
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tilemap == null) return result;

        int tileRange = TowerAttackAction.ToTileRange(worldRange);
        Vector3Int originCell = tilemap.WorldToCell(origin);
        foreach (Vector3Int cell in GetPathCellsInSquare(originCell, tileRange))
        {
            result.Add(tilemap.GetCellCenterWorld(cell));
        }

        return result;
    }

    // 씬 뷰 시각화
    /// <summary>
    /// 에디터 전용 경로 점검 표시입니다. 노란 선은 인덱스 순서, 숫자는 논리 진행도입니다.
    /// 런타임 전투 데이터에는 관여하지 않으므로 경로가 이상할 때만 이를 기준으로 리스트 순서를 검증합니다.
    /// </summary>
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
