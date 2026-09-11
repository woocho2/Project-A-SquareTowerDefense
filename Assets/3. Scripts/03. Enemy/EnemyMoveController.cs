using System.Collections;
using UnityEngine;

public class EnemyMovementController : MonoBehaviour
{
    [Header("이동 속도 설정")]
    [Tooltip("한 칸을 이동하는 속도 (인스펙터 조절)")]
    [SerializeField] private float m_moveSpeed = 3;
    [SerializeField] private bool m_spriteFacesLeftByDefault = true;

    [Header("런타임 턴 스탯 (디버그 확인용)")]
    [SerializeField] private int m_currentTileIndex = 0;
    [SerializeField] private int m_actionInterval = 1;      // 주사위 굴리는 턴 주기 (Action)
    [SerializeField] private int m_baseActionInterval = 1;  // 원본 기본 행동력 캐싱용
    [SerializeField] private int m_currentActionCounter = 0; // 턴 누적 카운터
    [SerializeField] private int m_baseDiceMaxSpeed = 1;     // EnemyData에서 받아온 주사위 최댓값 (Speed)
    [SerializeField] private int m_bonusSpeed = 0;          // 스피드 타일 등으로 더해지는 추가 칸 수
    private int m_permanentActionPenalty;
    private int m_temporaryActionPenalty;
    private int m_temporaryActionPenaltyTurns;
    private int m_diceMaxModifier;
    private int m_temporaryDiceMaxModifier;
    private int m_temporaryDiceMaxModifierTurns;
    private int m_forcedNoMoveTurns;
    private bool m_reverseNextDice;

    private float m_speedMultiplier = 1f;
#if false // Synergy system temporarily disabled
    private float m_synergySlow = 1f;
#endif

    private EnemyHealthController m_health;
    private SpriteRenderer m_spriteRenderer;
    private Quaternion m_baseRotation;
    private Transform[] m_uiTransforms;
    private Vector3[] m_uiLocalPositions;
    private Quaternion[] m_uiLocalRotations;
    private TilePath m_tilePath;
    private bool m_isMoving = false;

    public int CurrentTileIndex => m_currentTileIndex;
    public bool IsMoving => m_isMoving;
    public float CurrentSpeed => FinalMoveSpeed;
    public float FinalMoveSpeed => m_moveSpeed * Mathf.Clamp(m_speedMultiplier, 0.1f, 5f);

    private void Awake()
    {
        m_health = GetComponent<EnemyHealthController>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_baseRotation = transform.rotation;

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        m_uiTransforms = new Transform[canvases.Length];
        m_uiLocalPositions = new Vector3[canvases.Length];
        m_uiLocalRotations = new Quaternion[canvases.Length];

        for (int i = 0; i < canvases.Length; i++)
        {
            m_uiTransforms[i] = canvases[i].transform;
            m_uiLocalPositions[i] = m_uiTransforms[i].localPosition;
            m_uiLocalRotations[i] = m_uiTransforms[i].localRotation;
        }
    }

    // ScriptableObject(EnemyData)의 Action과 Speed를 기반으로 초기화
    public void InitMovement(EnemyData data, TilePath tilePath)
    {
        ResetFacingDirection();
        m_tilePath = tilePath;
        m_currentTileIndex = 0;
        m_currentActionCounter = 0;
        m_bonusSpeed = 0;
        m_speedMultiplier = 1f;
        m_permanentActionPenalty = 0;
        m_temporaryActionPenalty = 0;
        m_temporaryActionPenaltyTurns = 0;
        m_diceMaxModifier = 0;
        m_temporaryDiceMaxModifier = 0;
        m_temporaryDiceMaxModifierTurns = 0;
        m_forcedNoMoveTurns = 0;
        m_reverseNextDice = false;

        m_isMoving = false;

        if (data != null)
        {
            m_baseActionInterval = Mathf.Max(1, data.Action);
            m_actionInterval = m_baseActionInterval;
            m_baseDiceMaxSpeed = Mathf.Max(1, data.Speed);
        }
        else
        {
            m_baseActionInterval = 1;
            m_actionInterval = 1;
            m_baseDiceMaxSpeed = 1;
        }

        if (m_tilePath != null)
        {
            transform.position = m_tilePath.GetWorldPosition(0);
            m_tilePath.RegisterEnemyAtIndex(m_health, m_currentTileIndex);
        }
    }

    public IEnumerator ProcessTurnMoveRoutine()
    {
        if (m_tilePath == null) yield break;

        if (m_forcedNoMoveTurns > 0)
        {
            m_forcedNoMoveTurns--;
            yield break;
        }

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

            // 1부터 EnemyData에 설정된 Speed 값까지 랜덤 굴림 + 멈춰있는 타일의 보너스 Speed
            int maxDice = Mathf.Max(0, m_baseDiceMaxSpeed + m_bonusSpeed + m_diceMaxModifier + m_temporaryDiceMaxModifier);
            if (maxDice <= 0) yield break;
            int totalSteps = Random.Range(1, maxDice + 1);
            if (m_reverseNextDice)
            {
                m_reverseNextDice = false;
                yield return StartCoroutine(MoveBackwardStepsRoutine(totalSteps));
            }
            else
            {
                yield return StartCoroutine(MoveStepsRoutine(totalSteps));
            }
        }
    }

    private IEnumerator MoveBackwardStepsRoutine(int steps)
    {
        m_isMoving = true;
        for (int i = 0; i < steps && m_currentTileIndex > 0; i++)
        {
            m_currentTileIndex--;
            Vector3 targetPos = m_tilePath.ReserveArrivalPosition(m_health, m_currentTileIndex);
            FaceMoveDirection(targetPos);
            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, FinalMoveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            m_tilePath.RegisterEnemyAtIndex(m_health, m_currentTileIndex);
        }
        m_isMoving = false;
        CheckAndApplyTileBuff();
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
            Vector3 targetPos = m_tilePath.ReserveArrivalPosition(m_health, m_currentTileIndex);
            FaceMoveDirection(targetPos);

            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, FinalMoveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            m_tilePath.RegisterEnemyAtIndex(m_health, m_currentTileIndex);
        }

        m_isMoving = false;

        // 도착점 도달 확인
        if (m_currentTileIndex >= m_tilePath.LastIndex)
        {
            OnReachEnd();
            yield break;
        }

        // 이동 정지 시 기존 버프 초기화 후 정지 타일의 버프 적용
        CheckAndApplyTileBuff();
    }

    public void StopMovement()
    {
        m_isMoving = false;
        StopAllCoroutines();
        m_tilePath?.UnregisterEnemy(m_health);
    }

    private void FaceMoveDirection(Vector3 targetPosition)
    {
        if (m_spriteRenderer == null) return;

        Vector2 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            bool isMovingLeft = direction.x < 0f;
            m_spriteRenderer.flipX = m_spriteFacesLeftByDefault ? !isMovingLeft : isMovingLeft;
            ApplyFacingRotation(0f);
            return;
        }

        bool isMovingUp = direction.y > 0f;
        m_spriteRenderer.flipX = false;
        float verticalRotation = (m_spriteFacesLeftByDefault ? -1f : 1f) * (isMovingUp ? 90f : -90f);
        ApplyFacingRotation(verticalRotation);
    }

    private void ResetFacingDirection()
    {
        if (m_spriteRenderer != null) m_spriteRenderer.flipX = false;
        ApplyFacingRotation(0f);
    }

    private void ApplyFacingRotation(float zAngle)
    {
        Quaternion rotationDelta = Quaternion.Euler(0f, 0f, zAngle);
        transform.rotation = m_baseRotation * rotationDelta;

        for (int i = 0; i < m_uiTransforms.Length; i++)
        {
            if (m_uiTransforms[i] == null) continue;

            m_uiTransforms[i].localPosition = Quaternion.Inverse(rotationDelta) * m_uiLocalPositions[i];
            m_uiTransforms[i].localRotation = Quaternion.Inverse(rotationDelta) * m_uiLocalRotations[i];
        }
    }

    // 멈춘 자리의 특수 타일 버프 적용 로직
    public void CheckAndApplyTileBuff()
    {
        // 1. 기존 버프 완전 초기화 (스피드 및 방어력 복구)
        ResetAllBuffs();

        if (TileManager.Instance == null || m_tilePath == null) return;
        if (m_currentTileIndex <= 0 || m_currentTileIndex >= m_tilePath.LastIndex) return;

        // 2. 현재 멈춰있는 타일 타입 확인
        Vector3Int gridPos = m_tilePath.GetGridPosition(m_currentTileIndex);
        SpecialTileType tileType = TileManager.Instance.GetTileTypeAt(gridPos);

        // 3. 타일 타입별 버프 분기 처리
        switch (tileType)
        {
            case SpecialTileType.SpeedTile:
                // 스피드 타일: 행동 주기 1 감소 (최소 1), 이동 주사위 최댓값 +2 증가
                m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty + m_temporaryActionPenalty - 1);
                m_bonusSpeed = 2;
                break;

            case SpecialTileType.DefendTile:
                // 방어 타일: 방어력 20% 증가 (계수 1.2배)
                if (m_health != null)
                {
                    m_health.SetDefendMultiplier(1.2f);
                }
                break;

            case SpecialTileType.HealTile:
                // 힐 타일: 현재 체력의 20% 즉시 회복
                if (m_health != null)
                {
                    m_health.HealMaxHealthPercent(0.2f);
                }
                break;

            case SpecialTileType.Normal:
            default:
                // 일반 타일은 기본 스탯 유지
                break;
        }
    }

    // 모든 타일 버프를 기본값으로 초기화
    private void ResetAllBuffs()
    {
        // 이동/행동력 버프 원복
        m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty + m_temporaryActionPenalty);
        m_bonusSpeed = 0;

        // 방어력 버프 원복
        if (m_health != null)
        {
            m_health.ResetTileDefendBuff();
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
    public void SetPermanentActionPenalty(int penalty)
    {
        m_permanentActionPenalty = Mathf.Max(0, penalty);
        m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty + m_temporaryActionPenalty);
    }

    public void ApplyTemporaryActionPenalty(int penalty, int turns)
    {
        m_temporaryActionPenalty = Mathf.Max(m_temporaryActionPenalty, Mathf.Max(0, penalty));
        m_temporaryActionPenaltyTurns = Mathf.Max(m_temporaryActionPenaltyTurns, Mathf.Max(1, turns));
        m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty + m_temporaryActionPenalty);
    }

    public void AdvanceDebuffTurn()
    {
        if (m_temporaryDiceMaxModifierTurns > 0)
        {
            m_temporaryDiceMaxModifierTurns--;
            if (m_temporaryDiceMaxModifierTurns == 0) m_temporaryDiceMaxModifier = 0;
        }
        if (m_temporaryActionPenaltyTurns <= 0) return;
        m_temporaryActionPenaltyTurns--;
        if (m_temporaryActionPenaltyTurns == 0)
        {
            m_temporaryActionPenalty = 0;
            m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty);
        }
    }

    public void ForceNoMoveForTurns(int turns) => m_forcedNoMoveTurns = Mathf.Max(m_forcedNoMoveTurns, Mathf.Max(1, turns));
    public void SetPermanentDiceMaxModifier(int modifier) => m_diceMaxModifier = Mathf.Min(0, modifier);
    public void ApplyTemporaryDiceMaxModifier(int modifier, int turns)
    {
        m_temporaryDiceMaxModifier = modifier;
        m_temporaryDiceMaxModifierTurns = Mathf.Max(1, turns);
    }
    public void ReverseNextDiceMove() => m_reverseNextDice = true;

    public int GetPathIndexClosestTo(Vector3 worldPosition)
    {
        if (m_tilePath == null) return m_currentTileIndex;
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        for (int i = 0; i <= m_tilePath.LastIndex; i++)
        {
            float distance = Vector2.SqrMagnitude(m_tilePath.GetWorldPosition(i) - worldPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
#if false // Synergy system temporarily disabled
    public void SetSynergySlow(float slow) => m_synergySlow = slow;
#endif
}
