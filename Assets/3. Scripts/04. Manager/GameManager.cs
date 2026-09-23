using System;
using System.Collections;
using System.Collections.Generic;
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
/// 플레이어 턴과 10회의 적 턴을 순환시키고 게임 속도, 라이프, 보호막과 종료 상태를 관리합니다.
/// 웨이브 구성과 적 능력치 계산은 WaveManager에 위임합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton and Configuration

    public static GameManager Instance { get; private set; }

    private const int TurnsPerWave = 10;
    private const int EnemySpawnTurns = 5;

    [Header("게임 배속 및 라이프 설정")]
    [SerializeField] private float m_gameSpeed = 1.0f;
    [SerializeField] private int m_totalLife = 20;


    [Header("에너미 턴 연출 속도")]
    [SerializeField, Min(0f)] private float m_towerAttackResolutionDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float m_projectileWaitTimeout = 10f;

    #endregion

    #region Public State and Events

    public TurnState CurrentState { get; private set; } = TurnState.None;
    public bool CanPerformPlayerAction => m_currentTurnState != null && m_currentTurnState.CanPerformPlayerAction;
    public int CurrentShield => m_currentShield;

    public event Action<TurnState> OnTurnStateChanged;
    public event Action<int> OnShieldChanged;

    #endregion

    #region Runtime State

    private IGameTurnState m_currentTurnState;

    private Coroutine turnRoutine;
    private bool isPlayerTurnEnd = false;

    private sealed class ShieldBuff
    {
        public int MaxShield;
        public int RemainingShield;
    }

    private readonly Dictionary<int, ShieldBuff> m_shieldsBySource = new Dictionary<int, ShieldBuff>();
    private int m_currentShield;
    private int m_currentShieldSourceID;

    #endregion

    #region Unity Lifecycle

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
        OnStartGame();
    }

    #endregion

    #region Main Turn Loop

    public void OnStartGame()
    {
        Time.timeScale = m_gameSpeed;
        ChangeState(new IdleTurnState());

        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnLoopRoutine());
    }

    /// <summary>플레이어 턴과 적 웨이브를 게임 종료 전까지 반복합니다.</summary>
    private IEnumerator TurnLoopRoutine()
    {
        while (CurrentState != TurnState.GameOver && CurrentState != TurnState.GameClear)
        {
            yield return StartCoroutine(PlayerTurnRoutine());
            if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;

            yield return StartCoroutine(EnemyWaveRoutine());
        }
    }

    private IEnumerator EnemyWaveRoutine()
    {
        // 모든 웨이브는 10턴이며, 처음 5턴에만 새로운 적을 소환합니다.
        for (int turn = 0; turn < TurnsPerWave; turn++)
        {
            if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;

            bool shouldSpawn = turn < EnemySpawnTurns;

            yield return StartCoroutine(EnemyTurnRoutine(shouldSpawn));            
        }

        if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;

        if (WaveManager.Instance != null)
        {
            int completedWave = WaveManager.Instance.CurrentWave;

            if (completedWave < 40)
            {
                WaveManager.Instance.NextWave();
            }

            TowerManager.Instance?.ProcessEarthTowerWave(completedWave);
        }
    }
    #endregion

    #region Player Turn

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

    #endregion

    #region Enemy Turn

    private IEnumerator EnemyTurnRoutine(bool shouldSpawn)
    {
        ChangeState(new EnemyTurnState());

        // 1) 필요 시 적 소환 후 모든 적 이동
        yield return StartCoroutine(ProcessEnemyMovement(shouldSpawn));

        // 2) 이동을 마친 타일의 즉시 효과 적용
        yield return StartCoroutine(ProcessTowerSupport());
        yield return StartCoroutine(ProcessTileBuffs());

        // 3) 타워 행동 및 투사체 명중 처리
        yield return StartCoroutine(ProcessTowerAttacks());
        // 4) 타워 공격 이후 지속 피해형 디버프 처리
        yield return StartCoroutine(ProcessDamageDebuffs());

        // 몬스터 턴 종료 (다음 루프에서 자동으로 플레이어 턴 시작)
        yield return new WaitForSeconds(0.3f);
    }

    // 몬스터 이동 처리 코루틴 (EnemyManager 연동 지점)
    private IEnumerator ProcessEnemyMovement(bool shouldSpawn)
    {
        if (EnemyManager.Instance != null)
        {
            // 에너미 매니저를 통해 신규 몬스터 스폰 및 이동 실행
            yield return StartCoroutine(EnemyManager.Instance.ProcessEnemyTurnRoutine(shouldSpawn));
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

    #endregion

    #region Game Speed and Pause

    public void OnClickGameSpeed(float gamespeed)
    {
        float nextSpeed = m_gameSpeed switch
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
        m_gameSpeed = speed;
        Time.timeScale = m_gameSpeed;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
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
        return m_gameSpeed;
    }

    #endregion

    #region Life and Shield

    public void DecreaseLife(int amount)
    {
        if (m_currentShield > 0)
        {
            int absorbedAmount = Mathf.Min(m_currentShield, amount);
            m_currentShield -= absorbedAmount;
            amount -= absorbedAmount;

            if (m_shieldsBySource.TryGetValue(m_currentShieldSourceID, out ShieldBuff shieldEffect))
            {
                shieldEffect.RemainingShield = m_currentShield;
            }

            // 현재 가장 높은 쉴드가 줄어들었을 수 있으므로 적용 대상을 다시 선택합니다.
            RefreshShield();

            if (amount <= 0) return;
        }

        m_totalLife = Mathf.Max(0, m_totalLife - amount);
        if (m_totalLife <= 0)
        {
            OnGameOver();
        }
    }

    /// <summary>
    /// 쉴드 타워가 행동할 때마다 쉴드 1을 충전합니다.
    /// 각 타워는 자신의 티어만큼만 쉴드를 보유할 수 있습니다.
    /// </summary>
    public void AddShield(int sourceID, int tier)
    {
        int maxShield = Mathf.Clamp(tier, 1, 5);

        if (!m_shieldsBySource.TryGetValue(sourceID, out ShieldBuff shield))
        {
            shield = new ShieldBuff
            {
                MaxShield = maxShield,
                RemainingShield = 0
            };
            m_shieldsBySource.Add(sourceID, shield);
        }

        // 티어 강화 등으로 같은 출처의 최대치가 달라질 가능성도 반영합니다.
        shield.MaxShield = maxShield;
        shield.RemainingShield = Mathf.Min(shield.RemainingShield + 1, shield.MaxShield);

        RefreshShield();
    }


    private void RefreshShield()
    {
        int highestShield = 0;
        int highestShieldSourceID = 0;
        foreach (KeyValuePair<int, ShieldBuff> pair in m_shieldsBySource)
        {
            if (pair.Value.RemainingShield > highestShield)
            {
                highestShield = pair.Value.RemainingShield;
                highestShieldSourceID = pair.Key;
            }
        }
        m_currentShield = highestShield;
        m_currentShieldSourceID = highestShieldSourceID;
        OnShieldChanged?.Invoke(m_currentShield);
    }

    #endregion

    #region Game End and State Transition

    public void OnGameOver()
    {
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
        if (CurrentState == TurnState.GameClear) return;

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
        OnTurnStateChanged?.Invoke(CurrentState);
    }

    #endregion
}
