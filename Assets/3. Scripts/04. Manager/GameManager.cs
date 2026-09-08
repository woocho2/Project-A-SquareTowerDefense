using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 턴 상태 정의
public enum TurnState
{
    None,
    PlayerTurn,
    EnemyTurn,
    GameOver,
    GameClear
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 배속 및 라이프 설정")]
    [SerializeField] private float m_gameSpeed = 1.0f;
    [SerializeField] private int m_totalLife = 20;


    [Header("에너미 턴 연출 속도")]
    [SerializeField, Min(0f)] private float m_towerAttackResolutionDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float m_projectileWaitTimeout = 5f;
    [Tooltip("같은 웨이브에서 다음 적이 소환되기 전 대기 시간(초)")]
    [SerializeField, Min(0f)] private float m_enemySpawnInterval = 0.5f;

    public TurnState CurrentState { get; private set; } = TurnState.None;
    public bool CanPerformPlayerAction => m_currentTurnState != null && m_currentTurnState.CanPerformPlayerAction;
    public int CurrentWave { get; private set; } = 1;

    private IGameTurnState m_currentTurnState;

    // 턴 진행 제어용 코루틴
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

    // UI 및 외부 시스템 알림용 델리게이트
    public event Action<TurnState> OnTurnStateChanged;
    public event Action<float> OnPlayerTurnTimerUpdated; // 남은 시간 UI 갱신용

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

    // ==========================================
    // 게임 시작 및 턴 루프 제어
    // ==========================================

    public void OnStartGame()
    {
        Time.timeScale = m_gameSpeed;
        ChangeState(new IdleTurnState());

        if (turnRoutine != null) StopCoroutine(turnRoutine);
        turnRoutine = StartCoroutine(TurnLoopRoutine());
    }

    // [플레이어턴 -> 에너미턴 -> 반복] 메인 루프
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
        int enemyTurns = WaveManager.Instance != null ? WaveManager.Instance.GetEnemiesPerWave() : 1;

        for (int i = 0; i < enemyTurns; i++)
        {
            if (CurrentState == TurnState.GameOver || CurrentState == TurnState.GameClear) yield break;
            yield return StartCoroutine(EnemyTurnRoutine());

            // 마지막 적 뒤에는 다음 웨이브로 바로 넘어가고, 적과 적 사이에만 대기합니다.
            if (i < enemyTurns - 1 && m_enemySpawnInterval > 0f)
            {
                yield return new WaitForSeconds(m_enemySpawnInterval);
            }
        }

        if (WaveManager.Instance != null)
        {
            int completedWave = WaveManager.Instance.GetWave();
            WaveManager.Instance.NextWave();
            CurrentWave = WaveManager.Instance.GetWave();
            TowerManager.Instance?.ProcessEarthTowerWave(completedWave);
        }
        else
        {
            CurrentWave++;
        }
    }
    // ==========================================
    // 1. 플레이어 턴 로직
    // ==========================================

    private IEnumerator PlayerTurnRoutine()
    {
        ChangeState(new PlayerTurnState());
        isPlayerTurnEnd = false;

        // 스킵 버튼을 누를 때까지 플레이어 행동을 기다립니다.
        while (!isPlayerTurnEnd && CurrentState != TurnState.GameOver && CurrentState != TurnState.GameClear)
        {
            yield return null;
        }

        OnPlayerTurnTimerUpdated?.Invoke(0f);
    }

    // 플레이어 턴 스킵 버튼 UI 이벤트 연결용 함수
    public void OnClickSkipPlayerTurn()
    {
        if (CurrentState == TurnState.PlayerTurn)
        {
            isPlayerTurnEnd = true;
        }
    }

    // ==========================================
    // 2. 에너미 턴 로직
    // ==========================================

    private IEnumerator EnemyTurnRoutine()
    {
        ChangeState(new EnemyTurnState());

        // 단계 1: 타워의 투사체가 실제로 명중한 뒤에만 적 이동을 시작합니다.
        yield return StartCoroutine(ProcessTowerAttacks());

        // 단계 2: 신규 몬스터 소환 및 전체 몬스터 이동
        yield return StartCoroutine(ProcessEnemyMovement());

        // 단계 3: 몬스터 아래 타일 검사 및 턴 기반 디버프 처리
        yield return StartCoroutine(ProcessTileBuffs());

        // 몬스터 턴 종료 (다음 루프에서 자동으로 플레이어 턴 시작)
        yield return new WaitForSeconds(0.3f);
    }

    // 몬스터 이동 처리 코루틴 (EnemyManager 연동 지점)
    private IEnumerator ProcessEnemyMovement()
    {
        if (EnemyManager.Instance != null)
        {
            // 에너미 매니저를 통해 신규 몬스터 스폰 및 이동 실행
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
        EnemyManager.Instance?.AdvanceAllDebuffTurns();
        yield return null;
    }

    // 타워 공격 및 데미지 연산 코루틴
    private IEnumerator ProcessTowerAttacks()
    {
        TowerManager.Instance?.ExecuteTowerActionTurn();

        float elapsed = 0f;
        while (GlobalProjectileManager.Instance != null && GlobalProjectileManager.Instance.HasActiveProjectiles() && elapsed < m_projectileWaitTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (m_towerAttackResolutionDelay > 0f)
        {
            yield return new WaitForSeconds(m_towerAttackResolutionDelay);
        }
    }

    // ==========================================
    // 게임 속도 및 일시정지 제어
    // ==========================================

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

    // ==========================================
    // 라이프 및 게임 종료 처리
    // ==========================================

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
    }

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
}
