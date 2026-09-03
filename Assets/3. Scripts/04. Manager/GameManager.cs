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

    [Header("턴 관리 설정")]
    [Tooltip("플레이어 턴 제한 시간(초)")]
    [SerializeField] private float playerTurnDuration = 45f;

    public TurnState CurrentState { get; private set; } = TurnState.None;
    public int CurrentWave { get; private set; } = 1;

    // 턴 진행 제어용 코루틴
    private Coroutine turnRoutine;
    private Coroutine playerTimerRoutine;
    private bool isPlayerTurnSkipped = false;

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
        CurrentState = TurnState.None;

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
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.NextWave();
            CurrentWave = WaveManager.Instance.GetWave();
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
        CurrentState = TurnState.PlayerTurn;
        OnTurnStateChanged?.Invoke(CurrentState);
        isPlayerTurnSkipped = false;

        float remainingTime = playerTurnDuration;

        while (remainingTime > 0f)
        {
            // 스킵 버튼을 누르면 즉시 루프 탈출
            if (isPlayerTurnSkipped) break;

            remainingTime -= Time.deltaTime;
            OnPlayerTurnTimerUpdated?.Invoke(Mathf.Max(0f, remainingTime));
            yield return null;
        }

        OnPlayerTurnTimerUpdated?.Invoke(0f);
    }

    // 플레이어 턴 스킵 버튼 UI 이벤트 연결용 함수
    public void OnClickSkipPlayerTurn()
    {
        if (CurrentState == TurnState.PlayerTurn)
        {
            isPlayerTurnSkipped = true;
        }
    }

    // ==========================================
    // 2. 에너미 턴 로직
    // ==========================================

    private IEnumerator EnemyTurnRoutine()
    {
        CurrentState = TurnState.EnemyTurn;
        OnTurnStateChanged?.Invoke(CurrentState);

        // 단계 1 & 2: 신규 몬스터 소환 및 전체 몬스터 이동
        // (EnemyManager/WaveManager에서 소환 후 0번은 1칸, 기존 몬스터는 주사위만큼 이동 수행)
        yield return StartCoroutine(ProcessEnemyMovement());

        // 단계 3 & 4: 몬스터 아래 타일 검사 및 특수 타일 버프 적용
        yield return StartCoroutine(ProcessTileBuffs());

        // 단계 5 & 6: 플레이어 타워 공격 및 몬스터 체력 감소 처리
        yield return StartCoroutine(ProcessTowerAttacks());

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
        yield return null;
    }

    // 타워 공격 및 데미지 연산 코루틴
    private IEnumerator ProcessTowerAttacks()
    {
        TowerManager.Instance?.ExecuteTowerActionTurn();
        yield return null;
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
        m_totalLife = Mathf.Max(0, m_totalLife - amount);
        if (m_totalLife <= 0)
        {
            OnGameOver();
        }
    }

    public void OnGameOver()
    {
        CurrentState = TurnState.GameOver;
        if (turnRoutine != null) StopCoroutine(turnRoutine);
        OnPauseGame();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
    }

    public void OnGameClear()
    {
        CurrentState = TurnState.GameClear;
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
}