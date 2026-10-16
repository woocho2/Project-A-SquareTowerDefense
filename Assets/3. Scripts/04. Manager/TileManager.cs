using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;



public enum PathTileBuffType
{
    Normal,
    DefendTile,
    MoveTile,
    HealTile,
    ActionTile,
    PurifyTile,
    Bifrost
}

public enum TowerTileBuffType
{
    Normal,         // 효과 없음
    AttackPowerUp,  // 공격력 증가
    ActionCountUp,  // 행동력 증가
    AttackCountUp   // 공격횟수 증가
}

public sealed class TowerTileBuffEffect
{
    public int SourceID;
    public BuffTarget Target;
    public int Tier;
    public float AbilityValue;
    public int RemainingTurns;
    public int ApplicationVersion;
    public int StackThreshold;
    public int StackLifetime;
    // 스택형 버프(스킬 1 + 스킬 2 틀)에서만 씁니다. Power = 스킬 1의 세기, StackGain = 한 번 행동에 쌓는 스택 수.
    public float Power;
    public int StackGain;
}


public sealed class PathTileDebuffEffect
{
    public int SourceID;
    public int SourceCreationOrder;
    public DebuffTarget Target;
    public int Tier;
    public float AbilityValue;
    public int Duration;
    public int StackThreshold;
    public int ApplicationVersion;
    public int ZonePathIndex;
    // 스택형 디버프(스킬 1 + 스킬 2 틀)에서만 씁니다. Power = 스킬 1의 세기, StackGain = 한 번 행동에 쌓는 스택 수.
    // StackGain이 0이면 예전 방식(티어별 스택 수)을 씁니다.
    public float Power;
    public int StackGain;
}

[RequireComponent(typeof(Tilemap))]
public partial class TileManager : MonoBehaviour
{
    #region 설정

    public static TileManager Instance { get; private set; }

    [Header("에너미 경로 타일맵과 이동 순서")]
    [SerializeField] private Tilemap tilemap;
    [Header("타워 소환 타일맵")]
    [SerializeField] private Tilemap m_towerSpawnTilemap;
    [Tooltip("Start(0번)부터 한 바퀴를 도는 순환 경로 좌표. 목록 순서가 이동 순서이고, 마지막 칸은 0번 칸과 이어집니다.")]
    [SerializeField] private List<Vector3Int> m_pathGridPositions = new List<Vector3Int>();

    public IReadOnlyList<Vector3Int> PathGridPositions => m_pathGridPositions;
    public Tilemap TowerSpawnTilemap => m_towerSpawnTilemap;

    [Header("공용 배경·장식 / 스테이지 색상")]
    [SerializeField] private SharedBoardVisuals m_sharedVisuals;
    [SerializeField] private StageRealm m_stageRealm = StageRealm.Asgard;
    [SerializeField] private Tilemap m_towerDecorationTilemap;
    [SerializeField] private Tilemap m_pathArrowTilemap;
    public StageRealm StageRealm => m_stageRealm;

    [Header("패스 장식과 화살표 순차 발광")]
    [Tooltip("테두리 장식, 중앙 방향 화살표와 END 표시가 있는 Path Direction 타일맵입니다.")]
    [SerializeField] private Tilemap m_pathDirectionTilemap;
    [SerializeField] private bool m_enablePathArrowPulse = true;
    [Tooltip("다음 경로 인덱스가 빛나기까지 걸리는 실제 시간(초)입니다.")]
    [SerializeField, Min(0.02f)] private float m_pathArrowStepSeconds = 0.12f;
    [Tooltip("한 타일의 테두리와 화살표가 함께 밝아졌다가 서서히 사라지는 시간입니다.")]
    [SerializeField, Min(0.05f)] private float m_pathArrowFadeSeconds = 0.38f;
    [Tooltip("END의 잔광이 사라진 뒤 START에서 다시 시작하기까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float m_pathArrowLoopPause = 0.8f;
    [SerializeField, Range(0.1f, 1f)] private float m_pathArrowIdleOpacity = 0.65f;
    [SerializeField, Range(1f, 10f)] private float m_pathArrowGlowMultiplier = 1.8f;
    [Tooltip("적 턴에는 발광을 줄여 적과 투사체가 잘 보이게 합니다.")]
    [SerializeField, Range(0f, 1f)] private float m_pathArrowCombatStrength = 0.55f;

    [Header("에너미 특수 타일 생성 개수 설정")]
    [SerializeField] private int defendTileCount = 2;
    [SerializeField] private int moveTileCount = 2;
    [SerializeField] private int actionTileCount = 2;
    [SerializeField] private int healTileCount = 2;
    [SerializeField] private int purifyTileCount = 1;

    [Header("타워 특수 타일 생성 개수 설정")]
    [Tooltip("공격력 증가 타일 생성 개수")]
    [SerializeField] private int towerAttackPowerCount = 3;
    [SerializeField] private int towerActionCount = 3;
    [FormerlySerializedAs("towerAttackSpeedCount")]
    [SerializeField] private int towerAttackCountTileCount = 2;

    [Header("맵 특수 타일 이펙트")]
    [Tooltip("타워 소환 타일의 공전형 맵 버프 표시용 프리팹. 패스 전용 프리팹이 없을 때도 사용합니다.")]
    [FormerlySerializedAs("towerTileBuffEffectPrefab")]
    [SerializeField] private TileSatelliteOrbiter mapTileBuffEffectPrefab;
    [Tooltip("패스 맵 버프용 상승 기포 프리팹. 비어 있으면 기존 위성을 사용합니다.")]
    [SerializeField] private GameObject m_pathMapBuffVisualPrefab;
    [Tooltip("이펙트 부모. 비우면 TileManager 하위에 생성합니다.")]
    [FormerlySerializedAs("towerTileBuffEffectParent")]
    [SerializeField] private Transform mapTileBuffEffectParent;

    [Header("패스 타일 적 수 표시")]
    [SerializeField] private TMP_FontAsset m_enemyCountFont;
    [SerializeField, Min(0.1f)] private float m_enemyCountFontSize = 2.5f;
    [SerializeField] private Color m_enemyCountColor = Color.white;
    [Tooltip("타일 크기에 비례한 표시 위치입니다. 기본값은 오른쪽 아래입니다.")]
    [SerializeField] private Vector2 m_enemyCountOffset = new Vector2(0.27f, -0.3f);

    public event Action EnemyCountChanged;

    #endregion

    #region 런타임 데이터

    // 패스 타일: 좌표 ↔ 이동 순서
    private readonly Dictionary<Vector3Int, int> m_pathIndexByCell = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<int, Vector3Int> m_pathCellsByIndex = new Dictionary<int, Vector3Int>();
    private readonly Dictionary<Vector3Int, List<int>> m_pathIndicesByCell = new Dictionary<Vector3Int, List<int>>();
    // 타워 소환 타일맵의 셀 좌표계로 옮긴 패스 칸 위치: 타워↔패스 거리 계산 전용
    private readonly Dictionary<int, Vector3Int> m_pathBoardCellsByIndex = new Dictionary<int, Vector3Int>();

    // 타워 소환 타일: 왼쪽 위 1번부터 좌표 ↔ 번호
    private readonly Dictionary<Vector3Int, int> m_towerSpawnIndexByCell = new Dictionary<Vector3Int, int>();
    private readonly Dictionary<int, Vector3Int> m_towerSpawnCellsByIndex = new Dictionary<int, Vector3Int>();
    private readonly Dictionary<int, Vector3> m_towerSpawnWorldByIndex = new Dictionary<int, Vector3>();

    // 점유 정보의 원본: 경로는 0번부터, 타워 소환 칸은 1번부터 시작한다.
    private readonly Dictionary<int, List<EnemyHealthController>> m_enemiesByPathIndex =
        new Dictionary<int, List<EnemyHealthController>>();
    private readonly Dictionary<EnemyHealthController, int> m_pathIndexByEnemy =
        new Dictionary<EnemyHealthController, int>();
    private readonly Dictionary<EnemyHealthController, long> m_enemyArrivalOrder = new Dictionary<EnemyHealthController, long>();
    private readonly Dictionary<Vector3Int, TextMeshPro> m_enemyCountLabels = new Dictionary<Vector3Int, TextMeshPro>();
    private long m_nextEnemyArrivalOrder;
    private readonly Dictionary<int, TowerController> m_towersBySpawnIndex =
        new Dictionary<int, TowerController>();
    private readonly SortedSet<int> m_emptyTowerSpawnIndices = new SortedSet<int>();


    public int PathTileCount => m_pathCellsByIndex.Count;
    /// <summary>마지막 패스 인덱스입니다. 순환 경로라 이 칸 다음은 Start(0번)입니다. 경로가 비어 있으면 0을 반환합니다.</summary>
    public int LastPathIndex => Mathf.Max(0, m_pathGridPositions.Count - 1);
    /// <summary>패스 인덱스를 순환 경로 위의 번호로 감습니다. 마지막 번호 다음은 0번, 0번 앞은 마지막 번호입니다.</summary>
    public int WrapPathIndex(int index)
    {
        int count = PathTileCount;
        if (count <= 0) return 0;

        return ((index % count) + count) % count;
    }
    public int TowerSpawnTileCount => m_towerSpawnCellsByIndex.Count;

    // 스테이지 시작 시 배치되는 고정 맵 버프
    private readonly Dictionary<int, PathTileBuffType> m_pathTileBuffByIndex = new Dictionary<int, PathTileBuffType>();
    private readonly Dictionary<int, TowerTileBuffType> m_towerTileBuffByIndex = new Dictionary<int, TowerTileBuffType>();

    // 버프 타워는 소환 타일에, 디버프 타워는 패스 타일에 효과를 남긴다.
    private readonly Dictionary<int, List<TowerTileBuffEffect>> m_towerBuffEffectsByIndex = new Dictionary<int, List<TowerTileBuffEffect>>();
    private readonly Dictionary<int, List<PathTileDebuffEffect>> m_pathDebuffEffectsByIndex = new Dictionary<int, List<PathTileDebuffEffect>>();

    // 효과를 준 타워별 적용 번호: 같은 행동의 중복 적용 방지
    private readonly Dictionary<int, int> m_buffApplicationVersions = new Dictionary<int, int>();
    private readonly Dictionary<int, int> m_debuffApplicationVersions = new Dictionary<int, int>();

    // 패스 맵 버프 이펙트 색상
    private readonly Color defendColor = HexToColor("ffc74f");
    private readonly Color moveColor = HexToColor("57cfff");
    private readonly Color healColor = HexToColor("60ff68");
    private readonly Color actionColor = HexToColor("c78bff");
    private readonly Color purifyColor = HexToColor("ffffff");
    private readonly Color bifrostColor = HexToColor("ff9de2");

    // 이번 판의 비프로스트 두 칸. 판마다 AssignRandomSpecialTiles에서 정하고, 적이 건너갈 때 씁니다.
    private int m_bifrostIndexA = -1;
    private int m_bifrostIndexB = -1;

    // 타워 소환 맵 버프 이펙트 색상
    private readonly Color towerAttackPowerColor = HexToColor("ff5d5d");
    private readonly Color towerActionCountColor = HexToColor("57cfff");
    private readonly Color towerAttackCountColor = HexToColor("ffc74f");

    // 랜덤 배치를 다시 실행할 때 이전 표시 이펙트를 중복 생성하지 않기 위한 목록.
    private readonly List<GameObject> m_pathMapBuffEffects = new List<GameObject>();
    private readonly List<GameObject> m_towerMapBuffEffects = new List<GameObject>();

    private sealed class PathArrowCell
    {
        public Vector3Int Cell;
        public Color OriginalColor;
        public TileFlags OriginalFlags;
        public Color LastColor;
        public Color ArrowColor;
        public Color LastArrowColor;
        public float Intensity;
    }

    private readonly List<PathArrowCell> m_pathArrowCells = new List<PathArrowCell>();
    private int[] m_pathArrowCellIndices;
    private float m_pathArrowElapsed;
    private bool m_pathArrowColorsOverridden;

    #endregion

    #region 초기화

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[TileManager] 중복 인스턴스 제거: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        RestorePathArrowColors();
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        m_pathArrowElapsed = 0f;
        SharedBoardVisuals.Changed += ApplySharedVisuals;
    }

    private void OnDisable()
    {
        SharedBoardVisuals.Changed -= ApplySharedVisuals;
        RestorePathArrowColors();
    }

    private void Update()
    {
        UpdatePathArrowPulse();
    }

    private void Start()
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        InitializePathTileIndices();
        InitializeTowerSpawnTileIndices();
        ApplySharedVisuals();

        AssignRandomSpecialTiles();

        AssignRandomTowerTileBuffs();
    }

    #endregion

    #region 패스 장식과 화살표 순차 발광

    private void InitializePathArrowPulse()
    {
        RestorePathArrowColors();
        m_pathArrowCells.Clear();
        if (m_pathDirectionTilemap == null) return;

        // 표시 타일맵의 좌표계가 달라도 경로의 월드 위치를 기준으로 연결합니다.
        Dictionary<Vector3Int, int> cellIndices = new Dictionary<Vector3Int, int>();
        m_pathArrowCellIndices = new int[m_pathGridPositions.Count];
        for (int index = 0; index < m_pathGridPositions.Count; index++)
        {
            Vector3Int cell = m_pathDirectionTilemap.WorldToCell(
                tilemap.GetCellCenterWorld(m_pathGridPositions[index]));
            m_pathArrowCellIndices[index] = -1;
            if (!m_pathDirectionTilemap.HasTile(cell)) continue;

            if (!cellIndices.TryGetValue(cell, out int cellIndex))
            {
                cellIndex = m_pathArrowCells.Count;
                Color color = m_pathDirectionTilemap.GetColor(cell);
                m_pathArrowCells.Add(new PathArrowCell
                {
                    Cell = cell,
                    OriginalColor = color,
                    OriginalFlags = m_pathDirectionTilemap.GetTileFlags(cell),
                    LastColor = color,
                    ArrowColor = m_pathArrowTilemap != null ? m_pathArrowTilemap.GetColor(cell) : Color.white,
                    LastArrowColor = m_pathArrowTilemap != null ? m_pathArrowTilemap.GetColor(cell) : Color.white
                });
                cellIndices.Add(cell, cellIndex);
            }
            m_pathArrowCellIndices[index] = cellIndex;
        }
    }

    private void UpdatePathArrowPulse()
    {
        if (!m_enablePathArrowPulse)
        {
            RestorePathArrowColors();
            return;
        }
        if (m_pathDirectionTilemap == null || m_pathArrowCells.Count == 0 ||
            m_pathArrowCellIndices == null || m_pathArrowCellIndices.Length == 0) return;

        if (!m_pathArrowColorsOverridden)
        {
            foreach (PathArrowCell entry in m_pathArrowCells)
                m_pathDirectionTilemap.SetTileFlags(entry.Cell, entry.OriginalFlags & ~TileFlags.LockColor);
            m_pathArrowColorsOverridden = true;
        }

        float step = Mathf.Max(0.02f, m_pathArrowStepSeconds);
        float fade = Mathf.Max(0.05f, m_pathArrowFadeSeconds);
        float cycle = (m_pathArrowCellIndices.Length - 1) * step + fade + Mathf.Max(0f, m_pathArrowLoopPause);
        // 게임 배속과 독립적으로 읽기 쉬운 속도를 유지합니다.
        m_pathArrowElapsed = Mathf.Repeat(m_pathArrowElapsed + Time.unscaledDeltaTime, cycle);
        foreach (PathArrowCell entry in m_pathArrowCells) entry.Intensity = 0f;

        for (int index = 0; index < m_pathArrowCellIndices.Length; index++)
        {
            int cellIndex = m_pathArrowCellIndices[index];
            if (cellIndex < 0) continue;
            float age = m_pathArrowElapsed - index * step;
            if (age < 0f || age >= fade) continue;

            float rise = Mathf.Min(0.04f, fade * 0.25f);
            float intensity = age < rise
                ? Mathf.SmoothStep(0f, 1f, age / rise)
                : 1f - Mathf.SmoothStep(0f, 1f, (age - rise) / (fade - rise));
            // 같은 칸을 다시 지나는 경로도 각 방문 순서에 맞춰 빛납니다.
            PathArrowCell entry = m_pathArrowCells[cellIndex];
            if (intensity > entry.Intensity && m_pathArrowTilemap != null && TryGetPathArrowRotation(index, out Quaternion rotation))
                m_pathArrowTilemap.SetTransformMatrix(entry.Cell, Matrix4x4.Rotate(rotation));
            entry.Intensity = Mathf.Max(entry.Intensity, intensity);
        }

        float strength = GameManager.Instance != null && GameManager.Instance.CurrentState == TurnState.EnemyTurn
            ? Mathf.Clamp01(m_pathArrowCombatStrength) : 1f;
        foreach (PathArrowCell entry in m_pathArrowCells)
        {
            float intensity = entry.Intensity * strength;
            float brightness = Mathf.Lerp(1f, Mathf.Max(1f, m_pathArrowGlowMultiplier), intensity);
            Color color = entry.OriginalColor;
            color.r *= brightness;
            color.g *= brightness;
            color.b *= brightness;
            color.a *= Mathf.Lerp(Mathf.Clamp01(m_pathArrowIdleOpacity), 1f, intensity);
            if (color != entry.LastColor) m_pathDirectionTilemap.SetColor(entry.Cell, color);
            entry.LastColor = color;
            Color arrowColor = entry.ArrowColor;
            arrowColor.r *= brightness;
            arrowColor.g *= brightness;
            arrowColor.b *= brightness;
            arrowColor.a *= Mathf.Lerp(Mathf.Clamp01(m_pathArrowIdleOpacity), 1f, intensity);
            if (m_pathArrowTilemap != null && arrowColor != entry.LastArrowColor)
                m_pathArrowTilemap.SetColor(entry.Cell, arrowColor);
            entry.LastArrowColor = arrowColor;
        }
    }

    private void RestorePathArrowColors()
    {
        if (!m_pathArrowColorsOverridden || m_pathDirectionTilemap == null) return;
        foreach (PathArrowCell entry in m_pathArrowCells)
        {
            m_pathDirectionTilemap.SetColor(entry.Cell, entry.OriginalColor);
            m_pathDirectionTilemap.SetTileFlags(entry.Cell, entry.OriginalFlags);
            entry.LastColor = entry.OriginalColor;
            if (m_pathArrowTilemap != null) m_pathArrowTilemap.SetColor(entry.Cell, entry.ArrowColor);
            entry.LastArrowColor = entry.ArrowColor;
        }
        m_pathArrowColorsOverridden = false;
    }

    #endregion

    #region 타일 번호

    private void InitializePathTileIndices()
    {
        m_pathCellsByIndex.Clear();
        m_pathIndexByCell.Clear();
        m_pathIndicesByCell.Clear();
        m_pathBoardCellsByIndex.Clear();

        for (int index = 0; index < m_pathGridPositions.Count; index++)
        {
            Vector3Int cell = m_pathGridPositions[index];
            m_pathCellsByIndex.Add(index, cell);
            m_pathBoardCellsByIndex.Add(index, tilemap != null && m_towerSpawnTilemap != null
                ? m_towerSpawnTilemap.WorldToCell(tilemap.GetCellCenterWorld(cell))
                : cell);
            if (!m_pathIndicesByCell.TryGetValue(cell, out List<int> indices))
            {
                indices = new List<int>();
                m_pathIndicesByCell.Add(cell, indices);
            }
            indices.Add(index);

            // 같은 셀을 다시 지나면 좌표 조회에는 첫 방문 번호를 사용한다.
            if (!m_pathIndexByCell.ContainsKey(cell))
            {
                m_pathIndexByCell.Add(cell, index);
            }

            if (tilemap != null && !tilemap.HasTile(cell))
            {
                Debug.LogWarning($"[TileManager] Path index {index} has no tile at {cell}.");
            }
        }
    }

    private void InitializeTowerSpawnTileIndices()
    {
        m_towerSpawnCellsByIndex.Clear();
        m_towerSpawnIndexByCell.Clear();
        m_towerSpawnWorldByIndex.Clear();
        m_emptyTowerSpawnIndices.Clear();

        if (m_towerSpawnTilemap == null)
        {
            Debug.LogError("[TileManager] Tower spawn tilemap is missing; tower spawn indices were not created.");
            return;
        }

        BoundsInt bounds = m_towerSpawnTilemap.cellBounds;
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!m_towerSpawnTilemap.HasTile(cell)) continue;

                int index = m_towerSpawnCellsByIndex.Count + 1;
                m_towerSpawnCellsByIndex.Add(index, cell);
                m_towerSpawnIndexByCell.Add(cell, index);
                m_towerSpawnWorldByIndex.Add(index, m_towerSpawnTilemap.GetCellCenterWorld(cell));
                if (!m_towersBySpawnIndex.TryGetValue(index, out TowerController tower) || tower == null)
                    m_emptyTowerSpawnIndices.Add(index);
            }
        }
    }

    public bool TryGetTowerSpawnTileCell(int index, out Vector3Int cell)
    {
        return m_towerSpawnCellsByIndex.TryGetValue(index, out cell);
    }

    public bool TryGetPathTileIndex(Vector3Int cell, out int index)
    {
        return m_pathIndexByCell.TryGetValue(cell, out index);
    }

    public bool TryGetTowerSpawnTileIndex(Vector3Int cell, out int index)
    {
        return m_towerSpawnIndexByCell.TryGetValue(cell, out index);
    }

    public bool TryGetTowerSpawnWorldPosition(int index, out Vector3 position)
    {
        return m_towerSpawnWorldByIndex.TryGetValue(index, out position);
    }

    public bool TryGetFirstEmptyTowerSpawnIndex(out int index)
    {
        if (m_emptyTowerSpawnIndices.Count > 0)
        {
            index = m_emptyTowerSpawnIndices.Min;
            return true;
        }

        index = -1;
        return false;
    }

    public bool AreTowerSpawnIndicesWithinRange(int firstIndex, int secondIndex, int range)
    {
        if (!m_towerSpawnCellsByIndex.TryGetValue(firstIndex, out Vector3Int firstCell) ||
            !m_towerSpawnCellsByIndex.TryGetValue(secondIndex, out Vector3Int secondCell)) return false;

        return GetCellDistance(firstCell, secondCell) <= range;
    }

    // 대각선도 1칸으로 세는 타일 거리입니다. Range 1 = 3x3, Range 2 = 5x5.
    private static int GetCellDistance(Vector3Int first, Vector3Int second)
    {
        return Mathf.Max(Mathf.Abs(first.x - second.x), Mathf.Abs(first.y - second.y));
    }

    /// <summary>타워 소환 칸과 패스 칸 사이의 타일 거리입니다. 인덱스가 유효하지 않으면 -1을 반환합니다.</summary>
    public int GetTileDistance(int towerSpawnIndex, int pathIndex)
    {
        if (!m_towerSpawnCellsByIndex.TryGetValue(towerSpawnIndex, out Vector3Int towerCell) ||
            !m_pathBoardCellsByIndex.TryGetValue(pathIndex, out Vector3Int pathCell)) return -1;

        return GetCellDistance(towerCell, pathCell);
    }

    /// <summary>
    /// 해당 타워 스폰 타일을 지금 사거리 안에 두고 있는 그 문양의 버프 타워 중 Power가 가장 높은 것의 스탯을 반환합니다.
    /// 스택형 버프는 버프 타워가 존재하고 사거리 안에 있을 때만 유지되고, 버프의 세기는 그 버프 타워의 지금 스탯을 따릅니다.
    /// 사거리 안에 그런 버프 타워가 없으면 false를 반환합니다.
    /// </summary>
    public bool TryGetStrongestBuffTowerStatsInRange(int index, BuffTarget target, out TowerStats strongestStats)
    {
        strongestStats = default;
        if (!m_towerSpawnCellsByIndex.TryGetValue(index, out Vector3Int cell)) return false;

        bool found = false;
        foreach (KeyValuePair<int, TowerController> pair in m_towersBySpawnIndex)
        {
            TowerController source = pair.Value;
            if (source == null) continue;

            TowerData data = source.GetTowerData();
            if (data == null || data.attackType != AttackType.Buff || data.buffTarget != target) continue;
            if (!m_towerSpawnCellsByIndex.TryGetValue(pair.Key, out Vector3Int sourceCell)) continue;

            TowerStats sourceStats = source.GetFinalStats();
            if (GetCellDistance(sourceCell, cell) > TowerAttackAction.ToTileRange(sourceStats.Range)) continue;

            if (!found || sourceStats.AttackPower > strongestStats.AttackPower)
            {
                strongestStats = sourceStats;
                found = true;
            }
        }
        return found;
    }

    public List<int> GetTowerSpawnIndicesInRange(int centerIndex, int tileRange)
    {
        List<int> indices = new List<int>();
        if (!m_towerSpawnCellsByIndex.TryGetValue(centerIndex, out Vector3Int center)) return indices;

        for (int index = 1; index <= TowerSpawnTileCount; index++)
        {
            if (m_towerSpawnCellsByIndex.TryGetValue(index, out Vector3Int cell) &&
                GetCellDistance(center, cell) <= tileRange)
                indices.Add(index);
        }
        return indices;
    }

    /// <summary>
    /// 타워 사거리 안의 패스 인덱스를 이동 순서대로 반환합니다.
    /// uniqueCells가 true면 경로가 같은 칸을 여러 번 지나도 첫 방문 인덱스만 포함합니다.
    /// </summary>
    public List<int> GetPathIndicesInTowerRange(int towerSpawnIndex, int tileRange, bool uniqueCells = false)
    {
        List<int> indices = new List<int>();
        if (!m_towerSpawnCellsByIndex.TryGetValue(towerSpawnIndex, out Vector3Int towerCell)) return indices;

        HashSet<Vector3Int> visitedCells = uniqueCells ? new HashSet<Vector3Int>() : null;
        for (int index = 0; index < PathTileCount; index++)
        {
            Vector3Int pathCell = m_pathBoardCellsByIndex[index];
            if (GetCellDistance(towerCell, pathCell) > tileRange) continue;
            if (visitedCells != null && !visitedCells.Add(pathCell)) continue;
            indices.Add(index);
        }
        return indices;
    }

    /// <summary>중심 패스 칸에서 정사각 반경 안의 패스 인덱스입니다. 반경 0은 같은 칸의 모든 방문 인덱스입니다.</summary>
    public List<int> GetPathIndicesInSquare(int centerPathIndex, int tileRadius)
    {
        List<int> indices = new List<int>();
        if (!m_pathBoardCellsByIndex.TryGetValue(centerPathIndex, out Vector3Int center)) return indices;

        int radius = Mathf.Max(0, tileRadius);
        for (int index = 0; index < PathTileCount; index++)
        {
            if (GetCellDistance(center, m_pathBoardCellsByIndex[index]) <= radius) indices.Add(index);
        }
        return indices;
    }

    /// <summary>
    /// 타워에서 가장 가까운 패스 인덱스입니다. 타일 거리가 같으면 직선상 가까운 칸, 그다음 앞 번호를 고릅니다.
    /// 디버프 장판의 첫 배치에 쓰므로, 장판을 놓을 수 없는 비프로스트 칸은 제외합니다.
    /// </summary>
    public bool TryGetClosestPathIndexToTower(int towerSpawnIndex, out int pathIndex)
    {
        pathIndex = -1;
        if (!m_towerSpawnCellsByIndex.TryGetValue(towerSpawnIndex, out Vector3Int towerCell)) return false;

        int bestTileDistance = int.MaxValue;
        int bestSqrDistance = int.MaxValue;
        for (int index = 0; index < PathTileCount; index++)
        {
            if (GetTileTypeAt(index) == PathTileBuffType.Bifrost) continue;

            Vector3Int offset = m_pathBoardCellsByIndex[index] - towerCell;
            int tileDistance = GetCellDistance(m_pathBoardCellsByIndex[index], towerCell);
            int sqrDistance = offset.x * offset.x + offset.y * offset.y;
            if (tileDistance > bestTileDistance ||
                (tileDistance == bestTileDistance && sqrDistance >= bestSqrDistance)) continue;

            bestTileDistance = tileDistance;
            bestSqrDistance = sqrDistance;
            pathIndex = index;
        }
        return pathIndex >= 0;
    }

    public bool TryGetPathWorldPosition(int index, out Vector3 position)
    {
        position = default;
        if (tilemap == null || !m_pathCellsByIndex.TryGetValue(index, out Vector3Int cell)) return false;

        position = tilemap.GetCellCenterWorld(cell);
        return true;
    }

    /// <summary>
    /// 패스 인덱스의 타일 중심 월드 좌표입니다.
    /// 범위를 벗어난 인덱스는 가장 가까운 유효 인덱스로 보정합니다. (복귀·풀링 중 예외 방지용)
    /// </summary>
    public Vector3 GetPathWorldPosition(int index)
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (m_pathGridPositions.Count == 0) return transform.position;

        return tilemap.GetCellCenterWorld(m_pathGridPositions[Mathf.Clamp(index, 0, LastPathIndex)]);
    }

    public bool TryGetTowerSpawnIndexAtWorldPosition(Vector3 worldPosition, out int index)
    {
        index = -1;
        return m_towerSpawnTilemap != null &&
               m_towerSpawnIndexByCell.TryGetValue(m_towerSpawnTilemap.WorldToCell(worldPosition), out index);
    }

    public bool TryGetPathIndexAtWorldPosition(Vector3 worldPosition, out int index)
    {
        index = -1;
        return tilemap != null &&
               m_pathIndexByCell.TryGetValue(tilemap.WorldToCell(worldPosition), out index);
    }

    #region 타일 점유

    public bool SetEnemyAtPathIndex(EnemyHealthController enemy, int index, out int previousIndex)
    {
        previousIndex = -1;
        if (enemy == null || !m_pathCellsByIndex.ContainsKey(index)) return false;

        bool wasRegistered = m_pathIndexByEnemy.TryGetValue(enemy, out int currentIndex);
        if (wasRegistered && currentIndex == index)
        {
            previousIndex = index;
            return true;
        }

        Vector3Int destinationCell = m_pathCellsByIndex[index];
        Vector3Int previousCell = default;
        if (wasRegistered)
        {
            previousIndex = currentIndex;
            previousCell = m_pathCellsByIndex[currentIndex];
            RemoveEnemyOccupancy(enemy, currentIndex);
        }
        // 같은 물리 타일의 다른 방문 인덱스로 바뀌는 경우 도착 순서는 유지합니다.
        if (!wasRegistered || previousCell != destinationCell)
            m_enemyArrivalOrder[enemy] = m_nextEnemyArrivalOrder++;
        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants))
        {
            occupants = new List<EnemyHealthController>();
            m_enemiesByPathIndex.Add(index, occupants);
        }

        occupants.Add(enemy);
        m_pathIndexByEnemy[enemy] = index;
        if (wasRegistered && previousCell != destinationCell) RefreshEnemyStack(previousCell);
        RefreshEnemyStack(destinationCell);

        EnemyCountChanged?.Invoke();
        return true;
    }

    public bool RemoveEnemyFromPath(EnemyHealthController enemy, out int previousIndex)
    {
        previousIndex = -1;
        if (ReferenceEquals(enemy, null)) return false;

        if (!m_pathIndexByEnemy.TryGetValue(enemy, out previousIndex)) return false;

        m_pathIndexByEnemy.Remove(enemy);
        m_enemyArrivalOrder.Remove(enemy);
        RemoveEnemyOccupancy(enemy, previousIndex);
        RefreshEnemyStack(m_pathCellsByIndex[previousIndex]);
        EnemyCountChanged?.Invoke();
        return true;
    }

    private void RemoveEnemyOccupancy(EnemyHealthController enemy, int index)
    {
        if (m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants))
        {
            occupants.Remove(enemy);
            if (occupants.Count == 0) m_enemiesByPathIndex.Remove(index);
        }
    }

    // 소환 초기화 중에도 수와 표시 순서가 필요하므로 HP 초기화 여부는 거르지 않습니다.
    public List<EnemyHealthController> GetEnemyOccupantsAtPathIndex(int index)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants)) return result;
        foreach (EnemyHealthController enemy in occupants)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy) result.Add(enemy);
        }
        return result;
    }

    // 경로가 같은 좌표를 여러 번 지나도 그 좌표의 모든 방문 인덱스를 합친다.
    public List<EnemyHealthController> GetEnemyOccupantsAtPathCell(Vector3Int cell)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (!m_pathIndicesByCell.TryGetValue(cell, out List<int> visits)) return result;
        foreach (int index in visits)
        {
            result.AddRange(GetEnemyOccupantsAtPathIndex(index));
        }
        result.Sort((left, right) => m_enemyArrivalOrder[left].CompareTo(m_enemyArrivalOrder[right]));
        return result;
    }

    private void RefreshEnemyStack(Vector3Int cell)
    {
        List<EnemyHealthController> enemies = GetEnemyOccupantsAtPathCell(cell);
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyMoveController movement = enemies[i].Movement;
            if (movement != null) movement.SetStackRenderOrder(Mathf.Max(-20000, 20000 - i * 2), i == 0);
        }

        if (enemies.Count == 0)
        {
            if (m_enemyCountLabels.TryGetValue(cell, out TextMeshPro emptyLabel) && emptyLabel != null)
                emptyLabel.gameObject.SetActive(false);
            return;
        }

        if (!m_enemyCountLabels.TryGetValue(cell, out TextMeshPro label) || label == null)
        {
            var labelObject = new GameObject($"EnemyCount_{cell.x}_{cell.y}", typeof(TextMeshPro));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            label = labelObject.GetComponent<TextMeshPro>();
            label.font = m_enemyCountFont != null ? m_enemyCountFont : TMP_Settings.defaultFontAsset;
            label.fontSize = m_enemyCountFontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = m_enemyCountColor;
            label.outlineColor = Color.black;
            label.outlineWidth = 0.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.renderer.sortingLayerID = SortingLayer.NameToID("Enemy");
            label.renderer.sortingOrder = 30000;
            Vector3 size = tilemap.cellSize;
            label.rectTransform.sizeDelta = new Vector2(size.x * 0.5f, size.y * 0.3f);
            label.transform.position = tilemap.GetCellCenterWorld(cell)
                + new Vector3(size.x * m_enemyCountOffset.x, size.y * m_enemyCountOffset.y, -2f);
            m_enemyCountLabels[cell] = label;
        }
        label.SetText("x{0}", enemies.Count);
        label.gameObject.SetActive(true);
    }

    /// <summary>패스에 등록된 모든 적을 복사해 반환합니다. 순회 중 적이 죽거나 이동해도 안전합니다.</summary>
    public List<EnemyHealthController> GetAllEnemiesOnPath()
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>(m_pathIndexByEnemy.Count);
        foreach (EnemyHealthController enemy in m_pathIndexByEnemy.Keys)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy) result.Add(enemy);
        }
        return result;
    }

    private static bool IsLivingEnemy(EnemyHealthController enemy)
    {
        return enemy != null && enemy.gameObject.activeInHierarchy && enemy.CurrentHP > 0f;
    }

    /// <summary>특정 패스 인덱스를 점유한 살아 있는 적 목록입니다. 공격·스플래시 대상 판정에 사용합니다.</summary>
    public List<EnemyHealthController> GetLivingEnemiesAtPathIndex(int index)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        AddLivingEnemiesAtPathIndex(index, result);
        return result;
    }

    /// <summary>경로가 같은 좌표를 여러 번 지나도 그 좌표의 살아 있는 적을 모두 반환합니다.</summary>
    public List<EnemyHealthController> GetLivingEnemiesAtPathCell(Vector3Int cell)
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>();
        if (!m_pathIndicesByCell.TryGetValue(cell, out List<int> visits)) return result;
        foreach (int index in visits)
        {
            AddLivingEnemiesAtPathIndex(index, result);
        }
        return result;
    }

    /// <summary>
    /// 모든 패스 타일의 살아 있는 적을 반환합니다. 체인 공격처럼 경로 전체 후보가 필요할 때 사용합니다.
    /// 호출자는 반환 리스트를 장기 보관하지 않아야 합니다.
    /// </summary>
    public List<EnemyHealthController> GetAllLivingEnemies()
    {
        List<EnemyHealthController> result = new List<EnemyHealthController>(m_pathIndexByEnemy.Count);
        foreach (List<EnemyHealthController> occupants in m_enemiesByPathIndex.Values)
        {
            foreach (EnemyHealthController enemy in occupants)
            {
                if (IsLivingEnemy(enemy)) result.Add(enemy);
            }
        }
        return result;
    }

    private void AddLivingEnemiesAtPathIndex(int index, List<EnemyHealthController> result)
    {
        if (!m_enemiesByPathIndex.TryGetValue(index, out List<EnemyHealthController> occupants)) return;
        foreach (EnemyHealthController enemy in occupants)
        {
            if (IsLivingEnemy(enemy)) result.Add(enemy);
        }
    }

    #endregion

    /// <summary>Select a path visit without an occupancy limit.</summary>
    public bool TryGetRandomEnemySpawnIndex(out int index)
    {
        index = PathTileCount > 0 ? Random.Range(0, PathTileCount) : -1;
        return index >= 0;
    }

    #region 타워 점유

    public bool TryGetTowerAt(int index, out TowerController tower)
    {
        tower = null;
        return m_towerSpawnCellsByIndex.ContainsKey(index) &&
               m_towersBySpawnIndex.TryGetValue(index, out tower) && tower != null;
    }

    public bool TryGetTowerAt(Vector3Int cell, out TowerController tower)
    {
        tower = null;
        return m_towerSpawnIndexByCell.TryGetValue(cell, out int index) && TryGetTowerAt(index, out tower);
    }

    public bool TryGetTowerData(int index, out TowerController tower, out TowerData data)
    {
        data = null;
        if (!TryGetTowerAt(index, out tower)) return false;

        data = tower.GetTowerData();
        return data != null;
    }

    public bool TryGetTowerInfo(int index, out TowerController tower, out TowerData data, out TowerStats stats)
    {
        stats = default;
        if (!TryGetTowerData(index, out tower, out data)) return false;
        stats = tower.GetFinalStats();
        return true;
    }

    public bool TryPlaceTowerAt(int index, TowerController tower)
    {
        if (tower == null || !m_towerSpawnCellsByIndex.ContainsKey(index) || TryGetTowerAt(index, out _)) return false;

        m_towersBySpawnIndex[index] = tower;
        m_emptyTowerSpawnIndices.Remove(index);
        tower.SpawnIndex = index;
        GameStateVersion.MarkChanged();
        return true;
    }

    public bool TryPlaceTowerAt(Vector3Int cell, TowerController tower) =>
        m_towerSpawnIndexByCell.TryGetValue(cell, out int index) && TryPlaceTowerAt(index, tower);

    public bool SetTowerAt(int index, TowerController tower)
    {
        if (tower == null || !m_towerSpawnCellsByIndex.ContainsKey(index)) return false;
        if (m_towersBySpawnIndex.TryGetValue(index, out TowerController previous) && previous != null && previous != tower)
            previous.SpawnIndex = -1;
        m_towersBySpawnIndex[index] = tower;
        m_emptyTowerSpawnIndices.Remove(index);
        tower.SpawnIndex = index;
        GameStateVersion.MarkChanged();
        return true;
    }

    public bool SetTowerAt(Vector3Int cell, TowerController tower) =>
        m_towerSpawnIndexByCell.TryGetValue(cell, out int index) && SetTowerAt(index, tower);

    public bool RemoveTowerAt(int index)
    {
        if (!m_towerSpawnCellsByIndex.ContainsKey(index) || !m_towersBySpawnIndex.TryGetValue(index, out TowerController tower) ||
            !m_towersBySpawnIndex.Remove(index)) return false;
        if (tower != null) tower.SpawnIndex = -1;
        m_emptyTowerSpawnIndices.Add(index);
        GameStateVersion.MarkChanged();
        return true;
    }

    public void RemoveTowerIfMatches(int index, TowerController tower)
    {
        if (!ReferenceEquals(tower, null) && m_towersBySpawnIndex.TryGetValue(index, out TowerController occupant) &&
            ReferenceEquals(occupant, tower))
            RemoveTowerAt(index);
    }

    public bool RemoveTowerAt(Vector3Int cell) =>
        m_towerSpawnIndexByCell.TryGetValue(cell, out int index) && RemoveTowerAt(index);

    public bool TryMoveTower(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || !m_towerSpawnCellsByIndex.ContainsKey(toIndex) ||
            !TryGetTowerAt(fromIndex, out TowerController tower) || TryGetTowerAt(toIndex, out _)) return false;

        m_towersBySpawnIndex.Remove(fromIndex);
        m_towersBySpawnIndex[toIndex] = tower;
        tower.SpawnIndex = toIndex;
        m_emptyTowerSpawnIndices.Add(fromIndex);
        m_emptyTowerSpawnIndices.Remove(toIndex);
        GameStateVersion.MarkChanged();
        return true;
    }

    public bool TryMoveTower(Vector3Int fromCell, Vector3Int toCell) =>
        m_towerSpawnIndexByCell.TryGetValue(fromCell, out int fromIndex) &&
        m_towerSpawnIndexByCell.TryGetValue(toCell, out int toIndex) &&
        TryMoveTower(fromIndex, toIndex);

    public bool TrySwapTowers(int firstIndex, int secondIndex)
    {
        if (firstIndex == secondIndex || !TryGetTowerAt(firstIndex, out TowerController firstTower) ||
            !TryGetTowerAt(secondIndex, out TowerController secondTower)) return false;

        m_towersBySpawnIndex[firstIndex] = secondTower;
        m_towersBySpawnIndex[secondIndex] = firstTower;
        firstTower.SpawnIndex = secondIndex;
        secondTower.SpawnIndex = firstIndex;
        GameStateVersion.MarkChanged();
        return true;
    }

    public bool TrySwapTowers(Vector3Int firstCell, Vector3Int secondCell) =>
        m_towerSpawnIndexByCell.TryGetValue(firstCell, out int firstIndex) &&
        m_towerSpawnIndexByCell.TryGetValue(secondCell, out int secondIndex) &&
        TrySwapTowers(firstIndex, secondIndex);

    public List<KeyValuePair<int, TowerController>> GetTowerOccupancyByIndexSnapshot()
    {
        List<KeyValuePair<int, TowerController>> result = new List<KeyValuePair<int, TowerController>>();
        for (int index = 1; index <= m_towerSpawnCellsByIndex.Count; index++)
        {
            if (TryGetTowerAt(index, out TowerController tower))
                result.Add(new KeyValuePair<int, TowerController>(index, tower));
        }
        return result;
    }

    #endregion

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (tilemap == null || m_pathGridPositions == null || m_pathGridPositions.Count == 0) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        for (int i = 0; i < m_pathGridPositions.Count; i++)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(m_pathGridPositions[i]);
            UnityEditor.Handles.Label(worldPos + new Vector3(-0.1f, 0.1f, 0), i.ToString(), style);

            if (i < m_pathGridPositions.Count - 1)
            {
                Vector3 nextWorldPos = tilemap.GetCellCenterWorld(m_pathGridPositions[i + 1]);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(worldPos, nextWorldPos);
            }
        }
#endif
    }

    #endregion

    #region 고정 타워 타일 버프

    [ContextMenu("타워 스폰 타일 버프 랜덤 생성")]
    public void AssignRandomTowerTileBuffs()
    {
        if (m_towerSpawnTilemap == null)
        {
            Debug.LogError("[TileManager] 타워 소환 타일맵이 연결되지 않았습니다.");
            return;
        }

        // 스테이지 시작 시 버프 데이터를 새로 만든다.
        ClearMapTileEffects(m_towerMapBuffEffects);
        m_towerTileBuffByIndex.Clear();

        // 타워 소환 타일 번호를 한 번 섞고, 앞에서부터 종류별로 배정한다.
        List<int> shuffledIndices = new List<int>(TowerSpawnTileCount);
        for (int index = 1; index <= TowerSpawnTileCount; index++)
        {
            shuffledIndices.Add(index);
        }

        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[randomIndex];
            shuffledIndices[randomIndex] = temp;
        }

        int nextIndex = 0;
        SetRandomTowerTiles(m_towerSpawnTilemap, shuffledIndices, ref nextIndex, towerAttackPowerCount, TowerTileBuffType.AttackPowerUp, towerAttackPowerColor);
        SetRandomTowerTiles(m_towerSpawnTilemap, shuffledIndices, ref nextIndex, towerActionCount, TowerTileBuffType.ActionCountUp, towerActionCountColor);
        SetRandomTowerTiles(m_towerSpawnTilemap, shuffledIndices, ref nextIndex, towerAttackCountTileCount, TowerTileBuffType.AttackCountUp, towerAttackCountColor);
    }

    private void SetRandomTowerTiles(Tilemap map, List<int> shuffledIndices, ref int nextIndex, int count, TowerTileBuffType type, Color color)
    {
        for (int i = 0; i < count && nextIndex < shuffledIndices.Count; i++)
        {
            int tileIndex = shuffledIndices[nextIndex++];
            Vector3Int selectedPos = m_towerSpawnCellsByIndex[tileIndex];

            // 실제 효과는 맵 버프 데이터에서 조회한다.
            m_towerTileBuffByIndex[tileIndex] = type;

            // 이펙트는 시각 표시만 담당한다.
            SpawnMapTileEffect(map, selectedPos, color, "TowerMapBuff", m_towerMapBuffEffects, false);
        }
    }

    private void SpawnMapTileEffect(
        Tilemap map,
        Vector3Int cellPosition,
        Color color,
        string effectNamePrefix,
        List<GameObject> createdEffects,
        bool isPath)
    {
        GameObject prefab = isPath && m_pathMapBuffVisualPrefab != null
            ? m_pathMapBuffVisualPrefab
            : mapTileBuffEffectPrefab != null ? mapTileBuffEffectPrefab.gameObject : null;
        if (prefab == null)
        {
            Debug.LogWarning("[TileManager] Map Tile Buff Effect Prefab이 연결되지 않았습니다.");
            return;
        }

        // 부모를 지정하지 않으면 TileManager 하위에 생성한다.
        Transform parent = mapTileBuffEffectParent != null ? mapTileBuffEffectParent : transform;

        // 타일맵의 셀 중심에 이펙트를 배치한다.
        Vector3 worldPosition = map.GetCellCenterWorld(cellPosition);
        GameObject effect = Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        effect.name = $"{effectNamePrefix}_{cellPosition.x}_{cellPosition.y}";

        PathTileBubbleEffect bubbles = effect.GetComponent<PathTileBubbleEffect>();
        TileSatelliteOrbiter orbiter = effect.GetComponent<TileSatelliteOrbiter>();
        if (bubbles != null)
        {
            // 부모 배율을 감안한 타일 크기를 전달한다. 방향표시보다 뒤에 그린다.
            Vector3 width = map.CellToWorld(cellPosition + Vector3Int.right) - map.CellToWorld(cellPosition);
            Vector3 height = map.CellToWorld(cellPosition + Vector3Int.up) - map.CellToWorld(cellPosition);
            Vector2 localSize = new Vector2(
                effect.transform.InverseTransformVector(width).magnitude,
                effect.transform.InverseTransformVector(height).magnitude);
            TilemapRenderer tileRenderer = map.GetComponent<TilemapRenderer>();
            bubbles.Configure(color, localSize,
                tileRenderer != null ? tileRenderer.sortingLayerID : 0,
                tileRenderer != null ? tileRenderer.sortingOrder : 0);
            // 같은 정렬 순서의 바닥보다 카메라 쪽에 배치한다.
            effect.transform.position += new Vector3(0f, 0f, -0.02f);
        }
        else if (orbiter != null) orbiter.SetColor(color);
        else
        {
            SpriteRenderer[] renderers = effect.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = color;
        }
        createdEffects.Add(effect);
    }

    private static void ClearMapTileEffects(List<GameObject> effects)
    {
        foreach (GameObject effect in effects)
        {
            if (effect == null) continue;
            if (Application.isPlaying) Destroy(effect);
            else DestroyImmediate(effect);
        }
        effects.Clear();
    }

    public TowerTileBuffType GetTowerTileBuffAt(int index)
    {
        if (m_towerTileBuffByIndex.TryGetValue(index, out TowerTileBuffType buffType))
        {
            return buffType;
        }
        return TowerTileBuffType.Normal;
    }

    public TowerTileBuffType GetTowerTileBuffAt(Vector3Int cell) =>
        m_towerSpawnIndexByCell.TryGetValue(cell, out int index)
            ? GetTowerTileBuffAt(index)
            : TowerTileBuffType.Normal;

    #endregion

    #region 타워 버프와 패스 디버프

    public IReadOnlyList<TowerTileBuffEffect> GetTowerBuffEffectsAt(int index)
    {
        return m_towerBuffEffectsByIndex.TryGetValue(index, out List<TowerTileBuffEffect> effects)
            ? effects
            : System.Array.Empty<TowerTileBuffEffect>();
    }

    public IReadOnlyList<TowerTileBuffEffect> GetTowerBuffEffectsAt(Vector3Int cell) =>
        m_towerSpawnIndexByCell.TryGetValue(cell, out int index)
            ? GetTowerBuffEffectsAt(index)
            : System.Array.Empty<TowerTileBuffEffect>();

    public IReadOnlyList<PathTileDebuffEffect> GetPathDebuffEffectsAt(int index)
    {
        return m_pathDebuffEffectsByIndex.TryGetValue(index, out List<PathTileDebuffEffect> effects)
            ? effects
            : System.Array.Empty<PathTileDebuffEffect>();
    }

    public IReadOnlyList<PathTileDebuffEffect> GetPathDebuffEffectsAt(Vector3Int cell) =>
        m_pathIndexByCell.TryGetValue(cell, out int index)
            ? GetPathDebuffEffectsAt(index)
            : System.Array.Empty<PathTileDebuffEffect>();

    /// <summary>해당 타워가 남긴 버프 효과를 모든 소환 타일 인덱스에서 제거합니다.</summary>
    public void RemoveTowerBuffEffectsBySource(int sourceID)
    {
        List<int> emptyIndices = new List<int>();
        foreach (KeyValuePair<int, List<TowerTileBuffEffect>> pair in m_towerBuffEffectsByIndex)
        {
            pair.Value.RemoveAll(effect => effect.SourceID == sourceID);
            if (pair.Value.Count == 0) emptyIndices.Add(pair.Key);
        }

        foreach (int index in emptyIndices) m_towerBuffEffectsByIndex.Remove(index);
    }

    public void AddTowerBuffEffect(Vector3Int cell, int sourceID, BuffTarget target, int tier, float abilityValue, float duration)
    {
        if (m_towerSpawnIndexByCell.TryGetValue(cell, out int index))
            AddTowerBuffEffect(index, sourceID, target, tier, abilityValue, duration);
    }

    public void AddTowerBuffEffect(int index, int sourceID, BuffTarget target, int tier, float abilityValue, float duration)
    {
        if (target == BuffTarget.None || !m_towerSpawnCellsByIndex.ContainsKey(index)) return;
        if (!m_towerBuffEffectsByIndex.TryGetValue(index, out List<TowerTileBuffEffect> effects))
        {
            effects = new List<TowerTileBuffEffect>();
            m_towerBuffEffectsByIndex.Add(index, effects);
        }

        effects.Add(new TowerTileBuffEffect
        {
            SourceID = sourceID,
            Target = target,
            Tier = Mathf.Clamp(tier, 1, 5),
            AbilityValue = abilityValue,
            RemainingTurns = Mathf.Max(1, Mathf.RoundToInt(duration)),
            ApplicationVersion = 0,
            StackThreshold = 0,
            StackLifetime = 0
        });
    }

    /// <summary>범위 셀에 이번 예열 효과를 기록합니다. 스택은 각 타워가 관리합니다.</summary>
    public void SetFirePreheatEffects(
        IReadOnlyList<Vector3Int> cells,
        int sourceID,
        int tier,
        float abilityValue,
        int stackThreshold,
        int stackLifetime,
        float power = 0f,
        int stackGain = 1)
    {
        List<int> indices = new List<int>();
        if (cells != null)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (m_towerSpawnIndexByCell.TryGetValue(cells[i], out int index)) indices.Add(index);
            }
        }
        SetFirePreheatEffects(indices, sourceID, tier, abilityValue, stackThreshold, stackLifetime, power, stackGain);
    }

    /// <summary>
    /// stackLifetime은 스킬 1(예열)과 스킬 2(과열)의 효과가 유지되는 턴 수(Duration)입니다.
    /// power는 스킬 1의 세기, stackGain은 한 번 행동에 쌓는 스택 수(AttackCount)입니다.
    /// </summary>
    public void SetFirePreheatEffects(
        IReadOnlyList<int> indices,
        int sourceID,
        int tier,
        float abilityValue,
        int stackThreshold,
        int stackLifetime,
        float power = 0f,
        int stackGain = 1)
    {
        RemoveTowerBuffEffectsBySource(sourceID);
        if (indices == null || indices.Count == 0) return;

        int nextVersion = m_buffApplicationVersions.TryGetValue(sourceID, out int version) ? version + 1 : 1;
        m_buffApplicationVersions[sourceID] = nextVersion;

        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (!m_towerSpawnCellsByIndex.ContainsKey(index)) continue;
            if (!m_towerBuffEffectsByIndex.TryGetValue(index, out List<TowerTileBuffEffect> effects))
            {
                effects = new List<TowerTileBuffEffect>();
                m_towerBuffEffectsByIndex.Add(index, effects);
            }

            effects.Add(new TowerTileBuffEffect
            {
                SourceID = sourceID,
                Target = BuffTarget.Fire,
                Tier = Mathf.Clamp(tier, 1, 5),
                AbilityValue = abilityValue,
                // 다음 에너미 턴 시작 전까지 유지한다.
                RemainingTurns = 1,
                ApplicationVersion = nextVersion,
                StackThreshold = Mathf.Max(1, stackThreshold),
                StackLifetime = Mathf.Max(1, stackLifetime),
                Power = power,
                StackGain = Mathf.Max(1, stackGain)
            });
        }
    }

    /// <summary>타워 버프 효과의 남은 턴을 1 줄입니다.</summary>
    public void AdvanceTowerBuffEffectTurns()
    {
        List<int> emptyIndices = new List<int>();
        foreach (KeyValuePair<int, List<TowerTileBuffEffect>> pair in m_towerBuffEffectsByIndex)
        {
            pair.Value.RemoveAll(effect => --effect.RemainingTurns <= 0);
            if (pair.Value.Count == 0) emptyIndices.Add(pair.Key);
        }

        foreach (int index in emptyIndices) m_towerBuffEffectsByIndex.Remove(index);
    }

    /// <summary>한 번의 디버프 적용을 여러 패스 타일에 같은 번호로 기록합니다.</summary>
    public void SetPathDebuffEffects(
        IReadOnlyList<int> indices,
        int sourceID,
        DebuffTarget target,
        int tier,
        float abilityValue,
        float duration,
        int zonePathIndex,
        int sourceCreationOrder = int.MaxValue,
        float power = 0f,
        int stackGain = 0,
        int stackThreshold = 0)
    {
        if (target == DebuffTarget.None) return;

        RemovePathDebuffEffectsBySource(sourceID);
        if (indices == null || indices.Count == 0) return;
        int nextVersion = m_debuffApplicationVersions.TryGetValue(sourceID, out int version) ? version + 1 : 1;
        m_debuffApplicationVersions[sourceID] = nextVersion;

        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (!m_pathCellsByIndex.ContainsKey(index)) continue;
            if (!m_pathDebuffEffectsByIndex.TryGetValue(index, out List<PathTileDebuffEffect> effects))
            {
                effects = new List<PathTileDebuffEffect>();
                m_pathDebuffEffectsByIndex.Add(index, effects);
            }

            effects.Add(new PathTileDebuffEffect
            {
                SourceID = sourceID,
                SourceCreationOrder = sourceCreationOrder,
                Target = target,
                Tier = Mathf.Clamp(tier, 1, 5),
                AbilityValue = abilityValue,
                Duration = Mathf.Max(1, Mathf.RoundToInt(duration)),
                StackThreshold = stackThreshold > 0 ? stackThreshold : Mathf.Max(1, Mathf.RoundToInt(duration)),
                ApplicationVersion = nextVersion,
                ZonePathIndex = zonePathIndex,
                Power = power,
                StackGain = stackGain
            });
        }
    }

    /// <summary>
    /// 장판 위치를 먼저 등록합니다. 적용 번호 0은 UI 표시용이며 적에게 적용되지 않습니다.
    /// 미리보기와 실제 적용이 같은 범위 계산을 사용할 수 있도록 여러 칸을 등록합니다.
    /// </summary>
    public void RegisterPathDebuffZone(IReadOnlyList<int> indices, int sourceID, DebuffTarget target, int tier, float abilityValue, float duration, int zonePathIndex, int sourceCreationOrder = int.MaxValue, float power = 0f, int stackGain = 0, int stackThreshold = 0)
    {
        if (target == DebuffTarget.None) return;

        RemovePathDebuffEffectsBySource(sourceID);
        if (indices == null) return;

        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (!m_pathCellsByIndex.ContainsKey(index)) continue;
            if (!m_pathDebuffEffectsByIndex.TryGetValue(index, out List<PathTileDebuffEffect> effects))
            {
                effects = new List<PathTileDebuffEffect>();
                m_pathDebuffEffectsByIndex.Add(index, effects);
            }

            effects.Add(new PathTileDebuffEffect
            {
                SourceID = sourceID,
                SourceCreationOrder = sourceCreationOrder,
                Target = target,
                Tier = Mathf.Clamp(tier, 1, 5),
                AbilityValue = abilityValue,
                Duration = Mathf.Max(1, Mathf.RoundToInt(duration)),
                StackThreshold = stackThreshold > 0 ? stackThreshold : Mathf.Max(1, Mathf.RoundToInt(duration)),
                ApplicationVersion = 0,
                ZonePathIndex = zonePathIndex,
                Power = power,
                StackGain = stackGain
            });
        }
    }

    public void RemovePathDebuffEffectsBySource(int sourceID)
    {
        List<int> emptyIndices = new List<int>();
        foreach (KeyValuePair<int, List<PathTileDebuffEffect>> pair in m_pathDebuffEffectsByIndex)
        {
            pair.Value.RemoveAll(effect => effect.SourceID == sourceID);
            if (pair.Value.Count == 0) emptyIndices.Add(pair.Key);
        }

        foreach (int index in emptyIndices) m_pathDebuffEffectsByIndex.Remove(index);
    }

    /// <summary>타워가 남긴 모든 동적 타일 효과와 적용 번호를 제거합니다.</summary>
    public void RemoveAllDynamicEffectsBySource(int sourceID)
    {
        RemoveTowerBuffEffectsBySource(sourceID);
        RemovePathDebuffEffectsBySource(sourceID);
        m_buffApplicationVersions.Remove(sourceID);
        m_debuffApplicationVersions.Remove(sourceID);
    }

    #endregion

    #region 고정 패스 타일 버프

    [ContextMenu("랜덤 특수 타일 생성")]
    public void AssignRandomSpecialTiles()
    {
        ClearMapTileEffects(m_pathMapBuffEffects);
        m_pathTileBuffByIndex.Clear();

        if (m_pathGridPositions.Count < 3)
        {
            Debug.LogWarning("[TileManager] 특수 타일을 배치하기 위한 경로 타일 개수가 부족합니다.");
            return;
        }

        // 0번(Start)도 평범한 칸이라 후보에 넣습니다.
        List<Vector3Int> availableTiles = new List<Vector3Int>();
        HashSet<Vector3Int> uniqueCells = new HashSet<Vector3Int>();
        for (int i = 0; i < m_pathGridPositions.Count; i++)
        {
            if (uniqueCells.Add(m_pathGridPositions[i])) availableTiles.Add(m_pathGridPositions[i]);
        }

        // 비프로스트를 먼저 깔아야 나머지 특수 타일이 그 두 칸을 피합니다.
        // 첫 칸은 무작위, 다른 칸은 길 인덱스 수의 절반만큼 떨어진 맞은편입니다.
        m_bifrostIndexA = Random.Range(0, PathTileCount);
        m_bifrostIndexB = WrapPathIndex(m_bifrostIndexA + PathTileCount / 2);
        PlaceBifrostTile(m_bifrostIndexA, availableTiles);
        PlaceBifrostTile(m_bifrostIndexB, availableTiles);

        SetRandomTiles(availableTiles, defendTileCount, PathTileBuffType.DefendTile, defendColor);
        SetRandomTiles(availableTiles, moveTileCount, PathTileBuffType.MoveTile, moveColor);
        SetRandomTiles(availableTiles, actionTileCount, PathTileBuffType.ActionTile, actionColor);
        SetRandomTiles(availableTiles, healTileCount, PathTileBuffType.HealTile, healColor);
        SetRandomTiles(availableTiles, purifyTileCount, PathTileBuffType.PurifyTile, purifyColor);
    }

    // 비프로스트를 놓고, 무작위 타일 후보(pool)에서 그 칸을 뺍니다.
    private void PlaceBifrostTile(int index, List<Vector3Int> pool)
    {
        if (!m_pathCellsByIndex.TryGetValue(index, out Vector3Int cell)) return;

        if (m_pathIndicesByCell.TryGetValue(cell, out List<int> visits))
        {
            foreach (int visit in visits) m_pathTileBuffByIndex[visit] = PathTileBuffType.Bifrost;
        }

        SpawnMapTileEffect(tilemap, cell, bifrostColor, "PathMapBuff", m_pathMapBuffEffects, true);
        pool.Remove(cell);
    }

    /// <summary>
    /// 비프로스트 칸이면 건너편 칸의 인덱스를 돌려줍니다. 같은 칸을 여러 번 지나는 길도 칸(좌표)으로 비교합니다.
    /// </summary>
    public bool TryGetBifrostDestination(int index, out int destination)
    {
        destination = -1;
        if (!m_pathCellsByIndex.TryGetValue(index, out Vector3Int cell)) return false;
        if (!m_pathCellsByIndex.TryGetValue(m_bifrostIndexA, out Vector3Int cellA)) return false;
        if (!m_pathCellsByIndex.TryGetValue(m_bifrostIndexB, out Vector3Int cellB)) return false;

        if (cell == cellA) destination = m_bifrostIndexB;
        else if (cell == cellB) destination = m_bifrostIndexA;
        return destination >= 0 && m_pathCellsByIndex[destination] != cell;
    }

    private void SetRandomTiles(List<Vector3Int> pool, int count, PathTileBuffType type, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            int randomIndex = Random.Range(0, pool.Count);
            Vector3Int selectedPos = pool[randomIndex];

            if (m_pathIndicesByCell.TryGetValue(selectedPos, out List<int> visits))
            {
                foreach (int index in visits) m_pathTileBuffByIndex[index] = type;
            }

            // 효과는 타일 인덱스에 저장하고, 좌표는 이펙트 배치에만 사용합니다.
            SpawnMapTileEffect(tilemap, selectedPos, color, "PathMapBuff", m_pathMapBuffEffects, true);

            pool.RemoveAt(randomIndex);
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

    public PathTileBuffType GetTileTypeAt(int index)
    {
        if (m_pathTileBuffByIndex.TryGetValue(index, out PathTileBuffType type))
        {
            return type;
        }
        return PathTileBuffType.Normal;
    }

    public PathTileBuffType GetTileTypeAt(Vector3Int cell) =>
        m_pathIndexByCell.TryGetValue(cell, out int index)
            ? GetTileTypeAt(index)
            : PathTileBuffType.Normal;

    /// <summary>월드 위치의 패스 타일 존재 여부와 셀 좌표를 반환합니다.</summary>
    public bool TryGetPathCellAtWorldPosition(Vector3 worldPosition, out Vector3Int cell)
    {
        cell = default;
        if (tilemap == null) return false;

        cell = tilemap.WorldToCell(worldPosition);
        return tilemap.HasTile(cell);
    }

    #endregion
}
