using System.Collections;
using UnityEngine;

public class EnemyMovementController : MonoBehaviour
{
    [Header("이동 속도 설정")]
    [Tooltip("한 칸을 이동하는 속도 (인스펙터 조절)")]
    [SerializeField] private float m_moveSpeed = 8f; 

    [Header("런타임 턴 스탯 (디버그 확인용)")]
    [SerializeField] private int m_currentTileIndex = 0; 
    [SerializeField] private int m_actionInterval = 1;      // 주사위 굴리는 턴 주기 (Action)
    [SerializeField] private int m_currentActionCounter = 0; // 턴 누적 카운터
    [SerializeField] private int m_baseDiceMaxSpeed = 1;     // EnemyData에서 받아온 주사위 최댓값 (Speed)
    [SerializeField] private int m_bonusSpeed = 0;          // 스피드 타일 등으로 더해지는 추가 칸 수

    private float m_speedMultiplier = 1f; 
    private float m_synergySlow = 1f; 

    private EnemyHealthController m_health; 
    private TilePath m_tilePath; 
    private bool m_isMoving = false; 
    private Coroutine m_moveRoutine; 

    public int CurrentTileIndex => m_currentTileIndex; 
    public bool IsMoving => m_isMoving; 
    public float CurrentSpeed => FinalMoveSpeed; 
    public float FinalMoveSpeed => m_moveSpeed * Mathf.Clamp(m_speedMultiplier * m_synergySlow, 0.1f, 5f); 

    private void Awake() 
    {
        m_health = GetComponent<EnemyHealthController>(); 
    }

    // ScriptableObject(EnemyData)의 Action과 Speed를 기반으로 초기화
    public void InitMovement(EnemyData data, TilePath tilePath) 
    {
        m_tilePath = tilePath; 
        m_currentTileIndex = 0; 
        m_currentActionCounter = 0; 
        m_bonusSpeed = 0; 
        m_speedMultiplier = 1f; 
        m_synergySlow = 1f; 
        m_isMoving = false; 

        if (data != null) 
        {
            // 행동력(Action) 초기화 (최소 1 보장)
            m_actionInterval = Mathf.Max(1, data.Action); 

            // 데이터의 Speed 값을 주사위 최댓값으로 설정 (최소 1 보장)
            m_baseDiceMaxSpeed = Mathf.Max(1, data.Speed);
        }
        else
        {
            m_actionInterval = 1; 
            m_baseDiceMaxSpeed = 1;
        }

        if (m_tilePath != null) 
        {
            transform.position = m_tilePath.GetWorldPosition(0); 
        }
    }

    public IEnumerator ProcessTurnMoveRoutine() 
    {
        if (m_tilePath == null) yield break; 

        if (m_currentTileIndex >= m_tilePath.LastIndex) 
        {
            OnReachEnd(); 
            yield break; 
        }

        // 0번 타일의 신규 소환 몬스터는 무조건 1칸 전진
        if (m_currentTileIndex == 0) 
        {
            yield return StartCoroutine(MoveStepsRoutine(1)); 
            yield break; 
        }

        // 행동 주기 도달 검사
        m_currentActionCounter++; 
        if (m_currentActionCounter >= m_actionInterval) 
        {
            m_currentActionCounter = 0; 

            // 1부터 EnemyData에 설정된 Speed 값까지 랜덤 굴림 + 특수 타일 버프 보너스
            int maxDice = m_baseDiceMaxSpeed + m_bonusSpeed;
            int totalSteps = Random.Range(1, maxDice + 1);

            yield return StartCoroutine(MoveStepsRoutine(totalSteps));
        }
    }

    private IEnumerator MoveStepsRoutine(int steps) 
    {
        m_isMoving = true; 

        for (int i = 0; i < steps; i++) 
        {
            if (m_currentTileIndex >= m_tilePath.LastIndex) 
            {
                break; 
            }

            m_currentTileIndex++; 
            Vector3 targetPos = m_tilePath.GetWorldPosition(m_currentTileIndex); 

            while (Vector3.Distance(transform.position, targetPos) > 0.02f) 
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, FinalMoveSpeed * Time.deltaTime); 
                yield return null; 
            }

            transform.position = targetPos; 
        }

        m_isMoving = false; 

        if (m_currentTileIndex >= m_tilePath.LastIndex) 
        {
            OnReachEnd(); 
        }
    }

    public void StopMovement() 
    {
        m_isMoving = false; 
        StopAllCoroutines(); 
    }

    public void CheckAndApplyTileBuff() 
    {
        if (TileManager.Instance == null || m_tilePath == null) return; 
        if (m_currentTileIndex <= 0 || m_currentTileIndex >= m_tilePath.LastIndex) return; 

        Vector3Int gridPos = m_tilePath.GetGridPosition(m_currentTileIndex); 
        SpecialTileType tileType = TileManager.Instance.GetTileTypeAt(gridPos); 

        switch (tileType) 
        {
            case SpecialTileType.SpeedTile: 
                // 스피드 타일 버프: 행동력 1 감소 (최소 1), 이동 가능한 스피드(칸 수) +2 증가
                m_actionInterval = Mathf.Max(1, m_actionInterval - 1); 
                m_bonusSpeed += 2; 
                break; 

            case SpecialTileType.DefendTile: 
                break; 

            case SpecialTileType.HealTile: 
                break; 

            case SpecialTileType.Normal: 
            default: 
                break; 
        }
    }

    public void OnReachEnd() 
    {
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.DecreaseLife(1); 
        }

        UIManager.Instance?.OnPlayerHit(m_health != null && m_health.IsBoss); 
        m_health?.ReturnToPool(); 
    }

    public void SetSpeedMultiplier(float multiplier) => m_speedMultiplier = multiplier; 
    public void SetSynergySlow(float slow) => m_synergySlow = slow; 
}