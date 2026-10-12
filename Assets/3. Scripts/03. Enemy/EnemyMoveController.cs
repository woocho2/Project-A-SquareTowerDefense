using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class EnemyMoveController : MonoBehaviour
{
    [Header("이동 속도 설정")]
    [Tooltip("실제 화면 이동 속도(월드 단위/초). 낮을수록 이동과 굴림이 함께 느려집니다. 이동 주사위의 칸 수와는 별개입니다.")]
    [SerializeField, Min(0.01f)] private float m_moveSpeed = 1;
    [SerializeField] private bool m_spriteFacesLeftByDefault = true;

    [Header("큐브 굴림")]
    [SerializeField] private EnemyCubeVisual m_cubeVisual;
    private EnemyCubeOrientation m_cubeOrientation = EnemyCubeOrientation.Identity;
    public EnemyCubeFace VisibleCubeFace => m_cubeOrientation.VisibleFace;
    public int CubeFaceQuarterTurns => m_cubeOrientation.CounterClockwiseQuarterTurns;

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
    private bool m_hasMadeFirstMove; // 소환된 뒤 첫 이동(무조건 한 칸)을 했는지

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
    // 이동 주사위 최댓값은 어떤 경우에도 1 아래로 내려가지 않습니다.
    // 1 아래로 깎이려던 만큼은 버리지 않고 행동 주기를 그만큼 늘립니다.
    private int RawDiceMaxSpeed => m_baseDiceMaxSpeed + m_tileBuffSpeed + m_diceMaxModifier + m_temporaryDiceMaxModifier;
    private int IgnoredDiceReduction => Mathf.Max(0, 1 - RawDiceMaxSpeed);

    public int ActionInterval => m_actionInterval + IgnoredDiceReduction;
    public int BaseActionInterval => m_baseActionInterval;
    public int CurrentActionCounter => m_currentActionCounter;
    public int BaseDiceMaxSpeed => m_baseDiceMaxSpeed;
    public int TileBuffSpeed => m_tileBuffSpeed;
    public int CurrentDiceMaxSpeed => Mathf.Max(1, RawDiceMaxSpeed);

    // Legacy DebuffBase assets are still present in the project but are no longer
    // called by EnemyDebuffController.  Keep this compatibility surface inert until
    // those obsolete classes are removed in the wider debuff refactor.
    public float CurrentSpeed => m_moveSpeed;
    public void SetSpeedMultiplier(float _) { }

    private void Awake()
    {
        m_health = GetComponent<EnemyHealthController>();
        m_spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (m_cubeVisual == null) m_cubeVisual = GetComponentInChildren<EnemyCubeVisual>(true);
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
        StopAllCoroutines();
        ResetFacingDirection();
        m_cubeOrientation = EnemyCubeOrientation.Identity;
        if (m_cubeVisual != null) m_cubeVisual.ShowOrientation(m_cubeOrientation);
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
        m_hasMadeFirstMove = false;

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

        // 갓 소환된 적은 첫 이동에 무조건 한 칸 전진합니다.
        // 한 바퀴 돌아 Start(0번)에 선 적과 구분하려고, 칸 번호가 아니라 첫 이동 여부로 판정합니다.
        if (!m_hasMadeFirstMove)
        {
            m_hasMadeFirstMove = true;
            yield return StartCoroutine(MoveStepsRoutine(1));
            yield break;
        }

        // 행동 주기 도달 검사
        m_currentActionCounter++;
        if (m_currentActionCounter >= ActionInterval)
        {
            m_currentActionCounter = 0;

            // 1부터 EnemyData에 설정된 Speed 값까지 랜덤 굴림 + 멈춰있는 타일의 보너스 Speed
            int totalSteps = Random.Range(1, CurrentDiceMaxSpeed + 1);
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

    // 순환 경로를 거꾸로 돕니다. 하한이 없어 Start(0번)에서 한 칸 뒤는 마지막 번호입니다.
    private IEnumerator MoveBackwardStepsRoutine(int steps)
    {
        m_isMoving = true;
        for (int i = 0; i < steps; i++)
        {
            int nextIndex = TileManager.Instance.WrapPathIndex(m_currentTileIndex - 1);
            Vector3 targetPos = TileManager.Instance.ReserveEnemyArrivalPosition(m_health, nextIndex);
            yield return MoveOneTileRoutine(nextIndex, targetPos);
            // 도착한 순간에만 인덱스를 바꿔 TileManager의 점유 인덱스와 항상 일치시킵니다.
            m_currentTileIndex = nextIndex;
            RegisterAtTile(m_currentTileIndex);
            if (m_cubeVisual != null && m_cubeVisual.HasAllFaces && m_cubeVisual.LandingPause > 0f)
                yield return new WaitForSeconds(m_cubeVisual.LandingPause);
        }
        m_isMoving = false;
    }

    // 순환 경로를 앞으로 돕니다. 도착점이 없어 마지막 번호 다음은 Start(0번)입니다.
    private IEnumerator MoveStepsRoutine(int steps)
    {
        m_isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            int nextIndex = TileManager.Instance.WrapPathIndex(m_currentTileIndex + 1);
            Vector3 targetPos = TileManager.Instance.ReserveEnemyArrivalPosition(m_health, nextIndex);
            yield return MoveOneTileRoutine(nextIndex, targetPos);
            // 도착한 순간에만 인덱스를 바꿔 TileManager의 점유 인덱스와 항상 일치시킵니다.
            m_currentTileIndex = nextIndex;
            RegisterAtTile(m_currentTileIndex);

            if (m_cubeVisual != null && m_cubeVisual.HasAllFaces && m_cubeVisual.LandingPause > 0f)
                yield return new WaitForSeconds(m_cubeVisual.LandingPause);

        }
        m_isMoving = false;

        yield break;
    }

    private IEnumerator MoveOneTileRoutine(int nextIndex, Vector3 targetPosition)
    {
        // 진형 때문에 목적지가 조금 벌어져 있어도, 굴림 방향은 패스 타일의 중심으로 계산합니다.
        Vector3 pathDirection = TileManager.Instance.GetPathWorldPosition(nextIndex)
            - TileManager.Instance.GetPathWorldPosition(m_currentTileIndex);
        bool rollCube = m_cubeVisual != null && m_cubeVisual.HasAllFaces && pathDirection.sqrMagnitude > 0.0001f;
        EnemyCubeRollDirection direction = Mathf.Abs(pathDirection.x) >= Mathf.Abs(pathDirection.y)
            ? (pathDirection.x >= 0f ? EnemyCubeRollDirection.Right : EnemyCubeRollDirection.Left)
            : (pathDirection.y >= 0f ? EnemyCubeRollDirection.Up : EnemyCubeRollDirection.Down);
        EnemyCubeOrientation nextOrientation = rollCube ? m_cubeOrientation.Rolled(direction) : m_cubeOrientation;
        if (!rollCube) FaceMoveDirection(targetPosition);

        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (rollCube && distance > 0.0001f)
        {
            float duration = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, m_moveSpeed));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                // 일정한 슬라이드 대신, 모서리를 넘어가는 큐브 중심의 궤적을 따라갑니다.
                transform.position = Vector3.Lerp(startPosition, targetPosition, EnemyCubeRollGeometry.Sample(progress).Travel);
                m_cubeVisual.AnimateRoll(m_cubeOrientation, nextOrientation, direction, progress);
                yield return null;
            }
        }
        else
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Mathf.Max(0.01f, m_moveSpeed) * Time.deltaTime);
                yield return null;
            }
        }
        transform.position = targetPosition;
        // 이동 중단 시 미완료 굴림을 누적하지 않고, 도착한 한 칸의 자세만 확정합니다.
        m_cubeOrientation = nextOrientation;
        if (m_cubeVisual != null) m_cubeVisual.ShowOrientation(m_cubeOrientation);
    }

    public void StopMovement()
    {
        m_isMoving = false;
        StopAllCoroutines();
        m_hasFormationTarget = false;
        if (m_cubeVisual != null) m_cubeVisual.ShowOrientation(m_cubeOrientation);

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
        // Start(0번)에는 타일 효과와 디버프존이 없습니다. 나머지 칸은 마지막 번호까지 모두 평범한 칸입니다.
        if (m_currentTileIndex <= 0) return;

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

    // [호출하는 곳 없음] 한 줄짜리 길에서 도착점에 닿은 적을 처리하던 함수입니다.
    // 순환 경로에는 도착점이 없습니다. 패배 조건을 적 수 한도로 바꿀 때 아래 상수와 함께 지웁니다.
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
