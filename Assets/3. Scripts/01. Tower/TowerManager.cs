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
    [SerializeField] private Tilemap m_synergySpawnPoint;

    [Tooltip("타워 프리팹을 포함하는 데이터 배열입니다.")]
    [SerializeField] private TowerData[] m_towerData;

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

    [Header("타워 생성 및 업그레이드 비용")]
    private int BuildCost = 50;
    private int UpgradeGemCost = 5;

    public struct GridTowerInfo
    {
        public TowerController Controller;
        public int Tier;
        public int Type;
        public int Variant;
    }

    private Dictionary<Vector3Int, GridTowerInfo> m_towersOnGrid = new Dictionary<Vector3Int, GridTowerInfo>();
    private Dictionary<int, TowerStats>      m_globalTowerStats  = new Dictionary<int, TowerStats>();
    private Dictionary<int, TowerStats>      m_synergyTowerStats = new Dictionary<int, TowerStats>();
    private Dictionary<int, List<TowerData>> m_towerTier         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerType         = new Dictionary<int, List<TowerData>>();
    private Dictionary<int, List<TowerData>> m_towerVariant      = new Dictionary<int, List<TowerData>>();

    public event Action<int> OnTowerTypeUpgrade;

    private List<SynergyBase> m_synergies = new List<SynergyBase>();

    public bool IsAttributionArrowActive { get; set; } = false;
    public bool IsHeatWaveActive { get; set; } = false;

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

        GlobalStats();

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

    private void GlobalStats()
    {
        if (m_towerData == null || m_towerData.Length == 0)
        {
            return;
        }

        foreach (var data in m_towerData)
        {
            TowerStats initStats = new TowerStats
            {
                Name      = data.towerName,
                Level          = 1,
                AttackPower = data.attackPower,
                Range          = data.range,
                AttackSpeed    = data.attackSpeed,
                CriticalRate   = data.criticalRate,
                CriticalDamage = data.criticalDamage,
                Duration       = data.duration,
                AbilityValue   = data.abilityValue
            };

            m_globalTowerStats[data.towerID] = initStats;

            int tier    = data.towerID / 1000;
            int type    = (data.towerID % 1000) / 100;
            int variant = data.towerID % 100;

            if (!m_towerTier.ContainsKey(tier)) m_towerTier[tier] = new List<TowerData>();
            m_towerTier[tier].Add(data);

            if (!m_towerType.ContainsKey(type)) m_towerType[type] = new List<TowerData>();
            m_towerType[type].Add(data);

            if (!m_towerVariant.ContainsKey(variant)) m_towerVariant[variant] = new List<TowerData>();
            m_towerVariant[variant].Add(data);
        }

        TowerData[] synergyDatas = {
            m_knightData, m_sniperData, m_berserkerData,
            m_contradictionData, m_thorData, m_gunData, m_wizardData
        };

        foreach (var sData in synergyDatas)
        {
            if (sData != null)
            {
                TowerStats synergyStats = new TowerStats
                {
                    Name      = sData.towerName,
                    Level          = 1,
                    AttackPower = sData.attackPower,
                    Range = sData.range,
                    AttackSpeed    = sData.attackSpeed,
                    CriticalRate   = sData.criticalRate,
                    CriticalDamage = sData.criticalDamage,
                    Duration       = sData.duration,
                    AbilityValue   = sData.abilityValue
                };

                m_globalTowerStats[sData.towerID] = synergyStats;
            }
        }
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

        Vector3Int? randomCell = GetRandomEmptyCell();

        if (randomCell.HasValue)
        {
            CurrencyManager.Instance.SpendMoney(BuildCost);
            if (BuildCost < 300) BuildCost += 2;

            int randomIndex = UnityEngine.Random.Range(0, buildableTowers.Count);
            TowerData selectedData = buildableTowers[randomIndex];

            if (selectedData.towerPrefab == null)
            {
                Debug.LogError($"타워 데이터 {selectedData.towerName}에 할당된 프리팹이 없습니다.");
                return;
            }

            Vector3 spawnPos = CellToWorld(randomCell.Value);

            GameObject spawnedTower = Instantiate(selectedData.towerPrefab, spawnPos, Quaternion.identity);
            TowerController towerController = spawnedTower.GetComponent<TowerController>();

            if (towerController != null)
            {
                towerController.Init(selectedData);
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

            m_towersOnGrid.Add(randomCell.Value, newInfo);
            CheckTowerSynergy();
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

    private Vector3Int? GetRandomEmptyCell()
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();

        BoundsInt bounds = m_spawnPoint.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (m_spawnPoint.HasTile(pos) && !m_towersOnGrid.ContainsKey(pos))
            {
                emptyCells.Add(pos);
            }
        }
        if (emptyCells.Count == 0) return null;
        return emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];
    }

    private Vector3 CellToWorld(Vector3Int cell)
    {
        if (m_synergySpawnPoint.HasTile(cell))
        {
            return m_synergySpawnPoint.GetCellCenterWorld(cell);
        }
        return m_spawnPoint.GetCellCenterWorld(cell);
    }
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return m_spawnPoint.WorldToCell(worldPos);
    }

    // ==========================================================================================================
    // ================================================ 타워판매 ================================================
    // ==========================================================================================================

    public void SellTower(Vector3Int cell)
    {
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

            CheckTowerSynergy();
        }
    }

    // ==========================================================================================================
    // =============================================== 업그레이드 ================================================
    // ==========================================================================================================

    public void UpgradeTower(int towerID)
    {
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
                UpgradeStats.AttackPower      = towerData.AttackPower * multiplier;
                UpgradeStats.Range       = towerData.range * multiplier;
                UpgradeStats.AttackSpeed = towerData.attackSpeed * multiplier;

                m_globalTowerStats[key] = UpgradeStats;
                OnTowerTypeUpgrade?.Invoke(key);
            }
        }

        //if (UIManager.Instance != null)
        //{
        //    UIManager.Instance.RefreshUpgradeTowerInfoLabel();
        //}
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
        newTowerController.Init(resultData);

        GridTowerInfo newInfo = new GridTowerInfo
        {
            Controller = newTowerController,
            Tier       = targetInfo.Tier + 1,
            Type       = (resultData.towerID % 1000) / 100,
            Variant    = resultData.towerID % 100
        };

        m_towersOnGrid[targetCell] = newInfo;
        CheckTowerSynergy();
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

    //private void TierUpgrade(GridTowerInfo targetInfo)
    //{
    //    int nextTier = targetInfo.Tier + 1;

    //    if (!m_towerTier.ContainsKey(nextTier)) return;

    //    List<TowerData> nextTierTower = m_towerTier[nextTier];

    //    if (CurrencyManager.Instance.HasEnoughGem(UpgradeGemCost))
    //    {
    //        Vector3 spawnPos = targetInfo.Controller.transform.position;

    //        m_towersOnGrid.Remove(spawnPos, targetInfo);
    //        Destroy(targetInfo.Controller.gameObject);
    //    }

    //    TowerData UpgradeData = new TowerData();

    //    GameObject spawnedTower = Instantiate(UpgradeData.towerPrefab, spawnPos, Quaternion.identity);
    //    TowerController newTowerController = spawnedTower.GetComponent<TowerController>();
    //    newTowerController.Init(resultData);
    //}

    // ==========================================================================================================
    // ================================================= 시너지 ==================================================
    // ==========================================================================================================

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

    private Vector3Int? GetRandomSynergyEmptyCell()
    {
        List<Vector3Int> spcialEmptyCells = new List<Vector3Int>();

        BoundsInt specialBounds = m_synergySpawnPoint.cellBounds;

        foreach (var pos in specialBounds.allPositionsWithin)
        {
            if (m_synergySpawnPoint.HasTile(pos) && !m_towersOnGrid.ContainsKey(pos))
            {
                spcialEmptyCells.Add(pos);
            }
        }
        if (spcialEmptyCells.Count == 0) return null;
        return spcialEmptyCells[UnityEngine.Random.Range(0, spcialEmptyCells.Count)];
    }

    public TowerController CreateSynergyTower(TowerData synergyData)
    {
        Vector3Int? spawnCell = GetRandomSynergyEmptyCell();

        if (spawnCell.HasValue)
        {
            Vector3 spawnPos = m_synergySpawnPoint.GetCellCenterWorld(spawnCell.Value);

            GameObject spawnedTower = Instantiate(synergyData.towerPrefab, spawnPos, Quaternion.identity);

            TowerController synergyTowerController = spawnedTower.GetComponent<TowerController>();

            if (synergyTowerController != null)
            {
                synergyTowerController.Init(synergyData);
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
        newTowerController.Init(resultTowerData);

        GridTowerInfo newInfo = new GridTowerInfo
        {
            Controller = newTowerController,
            Tier       = resultTowerData.towerID / 1000,
            Type       = (resultTowerData.towerID % 1000) / 100,
            Variant    = resultTowerData.towerID % 100
        };
        m_towersOnGrid.Add(spawnCell, newInfo);

        CheckTowerSynergy();
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

        CheckTowerSynergy();
        return true;
    }

    private void RestoreTowerAtCell(Vector3Int cell, TowerData data)
    {
        Vector3 spawnPos = CellToWorld(cell);
        GameObject spawnedTower = Instantiate(data.towerPrefab, spawnPos, Quaternion.identity);
        TowerController newTowerController = spawnedTower.GetComponent<TowerController>();
        newTowerController.Init(data);

        GridTowerInfo newInfo = new GridTowerInfo
        {
            Controller = newTowerController,
            Tier       = data.towerID / 1000,
            Type       = (data.towerID % 1000) / 100,
            Variant    = data.towerID % 100
        };
        m_towersOnGrid.Add(cell, newInfo);
    }

    private List<Vector3Int> GetMultipleEmptyCells(int count)
    {
        List<Vector3Int> emptyCells = new List<Vector3Int>();
        BoundsInt bounds = m_spawnPoint.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (m_spawnPoint.HasTile(pos) && !m_towersOnGrid.ContainsKey(pos))
            {
                emptyCells.Add(pos);
                if (emptyCells.Count >= count) return emptyCells; // 필요한 만큼 찾으면 즉시 반환
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

    public void MoveTowerOnGrid(Vector3Int fromCell, Vector3Int toCell)
    {
        // 1. 이동시킬 타워가 딕셔너리에 존재하는지 확인
        if (!m_towersOnGrid.TryGetValue(fromCell, out GridTowerInfo movingInfo)) return;

        // 2. 이 타워가 현재 속한 타일맵 결정 (일반 맵 vs 시너지 맵)
        Tilemap originTilemap = m_spawnPoint.HasTile(fromCell) ? m_spawnPoint : m_synergySpawnPoint;

        // 3. [핵심] 제자리 드롭이거나, 목적지가 '자신이 속한 맵'이 아닌 경우 원위치로 강제 되돌림
        if (fromCell == toCell || !originTilemap.HasTile(toCell))
        {
            movingInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(fromCell);
            return;
        }

        // 4. 타겟 위치에 다른 타워가 있다면 서로 자리 교환 (Swap)
        if (m_towersOnGrid.TryGetValue(toCell, out GridTowerInfo targetInfo))
        {
            m_towersOnGrid[fromCell] = targetInfo;
            m_towersOnGrid[toCell] = movingInfo;

            movingInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(toCell);
            targetInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(fromCell);
            return;
        }

        // 5. 빈 공간으로 이동
        m_towersOnGrid.Remove(fromCell);
        m_towersOnGrid[toCell] = movingInfo;

        movingInfo.Controller.transform.position = originTilemap.GetCellCenterWorld(toCell);
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
        Vector3Int? randomCell = GetRandomEmptyCell();
        if (randomCell.HasValue)
        {
            Vector3 spawnPos = CellToWorld(randomCell.Value);
            GameObject spawnedTower = Instantiate(targetData.towerPrefab, spawnPos, Quaternion.identity);
            TowerController towerController = spawnedTower.GetComponent<TowerController>();

            if (towerController != null)
            {
                towerController.Init(targetData);
            }

            // 4. 그리드 매니저에 정보 등록
            GridTowerInfo newInfo = new GridTowerInfo
            {
                Controller = towerController,
                Tier       = targetData.towerID / 1000,
                Type       = (targetData.towerID % 1000) / 100,
                Variant    = targetData.towerID % 100
            };

            m_towersOnGrid.Add(randomCell.Value, newInfo);
            Debug.Log($"[치트] 성공! {targetData.towerName} (ID: {towerID}) 타워가 소환되었습니다!");

            // 5. 방금 치트로 소환된 타워 때문에 시너지가 발동될 수 있으므로 검사
            CheckTowerSynergy();
        }
        else
        {
            Debug.LogWarning("[치트] 맵에 타워를 소환할 빈 공간이 없습니다!");
        }
    }

    private void LoadTowerDataFromCSV()
    {
        // 1. Resources 폴더 최상단에 있는 CSV 파일을 텍스트 에셋으로 불러옵니다. (확장자 .csv는 생략)
        TextAsset csvData = Resources.Load<TextAsset>("TowerDataCSV");

        if (csvData == null)
        {
            Debug.LogError("Resources 폴더에서 TowerDataCSV 파일을 찾을 수 없습니다.");
            return;
        }

        // 2. 줄바꿈 문자를 기준으로 전체 텍스트를 한 줄씩 쪼개어 배열로 만듭니다.
        string[] lines = csvData.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        List<TowerData> loadedTowers = new List<TowerData>();

        // 3. 첫 줄(헤더)을 제외하고 인덱스 1부터 순회합니다.
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');

            // [수정] 투사체 데이터 3개(인덱스 15, 16, 17)가 추가되었으므로 최소 열 개수 조건을 18개로 늘립니다.
            if (values.Length < 18) continue;

            // 첫 번째 칸(towerID)이 비어있다면 의미 없는 잉여 줄로 간주하고 경고 없이 조용히 건너뜁니다.
            if (string.IsNullOrWhiteSpace(values[0])) continue;

            TowerData newData = ScriptableObject.CreateInstance<TowerData>();

            // try-catch 블록을 추가하여 변환 실패 시 게임 중단을 막고 원인을 로그로 출력합니다.
            try
            {
                // 타워 기본 스탯 파싱
                newData.towerID = int.Parse(values[0].Trim());
                newData.towerName = values[1].Replace("\"", "").Trim();
                newData.towerLevel = int.Parse(values[2].Trim());
                newData.attackPower = float.Parse(values[3].Trim());
                newData.range = float.Parse(values[4].Trim());
                newData.attackSpeed = float.Parse(values[5].Trim());
                newData.isCritical = bool.Parse(values[6].Trim());
                newData.criticalRate = float.Parse(values[7].Trim());
                newData.criticalDamage = float.Parse(values[8].Trim());
                newData.duration = float.Parse(values[9].Trim());
                newData.abilityValue = float.Parse(values[10].Trim());

                newData.attackType = ParseEnum<AttackType>(values[11]);

                string layerName = values[12].Trim();
                newData.targetLayer = !string.IsNullOrEmpty(layerName) ? LayerMask.GetMask(layerName) : 0;

                newData.buffTarget = string.IsNullOrEmpty(values[13].Trim()) ? BuffTarget.None : ParseEnum<BuffTarget>(values[13]);
                newData.debuffTarget = string.IsNullOrEmpty(values[14].Trim()) ? DebuffTarget.None : ParseEnum<DebuffTarget>(values[14]);

                // [추가] 통합된 투사체 데이터 파싱 (엑셀의 16, 17, 18번째 열)
                newData.projectileSpeed = float.Parse(values[15].Trim());
                newData.splashRadius = float.Parse(values[16].Trim());
                newData.hitEffectID = int.Parse(values[17].Trim());
            }
            catch (System.Exception)
            {
                // 엑셀의 몇 번째 줄에서 문제가 발생했는지 출력합니다.
                Debug.LogWarning($"[CSV 데이터 오류] 엑셀의 {i + 1}번째 줄에 숫자로 변환할 수 없는 값(또는 빈 줄)이 있습니다. 이 줄을 건너뜁니다. 내용: {lines[i]}");
                continue;
            }

            // 4. 타워 프리팹 로드 (타워 ID 자체를 이름으로 사용)
            string prefabName = newData.towerID.ToString();
            newData.towerPrefab = Resources.Load<GameObject>($"Towers/{prefabName}");

            if (newData.towerPrefab == null)
            {
                Debug.LogWarning($"[로드 실패] ID {newData.towerID}의 타워 프리팹('{prefabName}.prefab')을 Resources/Towers 경로에서 찾을 수 없습니다.");
                continue;
            }

            int sharedProjID = newData.towerID % 1000;

            // 결과적으로 1101, 2101, 3101 모두 "101_Proj"라는 동일한 프리팹 이름을 찾게 됩니다.
            string projPrefabName = sharedProjID.ToString();
            newData.projectilePrefab = Resources.Load<GameObject>($"Projectiles/{projPrefabName}");

            if (newData.projectilePrefab == null)
            {
                Debug.LogWarning($"[로드 실패] ID {newData.towerID}의 투사체 프리팹('{projPrefabName}.prefab')을 찾을 수 없습니다.");
            }

            // 6. 정상적으로 파싱된 데이터를 리스트에 추가합니다.
            loadedTowers.Add(newData);
        }

        // 7. 완성된 동적 데이터를 타워 매니저의 메인 배열에 덮어씌웁니다.
        m_towerData = loadedTowers.ToArray();
        Debug.Log($"총 {m_towerData.Length}개의 타워 데이터를 CSV로부터 성공적으로 로드했습니다.");
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
}
