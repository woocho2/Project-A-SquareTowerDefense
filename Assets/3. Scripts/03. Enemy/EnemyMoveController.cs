using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMoveController : MonoBehaviour
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
    [SerializeField] private int m_tileBuffSpeed = 0;          // 스피드 타일 등으로 더해지는 추가 칸 수
    private int m_permanentActionPenalty;
    private int m_temporaryActionPenalty;
    private int m_temporaryActionPenaltyTurns;
    private int m_diceMaxModifier;
    private int m_temporaryDiceMaxModifier;
    private int m_temporaryDiceMaxModifierTurns;
    private int m_forcedNoMoveTurns;
    private bool m_reverseNextDice;

    private EnemyHealthController m_health;
    private SpriteRenderer m_spriteRenderer;
    private Quaternion m_baseRotation;
    private Transform[] m_uiTransforms;
    private Vector3[] m_uiLocalPositions;
    private Quaternion[] m_uiLocalRotations;
    private bool m_isMoving = false;

    // 같은 타일의 적과 겹치지 않도록 벌려 설 화면상 목표 위치입니다. 논리적 타일 위치(m_currentTileIndex)와는 무관합니다.
    private Vector3 m_formationTarget;
    private bool m_hasFormationTarget;

    public int CurrentTileIndex => m_currentTileIndex;
    public bool IsMoving => m_isMoving;
    public int ActionInterval => m_actionInterval;
    public int BaseActionInterval => m_baseActionInterval;
    public int CurrentActionCounter => m_currentActionCounter;
    public int BaseDiceMaxSpeed => m_baseDiceMaxSpeed;
    public int TileBuffSpeed => m_tileBuffSpeed;
    public int CurrentDiceMaxSpeed => Mathf.Max(0, m_baseDiceMaxSpeed + m_tileBuffSpeed + m_diceMaxModifier + m_temporaryDiceMaxModifier);

    // Legacy DebuffBase assets are still present in the project but are no longer
    // called by EnemyDebuffController.  Keep this compatibility surface inert until
    // those obsolete classes are removed in the wider debuff refactor.
    public float CurrentSpeed => m_moveSpeed;
    public void SetSpeedMultiplier(float _) { }

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

    /// <summary>
    /// 이동 코루틴이 끝난 뒤, 진형 자리까지 부드럽게 보간합니다.
    /// 이동 중에는 코루틴이 Transform을 제어하므로 건드리지 않습니다.
    /// </summary>
    private void Update()
    {
        if (!m_hasFormationTarget || m_isMoving || TileManager.Instance == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            m_formationTarget,
            TileManager.Instance.EnemyFormationMoveSpeed * Time.deltaTime);

        if (transform.position == m_formationTarget) m_hasFormationTarget = false;
    }

    // ScriptableObject(EnemyData)의 Action과 Speed를 기반으로 초기화
    public void InitMovement(EnemyData data)
    {
        ResetFacingDirection();
        m_hasFormationTarget = false;
        m_currentTileIndex = 0;
        m_currentActionCounter = 0;
        m_tileBuffSpeed = 0;
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

        if (TileManager.Instance != null)
        {
            transform.position = TileManager.Instance.GetPathWorldPosition(0);
            RegisterAtTile(m_currentTileIndex);
        }
    }

    /// <summary>
    /// 이동이 완료된 순간 호출하여 전투 점유 타일을 확정하고, 떠난 타일과 도착한 타일의 진형을 다시 배치합니다.
    /// </summary>
    private void RegisterAtTile(int index)
    {
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null || !tileManager.SetEnemyAtPathIndex(m_health, index, out int previousIndex)) return;

        if (previousIndex >= 0 && previousIndex != index) ArrangeFormation(previousIndex);
        ArrangeFormation(index);
    }

    /// <summary>
    /// 한 타일의 점유 목록을 기준으로 모든 적의 진형 자리를 다시 계산합니다.
    /// Transform을 즉시 옮기지 않고 목표만 갱신하며, 각 적이 Update에서 스스로 이동합니다.
    /// </summary>
    private static void ArrangeFormation(int pathIndex)
    {
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null) return;

        List<EnemyHealthController> enemies = tileManager.GetEnemyOccupantsAtPathIndex(pathIndex);
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyMoveController movement = enemies[i].Movement;
            if (movement == null) continue;

            movement.m_formationTarget = tileManager.GetEnemyFormationPosition(pathIndex, i, enemies.Count);
            movement.m_hasFormationTarget = true;
        }
    }

    public IEnumerator ProcessTurnMoveRoutine()
    {
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null) yield break;

        if (m_forcedNoMoveTurns > 0)
        {
            m_forcedNoMoveTurns--;
            yield break;
        }

        if (m_currentTileIndex >= tileManager.LastPathIndex)
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
            int maxDice = Mathf.Max(0, m_baseDiceMaxSpeed + m_tileBuffSpeed + m_diceMaxModifier + m_temporaryDiceMaxModifier);
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
            int nextIndex = m_currentTileIndex - 1;
            Vector3 targetPos = TileManager.Instance.ReserveEnemyArrivalPosition(m_health, nextIndex);
            FaceMoveDirection(targetPos);
            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, m_moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            // 도착한 순간에만 인덱스를 바꿔 TileManager의 점유 인덱스와 항상 일치시킵니다.
            m_currentTileIndex = nextIndex;
            RegisterAtTile(m_currentTileIndex);
        }
        m_isMoving = false;
    }

    private IEnumerator MoveStepsRoutine(int steps)
    {
        m_isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            if (m_currentTileIndex >= TileManager.Instance.LastPathIndex)
            {
                break;
            }

            int nextIndex = m_currentTileIndex + 1;
            Vector3 targetPos = TileManager.Instance.ReserveEnemyArrivalPosition(m_health, nextIndex);
            FaceMoveDirection(targetPos);

            while (Vector3.Distance(transform.position, targetPos) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, m_moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            // 도착한 순간에만 인덱스를 바꿔 TileManager의 점유 인덱스와 항상 일치시킵니다.
            m_currentTileIndex = nextIndex;
            RegisterAtTile(m_currentTileIndex);
        }

        m_isMoving = false;

        // 도착점 도달 확인
        if (m_currentTileIndex >= TileManager.Instance.LastPathIndex)
        {
            OnReachEnd();
            yield break;
        }

        // 타일 효과는 모든 적의 이동이 끝난 뒤 EnemyManager가 한 번만 적용한다.
    }

    public void StopMovement()
    {
        m_isMoving = false;
        StopAllCoroutines();
        m_hasFormationTarget = false;

        // 점유와 도착 예약을 함께 제거하므로, 풀에 들어간 적이 공격 대상에 남지 않습니다.
        if (TileManager.Instance != null && TileManager.Instance.RemoveEnemyFromPath(m_health, out int previousIndex))
        {
            ArrangeFormation(previousIndex);
        }
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

        if (TileManager.Instance == null) return;
        if (m_currentTileIndex <= 0 || m_currentTileIndex >= TileManager.Instance.LastPathIndex) return;

        // 2. 현재 멈춰있는 타일 타입 확인
        PathTileBuffType tileType = TileManager.Instance.GetTileTypeAt(m_currentTileIndex);

        // 3. 타일 타입별 버프 분기 처리
        switch (tileType)
        {
            case PathTileBuffType.SpeedTile:
                // 스피드 타일: 행동 주기 1 감소 (최소 1), 이동 주사위 최댓값 +2 증가
                m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty + m_temporaryActionPenalty - 1);
                m_tileBuffSpeed = 2;
                break;

            case PathTileBuffType.DefendTile:
                // 방어 타일: 방어력 20% 증가 (해머 디버프의 감소와는 합연산)
                if (m_health != null)
                {
                    m_health.SetTileDefendBonus(0.2f);
                }
                break;

            case PathTileBuffType.HealTile:
                // 힐 타일: 이 타일에 멈춰 있는 동안 매 턴 최대 체력의 5% 회복
                if (m_health != null)
                {
                    m_health.HealMaxHealthPercent(0.05f);
                }
                break;

            case PathTileBuffType.Normal:
            default:
                // 일반 타일은 기본 스탯 유지
                break;
        }

        // 패스 타일의 디버프 데이터도 같은 좌표를 기준으로 읽습니다.
        // 이 시점에는 적이 이동을 마치고 TileManager의 해당 인덱스 리스트에 등록된 상태입니다.
        if (TryGetComponent(out EnemyDebuffController debuffController))
        {
            debuffController.RefreshTileDebuffs(m_currentTileIndex);
        }
    }

    // 모든 타일 버프를 기본값으로 초기화
    private void ResetAllBuffs()
    {
        // 이동/행동력 버프 원복
        m_actionInterval = Mathf.Max(1, m_baseActionInterval + m_permanentActionPenalty + m_temporaryActionPenalty);
        m_tileBuffSpeed = 0;

        // 방어력 버프 원복
        if (m_health != null)
        {
            m_health.ResetTileDefendBuff();
        }
    }

    private const int NormalLeakDamage = 1;
    private const int BossLeakDamage = 3;
    private const int FinalWave = 40;

    // 도착점에 닿은 적은 즉시 사라지고 플레이어에게 피해를 줍니다. 40웨이브 보스를 놓치면 바로 게임 오버입니다.
    public void OnReachEnd()
    {
        bool isBoss = m_health != null && m_health.EnemyType == EnemyType.Boss;

        if (GameManager.Instance != null)
        {
            bool isFinalBoss = isBoss && m_health.SpawnWave >= FinalWave;
            if (isFinalBoss) GameManager.Instance.OnGameOver();
            else GameManager.Instance.DecreaseLife(isBoss ? BossLeakDamage : NormalLeakDamage);
        }

        m_health?.ReturnToPool();
    }

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
}
