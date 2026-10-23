using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 패널(게임 정보, 플레이어 턴, 타워/타일/적 정보, 게임 종료)을 켜고 끕니다.
/// 플레이어 턴 패널과 타워/타일 정보 패널의 내용은 PlayerTurnPanel, TowerInfoPanel, TileinfoPanel이 직접 갱신합니다.
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
    [Tooltip("타일을 클릭했을 때 표시할 패널. 버프/디버프 목록은 TileinfoPanel이 담당합니다.")]
    [SerializeField] GameObject m_tileInfoPanel;
    [Tooltip("두 정보 패널 안에 있는 '타워 정보로 전환' 버튼들을 모두 넣습니다.")]
    [SerializeField] Button[] m_btnChangeTowerInfoPanels = System.Array.Empty<Button>();
    [Tooltip("두 정보 패널 안에 있는 '타일 정보로 전환' 버튼들을 모두 넣습니다.")]
    [SerializeField] Button[] m_btnChangeTileInfoPanels = System.Array.Empty<Button>();

    [Header("UI Enemy Info")]
    [Tooltip("패스 타일을 클릭했을 때 표시할 적 정보 패널")]
    [SerializeField] EnemyInfoPanel m_enemyInfoPanelUI;

    [Header("UI Option")]
    [Tooltip("버튼은 OptionPanel이 담당합니다.")]
    [SerializeField] GameObject m_panelOption;

    [Header("Game Clear")]
    [Tooltip("버튼은 GameClearPanel이 담당합니다.")]
    [SerializeField] GameObject m_panelGameclear;

    [Header("Game Over")]
    [Tooltip("버튼은 GameOverPanel이 담당합니다.")]
    [SerializeField] GameObject m_panelGameover;

    #endregion

    #region Runtime State

    // 타워/타일/적 정보 패널은 한 번에 하나만 켭니다.
    private enum InfoPanel { None, Tower, Tile, Enemy }

    private TowerController m_selectedTower;
    public TowerController SelectedTower => m_selectedTower;

    private Vector3Int m_selectedTileCell;
    private bool m_selectedTileIsTowerSpawn;
    private bool m_hasSelectedTile;
    public Vector3Int SelectedTileCell => m_selectedTileCell;
    public bool SelectedTileIsTowerSpawn => m_selectedTileIsTowerSpawn;
    public bool HasSelectedTile => m_hasSelectedTile;

    // 타일 정보 패널에 표시할 타일이 바뀌면 (셀, 타워 스폰 타일 여부)를 알립니다.
    public event System.Action<Vector3Int, bool> SelectedTileChanged;

    // 인스펙터 배열과 적 정보 패널 안의 전환 버튼을 합쳐 둔 목록입니다.
    private readonly List<Button> m_towerSwitchButtons = new List<Button>();
    private readonly List<Button> m_tileSwitchButtons = new List<Button>();

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

        if (m_enemyInfoPanelUI == null)
        {
            Debug.LogError("UIManager : m_enemyInfoPanelUI가 연결되지 않아 적 정보 패널을 열 수 없습니다.");
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
        BindInfoPanelSwitchButtons();
        InitUI();

        if (GameManager.Instance != null)
        {
            SetPlayerActionUI(GameManager.Instance.CanPerformPlayerAction);
        }

        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.TowerTierUpgraded += HandleTowerTierUpgraded;
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

    #region HUD and Selection

    public void InitUI()
    {
        if (m_gameInfoPanel != null) m_gameInfoPanel.SetActive(true);
        if (m_panelOption != null) m_panelOption.SetActive(false);
        if (m_panelGameover != null) m_panelGameover.SetActive(false);
        if (m_panelGameclear != null) m_panelGameclear.SetActive(false);
        SetInfoPanel(InfoPanel.None);
    }

    /// <summary>
    /// 지정한 정보 패널만 남기고 나머지를 끈 뒤 전환 버튼 상태를 갱신합니다.
    /// 적 정보 패널은 EnemyInfoPanel.OpenForTile이 직접 켜므로 여기서는 끄기만 합니다.
    /// </summary>
    private void SetInfoPanel(InfoPanel panel)
    {
        if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(panel == InfoPanel.Tower);
        if (m_tileInfoPanel != null) m_tileInfoPanel.SetActive(panel == InfoPanel.Tile);

        if (panel != InfoPanel.Enemy && m_enemyInfoPanelUI != null && m_enemyInfoPanelUI.gameObject.activeSelf)
        {
            m_enemyInfoPanelUI.Hide();
        }

        RefreshInfoPanelSwitchButtons();
    }

    private void ShowTowerpanel(TowerController clickedTower)
    {
        // 패널 내용은 TowerInfoPanel이 켜질 때 SelectedTower를 읽어 표시합니다.
        m_selectedTower = clickedTower;
        SetInfoPanel(InfoPanel.Tower);
    }

    /// <summary>판매·합성으로 선택한 타워가 사라졌을 때 TowerInfoPanel이 호출합니다.</summary>
    public void CloseTowerInfoPanel()
    {
        m_selectedTower = null;

        if (m_towerInfoPanel != null && m_towerInfoPanel.activeSelf)
        {
            SetInfoPanel(InfoPanel.None);
        }
    }

    public bool IsTowerInfoPanelElement(GameObject element)
    {
        return element != null && m_towerInfoPanel != null &&
               element.transform.IsChildOf(m_towerInfoPanel.transform);
    }

    /// <summary>
    /// 빈 월드 클릭 중 실제 타일을 클릭한 경우, 타일 종류에 맞는 패널을 엽니다.
    /// 타워 스폰 타일과 패스 타일은 서로 다른 Tilemap이므로 각각 먼저 확인합니다.
    /// </summary>
    private void HandleGroundWorldClick(Vector3 worldPosition)
    {
        // 타워가 아닌 곳을 클릭했으므로 이전에 선택한 타워는 더 이상 전환 대상이 아닙니다.
        m_selectedTower = null;

        if (TryGetTowerSpawnTileAtWorldPosition(worldPosition, out Vector3Int towerCell))
        {
            ShowTileInfoPanel(towerCell, true);
            return;
        }

        if (TileManager.Instance != null && TileManager.Instance.TryGetPathCellAtWorldPosition(worldPosition, out Vector3Int pathCell))
        {
            if (TileManager.Instance.GetLivingEnemiesAtPathCell(pathCell).Count > 0)
                ShowEnemyInfoPanel(pathCell);
            else
                ShowTileInfoPanel(pathCell, false);
            return;
        }

        SetInfoPanel(InfoPanel.None);
    }

    private static bool TryGetTowerSpawnTileAtWorldPosition(Vector3 worldPosition, out Vector3Int cell)
    {
        cell = default;
        if (TileManager.Instance == null) return false;

        return TileManager.Instance.TryGetTowerSpawnIndexAtWorldPosition(worldPosition, out int index) &&
               TileManager.Instance.TryGetTowerSpawnTileCell(index, out cell);
    }

    /// <summary>
    /// 선택한 타일을 기록하고 타일 정보 패널을 켭니다.
    /// 패널 내용은 TileinfoPanel이 SelectedTileChanged를 받거나, 켜질 때 선택된 타일을 읽어 표시합니다.
    /// </summary>
    private void ShowTileInfoPanel(Vector3Int cell, bool isTowerSpawnTile)
    {
        if (m_tileInfoPanel == null) return;

        m_selectedTileCell = cell;
        m_selectedTileIsTowerSpawn = isTowerSpawnTile;
        m_hasSelectedTile = true;

        SelectedTileChanged?.Invoke(cell, isTowerSpawnTile);
        SetInfoPanel(InfoPanel.Tile);
    }

    public void ShowEnemyInfoPanel(Vector3Int pathCell)
    {
        m_selectedTileCell = pathCell;
        m_selectedTileIsTowerSpawn = false;
        m_hasSelectedTile = true;

        if (m_enemyInfoPanelUI != null)
        {
            int pathIndex = TileManager.Instance != null && TileManager.Instance.TryGetPathTileIndex(pathCell, out int index)
                ? index
                : -1;
            m_enemyInfoPanelUI.OpenForTile(pathCell, pathIndex);
        }

        SetInfoPanel(InfoPanel.Enemy);
    }

    public void HideEnemyInfoPanel()
    {
        if (m_enemyInfoPanelUI != null && m_enemyInfoPanelUI.gameObject.activeSelf)
        {
            SetInfoPanel(InfoPanel.None);
        }
    }

    #endregion

    #region Info Panel Switching

    private void BindInfoPanelSwitchButtons()
    {
        m_towerSwitchButtons.Clear();
        m_tileSwitchButtons.Clear();

        foreach (Button button in m_btnChangeTowerInfoPanels)
        {
            if (button != null) m_towerSwitchButtons.Add(button);
        }

        foreach (Button button in m_btnChangeTileInfoPanels)
        {
            if (button != null) m_tileSwitchButtons.Add(button);
        }

        // 적 정보 패널 안의 전환 버튼은 이름으로 한 번만 찾아 둡니다.
        if (m_enemyInfoPanelUI != null)
        {
            Button[] enemyButtons = m_enemyInfoPanelUI.GetComponentsInChildren<Button>(true);
            foreach (Button button in enemyButtons)
            {
                if (button.name.Contains("Tower"))
                {
                    if (!m_towerSwitchButtons.Contains(button)) m_towerSwitchButtons.Add(button);
                }
                else if (button.name.Contains("Tile"))
                {
                    if (!m_tileSwitchButtons.Contains(button)) m_tileSwitchButtons.Add(button);
                }
            }
        }

        foreach (Button button in m_towerSwitchButtons)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SwitchToTowerInfoPanel);
        }

        foreach (Button button in m_tileSwitchButtons)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SwitchToTileInfoPanel);
        }
    }

    private void SwitchToTowerInfoPanel()
    {
        if (m_selectedTower != null)
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
        if (m_selectedTower != null)
        {
            if (TileManager.Instance != null &&
                TileManager.Instance.TryGetTowerSpawnTileCell(m_selectedTower.SpawnIndex, out Vector3Int towerCell))
                ShowTileInfoPanel(towerCell, true);
            else
                SetInfoPanel(InfoPanel.None);
            return;
        }

        if (m_hasSelectedTile)
        {
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

        foreach (Button button in m_towerSwitchButtons)
        {
            if (button != null) button.interactable = !isTowerInfoOpen;
        }

        foreach (Button button in m_tileSwitchButtons)
        {
            if (button != null) button.interactable = !isTileInfoOpen;
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

    /// <summary>
    /// 옵션 패널을 켜고 게임을 일시 정지합니다. 재개는 OptionPanel의 계속하기 버튼이 합니다.
    /// </summary>
    public void ShowOptionPanel()
    {
        if (m_panelOption == null)
        {
            Debug.LogError("UIManager : m_panelOption이 연결되지 않아 옵션 패널을 열 수 없습니다.");
            return;
        }

        m_panelOption.SetActive(true);
        GameManager.Instance?.OnPauseGame();
    }

    #endregion
}
