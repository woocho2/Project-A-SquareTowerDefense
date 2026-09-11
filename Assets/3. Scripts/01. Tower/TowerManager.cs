using System;
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
#if false // Synergy system temporarily disabled
    [SerializeField] private Tilemap m_synergySpawnPoint;
#endif

    [Tooltip("CSV에서 런타임으로 생성한 타워 데이터 배열입니다.")]
    [SerializeField] private TowerData[] m_towerData;

    [Header("타워 공통 프리팹")]
    [Tooltip("모든 타워가 공통으로 사용하는 원본 프리팹입니다. 티어·색상·엠블럼은 소환 시 TowerVisual이 ID에 맞춰 교체합니다.")]
    [SerializeField] private GameObject m_towerBasePrefab;

#if false // Synergy system temporarily disabled
    [Header("시너지 타워 데이터")]
    [SerializeField] private TowerData m_knightData;         // 불의 기사
    [SerializeField] private TowerData m_sniperData;         // 폭풍 저격수
    [SerializeField] private TowerData m_berserkerData;      // 서리 광전사
    [SerializeField] private TowerData m_contradictionData;  // 모순
    [SerializeField] private TowerData m_thorData;           // 토르
    [SerializeField] private TowerData m_gunData;            // 웨폰 마스터
    [SerializeField] private TowerData m_wizardData;         // 대마법사

    [Header("시너지 특수 타일맵")]
    [SerializeField] private Tilemap m_pathTilemap;          // 열풍

    [Header("시너지 UI 버튼")]
    [SerializeField] private UnityEngine.UI.Button m_btnCombineContradiction;
    [SerializeField] private UnityEngine.UI.Button m_btnUndoContradiction;
    [SerializeField] private UnityEngine.UI.Button m_btnCombineThor;
    [SerializeField] private UnityEngine.UI.Button m_btnUndoThor;

    [Header("시너지 INFO 버튼")]
    [SerializeField] private UnityEngine.UI.Button m_btnKnightOfFire;
    [SerializeField] private UnityEngine.UI.Image  m_imgKnightOfFireSword;
    [SerializeField] private UnityEngine.UI.Image  m_imgKnightOfFireShield;
    [SerializeField] private UnityEngine.UI.Image  m_imgKnightOfFireFire;
    [SerializeField] private UnityEngine.UI.Button m_btnStormSniper;
    [SerializeField] private UnityEngine.UI.Image  m_imgStormSniperBow;
    [SerializeField] private UnityEngine.UI.Image  m_imgStormSniperWind;
    [SerializeField] private UnityEngine.UI.Button m_btnFrostBerserker;
    [SerializeField] private UnityEngine.UI.Image  m_imgFrostBerserkerShield;
    [SerializeField] private UnityEngine.UI.Image  m_imgFrostBerserkerAxe;
    [SerializeField] private UnityEngine.UI.Image  m_imgFrostBerserkerIce;
    [SerializeField] private UnityEngine.UI.Button m_btnContradiction;
    [SerializeField] private UnityEngine.UI.Image  m_imgContradictionShield;
    [SerializeField] private UnityEngine.UI.Image  m_imgContradictionSpear;
    [SerializeField] private UnityEngine.UI.Button m_btnHeatWave;
    [SerializeField] private UnityEngine.UI.Image  m_imgHeatWaveFire;
    [SerializeField] private UnityEngine.UI.Image  m_imgHeatWaveElectricity;
    [SerializeField] private UnityEngine.UI.Image  m_imgHeatWaveWind;
    [SerializeField] private UnityEngine.UI.Button m_btnAttributionArrow;
    [SerializeField] private UnityEngine.UI.Image  m_imgAttributionArrowBow;
    [SerializeField] private UnityEngine.UI.Image  m_imgAttributionArrowFire;
    [SerializeField] private UnityEngine.UI.Image  m_imgAttributionArrowIce;
    [SerializeField] private UnityEngine.UI.Button m_btnThor;
    [SerializeField] private UnityEngine.UI.Image  m_imgThorSword;
    [SerializeField] private UnityEngine.UI.Image  m_imgThorAxe;
    [SerializeField] private UnityEngine.UI.Image  m_imgThorElectricity;
    [SerializeField] private UnityEngine.UI.Button m_btnWeaponMaster;
    [SerializeField] private UnityEngine.UI.Image  m_imgWeaponMasterSword;
    [SerializeField] private UnityEngine.UI.Image  m_imgWeaponMasterBow;
    [SerializeField] private UnityEngine.UI.Image  m_imgWeaponMasterShield;
    [SerializeField] private UnityEngine.UI.Image  m_imgWeaponMasterSpear;
    [SerializeField] private UnityEngine.UI.Image  m_imgWeaponMasterAxe;
    [SerializeField] private UnityEngine.UI.Button m_btnGrandWizard;
    [SerializeField] private UnityEngine.UI.Image  m_imgGrandWizardFire;
    [SerializeField] private UnityEngine.UI.Image  m_imgGrandWizardIce;
    [SerializeField] private UnityEngine.UI.Image  m_imgGrandWizardElectricity;
    [SerializeField] private UnityEngine.UI.Image  m_imgGrandWizardWind;

    private List<UnityEngine.UI.Button> m_allSynergyInfoButtons = new List<UnityEngine.UI.Button>();

#endif
    [Header("타워 생성 및 업그레이드 비용")]
    private int BuildCost = 50;
    private const int TierUpgradeBaseGemCost = 5;

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
#if false // Synergy system temporarily disabled
    private Dictionary<int, TowerStats>      m_synergyTowerStats = new Dictionary<int, TowerStats>();
#endif
    private Dictionary<int, List<TowerData>> m_towerTier         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerType         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerVariant      = new Dictionary<int, List<TowerData>>();

    public event Action<int> OnTowerTypeUpgrade;

#if false // Synergy system temporarily disabled
    private List<SynergyBase> m_synergies = new List<SynergyBase>();
#endif

#if false // Synergy system temporarily disabled
    public bool IsAttributionArrowActive { get; set; } = false;
#endif
#if false // Synergy system temporarily disabled
    public bool IsHeatWaveActive { get; set; } = false;
#endif

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

#if false // Synergy system temporarily disabled
        m_synergies.Add(new KnightOfFireSynergy     (this, m_knightData, m_btnKnightOfFire, m_imgKnightOfFireSword, m_imgKnightOfFireShield, m_imgKnightOfFireFire));
        m_synergies.Add(new StormSniperSynergy      (this, m_sniperData, m_btnStormSniper, m_imgStormSniperBow, m_imgStormSniperWind));
        m_synergies.Add(new FrostBerserkerSynergy   (this, m_berserkerData, m_btnFrostBerserker, m_imgFrostBerserkerShield, m_imgFrostBerserkerAxe, m_imgFrostBerserkerIce));
        m_synergies.Add(new ContradictionSynergy    (this, m_contradictionData, m_btnCombineContradiction, m_btnUndoContradiction, m_btnContradiction, m_imgContradictionShield, m_imgContradictionSpear));
        m_synergies.Add(new HeatWaveSynergy         (this, m_pathTilemap, m_btnHeatWave, m_imgHeatWaveFire, m_imgHeatWaveElectricity, m_imgHeatWaveWind));
        m_synergies.Add(new AttributionArrowSynergy (this, m_btnAttributionArrow, m_imgAttributionArrowBow, m_imgAttributionArrowFire, m_imgAttributionArrowIce));
        m_synergies.Add(new ThorSynergy             (this, m_thorData, m_btnCombineThor, m_btnUndoThor, m_btnThor, m_imgThorSword, m_imgThorAxe, m_imgThorElectricity));
        m_synergies.Add(new WeaponMasterSynergy     (this, m_gunData, m_btnWeaponMaster, m_imgWeaponMasterSword, m_imgWeaponMasterBow, m_imgWeaponMasterShield, m_imgWeaponMasterSpear, m_imgWeaponMasterAxe));
        m_synergies.Add(new GrandWizardSynergy      (this, m_wizardData, m_btnGrandWizard, m_imgGrandWizardFire, m_imgGrandWizardIce, m_imgGrandWizardElectricity, m_imgGrandWizardWind));

        m_allSynergyInfoButtons.Add(m_btnKnightOfFire);
        m_allSynergyInfoButtons.Add(m_btnStormSniper);
        m_allSynergyInfoButtons.Add(m_btnFrostBerserker);
        m_allSynergyInfoButtons.Add(m_btnContradiction);
        m_allSynergyInfoButtons.Add(m_btnHeatWave);
        m_allSynergyInfoButtons.Add(m_btnAttributionArrow);
        m_allSynergyInfoButtons.Add(m_btnThor);
        m_allSynergyInfoButtons.Add(m_btnWeaponMaster);
        m_allSynergyInfoButtons.Add(m_btnGrandWizard);

#endif
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

        // 시너지 타워 글로벌 스탯 등록
#if false // Synergy system temporarily disabled
        TowerData[] synergyDatas = {
            m_knightData, m_sniperData, m_berserkerData,
            m_contradictionData, m_thorData, m_gunData, m_wizardData
        };

        foreach (var sData in synergyDatas)
        {
            if (sData != null)
            {
                m_globalTowerStats[sData.towerID] = sData.ToTowerStats();
            }
        }

#endif
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

            // CheckTowerSynergy();
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
    /// </summary>
    public void ExecuteTowerActionTurn()
    {
        List<KeyValuePair<Vector3Int, GridTowerInfo>> orderedTowers = new List<KeyValuePair<Vector3Int, GridTowerInfo>>(m_towersOnGrid);
        orderedTowers.Sort((left, right) =>
        {
            int rowComparison = right.Key.y.CompareTo(left.Key.y);
            return rowComparison != 0 ? rowComparison : left.Key.x.CompareTo(right.Key.x);
        });

        // 이번 에너미 턴이 시작되었으므로, 기존 버프의 남은 턴을 먼저 차감합니다.
        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            tower.Value.Controller?.AdvanceBuffTurn();
        }

        foreach (KeyValuePair<Vector3Int, GridTowerInfo> tower in orderedTowers)
        {
            tower.Value.Controller?.ExecuteTurnAction();
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

            // CheckTowerSynergy();
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

        float currentUpgradeGemCost = Mathf.Pow(2 , currentStats.Level);

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
        // CheckTowerSynergy();
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

#if false // Synergy system temporarily disabled
    public void CheckTowerSynergy()
    {
        foreach (var synergy in m_synergies)
        {
            bool conditionMet = synergy.CheckCondition(m_towersOnGrid);

            if (conditionMet && !synergy.IsActive)
            {
                synergy.Activate();
            }
            else if (!conditionMet && synergy.IsActive)
            {
                synergy.Deactivate();
            }
        }
    }

    public HashSet<int> GetActiveVariants()
    {
        HashSet<int> activeVariants = new HashSet<int>();

        foreach (var kvp in m_towersOnGrid)
        {
            if (kvp.Value.Tier >= 4 && kvp.Value.Tier <= 5)
            {
                activeVariants.Add(kvp.Value.Variant);
            }
            if (kvp.Value.Tier >= 6)
            {
                if (kvp.Value.Controller != null)
                {
                    TowerData currentData = kvp.Value.Controller.GetTowerData();
                    if (currentData != null)
                    {
                        if (m_thorData != null && currentData.towerID == m_thorData.towerID)
                        {
                            activeVariants.Add(1);
                            activeVariants.Add(5);
                            activeVariants.Add(8);
                        }
                        else if (m_contradictionData != null && currentData.towerID == m_contradictionData.towerID)
                        {
                            activeVariants.Add(3);
                            activeVariants.Add(4);
                        }
                    }
                }            
            }
        }
        return activeVariants;
    }

    public TowerController CreateSynergyTower(TowerData synergyData)
    {
        Vector3Int? spawnCell = GetFirstEmptyCell(m_synergySpawnPoint);

        if (spawnCell.HasValue)
        {
            Vector3 spawnPos = m_synergySpawnPoint.GetCellCenterWorld(spawnCell.Value);

            GameObject spawnedTower = Instantiate(synergyData.towerPrefab, spawnPos, Quaternion.identity);

            TowerController synergyTowerController = spawnedTower.GetComponent<TowerController>();

            if (synergyTowerController != null)
            {
                synergyTowerController.Init(synergyData, GetGlobalStats(synergyData.towerID));
            }

            GridTowerInfo newInfo = new GridTowerInfo
            {
                Controller = synergyTowerController,
                Tier       = synergyData.towerID / 1000,
                Type       = (synergyData.towerID % 1000) / 100,
                Variant    = synergyData.towerID % 100
            };
            m_towersOnGrid.Add(spawnCell.Value, newInfo);

            Debug.Log($"[시너지] 조건 달성! {synergyData.towerName} 특수 타일에 소환되었습니다!");
            return synergyTowerController;
        }
        else
        {
            Debug.LogWarning("[시너지] 불의 기사를 소환할 특수 타일 공간이 부족합니다!");
            return null;
        }
    }

    public TowerController DestroySynergyTower(TowerController targetTower)
    {
        if (targetTower != null)
        {
            Vector3Int? targetCell = null;
            foreach (var kvp in m_towersOnGrid)
            {
                if (kvp.Value.Controller == targetTower)
                {
                    targetCell = kvp.Key;
                    break;
                }
            }

            if (targetCell.HasValue)
            {
                m_towersOnGrid.Remove(targetCell.Value);
            }

            Destroy(targetTower.gameObject);
        }
        return null;
    }

    public bool CombineSynergyTower(TowerData resultTowerData, int requiredTier, List<int> requiredVariants, out List<TowerData> consumeDatas)
    {
        consumeDatas = new List<TowerData>();
        List<Vector3Int> materialCells = new List<Vector3Int>();
        List<int> variantsToFind = new List<int>(requiredVariants); // 찾아야 할 문양 번호 리스트 복사

        // 1. 맵을 순회하며 필요한 재료 타워들의 위치를 탐색
        foreach (var kvp in m_towersOnGrid)
        {
            if (kvp.Value.Tier >= requiredTier)
            {
                int variant = kvp.Value.Variant;
                if (variantsToFind.Contains(variant))
                {
                    materialCells.Add(kvp.Key);
                    variantsToFind.Remove(variant); // 중복 탐색 방지를 위해 리스트에서 제거
                }
            }
            if (variantsToFind.Count == 0) break; // 모두 찾았으면 반복문 조기 종료
        }

        // 재료를 전부 찾지 못했다면 합성 실패
        if (variantsToFind.Count > 0) return false;

        // 2. 합성된 타워가 생성될 기준 위치 (첫 번째 재료 타워가 있던 자리)
        Vector3Int spawnCell = materialCells[0];

        // 3. 재료 타워 일괄 파괴
        foreach (Vector3Int cell in materialCells)
        {
            consumeDatas.Add(m_towersOnGrid[cell].Controller.GetTowerData());

            Destroy(m_towersOnGrid[cell].Controller.gameObject);
            m_towersOnGrid.Remove(cell);
        }

        // 4. 시너지 타워 1개 생성 및 등록
        Vector3 spawnPos = CellToWorld(spawnCell);
        GameObject spawnedTower = Instantiate(resultTowerData.towerPrefab, spawnPos, Quaternion.identity);
        TowerController newTowerController = spawnedTower.GetComponent<TowerController>();
        newTowerController.Init(resultTowerData, GetGlobalStats(resultTowerData.towerID));

        GridTowerInfo newInfo = new GridTowerInfo
        {
            Controller = newTowerController,
            Tier       = resultTowerData.towerID / 1000,
            Type       = (resultTowerData.towerID % 1000) / 100,
            Variant    = resultTowerData.towerID % 100
        };
        m_towersOnGrid.Add(spawnCell, newInfo);

        // CheckTowerSynergy();
        return true;
    }

    public bool UndoSynergyTower(TowerData synergyTowerData, List<TowerData> materialDatas)
    {
        Vector3Int? synergyCell = null;
        int targetTier = synergyTowerData.towerID / 1000;
        int targetVariant = synergyTowerData.towerID % 100;

        foreach (var kvp in m_towersOnGrid)
        {
            if (kvp.Value.Tier == targetTier && kvp.Value.Variant == targetVariant)
            {
                synergyCell = kvp.Key;
                break;
            }
        }
        if (synergyCell == null) return false;

        int requiredEmptySpaces = materialDatas.Count - 1;
        List<Vector3Int> availableEmptyCells = GetMultipleEmptyCells(requiredEmptySpaces);

        if (availableEmptyCells.Count < requiredEmptySpaces)
        {
            Debug.LogWarning("[시너지] 빈 공간이 부족하여 합성을 해제할 수 없습니다!");
            return false;
        }

        Destroy(m_towersOnGrid[synergyCell.Value].Controller.gameObject);
        m_towersOnGrid.Remove(synergyCell.Value);

        int emptyCellIndex = 0;
        for (int i = 0; i < materialDatas.Count; i++)
        {
            Vector3Int restoreCell;
            if (i == 0)
            {
                restoreCell = synergyCell.Value;
            }
            else
            {
                restoreCell = availableEmptyCells[emptyCellIndex];
                emptyCellIndex++;
            }
            RestoreTowerAtCell(restoreCell, materialDatas[i]);
        }
        materialDatas.Clear();

        // CheckTowerSynergy();
        return true;
    }

    private void RestoreTowerAtCell(Vector3Int cell, TowerData data)
    {
        Vector3 spawnPos = CellToWorld(cell);
        GameObject spawnedTower = Instantiate(data.towerPrefab, spawnPos, Quaternion.identity);
        TowerController newTowerController = spawnedTower.GetComponent<TowerController>();
        newTowerController.Init(data, GetGlobalStats(data.towerID));

        GridTowerInfo newInfo = new GridTowerInfo
        {
            Controller = newTowerController,
            Tier       = data.towerID / 1000,
            Type       = (data.towerID % 1000) / 100,
            Variant    = data.towerID % 100
        };
        m_towersOnGrid.Add(cell, newInfo);
    }

    /// <summary>
    /// 지정된 타일맵에서 첫 번째로 비어있는 셀을 순차적으로 탐색하여 반환합니다.
    /// </summary>
    private Vector3Int? GetFirstEmptyCell(Tilemap targetTilemap)
    {
        BoundsInt bounds = targetTilemap.cellBounds;

        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (targetTilemap.HasTile(pos) && !m_towersOnGrid.ContainsKey(pos))
                {
                    return pos;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 일반 스폰 타일맵에서 지정된 개수만큼의 빈 셀을 순차적으로 탐색하여 반환합니다.
    /// </summary>
    private List<Vector3Int> GetMultipleEmptyCells(int count)
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        BoundsInt bounds = m_spawnPoint.cellBounds;

        for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (m_spawnPoint.HasTile(pos) && !m_towersOnGrid.ContainsKey(pos))
                {
                    emptyCells.Add(pos);
                    if (emptyCells.Count >= count) return emptyCells;
                }
            }
        }

        return emptyCells;
    }

    public void HandleSynergyButtonToggle(UnityEngine.UI.Button clickedButton, bool isOpening)
    {
        foreach (var btn in m_allSynergyInfoButtons)
        {
            // 클릭된 '본인 버튼'은 끄지 않고 무시합니다.
            if (btn != null && btn != clickedButton)
            {
                // 본인의 창을 여는 중(isOpening == true)이면 다른 버튼은 끄고(!true = false)
                // 본인의 창을 닫는 중(isOpening == false)이면 다른 버튼은 다시 켭니다(!false = true)
                btn.gameObject.SetActive(!isOpening);
            }
        }
    }

    // ==========================================================================================================
    // ================================================ 타워이동 =================================================
    // ==========================================================================================================

#endif
    public void MoveTowerOnGrid(Vector3Int fromCell, Vector3Int toCell)
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanPerformPlayerAction) return;

        // 1. 이동시킬 타워가 딕셔너리에 존재하는지 확인
        if (!m_towersOnGrid.TryGetValue(fromCell, out GridTowerInfo movingInfo)) return;

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

            // 5. 방금 치트로 소환된 타워 때문에 시너지가 발동될 수 있으므로 검사
            // CheckTowerSynergy();
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
