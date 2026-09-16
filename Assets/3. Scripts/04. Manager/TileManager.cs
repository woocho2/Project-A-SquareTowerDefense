using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// 1. 에너미 경로 타일의 역할을 구분하는 열거형
public enum SpecialTileType
{
    Normal,
    DefendTile,
    SpeedTile,
    HealTile
}

// 2. 타워 배치 타일의 특수 버프 역할을 구분하는 열거형
public enum TowerTileBuffType
{
    Normal,         // 기본 타일
    AttackPowerUp,  // 공격력 증가
    ActionCountUp,  // 행동력 증가
    AttackCountUp   // 공격횟수 증가
}

/// <summary>
/// 버프 타워가 타워 스폰 타일에 남기는 런타임 효과입니다.
/// 이 데이터가 실제 판정의 기준이며, 타일 위 이펙트는 표현만 담당합니다.
/// </summary>
public sealed class TowerTileBuffEffect
{
    public int SourceID;
    public BuffTarget Target;
    public int Tier;
    public float AbilityValue;
    public int RemainingTurns;
}

/// <summary>
/// 디버프 타워가 패스 타일에 남기는 런타임 효과입니다.
/// ApplicationVersion은 같은 장판을 밟고 있는 적에게 같은 공격이 중복 스택되는 것을 막습니다.
/// </summary>
public sealed class PathTileDebuffEffect
{
    public int SourceID;
    public DebuffTarget Target;
    public int Tier;
    public float AbilityValue;
    public int Duration;
    public int ApplicationVersion;
    public Vector3 ZoneWorldPosition;
}

[RequireComponent(typeof(Tilemap))]
public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [Header("에너미 경로 컴포넌트 참조")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilePath tilePath;

    [Header("에너미 특수 타일 생성 개수 설정")]
    [SerializeField] private int defendTileCount = 5;
    [SerializeField] private int speedTileCount = 5;
    [SerializeField] private int healTileCount = 2;

    [Header("타워 특수 타일 생성 개수 설정")]
    [Tooltip("공격력 증가 타일 생성 개수")]
    [SerializeField] private int towerAttackPowerCount = 3;
    [SerializeField] private int towerActionCount = 3;
    [FormerlySerializedAs("towerAttackSpeedCount")]
    [SerializeField] private int towerAttackCountTileCount = 2;

    [Header("맵 특수 타일 이펙트")]
    [Tooltip("타워/에너미 특수 타일 위에 생성할 MapBuff 프리팹")]
    [FormerlySerializedAs("towerTileBuffEffectPrefab")]
    [SerializeField] private TileSatelliteOrbiter mapTileBuffEffectPrefab;
    [Tooltip("생성된 특수 타일 이펙트를 정리할 부모 Transform (비워두면 TileManager 하위에 생성)")]
    [FormerlySerializedAs("towerTileBuffEffectParent")]
    [SerializeField] private Transform mapTileBuffEffectParent;

    // 에너미 타일 정보 (좌표 -> 에너미 타일 속성)
    public Dictionary<Vector3Int, SpecialTileType> specialTileMap = new Dictionary<Vector3Int, SpecialTileType>();

    // 타워 타일 정보 (좌표 -> 타워 버프 속성)
    // 실제 버프 판정은 이 Dictionary만 사용한다.
    // 즉, MapBuff 이펙트가 꺼져 있거나 없어도 타워 버프 계산 자체는 영향을 받지 않는다.
    public Dictionary<Vector3Int, TowerTileBuffType> towerTileBuffMap = new Dictionary<Vector3Int, TowerTileBuffType>();

    // 버프/디버프 타워가 남긴 동적 효과입니다. 특수 맵 타일(towerTileBuffMap, specialTileMap)과는 별개입니다.
    private readonly Dictionary<Vector3Int, List<TowerTileBuffEffect>> m_towerBuffEffectsByCell = new Dictionary<Vector3Int, List<TowerTileBuffEffect>>();
    private readonly Dictionary<Vector3Int, List<PathTileDebuffEffect>> m_pathDebuffEffectsByCell = new Dictionary<Vector3Int, List<PathTileDebuffEffect>>();
    private readonly Dictionary<int, int> m_debuffApplicationVersions = new Dictionary<int, int>();

    // 현재 씬에 생성되어 있는 버프 이펙트 목록 (좌표 -> 이펙트 인스턴스)
    // 버프 타일을 다시 랜덤 배정할 때 이전 이펙트를 안전하게 제거하기 위해 관리한다.
    private readonly Dictionary<Vector3Int, TileSatelliteOrbiter> towerTileBuffEffects = new Dictionary<Vector3Int, TileSatelliteOrbiter>();

    // 에너미 경로 특수 타일의 시각 이펙트 목록이다.
    // 타워 타일 이펙트와 별도로 관리해야 한쪽만 재배정해도 다른 쪽 이펙트가 지워지지 않는다.
    private readonly Dictionary<Vector3Int, TileSatelliteOrbiter> specialTileEffects = new Dictionary<Vector3Int, TileSatelliteOrbiter>();

    // 에너미 경로 16진수 컬러 코드
    private readonly Color defendColor = HexToColor("ffc74f");
    private readonly Color speedColor = HexToColor("57cfff");
    private readonly Color healColor = HexToColor("60ff68");

    // 타워 스폰 타일 16진수 컬러 코드    
    private readonly Color towerAttackPowerColor = HexToColor("ff5d5d"); // 공격력 증가 타일 색상
    private readonly Color towerActionCountColor = HexToColor("57cfff"); // 행동력 증가 타일 색상
    private readonly Color towerAttackCountColor = HexToColor("ffc74f"); // 공격횟수 증가 타일 색상

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[TileManager] 중복 인스턴스 제거: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tilePath == null) tilePath = GetComponent<TilePath>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // 1. 에너미 경로 특수 타일 배치
        AssignRandomSpecialTiles();

        // 2. 타워 스폰 타일 버프 지정 개수 배정 및 색상 적용
        AssignRandomTowerTileBuffs();
    }

    // ==========================================================================================================
    // 타워 스폰 타일 버프 배정 로직 (지정 개수 방식)
    // ==========================================================================================================

    [ContextMenu("타워 스폰 타일 버프 랜덤 생성")]
    public void AssignRandomTowerTileBuffs()
    {
        if (TowerManager.Instance == null)
        {
            Debug.LogError("[TileManager] TowerManager.Instance를 찾을 수 없습니다.");
            return;
        }

        Tilemap spawnTilemap = TowerManager.Instance.GetSpawnPointTilemap();
        if (spawnTilemap == null)
        {
            Debug.LogError("[TileManager] TowerManager의 SpawnPoint Tilemap을 가져오지 못했습니다.");
            return;
        }

        // 기존 데이터와 시각 이펙트를 함께 초기화한다.
        // 둘 중 하나만 남으면 실제 버프와 화면 표시가 서로 달라질 수 있다.
        towerTileBuffMap.Clear();
        ClearTowerTileBuffEffects();

        BoundsInt bounds = spawnTilemap.cellBounds;
        List<Vector3Int> availableTilePositions = new List<Vector3Int>();

        // 1. 존재하는 모든 타워 스폰 타일 좌표 수집 및 초기화(기본 흰색)
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (spawnTilemap.HasTile(pos))
                {
                    availableTilePositions.Add(pos);
                    spawnTilemap.SetTileFlags(pos, TileFlags.None);
                    // 버프 색은 이제 타일 자체가 아닌 MapBuff 프리팹이 담당한다.
                    // 이전 방식으로 칠해져 있던 타일 색이 남지 않도록 항상 흰색으로 되돌린다.
                    spawnTilemap.SetColor(pos, Color.white);
                }
            }
        }

        // 2. 지정된 개수만큼 타일을 뽑아 버프 데이터와 시각 이펙트 배정
        SetRandomTowerTiles(spawnTilemap, availableTilePositions, towerAttackPowerCount, TowerTileBuffType.AttackPowerUp, towerAttackPowerColor);
        SetRandomTowerTiles(spawnTilemap, availableTilePositions, towerActionCount, TowerTileBuffType.ActionCountUp, towerActionCountColor);
        SetRandomTowerTiles(spawnTilemap, availableTilePositions, towerAttackCountTileCount, TowerTileBuffType.AttackCountUp, towerAttackCountColor);
    }

    // 타워 타일 풀에서 특정 개수만큼 랜덤 추출 후 배정하는 헬퍼 함수
    private void SetRandomTowerTiles(Tilemap map, List<Vector3Int> pool, int count, TowerTileBuffType type, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = Random.Range(0, pool.Count);
            Vector3Int selectedPos = pool[randomIndex];

            // 1) 게임 로직이 조회할 버프 타입을 좌표에 저장한다.
            towerTileBuffMap[selectedPos] = type;

            // 2) 같은 좌표에 시각 전용 이펙트를 생성하고, 버프 타입에 맞는 색상을 입힌다.
            // 이펙트는 보기 위한 것이며 실제 버프 판정에는 관여하지 않는다.
            SpawnMapTileEffect(map, selectedPos, color, towerTileBuffEffects, "TowerMapBuff");

            pool.RemoveAt(randomIndex);
        }
    }

    private void SpawnMapTileEffect(
        Tilemap map,
        Vector3Int cellPosition,
        Color color,
        Dictionary<Vector3Int, TileSatelliteOrbiter> effectMap,
        string effectNamePrefix)
    {
        if (mapTileBuffEffectPrefab == null)
        {
            Debug.LogWarning("[TileManager] Map Tile Buff Effect Prefab이 연결되지 않았습니다.");
            return;
        }

        // 별도 부모를 지정하지 않았다면 TileManager 하위에 생성해
        // Hierarchy에서 MapBuff 이펙트들을 한곳에 모아 볼 수 있게 한다.
        Transform parent = mapTileBuffEffectParent != null ? mapTileBuffEffectParent : transform;

        // Tilemap의 셀 중심 좌표를 사용해야 Grid의 셀 크기가 바뀌어도
        // 이펙트가 타일 정중앙에 생성된다.
        Vector3 worldPosition = map.GetCellCenterWorld(cellPosition);
        TileSatelliteOrbiter effect = Instantiate(mapTileBuffEffectPrefab, worldPosition, Quaternion.identity, parent);
        effect.name = $"{effectNamePrefix}_{cellPosition.x}_{cellPosition.y}";

        // MapBuff 프리팹 내부 SpriteRenderer들의 색상을 한 번에 변경한다.
        effect.SetColor(color);
        effectMap[cellPosition] = effect;
    }

    private void ClearTowerTileBuffEffects()
    {
        // Dictionary에 기록해 둔 이전 이펙트들을 모두 파괴한다.
        // Destroy는 프레임 종료 시 실행되므로, 이후 새 이펙트를 생성해도 충돌하지 않는다.
        foreach (TileSatelliteOrbiter effect in towerTileBuffEffects.Values)
        {
            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }

        towerTileBuffEffects.Clear();
    }

    private void ClearSpecialTileEffects()
    {
        foreach (TileSatelliteOrbiter effect in specialTileEffects.Values)
        {
            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }

        specialTileEffects.Clear();
    }

    // 특정 좌표에 위치한 타워의 버프 타입 조회 함수
    public TowerTileBuffType GetTowerTileBuffAt(Vector3Int gridPos)
    {
        if (towerTileBuffMap.TryGetValue(gridPos, out TowerTileBuffType buffType))
        {
            return buffType;
        }
        return TowerTileBuffType.Normal;
    }

    // ==========================================================================================================
    // 타워/적이 읽는 동적 타일 효과
    // ==========================================================================================================

    public IReadOnlyList<TowerTileBuffEffect> GetTowerBuffEffectsAt(Vector3Int cell)
    {
        return m_towerBuffEffectsByCell.TryGetValue(cell, out List<TowerTileBuffEffect> effects)
            ? effects
            : System.Array.Empty<TowerTileBuffEffect>();
    }

    public IReadOnlyList<PathTileDebuffEffect> GetPathDebuffEffectsAt(Vector3Int cell)
    {
        return m_pathDebuffEffectsByCell.TryGetValue(cell, out List<PathTileDebuffEffect> effects)
            ? effects
            : System.Array.Empty<PathTileDebuffEffect>();
    }

    /// <summary>한 버프 타워가 새로 행동할 때 기존 범위 기록을 지우고, 이번 행동의 타일 범위 기록을 새로 남깁니다.</summary>
    public void RemoveTowerBuffEffectsBySource(int sourceID)
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, List<TowerTileBuffEffect>> pair in m_towerBuffEffectsByCell)
        {
            pair.Value.RemoveAll(effect => effect.SourceID == sourceID);
            if (pair.Value.Count == 0) emptyCells.Add(pair.Key);
        }

        foreach (Vector3Int cell in emptyCells) m_towerBuffEffectsByCell.Remove(cell);
    }

    public void AddTowerBuffEffect(Vector3Int cell, int sourceID, BuffTarget target, int tier, float abilityValue, float duration)
    {
        if (target == BuffTarget.None) return;
        if (!m_towerBuffEffectsByCell.TryGetValue(cell, out List<TowerTileBuffEffect> effects))
        {
            effects = new List<TowerTileBuffEffect>();
            m_towerBuffEffectsByCell.Add(cell, effects);
        }

        effects.Add(new TowerTileBuffEffect
        {
            SourceID = sourceID,
            Target = target,
            Tier = Mathf.Clamp(tier, 1, 5),
            AbilityValue = abilityValue,
            RemainingTurns = Mathf.Max(1, Mathf.RoundToInt(duration))
        });
    }

    /// <summary>버프 타워의 남은 지속시간을 한 에너미 턴 단위로 줄입니다.</summary>
    public void AdvanceTowerBuffEffectTurns()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, List<TowerTileBuffEffect>> pair in m_towerBuffEffectsByCell)
        {
            pair.Value.RemoveAll(effect => --effect.RemainingTurns <= 0);
            if (pair.Value.Count == 0) emptyCells.Add(pair.Key);
        }

        foreach (Vector3Int cell in emptyCells) m_towerBuffEffectsByCell.Remove(cell);
    }

    /// <summary>디버프 장판의 현재 패스 타일을 갱신합니다. 장판 하나는 한 패스 타일에만 존재하므로 이전 위치 기록을 제거합니다.</summary>
    public void SetPathDebuffEffect(Vector3Int cell, int sourceID, DebuffTarget target, int tier, float abilityValue, float duration, Vector3 zoneWorldPosition)
    {
        if (target == DebuffTarget.None) return;

        RemovePathDebuffEffectsBySource(sourceID);
        int nextVersion = m_debuffApplicationVersions.TryGetValue(sourceID, out int version) ? version + 1 : 1;
        m_debuffApplicationVersions[sourceID] = nextVersion;

        if (!m_pathDebuffEffectsByCell.TryGetValue(cell, out List<PathTileDebuffEffect> effects))
        {
            effects = new List<PathTileDebuffEffect>();
            m_pathDebuffEffectsByCell.Add(cell, effects);
        }

        effects.Add(new PathTileDebuffEffect
        {
            SourceID = sourceID,
            Target = target,
            Tier = Mathf.Clamp(tier, 1, 5),
            AbilityValue = abilityValue,
            Duration = Mathf.Max(1, Mathf.RoundToInt(duration)),
            ApplicationVersion = nextVersion,
            ZoneWorldPosition = zoneWorldPosition
        });
    }

    /// <summary>
    /// 장판이 처음 배치되었을 때 위치와 설명을 타일에 등록합니다.
    /// ApplicationVersion 0은 아직 타워가 행동하지 않은 대기 상태이므로, UI에는 표시되지만 적에게는 적용되지 않습니다.
    /// </summary>
    public void RegisterPathDebuffZone(Vector3Int cell, int sourceID, DebuffTarget target, int tier, float abilityValue, float duration, Vector3 zoneWorldPosition)
    {
        if (target == DebuffTarget.None) return;

        RemovePathDebuffEffectsBySource(sourceID);
        if (!m_pathDebuffEffectsByCell.TryGetValue(cell, out List<PathTileDebuffEffect> effects))
        {
            effects = new List<PathTileDebuffEffect>();
            m_pathDebuffEffectsByCell.Add(cell, effects);
        }

        effects.Add(new PathTileDebuffEffect
        {
            SourceID = sourceID,
            Target = target,
            Tier = Mathf.Clamp(tier, 1, 5),
            AbilityValue = abilityValue,
            Duration = Mathf.Max(1, Mathf.RoundToInt(duration)),
            ApplicationVersion = 0,
            ZoneWorldPosition = zoneWorldPosition
        });
    }

    public void RemovePathDebuffEffectsBySource(int sourceID)
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        foreach (KeyValuePair<Vector3Int, List<PathTileDebuffEffect>> pair in m_pathDebuffEffectsByCell)
        {
            pair.Value.RemoveAll(effect => effect.SourceID == sourceID);
            if (pair.Value.Count == 0) emptyCells.Add(pair.Key);
        }

        foreach (Vector3Int cell in emptyCells) m_pathDebuffEffectsByCell.Remove(cell);
    }

    /// <summary>
    /// 드래그로 장판 위치만 바뀐 경우의 이동 처리입니다.
    /// 공격 버전은 유지하므로, 이미 이 공격을 받은 적에게 스택이 중복되지 않습니다.
    /// </summary>
    public void MovePathDebuffEffect(int sourceID, Vector3Int destinationCell, Vector3 zoneWorldPosition)
    {
        PathTileDebuffEffect movedEffect = null;
        Vector3Int sourceCell = default;

        foreach (KeyValuePair<Vector3Int, List<PathTileDebuffEffect>> pair in m_pathDebuffEffectsByCell)
        {
            int index = pair.Value.FindIndex(effect => effect.SourceID == sourceID);
            if (index < 0) continue;

            movedEffect = pair.Value[index];
            sourceCell = pair.Key;
            pair.Value.RemoveAt(index);
            break;
        }

        if (movedEffect == null) return;
        if (m_pathDebuffEffectsByCell.TryGetValue(sourceCell, out List<PathTileDebuffEffect> oldCellEffects) && oldCellEffects.Count == 0)
        {
            m_pathDebuffEffectsByCell.Remove(sourceCell);
        }

        movedEffect.ZoneWorldPosition = zoneWorldPosition;
        if (!m_pathDebuffEffectsByCell.TryGetValue(destinationCell, out List<PathTileDebuffEffect> destinationEffects))
        {
            destinationEffects = new List<PathTileDebuffEffect>();
            m_pathDebuffEffectsByCell.Add(destinationCell, destinationEffects);
        }
        destinationEffects.Add(movedEffect);
    }

    /// <summary>타워 판매/합성/파괴 시, 그 타워가 기록한 모든 동적 타일 효과를 즉시 정리합니다.</summary>
    public void RemoveAllDynamicEffectsBySource(int sourceID)
    {
        RemoveTowerBuffEffectsBySource(sourceID);
        RemovePathDebuffEffectsBySource(sourceID);
        m_debuffApplicationVersions.Remove(sourceID);
    }

    // ==========================================================================================================
    // 기존 에너미 경로 로직
    // ==========================================================================================================

    [ContextMenu("랜덤 특수 타일 생성")]
    public void AssignRandomSpecialTiles()
    {
        if (tilePath == null || tilePath.pathGridPositions.Count < 3)
        {
            Debug.LogWarning("[TileManager] 특수 타일을 배치하기 위한 경로 타일 개수가 부족합니다.");
            return;
        }

        ResetAllTileColors();
        specialTileMap.Clear();
        ClearSpecialTileEffects();

        List<Vector3Int> availableTiles = new List<Vector3Int>();
        for (int i = 1; i < tilePath.pathGridPositions.Count - 1; i++)
        {
            availableTiles.Add(tilePath.pathGridPositions[i]);
        }

        SetRandomTiles(availableTiles, defendTileCount, SpecialTileType.DefendTile, defendColor);
        SetRandomTiles(availableTiles, speedTileCount, SpecialTileType.SpeedTile, speedColor);
        SetRandomTiles(availableTiles, healTileCount, SpecialTileType.HealTile, healColor);
    }

    private void SetRandomTiles(List<Vector3Int> pool, int count, SpecialTileType type, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = Random.Range(0, pool.Count);
            Vector3Int selectedPos = pool[randomIndex];

            specialTileMap[selectedPos] = type;
            tilemap.SetTileFlags(selectedPos, TileFlags.None);

            // 실제 적 효과는 specialTileMap을 통해 계산하고,
            // 이펙트는 버프 타일이라는 사실과 종류를 색으로 보여주기만 한다.
            SpawnMapTileEffect(tilemap, selectedPos, color, specialTileEffects, "PathMapBuff");

            pool.RemoveAt(randomIndex);
        }
    }

    private void ResetAllTileColors()
    {
        foreach (Vector3Int pos in tilePath.pathGridPositions)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
            // 경로 타일은 기본 흰색으로 유지하고, 특수 타일의 색은 이펙트가 담당한다.
            tilemap.SetColor(pos, Color.white);
        }
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
        {
            return color;
        }
        return Color.white;
    }

    public SpecialTileType GetTileTypeAt(Vector3Int gridPos)
    {
        if (specialTileMap.TryGetValue(gridPos, out SpecialTileType type))
        {
            return type;
        }
        return SpecialTileType.Normal;
    }

    /// <summary>
    /// 월드 클릭 좌표가 실제 패스 타일인지 판별하고 해당 셀을 반환합니다.
    /// UI는 Tilemap을 직접 찾지 않고 이 API만 사용합니다.
    /// </summary>
    public bool TryGetPathCellAtWorldPosition(Vector3 worldPosition, out Vector3Int cell)
    {
        cell = default;
        if (tilemap == null) return false;

        cell = tilemap.WorldToCell(worldPosition);
        return tilemap.HasTile(cell);
    }
}
