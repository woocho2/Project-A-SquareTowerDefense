using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("Panel")]
        [SerializeField] GameObject m_gameInfoPanel;

        [Header("UI Player Action")]
        [SerializeField] GameObject m_playerTurnPanel;

        [Header("UI Tower Info")]
        [SerializeField] GameObject m_towerInfoPanel;

        [Header("UI Tile Info")]
        [SerializeField] GameObject m_tileInfoPanel;
        [SerializeField] Button[] m_btnChangeTowerInfoPanels = Array.Empty<Button>();
        [SerializeField] Button[] m_btnChangeTileInfoPanels = Array.Empty<Button>();

        [Header("UI Enemy Info")]
        [SerializeField] EnemyInfoPanel m_enemyInfoPanelUI;

        [Header("UI Zone Info")]
        [SerializeField] ZoneInfoPanel m_zoneInfoPanelUI;

        [Header("UI Option")]
        [SerializeField] GameObject m_panelOption;

        [Header("Game Clear")]
        [SerializeField] GameObject m_panelGameclear;

        [Header("Game Over")]
        [SerializeField] GameObject m_panelGameover;

        private enum InfoPanel { None, Tower, Tile, Enemy, Zone }

        public TowerController SelectedTower { get; private set; }
        public int SelectedPathIndex { get; private set; } = -1;
        public int SelectedSpawnIndex { get; private set; } = -1;
        public bool HasSelectedTile => SelectedPathIndex >= 0 || SelectedSpawnIndex >= 0;
        public bool IsPaused { get; private set; }

        public event Action SelectedTileChanged;
        public event Action<TowerController> SelectedTowerChanged;

        private readonly List<Button> m_towerSwitchButtons = new List<Button>();
        private readonly List<Button> m_tileSwitchButtons = new List<Button>();
        private float m_timeScaleBeforePause = 1f;

        private void Start()
        {
            BindInfoPanelSwitchButtons();
            InitUI();

            TurnManager.Instance.StateChanged += HandleStateChanged;
            HandleStateChanged(TurnManager.Instance.Current);

            TowerManager.Instance.SelectionChanged += HandleSelectionChanged;
            TowerManager.Instance.TowerRemoved += HandleTowerRemoved;
            m_input = FindFirstObjectByType<BoardInputController>();
            if (m_input != null) m_input.Clicked += HandleBoardClicked;
            TowerManager.Instance.TowerReplaced += HandleTowerReplaced;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (TurnManager.Instance != null) TurnManager.Instance.StateChanged -= HandleStateChanged;
            if (TowerManager.Instance != null)
            {
                TowerManager.Instance.SelectionChanged -= HandleSelectionChanged;
                TowerManager.Instance.TowerRemoved -= HandleTowerRemoved;
                TowerManager.Instance.TowerReplaced -= HandleTowerReplaced;
            }
            if (m_input != null) m_input.Clicked -= HandleBoardClicked;
            if (IsPaused) Time.timeScale = m_timeScaleBeforePause;
        }

        private BoardInputController m_input;

        private void HandleSelectionChanged(TowerController tower)
        {
            if (tower != null) ShowTowerPanel(tower);
        }

        private void HandleTowerRemoved(TowerController tower)
        {
            if (SelectedTower == tower) CloseTowerInfoPanel();
        }

        private void HandleTowerReplaced(TowerController old, TowerController created)
        {
            if (SelectedTower == old) ShowTowerPanel(created);
        }

        private void HandleBoardClicked(BoardPoint point)
        {
            BoardManager board = BoardManager.Instance;

            if (point.IsSpawn)
            {
                if (board.Slots.Get(point.SpawnIndex) == null) ShowTileInfoPanel(-1, point.SpawnIndex);
                return;
            }

            if (point.IsPath)
            {
                if (board.Occupancy.GetAt(point.PathIndex).Count > 0) ShowEnemyInfoPanel(point.PathIndex);
                else ShowTileInfoPanel(point.PathIndex, -1);
                return;
            }

            HideInfoPanels();
        }

        public void InitUI()
        {
            if (m_gameInfoPanel != null) m_gameInfoPanel.SetActive(true);
            if (m_panelOption != null) m_panelOption.SetActive(false);
            if (m_panelGameover != null) m_panelGameover.SetActive(false);
            if (m_panelGameclear != null) m_panelGameclear.SetActive(false);
            SetInfoPanel(InfoPanel.None);
        }

        private void HandleStateChanged(ITurnState state)
        {
            SetPlayerActionUI(state.CanPlayerAct);

            if (state is GameOverState) ShowGameOver();
            else if (state is GameClearState) ShowGameClear();
        }

        public void SetPlayerActionUI(bool isVisible)
        {
            if (m_playerTurnPanel != null) m_playerTurnPanel.SetActive(isVisible);
        }

        public void ShowTowerPanel(TowerController tower)
        {
            if (tower == null) return;

            SelectedTower = tower;
            SelectedSpawnIndex = tower.SlotIndex;
            SelectedPathIndex = -1;
            SelectedTowerChanged?.Invoke(tower);
            SetInfoPanel(InfoPanel.Tower);
        }

        public void ShowTileInfoPanel(int pathIndex, int spawnIndex)
        {
            SelectedPathIndex = pathIndex;
            SelectedSpawnIndex = spawnIndex;
            SelectedTileChanged?.Invoke();
            SetInfoPanel(InfoPanel.Tile);
        }

        public void ShowEnemyInfoPanel(int pathIndex)
        {
            SelectedTower = null;
            SelectedPathIndex = pathIndex;
            SelectedSpawnIndex = -1;

            if (m_enemyInfoPanelUI != null) m_enemyInfoPanelUI.OpenForTile(pathIndex);
            SetInfoPanel(InfoPanel.Enemy);
        }

        public void ShowZoneInfoPanel(int pathIndex, int spawnIndex)
        {
            SelectedPathIndex = pathIndex;
            SelectedSpawnIndex = spawnIndex;

            if (m_zoneInfoPanelUI != null) m_zoneInfoPanelUI.OpenForTile(pathIndex, spawnIndex);
            SetInfoPanel(InfoPanel.Zone);
        }

        public void CloseTowerInfoPanel()
        {
            SelectedTower = null;
            if (m_towerInfoPanel != null && m_towerInfoPanel.activeSelf) SetInfoPanel(InfoPanel.None);
        }

        public void HideEnemyInfoPanel()
        {
            if (m_enemyInfoPanelUI != null && m_enemyInfoPanelUI.gameObject.activeSelf) SetInfoPanel(InfoPanel.None);
        }

        public void HideInfoPanels()
        {
            SelectedTower = null;
            SetInfoPanel(InfoPanel.None);
        }

        public bool IsTowerInfoPanelElement(GameObject element)
        {
            return element != null && m_towerInfoPanel != null && element.transform.IsChildOf(m_towerInfoPanel.transform);
        }

        private void SetInfoPanel(InfoPanel panel)
        {
            if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(panel == InfoPanel.Tower);
            if (m_tileInfoPanel != null) m_tileInfoPanel.SetActive(panel == InfoPanel.Tile);

            if (panel != InfoPanel.Enemy && m_enemyInfoPanelUI != null && m_enemyInfoPanelUI.gameObject.activeSelf)
                m_enemyInfoPanelUI.Hide();
            if (panel != InfoPanel.Zone && m_zoneInfoPanelUI != null && m_zoneInfoPanelUI.gameObject.activeSelf)
                m_zoneInfoPanelUI.Hide();

            RefreshInfoPanelSwitchButtons();
        }

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

            if (m_enemyInfoPanelUI != null)
            {
                foreach (Button button in m_enemyInfoPanelUI.GetComponentsInChildren<Button>(true))
                {
                    if (button.name.Contains("Tower") && !m_towerSwitchButtons.Contains(button)) m_towerSwitchButtons.Add(button);
                    else if (button.name.Contains("Tile") && !m_tileSwitchButtons.Contains(button)) m_tileSwitchButtons.Add(button);
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
            if (SelectedTower != null)
            {
                ShowTowerPanel(SelectedTower);
                return;
            }

            if (SelectedSpawnIndex >= 0)
            {
                TowerController tower = BoardManager.Instance.Slots.Get(SelectedSpawnIndex);
                if (tower != null)
                {
                    ShowTowerPanel(tower);
                    return;
                }
            }

            Debug.Log("[UIManager] 선택된 타워가 없어 타워 정보 패널로 바꿀 수 없음");
        }

        private void SwitchToTileInfoPanel()
        {
            if (SelectedTower != null)
            {
                ShowTileInfoPanel(-1, SelectedTower.SlotIndex);
                return;
            }

            if (HasSelectedTile) ShowTileInfoPanel(SelectedPathIndex, SelectedSpawnIndex);
        }

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

        public void ShowGameOver()
        {
            if (m_panelGameover == null) return;
            if (m_towerInfoPanel != null) m_towerInfoPanel.SetActive(false);
            m_panelGameover.SetActive(true);
        }

        public void ShowGameClear()
        {
            if (m_panelGameclear != null) m_panelGameclear.SetActive(true);
        }

        public void ShowOptionPanel()
        {
            if (m_panelOption == null)
            {
                Debug.LogError("[UIManager] m_panelOption이 연결되지 않아 옵션 패널을 열 수 없음");
                return;
            }

            m_panelOption.SetActive(true);
            Pause();
        }

        public void CloseOptionPanel()
        {
            if (m_panelOption != null) m_panelOption.SetActive(false);
            Resume();
        }

        // 수업 43: 게임 속도가 생기면 멈추기 전 속도를 GameSpeed에서 읽고 되돌림
        public void Pause()
        {
            if (IsPaused) return;
            m_timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            Time.timeScale = m_timeScaleBeforePause;
            IsPaused = false;
        }
    }
}
