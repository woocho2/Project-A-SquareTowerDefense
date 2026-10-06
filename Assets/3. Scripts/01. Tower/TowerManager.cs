using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CombineMode
{
    ExactMatch,
    ColorMatch,
    EmblemMatch
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

    [Tooltip("CSV에서 런타임으로 생성한 타워 데이터 목록입니다.")]
    [SerializeField] private List<TowerData> m_towerData = new List<TowerData>();
    private readonly Dictionary<int, TowerData> m_towerDataById = new Dictionary<int, TowerData>();

    [Header("타워 공통 프리팹")]
    [Tooltip("모든 타워가 공통으로 사용하는 원본 프리팹입니다. 티어·색상·엠블럼은 소환 시 TowerVisual이 ID에 맞춰 교체합니다.")]
    [SerializeField] private GameObject m_towerBasePrefab;

    [Header("타워 생성 및 업그레이드 비용")]
    private int BuildCost = 50;
    private const int TierUpgradeBaseGemCost = 5;

    public int CreateTowerGold => BuildCost;
    //public int ColorUpgradeGem => GetColorUpgradeCost();
    //public int TierUpgradeGem => GetTierUpgradeCost(int towerID);

    [Header("타워 순차 행동")]
    [Tooltip("공격 타워의 투사체가 명중해 이펙트가 발동한 뒤 다음 공격 타워가 행동하기까지의 간격입니다.")]
    [SerializeField, Min(0f)] private float m_nextAttackTowerDelay = 0.3f;
    [Tooltip("한 타워의 공격에서 투사체 수가 이 시간(초) 동안 변하지 않으면 멈춘 것으로 보고 다음 타워로 넘어갑니다.")]
    [SerializeField, Min(0.5f)] private float m_attackStallTimeout = 5f;

    // 배치된 타워의 점유 원본은 TileManager의 소환 타일 인덱스에 있다.
    private List<KeyValuePair<int, TowerController>> GetPlacedTowers() =>
        TileManager.Instance != null
            ? TileManager.Instance.GetTowerOccupancyByIndexSnapshot()
            : new List<KeyValuePair<int, TowerController>>();
    // 소환된 타워의 스탯 정보를 담는 딕셔너리구조
    private Dictionary<int, TowerStats>      m_baseTowerStats  = new Dictionary<int, TowerStats>();
    private float m_sharedDarknessAbilityValue;
    private Dictionary<int, List<TowerData>> m_towerTier         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerColor        = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerEmblem       = new Dictionary<int, List<TowerData>>();

    public event Action<int> CurrentCreateTowerValueChanged;
    public event Action<int> CurrentColorUpgradeValueChanged;
    public event Action<int> CurrentTierUpgradeValueChanged;
    public event Action<TowerController> TowerTierUpgraded;

    private sealed class SynergyDefinition
    {
        public readonly int TowerID;
        public readonly string Name;
        public readonly int VisualTowerID;
        public readonly int[] RequiredEmblems;

        public SynergyDefinition(int towerID, string name, int visualTowerID, params int[] requiredEmblems)
        {
            TowerID = towerID;
            Name = name;
            VisualTowerID = visualTowerID;
            RequiredEmblems = requiredEmblems;
        }

        public bool IsSatisfiedBy(HashSet<int> activeEmblems)
        {
            for (int i = 0; i < RequiredEmblems.Length; i++)
            {
                if (!activeEmblems.Contains(RequiredEmblems[i])) return false;
            }
            return true;
        }
    }

    private sealed class ActiveSynergyTower
    {
        public TowerController Controller;
        public TowerData Data;
        public int SpawnIndex;
    }

    // 27. 기획 통합.txt 9장 순서. 각 항목은 4티어 이상 재료 문양을 모두 보유하면 생성된다.
    private static readonly SynergyDefinition[] s_synergyDefinitions =
    {
        new SynergyDefinition(6001, "오딘", 5104, TowerEmblem.SPEAR, TowerEmblem.LIGHT, TowerEmblem.DARKNESS),
        new SynergyDefinition(6002, "토르", 5106, TowerEmblem.HAMMER, TowerEmblem.ELECTRICITY, TowerEmblem.WIND),
        new SynergyDefinition(6003, "로키", 5102, TowerEmblem.BOW, TowerEmblem.FIRE, TowerEmblem.DARKNESS),
        new SynergyDefinition(6004, "헬", 5101, TowerEmblem.SWORD, TowerEmblem.AXE, TowerEmblem.DARKNESS),
        new SynergyDefinition(6005, "수르트", 5101, TowerEmblem.SWORD, TowerEmblem.FIRE, TowerEmblem.EARTH),
        new SynergyDefinition(6006, "헤임달", 5101, TowerEmblem.SWORD, TowerEmblem.WIND, TowerEmblem.LIGHT),
        new SynergyDefinition(6007, "발드르", 5103, TowerEmblem.SHIELD, TowerEmblem.EARTH, TowerEmblem.LIGHT),
        new SynergyDefinition(6008, "스카디", 5102, TowerEmblem.BOW, TowerEmblem.ICE, TowerEmblem.WIND),
        new SynergyDefinition(6009, "비다르", 5104, TowerEmblem.SPEAR, TowerEmblem.SHIELD, TowerEmblem.ICE),
        new SynergyDefinition(6010, "이미르", 5106, TowerEmblem.HAMMER, TowerEmblem.ICE, TowerEmblem.EARTH),
        new SynergyDefinition(6011, "트루드", 5103, TowerEmblem.SHIELD, TowerEmblem.AXE, TowerEmblem.ELECTRICITY),
        new SynergyDefinition(6012, "발키리", 5102, TowerEmblem.BOW, TowerEmblem.SPEAR, TowerEmblem.ELECTRICITY),
        new SynergyDefinition(6013, "브록 & 에이트리", 5105, TowerEmblem.AXE, TowerEmblem.HAMMER, TowerEmblem.FIRE)
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

    // CSV의 숫자는 실행 기기의 언어 설정과 무관하게 항상 같은 방식(소수점 '.')으로 읽습니다.
    private static float ParseCsvFloat(string value)
    {
        return float.Parse(value.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int ParseCsvInt(string value)
    {
        return int.Parse(value.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
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
        if (m_towerData == null) m_towerData = new List<TowerData>();
        else m_towerData.Clear();
        m_towerDataById.Clear();
        m_baseTowerStats.Clear();
        m_towerTier.Clear();
        m_towerColor.Clear();
        m_towerEmblem.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            // 열 순서: ID, Name, attackDamage, attackRange, Action, attackCount, SplashRadius, AdditionalHitCount,
            // criticalRate, criticalDamage, duration, abilityValue, attackType, buffTarget, debuffTarget,
            // Speed, HitEffectID, TargetPriority
            if (string.IsNullOrWhiteSpace(values[0])) continue;
            if (values.Length < 17)
            {
                Debug.LogWarning($"[CSV 데이터 오류] {i + 1}번째 줄의 열이 {values.Length}개입니다 (17개 이상 필요). 내용: {lines[i]}");
                continue;
            }

            TowerData newData = ScriptableObject.CreateInstance<TowerData>();

            try
            {
                newData.towerID = ParseCsvInt(values[0]);
                newData.towerName = values[1].Replace("\"", "").Trim();
                newData.power = ParseCsvFloat(values[2]);
                newData.range = ParseCsvFloat(values[3]);
                newData.action = Mathf.Max(1, ParseCsvInt(values[4]));
                newData.attackCount = Mathf.Max(1, Mathf.RoundToInt(ParseCsvFloat(values[5])));
                newData.splashRadius = Mathf.Max(0, Mathf.RoundToInt(ParseCsvFloat(values[6])));
                newData.additionalHitCount = Mathf.Max(0, ParseCsvInt(values[7]));
                newData.criticalRate = ParseCsvFloat(values[8]);
                newData.criticalDamage = ParseCsvFloat(values[9]);
                newData.duration = ParseCsvFloat(values[10]);
                newData.abilityValue = ParseCsvFloat(values[11]);

                newData.attackType = ParseEnum<AttackType>(values[12]);
                newData.buffTarget = string.IsNullOrEmpty(values[13].Trim()) ? BuffTarget.None : ParseEnum<BuffTarget>(values[13]);
                newData.debuffTarget = string.IsNullOrEmpty(values[14].Trim()) ? DebuffTarget.None : ParseEnum<DebuffTarget>(values[14]);

                newData.projectileSpeed = ParseCsvFloat(values[15]);
                newData.hitEffectID = ParseCsvInt(values[16]);
                newData.targetPriority = values.Length > 17 && !string.IsNullOrWhiteSpace(values[17])
                    ? ParseEnum<TargetPriority>(values[17])
                    : TargetPriority.Closest;
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"[CSV 데이터 오류] 엑셀의 {i + 1}번째 줄 파싱 실패. 내용: {lines[i]}");
                continue;
            }

            if (m_towerDataById.ContainsKey(newData.towerID))
            {
                Debug.LogWarning($"[CSV 데이터 오류] {i + 1}번째 줄의 ID {newData.towerID}가 중복입니다. 먼저 나온 줄을 사용합니다.");
                continue;
            }

            newData.InitializeIdentity();

            // 모든 타워는 TowerBase 하나만 소환한다.
            // 타워 ID에 따른 스탯과 외형은 TowerController.Init / TowerVisual.Apply에서 주입된다.
            newData.towerPrefab = m_towerBasePrefab;

            int sharedProjID = newData.towerID % 1000;
            string projPrefabName = sharedProjID.ToString();
            newData.projectilePrefab = Resources.Load<GameObject>($"Projectiles/{projPrefabName}");

            m_towerData.Add(newData);
            m_towerDataById[newData.towerID] = newData;

            // [핵심] 생성 즉시 ToTowerStats()를 호출하여 글로벌 스탯 딕셔너리에 등록
            m_baseTowerStats[newData.towerID] = newData.ToTowerStats();

            // 분류 딕셔너리 적재
            int tier = newData.Tier;
            int color = newData.ColorId;
            int emblem = newData.EmblemId;

            if (!m_towerTier.ContainsKey(tier)) m_towerTier[tier] = new List<TowerData>();
            m_towerTier[tier].Add(newData);

            if (!m_towerColor.ContainsKey(color)) m_towerColor[color] = new List<TowerData>();
            m_towerColor[color].Add(newData);

            if (!m_towerEmblem.ContainsKey(emblem)) m_towerEmblem[emblem] = new List<TowerData>();
            m_towerEmblem[emblem].Add(newData);
        }

        Debug.Log($"총 {m_towerData.Count}개의 타워 데이터 및 글로벌 스탯 로드 완료.");
    }


    public TowerStats GetGlobalStats(int towerID)
    {
        if (m_baseTowerStats.ContainsKey(towerID))
        {
            return m_baseTowerStats[towerID];
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

        if (!CurrencyManager.Instance.HasEnoughGold(BuildCost))
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

        TileManager tileManager = TileManager.Instance;
        if (tileManager != null && tileManager.TryGetFirstEmptyTowerSpawnIndex(out int spawnIndex) &&
            tileManager.TryGetTowerSpawnWorldPosition(spawnIndex, out Vector3 spawnPos))
        {
            int randomIndex = UnityEngine.Random.Range(0, buildableTowers.Count);
            TowerData selectedData = buildableTowers[randomIndex];

            if (selectedData.towerPrefab == null)
            {
                Debug.LogError($"타워 데이터 {selectedData.towerName}에 할당된 프리팹이 없습니다.");
                return;
            }

            GameObject spawnedTower = Instantiate(selectedData.towerPrefab, spawnPos, Quaternion.identity);
            TowerController towerController = spawnedTower.GetComponent<TowerController>();

            if (towerController == null)
            {
                Debug.LogError($"생성된 타워 프리팹에 TowerController 컴포넌트가 없습니다: {selectedData.towerName}");
                Destroy(spawnedTower);
                return;
            }

            if (!tileManager.TryPlaceTowerAt(spawnIndex, towerController))
            {
                Destroy(spawnedTower);
                return;
            }
            towerController.Init(selectedData, GetGlobalStats(selectedData.towerID));

            CurrencyManager.Instance.SpendGold(BuildCost);
            if (BuildCost < 300) BuildCost += 2;
            CurrentCreateTowerValueChanged?.Invoke(BuildCost);
            EvaluateSynergyTowers();
        }
        else
        {
            Debug.LogWarning("더 이상 타워를 건설할 공간이 없습니다!");
        }
    }


    public int GetBuildCost()
    {
        return BuildCost;
    }

    /// <summary>
    /// Runs exactly once per enemy turn before support actions.  Status duration is
    /// target-owned, while tile records from the previous turn expire here.
    /// </summary>
    public void BeginEnemyTurnForTowers()
    {
        List<KeyValuePair<int, TowerController>> orderedTowers = GetOrderedTowers();
        TileManager.Instance?.AdvanceTowerBuffEffectTurns();
        foreach (KeyValuePair<int, TowerController> tower in orderedTowers)
        {
            tower.Value?.AdvanceSkillTurn();
        }
    }

    /// <summary>Support towers act before enemies read path effects this turn.</summary>
    public IEnumerator ExecuteSupportTowerActionTurnRoutine()
    {
        List<KeyValuePair<int, TowerController>> orderedTowers = GetOrderedTowers();
        foreach (KeyValuePair<int, TowerController> tower in orderedTowers)
        {
            TowerController controller = tower.Value;
            TowerData data = controller != null ? controller.GetTowerData() : null;
            if (data == null || (data.attackType != AttackType.Buff && data.attackType != AttackType.Debuff)) continue;

            controller.ExecuteTurnAction();
        }

        // A Fire field is only a delivery record.  Each target reads it once here,
        // after all support towers have placed their effects.
        foreach (KeyValuePair<int, TowerController> tower in orderedTowers)
        {
            tower.Value?.RefreshStackBuffs();
        }
        yield return null;
    }

    /// <summary>Damage towers act after support and tile effects have resolved.</summary>
    public IEnumerator ExecuteAttackTowerActionTurnRoutine()
    {
        List<KeyValuePair<int, TowerController>> orderedTowers = GetOrderedTowers();
        foreach (KeyValuePair<int, TowerController> tower in orderedTowers)
        {
            TowerController controller = tower.Value;
            TowerData data = controller != null ? controller.GetTowerData() : null;
            if (data == null || (data.attackType != AttackType.Splash && data.attackType != AttackType.Target)) continue;

            // 스킬은 행동력과 무관하게 매 적 턴 자기 발동 조건을 확인하고, 그 뒤에 기본 공격이 이어집니다.
            controller.TryUseSkill();
            bool didAct = controller.ExecuteTurnAction();
            GameStateVersion.MarkChanged();
            if (!didAct && (GlobalProjectileManager.Instance == null || !GlobalProjectileManager.Instance.HasActiveProjectiles())) continue;

            // 이 타워의 공격이 끝날 때까지 기다립니다: 위성을 다 쏘았고, 남은 투사체가 없을 때.
            // 투사체 수가 m_attackStallTimeout 동안 변하지 않으면 멈춘 것으로 보고 다음 타워로 넘어갑니다.
            float stalledTime = 0f;
            int previousProjectileCount = -1;
            while (true)
            {
                bool isAttacking = controller != null && controller.IsAttackInProgress;
                int projectileCount = GlobalProjectileManager.Instance != null
                    ? GlobalProjectileManager.Instance.GetActiveProjectileCount()
                    : 0;
                if (!isAttacking && projectileCount <= 0) break;

                if (projectileCount != previousProjectileCount)
                {
                    previousProjectileCount = projectileCount;
                    stalledTime = 0f;
                }
                else
                {
                    stalledTime += Time.deltaTime;
                    if (stalledTime >= m_attackStallTimeout)
                    {
                        Debug.LogWarning($"[TowerManager] 타워 공격이 {m_attackStallTimeout}초 동안 끝나지 않아 다음 타워로 넘어갑니다. (남은 투사체 {projectileCount}개)");
                        break;
                    }
                }

                yield return null;
            }

            if (m_nextAttackTowerDelay > 0f)
            {
                yield return new WaitForSeconds(m_nextAttackTowerDelay);
            }
        }
    }

    private List<KeyValuePair<int, TowerController>> GetOrderedTowers()
    {
        return GetPlacedTowers();
    }

    // ==========================================================================================================
    // ================================================ 타워판매 ================================================
    // ==========================================================================================================

    public void SellTower(int spawnIndex)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;
        if (TileManager.Instance == null) return;

        if (TileManager.Instance.TryGetTowerData(spawnIndex, out TowerController tower, out TowerData towerData))
        {
            int soldTier = towerData != null ? towerData.Tier : 0;
            if (tower != null)
            {
                if (soldTier < 6)
                {
                    Destroy(tower.gameObject);
                }
                else
                {
                    if (TileManager.Instance.TryGetTowerSpawnWorldPosition(spawnIndex, out Vector3 position))
                        tower.transform.position = position;
                    Debug.Log("시너지 타워는 판매할 수 없습니다.");
                    return;
                }
            }

            TileManager.Instance.RemoveTowerAt(spawnIndex);

            if (soldTier > 0)
            {
                int amount = (int)Mathf.Pow(3, soldTier - 1);
                CurrencyManager.Instance.AddGem(amount);
            }
            EvaluateSynergyTowers();
        }
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
        if (!m_towerDataById.TryGetValue(towerID, out TowerData data)) return 0;
        int currentTier = data.Tier;
        if (currentTier < 1 || currentTier >= 5) return 0;

        return TierUpgradeBaseGemCost * Mathf.RoundToInt(Mathf.Pow(3f, currentTier - 1));
    }

    /// <summary>
    /// 선택한 셀의 타워를 다음 티어의 같은 색상/문양 ID 타워로 교체합니다.
    /// 예: 1234 → 2234
    /// </summary>
    public bool UpgradeTowerTier(int spawnIndex, out TowerController upgradedTower)
    {
        upgradedTower = null;

        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return false;
        if (TileManager.Instance == null) return false;

        if (!TileManager.Instance.TryGetTowerData(spawnIndex, out TowerController currentTower, out TowerData currentData))
        {
            Debug.LogWarning("티어 강화할 타워를 찾을 수 없습니다.");
            return false;
        }

        TargetPriority previousTargetPriority = currentTower.GetTargetPriority();
        bool hadDebuffZone = currentTower.TryGetDebuffZoneIndex(out int previousZoneIndex);
        if (currentData == null)
        {
            Debug.LogWarning("티어 강화할 타워 데이터가 없습니다.");
            return false;
        }

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

        int upgradedTowerID = currentData.NextTierTowerID;
        m_towerDataById.TryGetValue(upgradedTowerID, out TowerData upgradedData);

        if (upgradedData == null || upgradedData.towerPrefab == null)
        {
            Debug.LogError($"티어 강화 결과 타워(ID: {upgradedTowerID})를 찾을 수 없습니다.");
            return false;
        }

        if (!TileManager.Instance.TryGetTowerSpawnWorldPosition(spawnIndex, out Vector3 spawnPosition)) return false;
        GameObject spawnedTower = Instantiate(upgradedData.towerPrefab, spawnPosition, Quaternion.identity);
        upgradedTower = spawnedTower.GetComponent<TowerController>();
        if (upgradedTower == null)
        {
            Debug.LogError($"티어 강화 결과 프리팹에 TowerController가 없습니다: {upgradedData.towerName}");
            Destroy(spawnedTower);
            return false;
        }

        upgradedTower.InheritCreationOrder(currentTower.CreationOrder);
        upgradedTower.InheritBuffStatuses(currentTower);
        TileManager.Instance.SetTowerAt(spawnIndex, upgradedTower);
        upgradedTower.Init(upgradedData, GetGlobalStats(upgradedData.towerID));
        upgradedTower.SetTargetPriority(previousTargetPriority);
        if (hadDebuffZone) upgradedTower.TrySetDebuffZoneIndex(previousZoneIndex);

        Destroy(currentTower.gameObject);

        CurrencyManager.Instance.SpendGem(upgradeCost);
        EvaluateSynergyTowers();
        // 교체된 타워와 그 타워의 다음 강화 비용을 구독자에게 알립니다.
        TowerTierUpgraded?.Invoke(upgradedTower);
        CurrentTierUpgradeValueChanged?.Invoke(GetTierUpgradeCost(upgradedData.towerID));
        return true;
    }

    /// <summary>
    /// 매 3웨이브 완료 시 Earth 버프 타워가 주변의 낮은 티어 타워 하나를 무료 강화합니다.
    /// 1티어 Earth는 대상이 없으므로 자기 자신을 2티어로 강화합니다.
    /// </summary>
    public void ProcessEarthTowerWave(int completedWave)
    {
        if (completedWave <= 0 || completedWave % 3 != 0) return;

        List<KeyValuePair<int, TowerController>> earthTowers = new List<KeyValuePair<int, TowerController>>();
        foreach (KeyValuePair<int, TowerController> pair in GetPlacedTowers())
        {
            TowerData data = pair.Value != null ? pair.Value.GetTowerData() : null;
            if (data != null && data.attackType == AttackType.Buff && data.buffTarget == BuffTarget.Earth)
            {
                earthTowers.Add(pair);
            }
        }

        bool upgradedAnyTower = false;
        foreach (KeyValuePair<int, TowerController> earthTower in earthTowers)
        {
            if (!TileManager.Instance.TryGetTowerInfo(earthTower.Key, out TowerController liveTower, out TowerData earthData, out TowerStats earthStats)) continue;

            if (earthData == null) continue;
            int earthTier = earthData.Tier;
            if (earthTier == 1)
            {
                upgradedAnyTower |= UpgradeTowerTierWithoutCost(earthTower.Key);
                continue;
            }

            int tileRange = TowerAttackAction.ToTileRange(earthStats.Range);
            List<int> candidates = new List<int>();

            foreach (KeyValuePair<int, TowerController> candidate in GetPlacedTowers())
            {
                if (candidate.Key == earthTower.Key || candidate.Value == null) continue;
                if (!TileManager.Instance.TryGetTowerData(candidate.Key, out _, out TowerData candidateData)) continue;
                if (candidateData.Tier >= earthTier || candidateData.Tier >= 5) continue;

                if (TileManager.Instance.AreTowerSpawnIndicesWithinRange(earthTower.Key, candidate.Key, tileRange))
                {
                    candidates.Add(candidate.Key);
                }
            }

            if (candidates.Count > 0)
            {
                upgradedAnyTower |= UpgradeTowerTierWithoutCost(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
            }
        }

        if (upgradedAnyTower) EvaluateSynergyTowers();
    }

    private bool UpgradeTowerTierWithoutCost(int spawnIndex)
    {
        if (TileManager.Instance == null ||
            !TileManager.Instance.TryGetTowerData(spawnIndex, out TowerController currentTower, out TowerData currentData)) return false;

        TargetPriority previousTargetPriority = currentTower.GetTargetPriority();
        bool hadDebuffZone = currentTower.TryGetDebuffZoneIndex(out int previousZoneIndex);
        if (currentData == null || currentData.Tier >= 5) return false;

        int upgradedTowerID = currentData.NextTierTowerID;
        m_towerDataById.TryGetValue(upgradedTowerID, out TowerData upgradedData);
        if (upgradedData == null || upgradedData.towerPrefab == null) return false;

        if (!TileManager.Instance.TryGetTowerSpawnWorldPosition(spawnIndex, out Vector3 spawnPosition)) return false;
        GameObject spawnedTower = Instantiate(upgradedData.towerPrefab, spawnPosition, Quaternion.identity);
        TowerController upgradedTower = spawnedTower.GetComponent<TowerController>();
        if (upgradedTower == null)
        {
            Destroy(spawnedTower);
            return false;
        }

        upgradedTower.InheritCreationOrder(currentTower.CreationOrder);
        upgradedTower.InheritBuffStatuses(currentTower);
        TileManager.Instance.SetTowerAt(spawnIndex, upgradedTower);
        upgradedTower.Init(upgradedData, GetGlobalStats(upgradedData.towerID));
        upgradedTower.SetTargetPriority(previousTargetPriority);
        if (hadDebuffZone) upgradedTower.TrySetDebuffZoneIndex(previousZoneIndex);
        Destroy(currentTower.gameObject);
        return true;
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

        // 타워는 최종 스탯을 계산할 때마다 이 값을 다시 읽으므로 따로 알릴 필요가 없습니다.
        m_sharedDarknessAbilityValue += gainedValue;
        GameStateVersion.MarkChanged();
    }

    public void UpgradeTower(int towerID)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        if (!m_baseTowerStats.ContainsKey(towerID)) return;

        if (!m_towerDataById.TryGetValue(towerID, out TowerData targetData)) return;
        int targetColor = targetData.ColorId;

        TowerStats currentStats = m_baseTowerStats[towerID];

        if (currentStats.Level >= 5)
        {
            Debug.Log($"최대레벨에 도달하였습니다. 현재 레벨수치 {currentStats.Level}");
            return;
        }

        int currentUpgradeGemCost = GetColorUpgradeCost(towerID);

        if (!CurrencyManager.Instance.HasEnoughGem(currentUpgradeGemCost))
        {
            Debug.Log("업그레이드에 필요한 코스트가 부족합니다.");
            return;
        }

        CurrencyManager.Instance.SpendGem(currentUpgradeGemCost);

        if (!m_towerColor.ContainsKey(targetColor)) return;
        List<TowerData> towersToUpgrade = m_towerColor[targetColor];

        foreach (TowerData towerData in towersToUpgrade)
        {
            int key = towerData.towerID;

            TowerStats UpgradeStats = m_baseTowerStats[key];

            UpgradeStats.Level++;

            float multiplier = 1f + (Mathf.Pow(2f, UpgradeStats.Level - 2) / 10f);

            if (UpgradeStats.Level <= 5)
            {
                // 컬러 강화는 스킬만 강화합니다. 공격력·사거리·공격 횟수 같은 기본 스탯은 티어(CSV)가 정합니다.
                // 레벨은 UpgradeStats.Level로 함께 전달되어, 스킬이 발동 조건 등을 계산할 때 사용합니다.
                UpgradeStats.AbilityValue = towerData.abilityValue * multiplier;

                // 공격 타워는 스킬 발동 조건(Duration)이 레벨만큼 줄고, 레벨 3과 5에서 범위·추가 타격이 1씩 늘어납니다.
                // 버프·디버프 타워는 Duration의 뜻이 달라서 따로 재설계할 때까지 AbilityValue만 올립니다.
                if (towerData.attackType == AttackType.Splash || towerData.attackType == AttackType.Target)
                {
                    int levelBonus = UpgradeStats.Level >= 5 ? 2 : (UpgradeStats.Level >= 3 ? 1 : 0);
                    UpgradeStats.Duration = towerData.duration / UpgradeStats.Level;

                    if (towerData.attackType == AttackType.Splash)
                    {
                        UpgradeStats.ProjectileRadius = towerData.splashRadius + levelBonus;
                    }
                    else
                    {
                        UpgradeStats.AdditionalHitCount = towerData.additionalHitCount + levelBonus;
                    }
                }

                m_baseTowerStats[key] = UpgradeStats;
            }
        }
        GameStateVersion.MarkChanged();

        // 2. 필드에 배치된 해당 타입 타워들에게 최신 스탯 주입(Push)
        foreach (KeyValuePair<int, TowerController> placed in GetPlacedTowers())
        {
            TowerController tower = placed.Value;
            TowerData towerData = tower != null ? tower.GetTowerData() : null;
            if (towerData != null && towerData.ColorId == targetColor)
            {
                tower.UpdateBaseStats(m_baseTowerStats[towerData.towerID]);
            }
        }

        // 레벨이 오른 뒤의 다음 강화 비용을 구독자에게 알립니다.
        CurrentColorUpgradeValueChanged?.Invoke(GetColorUpgradeCost(towerID));
    }

    /// <summary>
    /// 같은 색상 타워 전체의 레벨을 한 단계 올리는 데 필요한 젬 비용을 반환합니다.
    /// 1→2: 2, 2→3: 4, 3→4: 8, 4→5: 16. 최대 레벨이거나 데이터가 없으면 0입니다.
    /// </summary>
    public int GetColorUpgradeCost(int towerID)
    {
        if (!m_baseTowerStats.TryGetValue(towerID, out TowerStats stats) || stats.Level >= 5) return 0;

        return (int)Mathf.Pow(2, stats.Level);
    }

    // ==========================================================================================================
    // ================================================ 타워합성 =================================================
    // ==========================================================================================================

    public bool CanCombine(int targetIndex, CombineMode mode)
    {
        if (TileManager.Instance == null) return false;
        if (!TileManager.Instance.TryGetTowerData(targetIndex, out _, out TowerData targetData)) return false;

        if (targetData.Tier >= 5) return false;

        int matchCount = 0;

        foreach (var kvp in GetPlacedTowers())
        {
            if (kvp.Key == targetIndex) continue;

            if (!TileManager.Instance.TryGetTowerData(kvp.Key, out _, out TowerData candidateData) ||
                candidateData.Tier != targetData.Tier) continue;

            if (mode == CombineMode.ExactMatch && candidateData.ColorId == targetData.ColorId && candidateData.EmblemId == targetData.EmblemId) matchCount++;
            else if (mode == CombineMode.ColorMatch && candidateData.ColorId == targetData.ColorId) matchCount++;
            else if (mode == CombineMode.EmblemMatch && candidateData.EmblemId == targetData.EmblemId) matchCount++;

            if (matchCount >= 2) return true;
        }

        return false;
    }

    public void ExecuteCombine(int targetIndex, CombineMode mode)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;
        if (TileManager.Instance == null) return;

        if (!CanCombine(targetIndex, mode)) return;

        if (!TileManager.Instance.TryGetTowerData(targetIndex, out TowerController targetTower, out TowerData targetData)) return;
        List<int> extraMaterialIndices = new List<int>(2);

        foreach (var kvp in GetPlacedTowers())
        {
            if (kvp.Key == targetIndex) continue;

            if (!TileManager.Instance.TryGetTowerData(kvp.Key, out _, out TowerData candidateData) ||
                candidateData.Tier != targetData.Tier) continue;

            bool isMatch = false;
            if (mode == CombineMode.ExactMatch && candidateData.ColorId == targetData.ColorId && candidateData.EmblemId == targetData.EmblemId) isMatch = true;
            else if (mode == CombineMode.ColorMatch && candidateData.ColorId == targetData.ColorId) isMatch = true;
            else if (mode == CombineMode.EmblemMatch && candidateData.EmblemId == targetData.EmblemId) isMatch = true;

            if (isMatch)
            {
                extraMaterialIndices.Add(kvp.Key);
                if (extraMaterialIndices.Count == 2) break;
            }
        }

        TowerData resultData = GetMergeResultData(targetData, mode);

        if (resultData == null || resultData.towerPrefab == null)
        {
            Debug.LogError($"[TowerManager] 합성 결과물을 찾을 수 없습니다.");
            return;
        }

        if (!TileManager.Instance.TryGetTowerSpawnWorldPosition(targetIndex, out Vector3 spawnPos)) return;

        GameObject spawnedTower = Instantiate(resultData.towerPrefab, spawnPos, Quaternion.identity);
        TowerController newTowerController = spawnedTower.GetComponent<TowerController>();
        if (newTowerController == null)
        {
            Debug.LogError($"[TowerManager] 합성 결과 프리팹에 TowerController가 없습니다: {resultData.towerName}");
            Destroy(spawnedTower);
            return;
        }

        TileManager.Instance.RemoveTowerAt(targetIndex);
        Destroy(targetTower.gameObject);
        for (int i = 0; i < extraMaterialIndices.Count; i++)
        {
            int materialIndex = extraMaterialIndices[i];
            if (!TileManager.Instance.TryGetTowerAt(materialIndex, out TowerController materialTower)) continue;

            TileManager.Instance.RemoveTowerAt(materialIndex);
            Destroy(materialTower.gameObject);
        }
        TileManager.Instance.SetTowerAt(targetIndex, newTowerController);
        newTowerController.Init(resultData, GetGlobalStats(resultData.towerID));
        EvaluateSynergyTowers();
    }

    private TowerData GetMergeResultData(TowerData targetData, CombineMode mode)
    {
        int nextTier = targetData.Tier + 1;

        if (mode == CombineMode.ExactMatch)
        {
            return m_towerDataById.TryGetValue(targetData.NextTierTowerID, out TowerData exactResult)
                ? exactResult
                : null;
        }

        if (!m_towerTier.TryGetValue(nextTier, out List<TowerData> nextTierTowers)) return null;
        List<TowerData> candidates = new List<TowerData>();

        for (int i = 0; i < nextTierTowers.Count; i++)
        {
            TowerData data = nextTierTowers[i];
            if (mode == CombineMode.ColorMatch && data.ColorId == targetData.ColorId) candidates.Add(data);
            else if (mode == CombineMode.EmblemMatch && data.EmblemId == targetData.EmblemId) candidates.Add(data);
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
    private HashSet<int> GetSynergyMaterialEmblems()
    {
        HashSet<int> activeEmblems = new HashSet<int>();
        foreach (KeyValuePair<int, TowerController> pair in GetPlacedTowers())
        {
            TowerController tower = pair.Value;
            TowerData towerData = tower != null ? tower.GetTowerData() : null;
            if (towerData != null && towerData.Tier >= 4 && towerData.Tier <= 5)
            {
                activeEmblems.Add(towerData.EmblemId);
            }
        }
        return activeEmblems;
    }

    /// <summary>
    /// 조건을 만족한 시너지는 일반 타워 타일의 빈 칸에 생성하고, 조건이 깨지면 생성물만 제거한다.
    /// 시너지 타워는 공격/버프/디버프/합성/해제 등 자체 효과를 갖지 않는 표시 전용 상태다.
    /// </summary>
    private void EvaluateSynergyTowers()
    {
        if (m_towerBasePrefab == null || TileManager.Instance == null || TileManager.Instance.TowerSpawnTilemap == null) return;

        HashSet<int> activeEmblems = GetSynergyMaterialEmblems();
        for (int i = 0; i < s_synergyDefinitions.Length; i++)
        {
            SynergyDefinition definition = s_synergyDefinitions[i];
            bool shouldExist = definition.IsSatisfiedBy(activeEmblems);
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
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null || !tileManager.TryGetFirstEmptyTowerSpawnIndex(out int spawnIndex) ||
            !tileManager.TryGetTowerSpawnWorldPosition(spawnIndex, out Vector3 spawnPosition))
        {
            Debug.LogWarning($"[시너지] {definition.Name} 생성 공간이 없습니다.");
            return false;
        }

        TowerData data = CreateSynergyTowerData(definition);
        GameObject spawnedTower = Instantiate(data.towerPrefab, spawnPosition, Quaternion.identity);
        TowerController controller = spawnedTower.GetComponent<TowerController>();
        if (controller == null)
        {
            Debug.LogError($"[시너지] {definition.Name} 생성 프리팹에 TowerController가 없습니다.");
            Destroy(spawnedTower);
            Destroy(data);
            return false;
        }

        if (!tileManager.TryPlaceTowerAt(spawnIndex, controller))
        {
            Destroy(spawnedTower);
            Destroy(data);
            return false;
        }
        controller.Init(data, data.ToTowerStats());
        m_activeSynergyTowers[definition.TowerID] = new ActiveSynergyTower
        {
            Controller = controller,
            Data = data,
            SpawnIndex = spawnIndex
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
        data.InitializeIdentity();
        data.towerName = $"{definition.Name} 시너지 타워";
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
            if (TileManager.Instance != null &&
                TileManager.Instance.TryGetTowerAt(active.SpawnIndex, out TowerController tower) && tower == active.Controller)
            {
                TileManager.Instance.RemoveTowerAt(active.SpawnIndex);
            }

            if (active.Controller != null) Destroy(active.Controller.gameObject);
            if (active.Data != null) Destroy(active.Data);
        }
        m_activeSynergyTowers.Remove(towerID);
    }

    public void MoveTowerOnGrid(int fromIndex, int toIndex)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null || !tileManager.TryGetTowerData(fromIndex, out TowerController movingTower, out TowerData movingData) ||
            !tileManager.TryGetTowerSpawnWorldPosition(fromIndex, out Vector3 fromPosition)) return;

        // 시너지 타워는 이동·교환하지 않습니다. 유효하지 않은 칸에 놓아도 원위치로 복구합니다.
        if (movingData.Tier >= 6 || fromIndex == toIndex ||
            !tileManager.TryGetTowerSpawnWorldPosition(toIndex, out Vector3 toPosition))
        {
            movingTower.transform.position = fromPosition;
            movingTower.OnMovedToNewPosition();
            return;
        }

        if (tileManager.TryGetTowerData(toIndex, out TowerController targetTower, out TowerData targetData))
        {
            if (targetData.Tier >= 6 || !tileManager.TrySwapTowers(fromIndex, toIndex))
            {
                movingTower.transform.position = fromPosition;
                movingTower.OnMovedToNewPosition();
                return;
            }

            movingTower.transform.position = toPosition;
            targetTower.transform.position = fromPosition;
            movingTower.OnMovedToNewPosition();
            targetTower.OnMovedToNewPosition();
            return;
        }

        if (!tileManager.TryMoveTower(fromIndex, toIndex))
        {
            movingTower.transform.position = fromPosition;
            return;
        }

        movingTower.transform.position = toPosition;
        movingTower.OnMovedToNewPosition();
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
        // 입력한 ID에 해당하는 타워 데이터를 조회합니다.
        if (!m_towerDataById.TryGetValue(towerID, out TowerData targetData))
        {
            Debug.LogWarning($"[치트] ID가 {towerID}인 타워를 찾을 수 없습니다! 데이터를 확인해 주세요.");
            return;
        }

        // 3. 빈 공간 찾아서 생성 (일반 스폰 포인트 사용)
        TileManager tileManager = TileManager.Instance;
        if (tileManager != null && tileManager.TryGetFirstEmptyTowerSpawnIndex(out int spawnIndex) &&
            tileManager.TryGetTowerSpawnWorldPosition(spawnIndex, out Vector3 spawnPos))
        {
            GameObject spawnedTower = Instantiate(targetData.towerPrefab, spawnPos, Quaternion.identity);
            TowerController towerController = spawnedTower.GetComponent<TowerController>();

            if (towerController == null)
            {
                Debug.LogError($"[치트] 타워 프리팹에 TowerController가 없습니다: {targetData.towerName}");
                Destroy(spawnedTower);
                return;
            }

            if (!tileManager.TryPlaceTowerAt(spawnIndex, towerController))
            {
                Destroy(spawnedTower);
                return;
            }
            towerController.Init(targetData, GetGlobalStats(targetData.towerID));
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

    public List<TowerData> GetTowerDataList()
    {
        return m_towerData;
    }
    
}
