using System.Collections.Generic;
using UnityEngine;
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
    AttackSpeedUp   // 공격속도 증가
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

    [Tooltip("행동력 증가 타일 생성 개수")]
    [SerializeField] private int towerActionCount = 3;

    [Tooltip("공격속도 증가 타일 생성 개수")]
    [SerializeField] private int towerAttackSpeedCount = 2;

    // 에너미 타일 정보 (좌표 -> 에너미 타일 속성)
    public Dictionary<Vector3Int, SpecialTileType> specialTileMap = new Dictionary<Vector3Int, SpecialTileType>();

    // 타워 타일 정보 (좌표 -> 타워 버프 속성)
    public Dictionary<Vector3Int, TowerTileBuffType> towerTileBuffMap = new Dictionary<Vector3Int, TowerTileBuffType>();

    // 에너미 경로 16진수 컬러 코드
    private readonly Color defendColor = HexToColor("FFFFAC");
    private readonly Color speedColor = HexToColor("8AFFFE");
    private readonly Color healColor = HexToColor("9EFFA8");

    // ==========================================================================================================
    // 타워 스폰 타일 16진수 컬러 코드
    // ==========================================================================================================
    private readonly Color towerAttackPowerColor = HexToColor("FF5555"); // 공격력 증가 타일 색상
    private readonly Color towerActionCountColor = HexToColor("FFAA00"); // 행동력 증가 타일 색상
    private readonly Color towerAttackSpeedColor = HexToColor("55AAFF"); // 공격속도 증가 타일 색상

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

        towerTileBuffMap.Clear();

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
                    spawnTilemap.SetColor(pos, Color.white);
                }
            }
        }

        // 2. 지정된 개수만큼 타일을 뽑아 버프 및 색상 배정
        SetRandomTowerTiles(spawnTilemap, availableTilePositions, towerAttackPowerCount, TowerTileBuffType.AttackPowerUp, towerAttackPowerColor);
        SetRandomTowerTiles(spawnTilemap, availableTilePositions, towerActionCount, TowerTileBuffType.ActionCountUp, towerActionCountColor);
        SetRandomTowerTiles(spawnTilemap, availableTilePositions, towerAttackSpeedCount, TowerTileBuffType.AttackSpeedUp, towerAttackSpeedColor);
    }

    // 타워 타일 풀에서 특정 개수만큼 랜덤 추출 후 배정하는 헬퍼 함수
    private void SetRandomTowerTiles(Tilemap map, List<Vector3Int> pool, int count, TowerTileBuffType type, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = Random.Range(0, pool.Count);
            Vector3Int selectedPos = pool[randomIndex];

            towerTileBuffMap[selectedPos] = type;
            map.SetColor(selectedPos, color);

            pool.RemoveAt(randomIndex);
        }
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
            tilemap.SetColor(selectedPos, color);

            pool.RemoveAt(randomIndex);
        }
    }

    private void ResetAllTileColors()
    {
        foreach (Vector3Int pos in tilePath.pathGridPositions)
        {
            tilemap.SetTileFlags(pos, TileFlags.None);
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
}