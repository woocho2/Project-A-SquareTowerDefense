using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public enum CombineMode
{
    ExactMatch,
    TypeMatch,
    VariantMatch
}

public class TowerManager : MonoBehaviour
{
    [Header("테스트")]
    private bool m_isTestMode = false;
    private string m_testInputBuffer = "";

    // ==========================================================================================================
    // =============================================== 변수선언 =================================================
    // ==========================================================================================================
    public static TowerManager Instance { get; private set; }

    [Tooltip("타워를 생성할 수 있는 타일맵을 지정합니다.")]
    [SerializeField] private Tilemap m_spawnPoint;
    [Header("시너지 타워 생성 위치")]
    [Tooltip("시너지 조건을 만족했을 때 생성 전용 타워가 배치될 타일맵입니다. 비어 있으면 일반 타워 타일맵을 사용합니다.")]
    [SerializeField] private Tilemap m_synergySpawnPoint;

    [Tooltip("CSV에서 런타임으로 생성한 타워 데이터 배열입니다.")]
    [SerializeField] private TowerData[] m_towerData;

    [Header("타워 공통 프리팹")]
    [Tooltip("모든 타워가 공통으로 사용하는 원본 프리팹입니다. 티어·색상·엠블럼은 소환 시 TowerVisual이 ID에 맞춰 교체합니다.")]
    [SerializeField] private GameObject m_towerBasePrefab;

    [Header("타워 생성 및 업그레이드 비용")]
    private int BuildCost = 50;
    private const int TierUpgradeBaseGemCost = 5;

    [Header("타워 순차 행동")]
    [Tooltip("공격 타워의 투사체가 명중해 이펙트가 발동한 뒤 다음 공격 타워가 행동하기까지의 간격입니다.")]
    [SerializeField, Min(0f)] private float m_nextAttackTowerDelay = 0.3f;

    public struct GridTowerInfo
    {
        public TowerController Controller;
        public int Tier;
        public int Type;
        public int Variant;
    }

    private Dictionary<Vector3Int, GridTowerInfo> m_towersOnGrid = new Dictionary<Vector3Int, GridTowerInfo>();
    private Dictionary<int, TowerStats>      m_globalTowerStats  = new Dictionary<int, TowerStats>();
    private float m_sharedDarknessAbilityValue;
    private Dictionary<int, List<TowerData>> m_towerTier         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerType         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerVariant      = new Dictionary<int, List<TowerData>>();

    public event Action<int> OnTowerTypeUpgrade;

    private sealed class SynergyDefinition
    {
        public readonly int TowerID;
        public readonly string Name;
        public readonly int VisualTowerID;
        public readonly int[] RequiredPatterns;

        public SynergyDefinition(int towerID, string name, int visualTowerID, params int[] requiredPatterns)
        {
            TowerID = towerID;
            Name = name;
            VisualTowerID = visualTowerID;
            RequiredPatterns = requiredPatterns;
        }

        public bool IsSatisfiedBy(HashSet<int> activePatterns)
        {
            for (int i = 0; i < RequiredPatterns.Length; i++)
            {
                if (!activePatterns.Contains(RequiredPatterns[i])) return false;
            }
            return true;
        }
    }

    private sealed class ActiveSynergyTower
    {
        public TowerController Controller;
        public TowerData Data;
        public Vector3Int Cell;
    }

    // 시너지 기획.txt 순서. 각 항목은 4티어 이상 재료 문양을 모두 보유하면 생성된다.
    private static readonly SynergyDefinition[] s_synergyDefinitions =
    {
        new SynergyDefinition(6001, "오딘", 5104, TowerPattern.SPEAR, TowerPattern.LIGHT, TowerPattern.DARKNESS),
        new SynergyDefinition(6002, "토르", 5106, TowerPattern.HAMMER, TowerPattern.ELECTRICITY, TowerPattern.WIND),
        new SynergyDefinition(6003, "로키", 5102, TowerPattern.BOW, TowerPattern.FIRE, TowerPattern.DARKNESS),
        new SynergyDefinition(6004, "헬", 5101, TowerPattern.SWORD, TowerPattern.AXE, TowerPattern.DARKNESS),
        new SynergyDefinition(6005, "수르트", 5101, TowerPattern.SWORD, TowerPattern.FIRE, TowerPattern.EARTH),
        new SynergyDefinition(6006, "헤임달", 5101, TowerPattern.SWORD, TowerPattern.WIND, TowerPattern.LIGHT),
        new SynergyDefinition(6007, "발드르", 5103, TowerPattern.SHIELD, TowerPattern.EARTH, TowerPattern.LIGHT),
        new SynergyDefinition(6008, "스카디", 5102, TowerPattern.BOW, TowerPattern.ICE, TowerPattern.WIND),
        new SynergyDefinition(6009, "비다르", 5104, TowerPattern.SPEAR, TowerPattern.SHIELD, TowerPattern.ICE),
        new SynergyDefinition(6010, "이미르", 5106, TowerPattern.HAMMER, TowerPattern.ICE, TowerPattern.EARTH),
        new SynergyDefinition(6011, "트루드", 5103, TowerPattern.SHIELD, TowerPattern.AXE, TowerPattern.ELECTRICITY),
        new SynergyDefinition(6012, "발키리", 5102, TowerPattern.BOW, TowerPattern.SPEAR, TowerPattern.ELECTRICITY),
        new SynergyDefinition(6013, "브록 & 에이트리", 5105, TowerPattern.AXE, TowerPattern.HAMMER, TowerPattern.FIRE)
    };

    private readonly Dictionary<int, ActiveSynergyTower> m_activeSynergyTowers =
        new Dictionary<int, ActiveSynergyTower>();

    // ==========================================================================================================
    // ================================================= 초기화 =================================================
    // ==========================================================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadTowerDataFromCSV();
        EvaluateSynergyTowers();
        if (GlobalProjectileManager.Instance != null)
        {
            GlobalProjectileManager.Instance.InitializeProjectileDatabase();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            m_isTestMode = !m_isTestMode;
            m_testInputBuffer = ""; // 입력 초기화
            Debug.Log($"[치트] 특정 타워 소환 모드 {(m_isTestMode ? "활성화! 4자리 숫자를 입력하세요." : "비활성화")}");
            return;
        }

        if (m_isTestMode)
        {
            CheckDigitInput();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LoadTowerDataFromCSV()
    {
        if (m_towerBasePrefab == null)
        {
            Debug.LogError("TowerBase 프리팹이 할당되지 않았습니다. TowerManager의 Tower Base Prefab 필드에 TowerBase를 연결하세요.");
            return;
        }

        TextAsset csvData = Resources.Load<TextAsset>("TowerDataCSV");
        if (csvData == null)
        {
            Debug.LogError("Resources 폴더에서 TowerDataCSV 파일을 찾을 수 없습니다.");
            return;
        }

        string[] lines = csvData.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        List<TowerData> loadedTowers = new List<TowerData>();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 20 || string.IsNullOrWhiteSpace(values[0])) continue;

            TowerData newData = ScriptableObject.CreateInstance<TowerData>();

            try
            {
                newData.towerID = int.Parse(values[0].Trim());
                newData.towerName = values[1].Replace("\"", "").Trim();
                newData.towerLevel = int.Parse(values[2].Trim());
                newData.power = float.Parse(values[3].Trim());
                newData.range = float.Parse(values[4].Trim());
                newData.action = Mathf.Max(1, int.Parse(values[5].Trim()));
                newData.attackCount = Mathf.Max(1, Mathf.RoundToInt(float.Parse(values[6].Trim())));
                newData.splashRadius = Mathf.Max(0, Mathf.RoundToInt(float.Parse(values[7].Trim())));
                newData.additionalHitCount = Mathf.Max(0, int.Parse(values[8].Trim()));
                newData.isCritical = bool.Parse(values[9].Trim());
                newData.criticalRate = float.Parse(values[10].Trim());
                newData.criticalDamage = float.Parse(values[11].Trim());
                newData.duration = float.Parse(values[12].Trim());
                newData.abilityValue = float.Parse(values[13].Trim());

                newData.attackType = ParseEnum<AttackType>(values[14]);
                string layerName = values[15].Trim();
                newData.targetLayer = !string.IsNullOrEmpty(layerName) ? LayerMask.GetMask(layerName) : 0;
                newData.buffTarget = string.IsNullOrEmpty(values[16].Trim()) ? BuffTarget.None : ParseEnum<BuffTarget>(values[16]);
                newData.debuffTarget = string.IsNullOrEmpty(values[17].Trim()) ? DebuffTarget.None : ParseEnum<DebuffTarget>(values[17]);

                newData.projectileSpeed = float.Parse(values[18].Trim());
                newData.hitEffectID = int.Parse(values[19].Trim());
                newData.targetPriority = values.Length > 20 && !string.IsNullOrWhiteSpace(values[20])
                    ? ParseEnum<TargetPriority>(values[20])
                    : TargetPriority.Closest;
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"[CSV 데이터 오류] 엑셀의 {i + 1}번째 줄 파싱 실패. 내용: {lines[i]}");
                continue;
            }

            // 모든 타워는 TowerBase 하나만 소환한다.
            // 타워 ID에 따른 스탯과 외형은 TowerController.Init / TowerVisual.Apply에서 주입된다.
            newData.towerPrefab = m_towerBasePrefab;

            int sharedProjID = newData.towerID % 1000;
            string projPrefabName = sharedProjID.ToString();
            newData.projectilePrefab = Resources.Load<GameObject>($"Projectiles/{projPrefabName}");

            loadedTowers.Add(newData);

            // [핵심] 생성 즉시 ToTowerStats()를 호출하여 글로벌 스탯 딕셔너리에 등록
            m_globalTowerStats[newData.towerID] = newData.ToTowerStats();

            // 분류 딕셔너리 적재
            int tier = newData.towerID / 1000;
            int type = (newData.towerID % 1000) / 100;
            int variant = newData.towerID % 100;

            if (!m_towerTier.ContainsKey(tier)) m_towerTier[tier] = new List<TowerData>();
            m_towerTier[tier].Add(newData);

            if (!m_towerType.ContainsKey(type)) m_towerType[type] = new List<TowerData>();
            m_towerType[type].Add(newData);

            if (!m_towerVariant.ContainsKey(variant)) m_towerVariant[variant] = new List<TowerData>();
            m_towerVariant[variant].Add(newData);
        }

        m_towerData = loadedTowers.ToArray();

        Debug.Log($"총 {m_towerData.Length}개의 타워 데이터 및 글로벌 스탯 로드 완료.");
    }


    public TowerStats GetGlobalStats(int towerID)
    {
        if (m_globalTowerStats.ContainsKey(towerID))
        {
            return m_globalTowerStats[towerID];
        }
        else
        {
            Debug.LogWarning($"타워 이름 '{towerID}'에 대한 글로벌 스탯이 존재하지 않습니다.");
            return default;
        }
    }

    // ==========================================================================================================
    // ================================================ 타워생성 ================================================
    // ==========================================================================================================

    public void BuildTower()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        if (!CurrencyManager.Instance.HasEnoughMoney(BuildCost))
        {
            Debug.Log("돈이 부족합니다.");
            return;
        }

        if (!m_towerTier.ContainsKey(1) || m_towerTier[1].Count == 0)
        {
            Debug.LogError("생성 가능한 1단계 타워 데이터가 없습니다.");            
            return;
        }

        List<TowerData> buildableTowers = m_towerTier[1];

        Vector3Int? emptyCell = GetFirstEmptyCell();

        if (emptyCell.HasValue)
        {
            int randomIndex = UnityEngine.Random.Range(0, buildableTowers.Count);
            TowerData selectedData = buildableTowers[randomIndex];

            if (selectedData.towerPrefab == null)
            {
                Debug.LogError($"타워 데이터 {selectedData.towerName}에 할당된 프리팹이 없습니다.");
                return;
            }

            Vector3 spawnPos = CellToWorld(emptyCell.Value);

            GameObject spawnedTower = Instantiate(selectedData.towerPrefab, spawnPos, Quaternion.identity);
            TowerController towerController = spawnedTower.GetComponent<TowerController>();

            if (towerController != null)
            {
                towerController.Init(selectedData, GetGlobalStats(selectedData.towerID));
            }
            else
            {
                Debug.LogError($"생성된 타워 프리팹에 TowerController 컴포넌트가 없습니다: {selectedData.towerName}");
            }

            GridTowerInfo newInfo = new GridTowerInfo
            {
                Controller = towerController,
                Tier       = selectedData.towerID / 1000,
                Type       = (selectedData.towerID % 1000) / 100,
                Variant    = selectedData.towerID % 100
            };

            m_towersOnGrid.Add(emptyCell.Value, newInfo);

            CurrencyManager.Instance.SpendMoney(BuildCost);
            if (BuildCost < 300) BuildCost += 2;
            EvaluateSynergyTowers();
        }
        else
        {
            Debug.LogWarning("더 이상 타워를 건설할 공간이 없습니다!");
        }
    }

    private Vector3Int? GetFirstEmptyCell()
    {
        BoundsInt bounds = m_spawnPoint.cellBounds;

        // 위(Y 최대치)에서 아래로, 왼쪽(X 최소치)에서 오른쪽으로 순차 탐색
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                // 타일맵에 타일이 존재하고, 현재 타워가 배치되어 있지 않은 첫 자리
                if (m_spawnPoint.HasTile(pos) && !m_towersOnGrid.ContainsKey(pos))
                {
                    return pos; // 발견 즉시 반환 (조기 종료)
                }
            }
        }

        return null; // 모든 자리가 가득 찬 경우
    }

    private Vector3 CellToWorld(Vector3Int cell)
    {
        return m_spawnPoint.GetCellCenterWorld(cell);
    }
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return m_spawnPoint.WorldToCell(worldPos);
    }

    public int GetBuildCost()
    {
        return BuildCost;
    }

    /// <summary>
    /// 에너미 턴에 한 번 호출됩니다. 보드의 위쪽부터 아래쪽, 왼쪽부터 오른쪽 순서로 타워가 행동합니다.
    /// 화면의 첫 번째 슬롯을 1번으로 보았을 때 1번부터 증가하는 순서입니다.
    /// </summary>
    public void ExecuteTowerActionTurn()
    {
        StartCoroutine(ExecuteTowerActionTurnRoutine());
    }

    /// <summary>
    /// Runs exactly once per enemy turn before support actions.  Status duration is
    /// target-owned, while tile records from the previous turn expire here.
    /// </summary>
    public void BeginEnemyTurnForTowers()
    {
        List<KeyValuePair<Vector3Int, GridTowerInfo>> orderedTowers = GetOrderedTowers();
        TileManager.Instance?.AdvanceTowerBuffEffectTurns();
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            tower.Value.Controller?.AdvanceBuffTurn();
            tower.Value.Controller?.AdvanceFireStatusTurn();
        }
    }

    /// <summary>Support towers act before enemies read path effects this turn.</summary>
    public IEnumerator ExecuteSupportTowerActionTurnRoutine()
    {
        List<KeyValuePair<Vector3Int, GridTowerInfo>> orderedTowers = GetOrderedTowers();
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            TowerController controller = tower.Value.Controller;
            TowerData data = controller != null ? controller.GetTowerData() : null;
            if (data == null || (data.attackType != AttackType.Buff && data.attackType != AttackType.Debuff)) continue;

            controller.ExecuteTurnAction();
        }

        // A Fire field is only a delivery record.  Each target reads it once here,
        // after all support towers have placed their effects.
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            tower.Value.Controller?.RefreshFireTileStatus();
        }
        yield return null;
    }

    /// <summary>Damage towers act after support and tile effects have resolved.</summary>
    public IEnumerator ExecuteAttackTowerActionTurnRoutine()
    {
        List<KeyValuePair<Vector3Int, GridTowerInfo>> orderedTowers = GetOrderedTowers();
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            TowerController controller = tower.Value.Controller;
            TowerData data = controller != null ? controller.GetTowerData() : null;
            if (data == null || (data.attackType != AttackType.Splash && data.attackType != AttackType.Target)) continue;

            // Fire Splash skill 2 checks its own Duration counter independently of
            // the basic action gauge, then the normal skill 1 attack follows.
            controller.TryTriggerFireMeteor();
            bool didAct = controller.ExecuteTurnAction();
            if (!didAct && (GlobalProjectileManager.Instance == null || !GlobalProjectileManager.Instance.HasActiveProjectiles())) continue;

            while (GlobalProjectileManager.Instance != null && GlobalProjectileManager.Instance.HasActiveProjectiles())
            {
                yield return null;
            }

            if (m_nextAttackTowerDelay > 0f)
            {
                yield return new WaitForSeconds(m_nextAttackTowerDelay);
            }
        }
    }

    private List<KeyValuePair<Vector3Int, GridTowerInfo>> GetOrderedTowers()
    {
        List<KeyValuePair<Vector3Int, GridTowerInfo>> orderedTowers = new List<KeyValuePair<Vector3Int, GridTowerInfo>>(m_towersOnGrid);
        orderedTowers.Sort((left, right) =>
        {
            int rowComparison = right.Key.y.CompareTo(left.Key.y);
            return rowComparison != 0 ? rowComparison : left.Key.x.CompareTo(right.Key.x);
        });
        return orderedTowers;
    }

    /// <summary>
    /// 공격 타워는 자신의 투사체가 모두 명중하거나 회수된 뒤 지정된 간격만큼 기다리고 다음 공격 타워로 넘어갑니다.
    /// 버프/디버프 타워는 이 대기 계산에서 제외하며 자기 순서에 즉시 행동합니다.
    /// </summary>
    public IEnumerator ExecuteTowerActionTurnRoutine()
    {
        List<KeyValuePair<Vector3Int, GridTowerInfo>> orderedTowers = new List<KeyValuePair<Vector3Int, GridTowerInfo>>(m_towersOnGrid);
        orderedTowers.Sort((left, right) =>
        {
            int rowComparison = right.Key.y.CompareTo(left.Key.y);
            return rowComparison != 0 ? rowComparison : left.Key.x.CompareTo(right.Key.x);
        });

        // 이번 에너미 턴이 시작되었으므로, 기존 버프의 남은 턴을 먼저 차감합니다.
        // 버프 타워가 기록한 타일 효과도 같은 턴 규칙으로 함께 갱신합니다.
        TileManager.Instance?.AdvanceTowerBuffEffectTurns();
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            tower.Value.Controller?.AdvanceBuffTurn();
        }

        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            TowerController controller = tower.Value.Controller;
            if (controller == null) continue;

            bool didAct = controller.ExecuteTurnAction();
            if (!didAct) continue;

            TowerData towerData = controller.GetTowerData();
            if (towerData == null || towerData.attackType == AttackType.Buff || towerData.attackType == AttackType.Debuff)
            {
                continue;
            }

            // 이 타워가 만든 위성 투사체의 생성·발사·명중이 끝날 때까지 기다립니다.
            // 명중 순간 EffectManager가 이펙트를 재생하므로, 이 반복문을 벗어날 때는 공격 이펙트도 발동된 상태입니다.
            while (GlobalProjectileManager.Instance != null && GlobalProjectileManager.Instance.HasActiveProjectiles())
            {
                yield return null;
            }

            if (m_nextAttackTowerDelay > 0f)
            {
                yield return new WaitForSeconds(m_nextAttackTowerDelay);
            }
        }
    }
    // ==========================================================================================================
    // ================================================ 타워판매 ================================================
    // ==========================================================================================================

    public void SellTower(Vector3Int cell)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        if (m_towersOnGrid.TryGetValue(cell, out GridTowerInfo info))
        {
            if (info.Controller != null)
            {
                if (info.Tier < 6)
                {
                    Destroy(info.Controller.gameObject);
                }
                else
                {
                    info.Controller.transform.position = CellToWorld(cell);
                    Debug.Log("시너지 타워는 판매할 수 없습니다.");
                    return;
                }
            }

            m_towersOnGrid.Remove(cell);

            int Amount = (int)Mathf.Pow(3, info.Tier - 1);
            CurrencyManager.Instance.AddGem(Amount);
            EvaluateSynergyTowers();
        }
    }

    /// <summary>UI가 타워 스폰 타일의 현재 타워를 안전하게 조회할 때 사용합니다.</summary>
    public bool TryGetTowerAt(Vector3Int cell, out TowerController tower)
    {
        tower = null;
        if (!m_towersOnGrid.TryGetValue(cell, out GridTowerInfo info) || info.Controller == null)
        {
            return false;
        }

        tower = info.Controller;
        return true;
    }

    // ==========================================================================================================
    // =============================================== 업그레이드 ================================================
    // ==========================================================================================================

    /// <summary>
    /// 현재 타워 ID의 천의 자리(티어)를 한 단계 올리는 데 필요한 젬 비용을 반환합니다.
    /// 1→2: 5, 2→3: 15, 3→4: 45, 4→5: 135
    /// </summary>
    public int GetTierUpgradeCost(int towerID)
    {
        int currentTier = towerID / 1000;
        if (currentTier < 1 || currentTier >= 5) return 0;

        return TierUpgradeBaseGemCost * Mathf.RoundToInt(Mathf.Pow(3f, currentTier - 1));
    }

    /// <summary>
    /// 선택한 셀의 타워를 다음 티어의 같은 색상/문양 ID 타워로 교체합니다.
    /// 예: 1234 → 2234
    /// </summary>
    public bool UpgradeTowerTier(Vector3Int targetCell, out TowerController upgradedTower)
    {
        upgradedTower = null;

        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return false;

        if (!m_towersOnGrid.TryGetValue(targetCell, out GridTowerInfo currentInfo) || currentInfo.Controller == null)
        {
            Debug.LogWarning("티어 강화할 타워를 찾을 수 없습니다.");
            return false;
        }

        TowerData currentData = currentInfo.Controller.GetTowerData();
        TargetPriority previousTargetPriority = currentInfo.Controller.GetTargetPriority();
        if (currentData == null)
        {
            Debug.LogWarning("티어 강화할 타워 데이터가 없습니다.");
            return false;
        }

        int currentTier = currentData.towerID / 1000;
        int upgradeCost = GetTierUpgradeCost(currentData.towerID);
        if (upgradeCost <= 0)
        {
            Debug.Log("5티어 타워는 더 이상 티어 강화할 수 없습니다.");
            return false;
        }

        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.HasEnoughGem(upgradeCost))
        {
            Debug.Log($"티어 강화에 필요한 젬이 부족합니다. 필요 젬: {upgradeCost}");
            return false;
        }

        int upgradedTowerID = currentData.towerID + 1000;
        TowerData upgradedData = null;
        for (int i = 0; i < m_towerData.Length; i++)
        {
            if (m_towerData[i] != null && m_towerData[i].towerID == upgradedTowerID)
            {
                upgradedData = m_towerData[i];
                break;
            }
        }

        if (upgradedData == null || upgradedData.towerPrefab == null)
        {
            Debug.LogError($"티어 강화 결과 타워(ID: {upgradedTowerID})를 찾을 수 없습니다.");
            return false;
        }

        Vector3 spawnPosition = currentInfo.Controller.transform.position;
        GameObject spawnedTower = Instantiate(upgradedData.towerPrefab, spawnPosition, Quaternion.identity);
        upgradedTower = spawnedTower.GetComponent<TowerController>();
        if (upgradedTower == null)
        {
            Debug.LogError($"티어 강화 결과 프리팹에 TowerController가 없습니다: {upgradedData.towerName}");
            Destroy(spawnedTower);
            return false;
        }

        upgradedTower.Init(upgradedData, GetGlobalStats(upgradedData.towerID));
        upgradedTower.SetTargetPriority(previousTargetPriority);

        Destroy(currentInfo.Controller.gameObject);
        m_towersOnGrid[targetCell] = new GridTowerInfo
        {
            Controller = upgradedTower,
            Tier = currentTier + 1,
            Type = (upgradedData.towerID % 1000) / 100,
            Variant = upgradedData.towerID % 100
        };

        CurrencyManager.Instance.SpendGem(upgradeCost);
        EvaluateSynergyTowers();
        return true;
    }

    /// <summary>
    /// 매 3웨이브 완료 시 Earth 버프 타워가 주변의 낮은 티어 타워 하나를 무료 강화합니다.
    /// 1티어 Earth는 대상이 없으므로 자기 자신을 2티어로 강화합니다.
    /// </summary>
    public void ProcessEarthTowerWave(int completedWave)
    {
        if (completedWave <= 0 || completedWave % 3 != 0) return;

        List<KeyValuePair<Vector3Int, GridTowerInfo>> earthTowers = new List<KeyValuePair<Vector3Int, GridTowerInfo>>();
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> pair in m_towersOnGrid)
        {
            TowerData data = pair.Value.Controller != null ? pair.Value.Controller.GetTowerData() : null;
            if (data != null && data.attackType == AttackType.Buff && data.buffTarget == BuffTarget.Earth)
            {
                earthTowers.Add(pair);
            }
        }

        foreach (KeyValuePair<Vector3Int, GridTowerInfo> earthTower in earthTowers)
        {
            if (!m_towersOnGrid.TryGetValue(earthTower.Key, out GridTowerInfo liveInfo) || liveInfo.Controller == null) continue;

            TowerData earthData = liveInfo.Controller.GetTowerData();
            int earthTier = earthData.towerID / 1000;
            if (earthTier == 1)
            {
                UpgradeTowerTierWithoutCost(earthTower.Key);
                continue;
            }

            int tileRange = TowerAttackAction.ToTileRange(liveInfo.Controller.GetFinalStats().Range);
            List<Vector3Int> candidates = new List<Vector3Int>();

            foreach (KeyValuePair<Vector3Int, GridTowerInfo> candidate in m_towersOnGrid)
            {
                if (candidate.Key == earthTower.Key || candidate.Value.Controller == null) continue;
                if (candidate.Value.Tier >= earthTier || candidate.Value.Tier >= 5) continue;

                if (Mathf.Abs(candidate.Key.x - earthTower.Key.x) <= tileRange &&
                    Mathf.Abs(candidate.Key.y - earthTower.Key.y) <= tileRange)
                {
                    candidates.Add(candidate.Key);
                }
            }

            if (candidates.Count > 0)
            {
                UpgradeTowerTierWithoutCost(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
            }
        }
    }

    private bool UpgradeTowerTierWithoutCost(Vector3Int targetCell)
    {
        if (!m_towersOnGrid.TryGetValue(targetCell, out GridTowerInfo currentInfo) || currentInfo.Controller == null) return false;

        TowerData currentData = currentInfo.Controller.GetTowerData();
        TargetPriority previousTargetPriority = currentInfo.Controller.GetTargetPriority();
        if (currentData == null || currentData.towerID / 1000 >= 5) return false;

        int upgradedTowerID = currentData.towerID + 1000;
        TowerData upgradedData = Array.Find(m_towerData, data => data != null && data.towerID == upgradedTowerID);
        if (upgradedData == null || upgradedData.towerPrefab == null) return false;

        Vector3 spawnPosition = currentInfo.Controller.transform.position;
        GameObject spawnedTower = Instantiate(upgradedData.towerPrefab, spawnPosition, Quaternion.identity);
        TowerController upgradedTower = spawnedTower.GetComponent<TowerController>();
        if (upgradedTower == null)
        {
            Destroy(spawnedTower);
            return false;
        }

        upgradedTower.Init(upgradedData, GetGlobalStats(upgradedData.towerID));
        upgradedTower.SetTargetPriority(previousTargetPriority);
        Destroy(currentInfo.Controller.gameObject);
        m_towersOnGrid[targetCell] = new GridTowerInfo
        {
            Controller = upgradedTower,
            Tier = upgradedData.towerID / 1000,
            Type = (upgradedData.towerID % 1000) / 100,
            Variant = upgradedData.towerID % 100
        };
        EvaluateSynergyTowers();
        return true;
    }

    public void RefreshBuffOnAllTowers(BuffTarget target)
    {
        foreach (GridTowerInfo info in m_towersOnGrid.Values)
        {
            info.Controller?.RefreshExternalBuff(target);
        }
    }

    public float GetSharedDarknessAbility()
    {
        return m_sharedDarknessAbilityValue;
    }

    public void AddSharedDarknessAbility(int towerTier)
    {
        float gainedValue = towerTier switch
        {
            1 => .1f,
            2 => .25f,
            3 => .5f,
            4 => 1f,
            _ => 2f
        };

        m_sharedDarknessAbilityValue += gainedValue;
        RefreshBuffOnAllTowers(BuffTarget.Darkness);
    }

    public void UpgradeTower(int towerID)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        if (!m_globalTowerStats.ContainsKey(towerID)) return;

        int targetType = (towerID % 1000) / 100;

        TowerStats currentStats = m_globalTowerStats[towerID];

        if (currentStats.Level >= 5)
        {
            Debug.Log($"최대레벨에 도달하였습니다. 현재 레벨수치 {currentStats.Level}");
            return;
        }

        int currentUpgradeGemCost = (int)Mathf.Pow(2, currentStats.Level);

        if (!CurrencyManager.Instance.HasEnoughGem(currentUpgradeGemCost))
        {
            Debug.Log("업그레이드에 필요한 코스트가 부족합니다.");
            return;
        }

        CurrencyManager.Instance.SpendGem(currentUpgradeGemCost);

        if (!m_towerType.ContainsKey(targetType)) return;
        List<TowerData> towersToUpgrade = m_towerType[targetType];

        foreach (TowerData towerData in towersToUpgrade)
        {
            int key = towerData.towerID;

            TowerStats UpgradeStats = m_globalTowerStats[key];

            UpgradeStats.Level++;

            float multiplier = 1f + (Mathf.Pow(2f, UpgradeStats.Level - 2) / 10f);

            if (UpgradeStats.Level <= 5)
            {
                UpgradeStats.AttackPower      = towerData.power * multiplier;
                UpgradeStats.Range       = TowerAttackAction.ToWorldRange(towerData.range * multiplier);
                UpgradeStats.AttackCount = Mathf.Max(1, Mathf.RoundToInt(towerData.attackCount * multiplier));

                m_globalTowerStats[key] = UpgradeStats;
                OnTowerTypeUpgrade?.Invoke(key);
            }
        }
        // 2. 필드에 배치된 해당 타입 타워들에게 최신 스탯 주입(Push)
        foreach (var gridInfo in m_towersOnGrid.Values)
        {
            if (gridInfo.Type == targetType && gridInfo.Controller != null)
            {
                int currentTowerID = gridInfo.Controller.GetTowerData().towerID;
                gridInfo.Controller.UpdateBaseStats(m_globalTowerStats[currentTowerID]);
            }
        }
    }

    // ==========================================================================================================
    // ================================================ 타워합성 =================================================
    // ==========================================================================================================

    public bool CanCombine(Vector3Int targetCell, CombineMode mode)
    {
        if (!m_towersOnGrid.ContainsKey(targetCell)) return false;
        GridTowerInfo targetInfo = m_towersOnGrid[targetCell];

        if (targetInfo.Tier >= 5) return false;

        int matchCount = 0;

        foreach (var kvp in m_towersOnGrid)
        {
            if (kvp.Key == targetCell) continue;

            GridTowerInfo checkInfo = kvp.Value;
            if (checkInfo.Tier != targetInfo.Tier) continue;

            if (mode == CombineMode.ExactMatch && checkInfo.Type == targetInfo.Type && checkInfo.Variant == targetInfo.Variant) matchCount++;
            else if (mode == CombineMode.TypeMatch && checkInfo.Type == targetInfo.Type) matchCount++;
            else if (mode == CombineMode.VariantMatch && checkInfo.Variant == targetInfo.Variant) matchCount++;

            if (matchCount >= 2) return true;
        }

        return false;
    }

    public void ExecuteCombine(Vector3Int targetCell, CombineMode mode)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        if (!CanCombine(targetCell, mode)) return;

        GridTowerInfo targetInfo = m_towersOnGrid[targetCell];
        List<Vector3Int> extraMaterialCells = new List<Vector3Int>(2);

        foreach (var kvp in m_towersOnGrid)
        {
            if (kvp.Key == targetCell) continue;

            GridTowerInfo checkInfo = kvp.Value;
            if (checkInfo.Tier != targetInfo.Tier) continue;

            bool isMatch = false;
            if (mode == CombineMode.ExactMatch && checkInfo.Type == targetInfo.Type && checkInfo.Variant == targetInfo.Variant) isMatch = true;
            else if (mode == CombineMode.TypeMatch && checkInfo.Type == targetInfo.Type) isMatch = true;
            else if (mode == CombineMode.VariantMatch && checkInfo.Variant == targetInfo.Variant) isMatch = true;

            if (isMatch)
            {
                extraMaterialCells.Add(kvp.Key);
                if (extraMaterialCells.Count == 2) break;
            }
        }

        TowerData resultData = GetMergeResultData(targetInfo, mode);

        if (resultData == null || resultData.towerPrefab == null)
        {
            Debug.LogError($"[TowerManager] 합성 결과물을 찾을 수 없습니다.");
            return;
        }

        Vector3 spawnPos = targetInfo.Controller.transform.position;

        m_towersOnGrid.Remove(targetCell);
        Destroy(targetInfo.Controller.gameObject);

        for (int i = 0; i < extraMaterialCells.Count; i++)
        {
            Vector3Int matCell = extraMaterialCells[i];
            GridTowerInfo matInfo = m_towersOnGrid[matCell];

            m_towersOnGrid.Remove(matCell);
            Destroy(matInfo.Controller.gameObject);
        }

        GameObject spawnedTower = Instantiate(resultData.towerPrefab, spawnPos, Quaternion.identity);
        TowerController newTowerController = spawnedTower.GetComponent<TowerController>();
        newTowerController.Init(resultData, GetGlobalStats(resultData.towerID));

        GridTowerInfo newInfo = new GridTowerInfo
        {
            Controller = newTowerController,
            Tier       = targetInfo.Tier + 1,
            Type       = (resultData.towerID % 1000) / 100,
            Variant    = resultData.towerID % 100
        };

        m_towersOnGrid[targetCell] = newInfo;
        EvaluateSynergyTowers();
    }

    private TowerData GetMergeResultData(GridTowerInfo targetInfo, CombineMode mode)
    {
        int nextTier = targetInfo.Tier + 1;

        if (!m_towerTier.ContainsKey(nextTier)) return null;
        List<TowerData> nextTierTowers = m_towerTier[nextTier];

        if (mode == CombineMode.ExactMatch)
        {
            int exactTargetID = targetInfo.Controller.GetTowerData().towerID + 1000;
            for (int i = 0; i < nextTierTowers.Count; i++)
            {
                if (nextTierTowers[i].towerID == exactTargetID) return nextTierTowers[i];
            }
            return null;
        }

        List<TowerData> candidates = new List<TowerData>();

        for (int i = 0; i < nextTierTowers.Count; i++)
        {
            TowerData data = nextTierTowers[i];
            int checkType = (data.towerID % 1000) / 100;
            int checkVariant = data.towerID % 100;

            if (mode == CombineMode.TypeMatch && checkType == targetInfo.Type) candidates.Add(data);
            else if (mode == CombineMode.VariantMatch && checkVariant == targetInfo.Variant) candidates.Add(data);
        }

        if (candidates.Count > 0)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
        return null;
    }   

    // ==========================================================================================================
    // ================================================= 시너지 ==================================================
    // ==========================================================================================================


    // ==========================================================================================================
    // ================================================ 시너지 타워 ===============================================
    // ==========================================================================================================

    /// <summary>
    /// 4티어 이상 일반 타워가 가진 문양만 시너지 재료로 사용한다.
    /// 생성된 6티어 시너지 타워는 다른 시너지의 재료가 되지 않는다.
    /// </summary>
    private HashSet<int> GetSynergyMaterialPatterns()
    {
        HashSet<int> activePatterns = new HashSet<int>();
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> pair in m_towersOnGrid)
        {
            if (pair.Value.Tier >= 4 && pair.Value.Tier <= 5)
            {
                activePatterns.Add(pair.Value.Variant);
            }
        }
        return activePatterns;
    }

    /// <summary>
    /// 조건을 만족한 시너지는 전용 타일에 생성하고, 조건이 깨지면 생성물만 제거한다.
    /// 시너지 타워는 공격/버프/디버프/합성/해제 등 자체 효과를 갖지 않는 표시 전용 상태다.
    /// </summary>
    private void EvaluateSynergyTowers()
    {
        if (m_towerBasePrefab == null || m_spawnPoint == null) return;

        HashSet<int> activePatterns = GetSynergyMaterialPatterns();
        for (int i = 0; i < s_synergyDefinitions.Length; i++)
        {
            SynergyDefinition definition = s_synergyDefinitions[i];
            bool shouldExist = definition.IsSatisfiedBy(activePatterns);
            bool exists = m_activeSynergyTowers.TryGetValue(definition.TowerID, out ActiveSynergyTower active) &&
                          active != null && active.Controller != null;

            if (shouldExist && !exists)
            {
                TrySpawnSynergyTower(definition);
            }
            else if (!shouldExist && exists)
            {
                RemoveSynergyTower(definition.TowerID, active);
            }
        }
    }

    private bool TrySpawnSynergyTower(SynergyDefinition definition)
    {
        Tilemap spawnTilemap = m_synergySpawnPoint != null ? m_synergySpawnPoint : m_spawnPoint;
        Vector3Int? cell = GetFirstEmptyCell(spawnTilemap);
        if (!cell.HasValue)
        {
            Debug.LogWarning($"[시너지] {definition.Name} 생성 공간이 없습니다.");
            return false;
        }

        TowerData data = CreateSynergyTowerData(definition);
        GameObject spawnedTower = Instantiate(data.towerPrefab, spawnTilemap.GetCellCenterWorld(cell.Value), Quaternion.identity);
        TowerController controller = spawnedTower.GetComponent<TowerController>();
        if (controller == null)
        {
            Debug.LogError($"[시너지] {definition.Name} 생성 프리팹에 TowerController가 없습니다.");
            Destroy(spawnedTower);
            Destroy(data);
            return false;
        }

        controller.Init(data, data.ToTowerStats());
        m_towersOnGrid[cell.Value] = new GridTowerInfo
        {
            Controller = controller,
            Tier = data.towerID / 1000,
            Type = 0,
            Variant = 0
        };
        m_activeSynergyTowers[definition.TowerID] = new ActiveSynergyTower
        {
            Controller = controller,
            Data = data,
            Cell = cell.Value
        };

        Debug.Log($"[시너지] {definition.Name} 조건 달성: 생성 전용 시너지 타워를 배치했습니다.");
        return true;
    }

    private TowerData CreateSynergyTowerData(SynergyDefinition definition)
    {
        TowerData data = ScriptableObject.CreateInstance<TowerData>();
        data.hideFlags = HideFlags.DontSave;
        data.towerID = definition.TowerID;
        data.visualTowerID = definition.VisualTowerID;
        data.towerName = $"{definition.Name} 시너지 타워";
        data.towerLevel = 1;
        data.power = 0f;
        data.range = 1f;
        data.action = 1;
        data.attackCount = 1;
        data.duration = 0f;
        data.abilityValue = 0f;
        data.attackType = AttackType.None;
        data.targetPriority = TargetPriority.Closest;
        data.buffTarget = BuffTarget.None;
        data.debuffTarget = DebuffTarget.None;
        data.towerPrefab = m_towerBasePrefab;
        return data;
    }

    private void RemoveSynergyTower(int towerID, ActiveSynergyTower active)
    {
        if (active != null)
        {
            if (m_towersOnGrid.TryGetValue(active.Cell, out GridTowerInfo info) && info.Controller == active.Controller)
            {
                m_towersOnGrid.Remove(active.Cell);
            }

            if (active.Controller != null) Destroy(active.Controller.gameObject);
            if (active.Data != null) Destroy(active.Data);
        }
        m_activeSynergyTowers.Remove(towerID);
    }

    private Vector3Int? GetFirstEmptyCell(Tilemap targetTilemap)
    {
        if (targetTilemap == null) return null;

        BoundsInt bounds = targetTilemap.cellBounds;
        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (targetTilemap.HasTile(position) && !m_towersOnGrid.ContainsKey(position))
                {
                    return position;
                }
            }
        }
        return null;
    }

    public void MoveTowerOnGrid(Vector3Int fromCell, Vector3Int toCell)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        // 1. 이동시킬 타워가 딕셔너리에 존재하는지 확인
        if (!m_towersOnGrid.TryGetValue(fromCell, out GridTowerInfo movingInfo)) return;

        // 시너지 타워는 조건 달성 보상으로 생성된 전용 타일의 표시물이라 드래그/교환 대상이 아니다.
        if (movingInfo.Tier >= 6)
        {
            Tilemap synergyTilemap = m_synergySpawnPoint != null ? m_synergySpawnPoint : m_spawnPoint;
            if (movingInfo.Controller != null)
            {
                movingInfo.Controller.transform.position = synergyTilemap.GetCellCenterWorld(fromCell);
            }
            return;
        }

        // 2. 이 타워가 현재 속한 타일맵 결정 (일반 맵 vs 시너지 맵)
        Tilemap originTilemap = m_spawnPoint;

        // 3. [핵심] 제자리 드롭이거나 목적지가 유효하지 않은 경우 원위치 복귀
        if (fromCell == toCell || !originTilemap.HasTile(toCell))
        {
            movingInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(fromCell);

            // 디버프존 기준 위치 원위치 동기화
            if (movingInfo.Controller != null)
            {
                movingInfo.Controller.OnMovedToNewPosition();
            }
            return;
        }

        // 4. 타겟 위치에 다른 타워가 있다면 서로 자리 교환 (Swap)
        if (m_towersOnGrid.TryGetValue(toCell, out GridTowerInfo targetInfo))
        {
            m_towersOnGrid[fromCell] = targetInfo;
            m_towersOnGrid[toCell] = movingInfo;

            movingInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(toCell);
            targetInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(fromCell);

            // 두 타워 모두 디버프존 기준 위치 갱신
            if (movingInfo.Controller != null) movingInfo.Controller.OnMovedToNewPosition();
            if (targetInfo.Controller != null) targetInfo.Controller.OnMovedToNewPosition();
            return;
        }

        // 5. 빈 공간으로 이동
        m_towersOnGrid.Remove(fromCell);
        m_towersOnGrid[toCell] = movingInfo;

        movingInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(toCell);

        // 이동 완료된 타워의 디버프존 기준 위치 갱신
        if (movingInfo.Controller != null)
        {
            movingInfo.Controller.OnMovedToNewPosition();
        }
    }
    // ==========================================================================================================
    // ==========================================================================================================

    private void CheckDigitInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (IsDigitPressed(i))
            {
                m_testInputBuffer += i.ToString();
                Debug.Log($"[치트] 입력 중... {m_testInputBuffer}");

                // 4자리가 꽉 차면 소환 시도!
                if (m_testInputBuffer.Length == 4)
                {
                    int targetTowerID = int.Parse(m_testInputBuffer);
                    SpawnTowerByTest(targetTowerID);

                    // 소환 후 깔끔하게 치트 모드 종료
                    m_isTestMode = false;
                    m_testInputBuffer = "";
                }
                break; // 한 프레임에는 숫자 하나만 처리
            }
        }
    }
    private bool IsDigitPressed(int digit)
    {
        var kb = Keyboard.current;
        switch (digit)
        {
            case 0: return kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame;
            case 1: return kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
            case 2: return kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
            case 3: return kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame;
            case 4: return kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
            case 5: return kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame;
            case 6: return kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame;
            case 7: return kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame;
            case 8: return kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame;
            case 9: return kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame;
            default: return false;
        }
    }
    private void SpawnTowerByTest(int towerID)
    {
        // 1. 전체 타워 데이터 배열에서 입력한 ID와 일치하는 타워를 찾음
        TowerData targetData = null;
        foreach (var data in m_towerData)
        {
            if (data.towerID == towerID)
            {
                targetData = data;
                break;
            }
        }

        // 2. 예외 처리: 없는 번호를 입력했을 때
        if (targetData == null)
        {
            Debug.LogWarning($"[치트] ID가 {towerID}인 타워를 찾을 수 없습니다! 데이터를 확인해 주세요.");
            return;
        }

        // 3. 빈 공간 찾아서 생성 (일반 스폰 포인트 사용)
        Vector3Int? emptyCell = GetFirstEmptyCell();
        if (emptyCell.HasValue)
        {
            Vector3 spawnPos = CellToWorld(emptyCell.Value);
            GameObject spawnedTower = Instantiate(targetData.towerPrefab, spawnPos, Quaternion.identity);
            TowerController towerController = spawnedTower.GetComponent<TowerController>();

            if (towerController != null)
            {
                towerController.Init(targetData, GetGlobalStats(targetData.towerID));
            }

            // 4. 그리드 매니저에 정보 등록
            GridTowerInfo newInfo = new GridTowerInfo
            {
                Controller = towerController,
                Tier       = targetData.towerID / 1000,
                Type       = (targetData.towerID % 1000) / 100,
                Variant    = targetData.towerID % 100
            };

            m_towersOnGrid.Add(emptyCell.Value, newInfo);
            Debug.Log($"[치트] 성공! {targetData.towerName} (ID: {towerID}) 타워가 소환되었습니다!");
            EvaluateSynergyTowers();
        }
        else
        {
            Debug.LogWarning("[치트] 맵에 타워를 소환할 빈 공간이 없습니다!");
        }
    }
    
    // Enum 파싱용 헬퍼 함수
    private T ParseEnum<T>(string value) where T : struct
    {
        string cleanValue = value.Trim();
        if (Enum.TryParse(cleanValue, true, out T result))
        {
            return result;
        }
        return default;
    }

    public TowerData[] GetTowerDataArray()
    {
        return m_towerData;
    }
    
    /// <summary>
     /// TileManager가 타워 스폰 타일맵의 색상을 변경할 수 있도록 타일맵 참조를 반환합니다.
     /// </summary>
    public Tilemap GetSpawnPointTilemap()
    {
        return m_spawnPoint;
    }
}
