using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 패널(게임 정보, 플레이어 턴, 타워/타일/적 정보, 게임 종료)을 켜고 끕니다.
/// 플레이어 턴 패널과 타워 정보 패널의 내용은 PlayerTurnPanel, TowerInfoPanel이 직접 갱신합니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton and Inspector References

    public static UIManager Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] GameObject m_gameInfoPanel;

    [Header("UI Player Action")]
    [Tooltip("플레이어 턴에만 켜는 패널입니다. 버튼과 비용 표시는 PlayerTurnPanel이 담당합니다.")]
    [SerializeField] GameObject m_playerTurnPanel;

    [Header("UI Tower Info")]
    [SerializeField] GameObject m_towerInfoPanel;

    [Header("UI Tile Info")]
    [Tooltip("타일을 클릭했을 때 표시할 패널")]
    [SerializeField] GameObject m_tileInfoPanel;
    [Tooltip("버프 항목을 생성할 부모 RectTransform. ScrollRect를 쓴다면 Content를 연결합니다.")]
    [SerializeField] RectTransform m_tileBuffContent;
    [Tooltip("디버프 항목을 생성할 부모 RectTransform. ScrollRect를 쓴다면 Content를 연결합니다.")]
    [SerializeField] RectTransform m_tileDebuffContent;
    [Tooltip("버프 한 줄을 표시하는 UI 프리팹. 하위에 TextMeshProUGUI 하나가 필요합니다.")]
    [SerializeField] GameObject m_tileBuffEntryPrefab;
    public GameObject TileBuffEntryPrefab => m_tileBuffEntryPrefab;
    [Tooltip("디버프 한 줄을 표시하는 UI 프리팹. 하위에 TextMeshProUGUI 하나가 필요합니다.")]
    [SerializeField] GameObject m_tileDebuffEntryPrefab;
    [Tooltip("선택 사항: 버프 목록만 스크롤할 ScrollRect")]
    [SerializeField] ScrollRect m_tileBuffScrollRect;
    [Tooltip("선택 사항: 디버프 목록만 스크롤할 ScrollRect")]
    [SerializeField] ScrollRect m_tileDebuffScrollRect;
    [SerializeField] float m_tileBuffStartY = 150f;
    [SerializeField] float m_tileDebuffStartY = 105f;
    [SerializeField] float m_tileEntrySpacing = 75f;
    [Tooltip("두 정보 패널 안에 있는 '타워 정보로 전환' 버튼들을 모두 넣습니다.")]
    [SerializeField] Button[] m_btnChangeTowerInfoPanels = System.Array.Empty<Button>();
    [Tooltip("두 정보 패널 안에 있는 '타일 정보로 전환' 버튼들을 모두 넣습니다.")]
    [SerializeField] Button[] m_btnChangeTileInfoPanels = System.Array.Empty<Button>();

    [Header("UI Enemy Info")]
    [Tooltip("패스 타일을 클릭했을 때 표시할 적 정보 패널")]
    [SerializeField] GameObject m_enemyInfoPanel;
    [SerializeField] EnemyInfoPanel m_enemyInfoPanelUI;

    [Header("UI Option")]
    [SerializeField] GameObject m_panelOption;
    [SerializeField] Button m_btnQuit;
    [SerializeField] Button m_btnRestart;
    [SerializeField] Button m_btnResume;

    [Header("Game Clear")]
    [SerializeField] GameObject m_panelGameclear;
    [SerializeField] Button m_btnGameClearHome;
    [SerializeField] Button m_btnGameClearMenu;
    [SerializeField] Button m_btnNextStage;

    [Header("Game Over")]
    [SerializeField] GameObject m_panelGameover;
    [SerializeField] Button m_btnGameOverHome;
    [SerializeField] Button m_btnGameOverMenu;
    [SerializeField] Button m_btnRetry;

    [Header("DebugLog")]
    [SerializeField] InGameDebugConsole m_debugconsole;
    [SerializeField] GameObject m_panelDebug;
    [SerializeField] TextMeshProUGUI m_txtDebug;
    [SerializeField] Button m_btnDebug;

    [SerializeField] string m_sceneName_Restart;
    [SerializeField] string m_sceneName_Menu;

    #endregion

    #region Runtime State

    private TowerController m_selectedTower;
    public TowerController SelectedTower => m_selectedTower;

    private int currentLifeIndex;
    private int m_lastWave = -1;
    private bool m_isPlayerActionUIVisible;
    private readonly List<GameObject> m_spawnedTileBuffEntries = new List<GameObject>();
    private readonly List<GameObject> m_spawnedTileDebuffEntries = new List<GameObject>();
    private Vector3Int m_selectedTileCell;
    private bool m_selectedTileIsTowerSpawn;
    private bool m_hasSelectedTile;

    #endregion

    #region Lifecycle and Binding

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (m_enemyInfoPanel == null)
        {
            m_enemyInfoPanel = GameObject.Find("EnemyInfoPanel");
        }

        if (m_enemyInfoPanel != null)
        {
            if (m_enemyInfoPanelUI == null)
            {
                m_enemyInfoPanelUI = m_enemyInfoPanel.GetComponent<EnemyInfoPanel>();
                if (m_enemyInfoPanelUI == null) m_enemyInfoPanelUI = m_enemyInfoPanel.AddComponent<EnemyInfoPanel>();
            }
        }
    }

    private void OnEnable()
    {
        BoardInputController.OnTowerClickedAction += ShowTowerpanel;
        BoardInputController.OnBoardWorldClickedAction += HandleGroundWorldClick;
    }

    private void OnDisable()
    {
        BoardInputController.OnTowerClickedAction -= ShowTowerpanel;
        BoardInputController.OnBoardWorldClickedAction -= HandleGroundWorldClick;
    }

    private void Start()
    {
        InitUI();

        if (GameManager.Instance != null)
        {
            SetPlayerActionUI(GameManager.Instance.CanPerformPlayerAction);
        }

        Button[] quitButtons = { m_btnQuit, m_btnGameOverHome, m_btnGameClearHome, m_btnNextStage };
        foreach (var btn in quitButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => GameManager.Instance.OnQuitGame());
            }
        }

        Button[] restartButtons = { m_btnRestart, m_btnRetry };
        foreach (var btn in restartButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(RestartGameLogic);
            }
        }

        Button[] MenuButtons = { m_btnGameOverMenu, m_btnGameClearMenu };
        foreach (var btn in MenuButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(MenuGameLogic);
            }
        }

        if (m_btnResume != null)
        {
            m_btnResume.onClick.RemoveAllListeners();
            m_btnResume.onClick.AddListener(OnResumeButtonClick);
        }

        if (m_btnNextStage != null)
        {
            m_btnNextStage.onClick.RemoveAllListeners();
            m_btnNextStage.onClick.AddListener(() =>
            {
                GameManager.Instance.OnQuitGame();
            });
        }

        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.TowerTierUpgraded += HandleTowerTierUpgraded;
        }

        BindInfoPanelSwitchButtons();

        if (m_btnDebug != null)
        {
            m_btnDebug.onClick.AddListener(() =>
            {
                m_debugconsole.TestLog();
            });
        }
    }

    private void OnDestroy()
    {
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.TowerTierUpgraded -= HandleTowerTierUpgraded;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 티어 강화로 선택 중이던 타워가 교체되면(교체된 타워는 SpawnIndex가 -1) 새 타워를 이어서 선택합니다.
    private void HandleTowerTierUpgraded(TowerController upgradedTower)
    {
        if (upgradedTower == null || m_selectedTower == null || m_selectedTower.SpawnIndex >= 0) return;

        m_selectedTower = upgradedTower;
        BoardInputController.SelectReplacementTower(upgradedTower);
    }

    #endregion

    #region General UI Actions and Cost Labels

    void RestartGameLogic()
    {
        GameManager.Instance.SetGameSpeed(1.0f);
        SceneLoader.StartLoad(m_sceneName_Restart);
    }

    void MenuGameLogic()
    {
        SceneLoader.StartLoad(m_sceneName_Menu);
    }

    public void OnResumeButtonClick()
    {
        if (m_panelOption != null)
        {
            m_panelOption.SetActive(false);
        }

        GameManager.Instance.OnResumeGame();
    }

    #endregion

    #region Buff and Debuff Names

    public static string GetBuffTargetName(BuffTarget buffTarget)
    {
        return buffTarget switch
        {
            BuffTarget.Sword => "공격력",
            BuffTarget.Bow => "사거리",
            BuffTarget.Shield => "방어막",
            BuffTarget.Spear => "치명타 확률",
            BuffTarget.Axe => "치명타 피해",
            BuffTarget.Hammer => "방어 관통",
            BuffTarget.Fire => "공격 횟수",
            BuffTarget.Ice => "스플래시/추가 타격",
            BuffTarget.Electricity => "추가 공격",
            BuffTarget.Wind => "행동력",
            BuffTarget.Earth => "티어 강화",
            BuffTarget.Light => "체인 공격",
            BuffTarget.Darkness => "다크니스 강화",
            _ => "버프"
        };
    }

    public static string GetDebuffTargetName(DebuffTarget debuffTarget)
    {
        return debuffTarget switch
        {
            DebuffTarget.Sword => "저주",
            DebuffTarget.Bow => "고정 피해",
            DebuffTarget.Shield => "속박",
            DebuffTarget.Spear => "투창",
            DebuffTarget.Axe => "취약",
            DebuffTarget.Hammer => "방어 파괴",
            DebuffTarget.Fire => "화상",
            DebuffTarget.Ice => "빙결",
            DebuffTarget.Electricity => "감전",
            DebuffTarget.Wind => "바람",
            DebuffTarget.Earth => "대지",
            DebuffTarget.Light => "빛",
            DebuffTarget.Darkness => "암흑",
            _ => "디버프"
        };
    }

    #endregion

    #region HUD and Selection

    public void InitUI()
    {
        if (m_gameInfoPanel != null) m_gameInfoPanel.SetActive(true);
        if (m_panelOption != null) m_panelOption.SetActive(false);
        if (m_panelGameover != null) m_panelGameover.SetActive(false);
        if (m_panelGameclear != null) m_panelGameclear.SetActive(false);
        if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(false);
        if (m_tileInfoPanel != null) m_tileInfoPanel.SetActive(false);
        HideEnemyInfoPanel();
        RefreshInfoPanelSwitchButtons();
    }


    float GetGameButtonSpeed()
    {
        return GameManager.Instance.GetGameSpeed();
    }

    public void RestartUI()
    {
        if (GameManager.Instance != null)
        {
            float gamespeed = GameManager.Instance.GetGameSpeed();
        }
    }

    private void ShowTowerpanel(TowerController clickedTower)
    {
        HideTileInfoPanel();
        HideEnemyInfoPanel();
        m_selectedTower = clickedTower;

        // 패널 내용은 TowerInfoPanel이 켜질 때 SelectedTower를 읽어 표시합니다.
        if (m_towerInfoPanel != null)
        {
            m_towerInfoPanel.SetActive(true);
        }

        RefreshInfoPanelSwitchButtons();
    }

    private void HideTowerPanel()
    {
        if (m_towerInfoPanel != null && m_towerInfoPanel.activeSelf)
        {
            m_towerInfoPanel.SetActive(false);
        }
        RefreshInfoPanelSwitchButtons();
    }

    /// <summary>판매·합성으로 선택한 타워가 사라졌을 때 TowerInfoPanel이 호출합니다.</summary>
    public void CloseTowerInfoPanel()
    {
        m_selectedTower = null;
        HideTowerPanel();
    }

    /// <summary>
    /// 빈 월드 클릭 중 실제 타일을 클릭한 경우, 타일 종류에 맞는 Dictionary 내용을 패널에 출력합니다.
    /// 타워 스폰 타일과 패스 타일은 서로 다른 Tilemap이므로 각각 먼저 확인합니다.
    /// </summary>
    public bool IsTowerInfoPanelElement(GameObject element)
    {
        return element != null && m_towerInfoPanel != null &&
               element.transform.IsChildOf(m_towerInfoPanel.transform);
    }

    private void HandleGroundWorldClick(Vector3 worldPosition)
    {
        HideTowerPanel();

        if (TryGetTowerSpawnTileAtWorldPosition(worldPosition, out Vector3Int towerCell))
        {
            HideEnemyInfoPanel();
            ShowTileInfoPanel(towerCell, true);
            return;
        }

        if (TileManager.Instance != null && TileManager.Instance.TryGetPathCellAtWorldPosition(worldPosition, out Vector3Int pathCell))
        {
            HideTileInfoPanel();
            if (TileManager.Instance.GetEnemyOccupantsAtPathCell(pathCell).Count > 0)
                ShowEnemyInfoPanel(pathCell);
            else
                ShowTileInfoPanel(pathCell, false);
            return;
        }

        HideTileInfoPanel();
        HideEnemyInfoPanel();
    }

    private static bool TryGetTowerSpawnTileAtWorldPosition(Vector3 worldPosition, out Vector3Int cell)
    {
        cell = default;
        if (TileManager.Instance == null) return false;

        return TileManager.Instance.TryGetTowerSpawnIndexAtWorldPosition(worldPosition, out int index) &&
               TileManager.Instance.TryGetTowerSpawnTileCell(index, out cell);
    }

    /// <summary>
    /// 선택한 타일의 고정 맵 효과와 동적 버프/디버프 효과를 각각 항목 프리팹으로 표시합니다.
    /// </summary>
    private void ShowTileInfoPanel(Vector3Int cell, bool isTowerSpawnTile)
    {
        if (m_tileInfoPanel == null) return;
        HideEnemyInfoPanel();

        m_selectedTileCell = cell;
        m_selectedTileIsTowerSpawn = isTowerSpawnTile;
        m_hasSelectedTile = true;

        ClearTileInfoEntries(m_spawnedTileBuffEntries);
        ClearTileInfoEntries(m_spawnedTileDebuffEntries);

        List<string> buffDescriptions = new List<string>();
        List<string> debuffDescriptions = new List<string>();

        if (TileManager.Instance != null)
        {
            if (isTowerSpawnTile)
            {
                AddTowerMapTileDescription(TileManager.Instance.GetTowerTileBuffAt(cell), buffDescriptions);
                IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(cell);
                for (int i = 0; i < effects.Count; i++)
                {
                    TowerTileBuffEffect effect = effects[i];
                    buffDescriptions.Add($"{GetBuffTargetName(effect.Target)} 버프\n티어 {effect.Tier} · 지속 {effect.RemainingTurns}턴");
                }
            }
            else
            {
                AddPathMapTileDescription(TileManager.Instance.GetTileTypeAt(cell), buffDescriptions);
                IReadOnlyList<PathTileDebuffEffect> effects = TileManager.Instance.GetPathDebuffEffectsAt(cell);
                for (int i = 0; i < effects.Count; i++)
                {
                    PathTileDebuffEffect effect = effects[i];
                    string state = effect.ApplicationVersion <= 0
                        ? "배치 예정"
                        : $"지속 {effect.Duration}턴";
                    debuffDescriptions.Add($"{GetDebuffTargetName(effect.Target)} 디버프\n티어 {effect.Tier} · {state}");
                }
            }
        }

        // ScrollRect가 연결되어 있다면 외부 Rect가 아니라 실제 Content에 생성해야
        // Viewport 마스크와 스크롤바가 정상적으로 목록을 제어합니다.
        RectTransform buffContent = m_tileBuffScrollRect != null && m_tileBuffScrollRect.content != null
            ? m_tileBuffScrollRect.content
            : m_tileBuffContent;
        RectTransform debuffContent = m_tileDebuffScrollRect != null && m_tileDebuffScrollRect.content != null
            ? m_tileDebuffScrollRect.content
            : m_tileDebuffContent;

        CreateTileInfoEntries(
            m_tileBuffEntryPrefab,
            buffContent,
            buffDescriptions,
            m_tileBuffStartY,
            m_spawnedTileBuffEntries);
        CreateTileInfoEntries(
            m_tileDebuffEntryPrefab,
            debuffContent,
            debuffDescriptions,
            m_tileDebuffStartY,
            m_spawnedTileDebuffEntries);

        m_tileInfoPanel.SetActive(true);
        if (m_tileBuffScrollRect != null) m_tileBuffScrollRect.verticalNormalizedPosition = 1f;
        if (m_tileDebuffScrollRect != null) m_tileDebuffScrollRect.verticalNormalizedPosition = 1f;
        RefreshInfoPanelSwitchButtons();
    }

    private void HideTileInfoPanel()
    {
        if (m_tileInfoPanel != null && m_tileInfoPanel.activeSelf)
        {
            m_tileInfoPanel.SetActive(false);
        }
        RefreshInfoPanelSwitchButtons();
    }

    public void ShowEnemyInfoPanel(Vector3Int pathCell)
    {
        HideTowerPanel();
        HideTileInfoPanel();

        m_selectedTileCell = pathCell;
        m_selectedTileIsTowerSpawn = false;
        m_hasSelectedTile = true;

        if (m_enemyInfoPanel == null)
        {
            m_enemyInfoPanel = GameObject.Find("EnemyInfoPanel");
        }

        if (m_enemyInfoPanel != null)
        {
            if (m_enemyInfoPanelUI == null)
            {
                m_enemyInfoPanelUI = m_enemyInfoPanel.GetComponent<EnemyInfoPanel>();
                if (m_enemyInfoPanelUI == null) m_enemyInfoPanelUI = m_enemyInfoPanel.AddComponent<EnemyInfoPanel>();
            }

            int pathIndex = TilePath.Instance != null ? TilePath.Instance.GetPathIndexAtGridPosition(pathCell) : -1;
            m_enemyInfoPanelUI.OpenForTile(pathCell, pathIndex);
        }

        RefreshInfoPanelSwitchButtons();
    }

    public void HideEnemyInfoPanel()
    {
        if (m_enemyInfoPanelUI != null)
        {
            m_enemyInfoPanelUI.Hide();
        }
        else if (m_enemyInfoPanel != null && m_enemyInfoPanel.activeSelf)
        {
            m_enemyInfoPanel.SetActive(false);
        }
        RefreshInfoPanelSwitchButtons();
    }

    #endregion

    #region Info Panel Switching and Tile Entries

    private void BindInfoPanelSwitchButtons()
    {
        foreach (Button button in m_btnChangeTowerInfoPanels)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SwitchToTowerInfoPanel);
        }

        foreach (Button button in m_btnChangeTileInfoPanels)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SwitchToTileInfoPanel);
        }

        if (m_enemyInfoPanel != null)
        {
            Button[] enemyButtons = m_enemyInfoPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in enemyButtons)
            {
                if (button.name.Contains("Tower"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(SwitchToTowerInfoPanel);
                }
                else if (button.name.Contains("Tile"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(SwitchToTileInfoPanel);
                }
            }
        }

        RefreshInfoPanelSwitchButtons();
    }

    private void SwitchToTowerInfoPanel()
    {
        HideEnemyInfoPanel();

        if (m_selectedTower != null && !m_selectedTower.Equals(null))
        {
            ShowTowerpanel(m_selectedTower);
            return;
        }

        if (m_hasSelectedTile && m_selectedTileIsTowerSpawn &&
            TileManager.Instance != null && TileManager.Instance.TryGetTowerAt(m_selectedTileCell, out TowerController tower))
        {
            ShowTowerpanel(tower);
            return;
        }

        Debug.Log("[UIManager] 선택된 타워가 없어 타워 정보 패널로 전환할 수 없습니다.");
    }

    private void SwitchToTileInfoPanel()
    {
        HideEnemyInfoPanel();

        if (m_selectedTower != null && !m_selectedTower.Equals(null) && TowerManager.Instance != null)
        {
            if (TileManager.Instance != null &&
                TileManager.Instance.TryGetTowerSpawnTileCell(m_selectedTower.SpawnIndex, out Vector3Int towerCell))
                ShowTileInfoPanel(towerCell, true);
            HideTowerPanel();
            return;
        }

        if (m_hasSelectedTile)
        {
            HideTowerPanel();
            ShowTileInfoPanel(m_selectedTileCell, m_selectedTileIsTowerSpawn);
        }
    }

    /// <summary>
    /// 현재 열려 있는 정보 패널에 해당하는 전환 버튼은 비활성화합니다.
    /// Button의 Transition이 Color Tint이면 interactable=false 상태가 자동으로 회색으로 표현됩니다.
    /// </summary>
    private void RefreshInfoPanelSwitchButtons()
    {
        bool isTowerInfoOpen = m_towerInfoPanel != null && m_towerInfoPanel.activeSelf;
        bool isTileInfoOpen = m_tileInfoPanel != null && m_tileInfoPanel.activeSelf;
        bool isEnemyInfoOpen = m_enemyInfoPanel != null && m_enemyInfoPanel.activeSelf;

        foreach (Button button in m_btnChangeTowerInfoPanels)
        {
            if (button != null) button.interactable = !isTowerInfoOpen;
        }

        foreach (Button button in m_btnChangeTileInfoPanels)
        {
            if (button != null) button.interactable = !isTileInfoOpen;
        }

        if (m_enemyInfoPanel != null)
        {
            Button[] enemyButtons = m_enemyInfoPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in enemyButtons)
            {
                if (button.name.Contains("Tower")) button.interactable = !isTowerInfoOpen;
                else if (button.name.Contains("Tile")) button.interactable = !isTileInfoOpen;
            }
        }
    }

    private void CreateTileInfoEntries(
        GameObject entryPrefab,
        RectTransform content,
        List<string> descriptions,
        float startY,
        List<GameObject> spawnedEntries)
    {
        if (entryPrefab == null || content == null) return;

        for (int index = 0; index < descriptions.Count; index++)
        {
            GameObject entry = Instantiate(entryPrefab, content);
            entry.name = $"{entryPrefab.name}_{index + 1}";
            entry.SetActive(true);

            RectTransform entryRect = entry.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                entryRect.anchoredPosition = new Vector2(entryRect.anchoredPosition.x, startY - m_tileEntrySpacing * index);
            }

            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = descriptions[index];
            }
            else
            {
                Debug.LogWarning($"[UIManager] {entryPrefab.name} 프리팹에서 TextMeshProUGUI를 찾지 못했습니다.");
            }

            spawnedEntries.Add(entry);
        }

        // Content 높이를 늘려 ScrollRect가 항목 전체를 스크롤할 수 있게 합니다.
        if (descriptions.Count > 0)
        {
            float requiredHeight = Mathf.Abs(startY - m_tileEntrySpacing * (descriptions.Count - 1)) + m_tileEntrySpacing;
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(content.sizeDelta.y, requiredHeight));
        }
    }

    private static void ClearTileInfoEntries(List<GameObject> entries)
    {
        foreach (GameObject entry in entries)
        {
            if (entry != null) Destroy(entry);
        }
        entries.Clear();
    }

    private static void AddTowerMapTileDescription(TowerTileBuffType type, List<string> descriptions)
    {
        switch (type)
        {
            case TowerTileBuffType.AttackPowerUp:
                descriptions.Add("공격력 타일\n공격력 50% 증가");
                break;
            case TowerTileBuffType.ActionCountUp:
                descriptions.Add("행동력 타일\n행동력 1 감소");
                break;
            case TowerTileBuffType.AttackCountUp:
                descriptions.Add("공격 횟수 타일\n공격 횟수 1 증가");
                break;
        }
    }

    private static void AddPathMapTileDescription(PathTileBuffType type, List<string> descriptions)
    {
        switch (type)
        {
            case PathTileBuffType.DefendTile:
                descriptions.Add("방어 타일\n방어력 20% 증가");
                break;
            case PathTileBuffType.SpeedTile:
                descriptions.Add("속도 타일\n행동 주기 1 감소 · 이동 주사위 최대값 +2");
                break;
            case PathTileBuffType.HealTile:
                descriptions.Add("회복 타일\n현재 체력의 20% 회복");
                break;
        }
    }

    #endregion

    #region Player Action and Game Result UI

    /// <summary>
    /// 턴 상태가 바뀔 때 행동 UI만 표시하거나 숨깁니다.
    /// 타워/적 정보 패널은 건드리지 않으므로 적 턴에도 정보를 확인할 수 있습니다.
    /// </summary>
    public void SetPlayerActionUI(bool isVisible)
    {
        m_isPlayerActionUIVisible = isVisible;

        if (m_playerTurnPanel != null)
        {
            m_playerTurnPanel.SetActive(isVisible);
        }
    }


    public void ShowGameOver()
    {
        // EnemyController 대신 EnemyHealthController를 탐색하여 체력바 숨김 처리
        EnemyHealthController[] allEnemies = FindObjectsByType<EnemyHealthController>(FindObjectsSortMode.None);
        for (int i = 0; i < allEnemies.Length; i++)
        {
            allEnemies[i].HideHPBar();
        }

        if (m_panelGameover != null)
        {
            if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(false);
            m_panelGameover.SetActive(true);
        }
    }

    public void ShowGameClear()
    {
        if (m_panelGameclear != null)
        {
            m_panelGameclear.SetActive(true);
        }
    }

    #endregion
}
