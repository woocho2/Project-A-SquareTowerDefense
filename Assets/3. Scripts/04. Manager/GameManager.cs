using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>게임 전체의 상위 턴 상태입니다.</summary>
public enum TurnState
{
    None,
    PlayerTurn,
    EnemyTurn,
    GameOver,
    GameClear
}

/// <summary>
/// 웨이브마다 소환 전 판정과 일괄 소환 뒤 플레이어 턴 1회와 적 턴 10회를 진행하고, 게임 속도, 적 수 한도와 종료 상태를 관리합니다.
/// 웨이브 구성과 적 능력치 계산은 WaveManager에 위임합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const int TurnsPerWave = 10;
    private const int FinalWave = 40;

    [Header("게임 배속 및 적 수 한도 설정")]
    private float m_currentgameSpeed = 1.0f;
    public float CurrentGameSpeed => m_currentgameSpeed;
    public event Action<int> EnemyLimitChanged;
    private int m_lastEnemyLimit;
    public int EnemyLimit
    {
        get
        {
            int cycle = WaveManager.Instance != null ? WaveManager.Instance.CurrentCycle : 1;
            int limitPerCycle = GameModeSession.SelectedDifficulty == GameDifficulty.Hard ? 15 : 20;

            return cycle * limitPerCycle;
        }
    }


    [Header("에너미 턴 연출 속도")]
    [SerializeField, Min(0f)] private float m_towerAttackResolutionDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float m_projectileWaitTimeout = 10f;

    public TurnState CurrentState { get; private set; } = TurnState.None;
    public bool CanPerformPlayerAction => m_currentTurnState != null && m_currentTurnState.CanPerformPlayerAction;

    public event Action<float> CurrentGameSpeedChanged;


    private IGameTurnState m_currentTurnState;

    private Coroutine turnRoutine;
    private bool isPlayerTurnEnd = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.CurrentWaveChanged += HandleWaveChanged;
        }

        RefreshEnemyLimit();

        OnStartGame();
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.CurrentWaveChanged -= HandleWaveChanged;
        }
    }

    public void OnStartGame()
    {
        Time.timeScale = m_currentgameSpeed;
        ChangeState(new IdleTurnState());

        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnLoopRoutine());
    }

    /// <summary>웨이브를 게임 종료 전까지 반복합니다.</summary>
    private IEnumerator TurnLoopRoutine()
    {
        // 다른 매니저의 Start(경로 초기화 등)가 모두 끝난 다음 프레임에 첫 웨이브를 시작합니다.
        yield return null;

        while (CurrentState != TurnState.GameOver && CurrentState != TurnState.GameClear)
        {
            // 웨이브 시작: 소환 전 잔존 적 수 판정 → 일괄 소환과 착지 대기
            if (CheckEnemyLimit()) yield break;
            if (EnemyManager.Instance != null)
            {
                yield return StartCoroutine(EnemyManager.Instance.SpawnWaveRoutine());
            }

            yield return StartCoroutine(EnemyWaveRoutine());
        }
    }

    private IEnumerator EnemyWaveRoutine()
    {
        // 소환된 적을 보고 배치하는 플레이어 턴은 웨이브마다 한 번입니다.
        yield return StartCoroutine(PlayerTurnRoutine());

        // 그 뒤 적 턴 10번을 자동으로 진행합니다. 웨이브 도중에는 소환하지 않습니다.
        for (int turn = 0; turn < TurnsPerWave; turn++)
        {
            if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;

            yield return StartCoroutine(EnemyTurnRoutine());
        }

        // 마지막 웨이브는 최종 보스가 처치되거나(클리어) 제한 턴 20이 다 될 때까지(게임 오버) 적 턴을 계속합니다.
        // 제한 턴은 EnemyTurnRoutine 끝에서 셉니다.
        while (WaveManager.Instance != null && WaveManager.Instance.CurrentWave >= FinalWave && IsFinalBossAlive())
        {
            if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;

            yield return StartCoroutine(EnemyTurnRoutine());
        }

        if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;

        if (WaveManager.Instance != null)
        {
            int completedWave = WaveManager.Instance.CurrentWave;

            if (completedWave < FinalWave)
            {
                WaveManager.Instance.NextWave();
            }

            TowerManager.Instance?.ProcessEarthTowerWave(completedWave);
        }
    }

    // 마지막 웨이브에 소환된 보스가 아직 길 위에 살아 있는지 확인합니다.
    private static bool IsFinalBossAlive()
    {
        if (TileManager.Instance == null) return false;

        List<EnemyHealthController> enemies = TileManager.Instance.GetAllLivingEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].EnemyType == EnemyType.Boss && enemies[i].SpawnWave >= FinalWave) return true;
        }
        return false;
    }

    private IEnumerator PlayerTurnRoutine()
    {
        ChangeState(new PlayerTurnState());
        isPlayerTurnEnd = false;

        // 스킵 버튼을 누를 때까지 플레이어 행동을 기다립니다.
        while (!isPlayerTurnEnd && CurrentState != TurnState.GameOver && CurrentState != TurnState.GameClear)
        {
            yield return null;
        }
    }

    // 플레이어 턴 스킵 버튼 UI 이벤트 연결용 함수
    public void OnClickSkipPlayerTurn()
    {
        if (CurrentState == TurnState.PlayerTurn)
        {
            CameraController.SmoothToEndTurnProjectionSize(this);
            UIManager.Instance?.SetPlayerActionUI(false);
            isPlayerTurnEnd = true;
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        ChangeState(new EnemyTurnState());

        // 1) 모든 적 이동
        yield return StartCoroutine(ProcessEnemyMovement());
        GameStateVersion.MarkChanged();

        // 2) 이동을 마친 타일의 즉시 효과 적용
        yield return StartCoroutine(ProcessTowerSupport());
        GameStateVersion.MarkChanged();
        yield return StartCoroutine(ProcessTileBuffs());
        GameStateVersion.MarkChanged();

        // 3) 타워 행동 및 투사체 명중 처리
        yield return StartCoroutine(ProcessTowerAttacks());
        GameStateVersion.MarkChanged();
        // 4) 타워 공격 이후 지속 피해형 디버프 처리
        yield return StartCoroutine(ProcessDamageDebuffs());
        GameStateVersion.MarkChanged();

        // 5) 보스·중간 보스의 제한 턴을 1 줄입니다. 그 턴의 공격과 지속 피해가 모두 끝난 뒤 셈하고, 다 된 보스가 있으면 게임 오버입니다.
        if (EnemyManager.Instance != null && EnemyManager.Instance.AdvanceAllBossTurnLimits())
        {
            OnGameOver();
            yield break;
        }

        GameStateVersion.MarkChanged();

        // 적 턴 하나 종료. 한 웨이브의 적 턴이 모두 끝나면 다음 웨이브의 소환 전 판정으로 넘어갑니다.
        yield return new WaitForSeconds(0.3f);
    }

    // 몬스터 이동 처리 코루틴 (EnemyManager 연동 지점)
    private IEnumerator ProcessEnemyMovement()
    {
        if (EnemyManager.Instance != null)
        {
            // 에너미 매니저를 통해 모든 적의 이동 실행
            yield return StartCoroutine(EnemyManager.Instance.ProcessEnemyTurnRoutine());
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 특수 타일 효과 적용 코루틴
    private IEnumerator ProcessTileBuffs()
    {
        EnemyManager.Instance?.ApplyAllTileBuffs();
        yield return null;
    }

    private IEnumerator ProcessTowerSupport()
    {
        if (TowerManager.Instance == null) yield break;

        TowerManager.Instance.BeginEnemyTurnForTowers();
        yield return StartCoroutine(TowerManager.Instance.ExecuteSupportTowerActionTurnRoutine());
    }

    private IEnumerator ProcessDamageDebuffs()
    {
        EnemyManager.Instance?.AdvanceAllDebuffTurns();
        yield return null;
    }

    // 타워 공격 및 데미지 연산 코루틴
    private IEnumerator ProcessTowerAttacks()
    {
        if (TowerManager.Instance != null)
        {
            yield return StartCoroutine(TowerManager.Instance.ExecuteAttackTowerActionTurnRoutine());
        }

        // 타임아웃은 전체 연출 시간이 아니라 "투사체 개수 변화가 없는 시간"을 감시합니다.
        // 위성을 여러 단계로 생성하더라도 정상적으로 변화가 이어지는 동안에는 적이 먼저 움직이지 않습니다.
        float inactiveProgressTime = 0f;
        int previousProjectileCount = -1;
        while (GlobalProjectileManager.Instance != null)
        {
            int activeProjectileCount = GlobalProjectileManager.Instance.GetActiveProjectileCount();
            if (activeProjectileCount <= 0) break;

            if (activeProjectileCount != previousProjectileCount)
            {
                previousProjectileCount = activeProjectileCount;
                inactiveProgressTime = 0f;
            }
            else
            {
                inactiveProgressTime += Time.deltaTime;
                if (inactiveProgressTime >= m_projectileWaitTimeout) break;
            }

            yield return null;
        }

        if (m_towerAttackResolutionDelay > 0f)
        {
            yield return new WaitForSeconds(m_towerAttackResolutionDelay);
        }
    }

    public void OnClickGameSpeed()
    {
        float nextSpeed = m_currentgameSpeed switch
        {
            1.0f => 1.5f,
            1.5f => 2.0f,
            2.0f => 3.0f,
            3.0f => 1.0f,
            _ => 1.0f
        };
        SetGameSpeed(nextSpeed);
    }

    public void SetGameSpeed(float speed)
    {
        m_currentgameSpeed = speed;
        Time.timeScale = m_currentgameSpeed;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        CurrentGameSpeedChanged?.Invoke(m_currentgameSpeed);
    }

    public void OnPauseGame()
    {
        Time.timeScale = 0.0f;
    }

    public void OnResumeGame()
    {
        Time.timeScale = GetGameSpeed();
    }

    public float GetGameSpeed()
    {
        return m_currentgameSpeed;
    }

    public bool CheckEnemyLimit()
    {
        if (TileManager.Instance == null)
        {
            return false;
        }

        int currentEnemyCount = TileManager.Instance.GetAllEnemiesOnPath().Count;

        if (currentEnemyCount > EnemyLimit)
        {
            OnGameOver();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void RefreshEnemyLimit()
    {
        int limit = EnemyLimit;

        if (m_lastEnemyLimit == limit)
        {
            return;
        }

        m_lastEnemyLimit = limit;

        EnemyLimitChanged?.Invoke(m_lastEnemyLimit);
    }

    private void HandleWaveChanged(int wave)
    {
        RefreshEnemyLimit();
    }

    public void OnGameOver()
    {
        if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) return;

        ChangeState(new GameOverTurnState());
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        OnPauseGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
    }

    public void OnGameClear()
    {
        if (CurrentState == TurnState.GameClear || CurrentState == TurnState.GameOver) return;

        ChangeState(new GameClearTurnState());
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        OnPauseGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameClear();
        }
    }

    public void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ChangeState(IGameTurnState nextState)
    {
        if (nextState == null) return;

        m_currentTurnState?.Exit(this);
        m_currentTurnState = nextState;
        CurrentState = nextState.Type;
        m_currentTurnState.Enter(this);
        GameStateVersion.MarkChanged();
    }
}
