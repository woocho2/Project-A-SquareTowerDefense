using System.Collections;
using UnityEngine;

public class EnemyMoveController : MonoBehaviour
{
    [Header("이동 속도 설정")]
    [Tooltip("메시 큐브는 굴림 속도 배율(1=타일당 약 0.4초), 기존 적은 월드 단위/초입니다. 주사위 이동 칸 수와는 별개입니다.")]
    [SerializeField, Min(0.01f)] private float m_moveSpeed = 1;
    [SerializeField] private bool m_spriteFacesLeftByDefault = true;

    [Header("큐브 굴림")]
    [SerializeField] private EnemyCubeVisual m_cubeVisual;
    [SerializeField] private EnemyCubeMeshVisual m_cubeMeshVisual;
    private EnemyCubeOrientation m_cubeOrientation = EnemyCubeOrientation.Identity;
    public EnemyCubeFace VisibleCubeFace => m_cubeOrientation.VisibleFace;
    public int VisibleCubeFaceIndex => (int)m_cubeOrientation.VisibleFace;
    public int VisibleCubeAssetIndex => UsesMeshCube ? m_cubeMeshVisual.GetFaceAssetIndex(VisibleCubeFaceIndex) : -1;
    public int CubeFaceQuarterTurns => m_cubeOrientation.CounterClockwiseQuarterTurns;
    private bool UsesMeshCube => m_cubeMeshVisual != null && m_cubeMeshVisual.HasAllFaces;
    private bool CanRollCube => UsesMeshCube || (m_cubeVisual != null && m_cubeVisual.HasAllFaces);
    private float CubeLandingPause => UsesMeshCube ? m_cubeMeshVisual.LandingPause : m_cubeVisual != null ? m_cubeVisual.LandingPause : 0f;

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
    private bool m_isSpawning;
    private Coroutine m_spawnRoutine;

    public int CurrentTileIndex => m_currentTileIndex;
    public bool IsTopOfStack { get; private set; } = true;
    public bool IsMoving => m_isMoving || m_isSpawning;
    public bool IsSpawning => m_isSpawning;
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
        if (m_cubeMeshVisual == null) m_cubeMeshVisual = GetComponentInChildren<EnemyCubeMeshVisual>(true);
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
    /// 먼저 도착한 적의 큐브가 위에 보이도록 렌더 순서를 적용합니다.
    /// </summary>
    public void SetStackRenderOrder(int sortingOrder, bool isTop)
    {
        IsTopOfStack = isTop;
        if (m_cubeMeshVisual != null) m_cubeMeshVisual.SetSortingOrder(sortingOrder);
        if (m_spriteRenderer != null) m_spriteRenderer.sortingOrder = sortingOrder;
        if (!isTop && m_health != null) m_health.HideHPBar();
    }

    // ScriptableObject(EnemyData)의 Action과 Speed를 기반으로 초기화
    public bool InitMovement(EnemyData data, int startIndex)
    {
        StopAllCoroutines();
        m_spawnRoutine = null;
        m_isSpawning = false;
        ResetFacingDirection();
        m_cubeOrientation = UsesMeshCube ? m_cubeMeshVisual.RandomizeForSpawn() : EnemyCubeOrientation.Identity;
        ShowCubeOrientation();
        m_currentTileIndex = startIndex;
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
            if (!RegisterAtTile(m_currentTileIndex)) return false;
            transform.position = TileManager.Instance.GetPathWorldPosition(m_currentTileIndex);
        }
        return true;
    }

    /// <summary>풀에서 초기화와 자리 배정을 마친 뒤 호출합니다. 같은 프레임에 소환한 큐브는 함께 떨어집니다.</summary>
    public void PlaySpawnDrop()
    {
        if (!UsesMeshCube || !isActiveAndEnabled) return;
        if (m_spawnRoutine != null) StopCoroutine(m_spawnRoutine);
        m_isSpawning = true;
        m_spawnRoutine = StartCoroutine(SpawnDropRoutine());
    }

    private IEnumerator SpawnDropRoutine()
    {
        float elapsed = 0f;
        float duration = m_cubeMeshVisual.SpawnDuration;
        while (elapsed < duration)
        {
            m_cubeMeshVisual.AnimateSpawn(m_cubeOrientation, elapsed);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ShowCubeOrientation();
        m_isSpawning = false;
        m_spawnRoutine = null;
    }

    private void OnDisable()
    {
        StopMovement();
    }

    /// <summary>
    /// 이동이 완료된 순간 전투 점유와 타일 도착 순서를 확정합니다.
    /// </summary>
    private bool RegisterAtTile(int index)
    {
        TileManager tileManager = TileManager.Instance;
        return tileManager != null && tileManager.SetEnemyAtPathIndex(m_health, index, out _);
    }

    public IEnumerator ProcessTurnMoveRoutine()
    {
        while (m_isSpawning && isActiveAndEnabled) yield return null;
        if (!isActiveAndEnabled) yield break;
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null) yield break;

        // 행동 불가 턴에는 움직이지 않으므로 비프로스트도 건너지 않습니다.
        if (m_forcedNoMoveTurns > 0)
        {
            m_forcedNoMoveTurns--;
            yield break;
        }

        // 비프로스트 위에서 행동을 시작했다면 이번에 또 비프로스트에 멈춰도 건너지 않습니다 (건너온 뒤 끝없이 오가지 않게).
        bool startedOnBifrost = tileManager.GetTileTypeAt(m_currentTileIndex) == PathTileBuffType.Bifrost;

        // 갓 소환된 적은 첫 이동에 무조건 한 칸 전진합니다.
        // 소환 위치가 무작위라 칸 번호로는 갓 소환된 적을 알 수 없으므로, 첫 이동 여부로 판정합니다.
        if (!m_hasMadeFirstMove)
        {
            int beforeIndex = m_currentTileIndex;
            yield return StartCoroutine(MoveStepsRoutine(1));
            m_hasMadeFirstMove = m_currentTileIndex != beforeIndex;
            if (m_hasMadeFirstMove && !startedOnBifrost) yield return StartCoroutine(CrossBifrostRoutine());
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

            if (!startedOnBifrost) yield return StartCoroutine(CrossBifrostRoutine());
        }
    }

    /// <summary>
    /// 이동을 마친 칸이 비프로스트면 건너편 칸으로 옮깁니다. 앞쪽 칸에서 건너면 뒤로 돌아가는 셈입니다.
    /// 메시 큐브는 건너편에서 소환 때처럼 위에서 떨어지며 나타납니다.
    /// </summary>
    private IEnumerator CrossBifrostRoutine()
    {
        TileManager tileManager = TileManager.Instance;
        if (tileManager == null || !tileManager.TryGetBifrostDestination(m_currentTileIndex, out int destination)) yield break;

        m_isMoving = true;
        m_currentTileIndex = destination;
        RegisterAtTile(m_currentTileIndex);
        transform.position = tileManager.GetPathWorldPosition(m_currentTileIndex);

        if (UsesMeshCube) yield return SpawnDropRoutine();
        m_isMoving = false;
    }

    // 순환 경로를 거꾸로 돕니다. 하한이 없어 Start(0번)에서 한 칸 뒤는 마지막 번호입니다.
    private IEnumerator MoveBackwardStepsRoutine(int steps)
    {
        m_isMoving = true;
        bool hasMoved = false;
        for (int i = 0; i < steps; i++)
        {
            int nextIndex = TileManager.Instance.WrapPathIndex(m_currentTileIndex - 1);
            Vector3 targetPos = TileManager.Instance.GetPathWorldPosition(nextIndex);
            yield return MoveOneTileRoutine(nextIndex, targetPos, i == 0);
            // 도착한 순간에만 인덱스를 바꿔 TileManager의 점유 인덱스와 항상 일치시킵니다.
            m_currentTileIndex = nextIndex;
            RegisterAtTile(m_currentTileIndex);
            hasMoved = true;
            if (!UsesMeshCube && CanRollCube && CubeLandingPause > 0f)
                yield return new WaitForSeconds(CubeLandingPause);
        }
        if (hasMoved) yield return FinishCubeTravelRoutine();
        m_isMoving = false;
    }

    // 순환 경로를 앞으로 돕니다. 도착점이 없어 마지막 번호 다음은 Start(0번)입니다.
    private IEnumerator MoveStepsRoutine(int steps)
    {
        m_isMoving = true;
        bool hasMoved = false;

        for (int i = 0; i < steps; i++)
        {
            int nextIndex = TileManager.Instance.WrapPathIndex(m_currentTileIndex + 1);
            Vector3 targetPos = TileManager.Instance.GetPathWorldPosition(nextIndex);
            yield return MoveOneTileRoutine(nextIndex, targetPos, i == 0);
            // 도착한 순간에만 인덱스를 바꿔 TileManager의 점유 인덱스와 항상 일치시킵니다.
            m_currentTileIndex = nextIndex;
            RegisterAtTile(m_currentTileIndex);
            hasMoved = true;

            if (!UsesMeshCube && CanRollCube && CubeLandingPause > 0f)
                yield return new WaitForSeconds(CubeLandingPause);

        }
        if (hasMoved) yield return FinishCubeTravelRoutine();
        m_isMoving = false;

        yield break;
    }

    private float MeshCubeTileDuration => Mathf.Max(0.12f, m_cubeMeshVisual.TileRollDuration / Mathf.Max(0.01f, m_moveSpeed));

    private IEnumerator FinishCubeTravelRoutine()
    {
        if (!UsesMeshCube) yield break;
        float duration = MeshCubeTileDuration * 0.175f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            m_cubeMeshVisual.AnimateUprightSquash(m_cubeOrientation, 0.16f * Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI));
            yield return null;
        }
        ShowCubeOrientation();
        yield return CorrectCubeUprightRoutine();
    }

    private IEnumerator CorrectCubeUprightRoutine()
    {
        if (!UsesMeshCube) yield break;
        // 마지막 도착 지점에서만 같은 면을 유지하며, 가장 짧은 1~2번의 점프로 일어납니다.
        while (m_cubeOrientation.CounterClockwiseQuarterTurns != 0)
        {
            EnemyCubeOrientation before = m_cubeOrientation;
            int direction = before.CounterClockwiseQuarterTurns == 3 ? 1 : -1;
            float impactDuration = m_cubeMeshVisual.UprightImpactDuration;
            float elapsed = 0f;
            float prepareDuration = impactDuration * 0.6f;
            while (elapsed < prepareDuration)
            {
                elapsed += Time.deltaTime;
                m_cubeMeshVisual.AnimateUprightSquash(before, 0.1f * Mathf.Clamp01(elapsed / prepareDuration));
                yield return null;
            }
            elapsed = 0f;
            float hopDuration = m_cubeMeshVisual.UprightHopDuration;
            while (elapsed < hopDuration)
            {
                elapsed += Time.deltaTime;
                m_cubeMeshVisual.AnimateUprightHop(before, direction, elapsed / hopDuration);
                yield return null;
            }
            // 90도 점프 착지 때마다 논리 자세도 확정합니다. 중단된 점프는 누적하지 않습니다.
            m_cubeOrientation = EnemyCubeOrientation.FromFace(before.VisibleFace, before.CounterClockwiseQuarterTurns + direction);
            ShowCubeOrientation();
            elapsed = 0f;
            while (elapsed < impactDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / impactDuration);
                m_cubeMeshVisual.AnimateUprightSquash(m_cubeOrientation, 0.16f * Mathf.Sin(progress * Mathf.PI));
                yield return null;
            }
            ShowCubeOrientation();
        }
    }

    private IEnumerator MoveOneTileRoutine(int nextIndex, Vector3 targetPosition, bool prepareLaunch)
    {
        // 타일 중심 사이의 방향으로 굴립니다. 도착 타일에 적이 있어도 이동합니다.
        Vector3 pathDirection = TileManager.Instance.GetPathWorldPosition(nextIndex)
            - TileManager.Instance.GetPathWorldPosition(m_currentTileIndex);
        bool rollCube = CanRollCube && pathDirection.sqrMagnitude > 0.0001f;
        EnemyCubeRollDirection direction = Mathf.Abs(pathDirection.x) >= Mathf.Abs(pathDirection.y)
            ? (pathDirection.x >= 0f ? EnemyCubeRollDirection.Right : EnemyCubeRollDirection.Left)
            : (pathDirection.y >= 0f ? EnemyCubeRollDirection.Up : EnemyCubeRollDirection.Down);
        EnemyCubeOrientation nextOrientation = rollCube ? m_cubeOrientation.Rolled(direction) : m_cubeOrientation;
        if (!rollCube) FaceMoveDirection(targetPosition);

        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        if (rollCube && UsesMeshCube && distance > 0.0001f)
        {
            float duration = MeshCubeTileDuration;
            if (prepareLaunch)
            {
                float elapsed = 0f;
                float prepareDuration = duration * 0.1f;
                while (elapsed < prepareDuration)
                {
                    elapsed += Time.deltaTime;
                    m_cubeMeshVisual.AnimateWindUp(m_cubeOrientation, direction, elapsed / prepareDuration);
                    yield return null;
                }
            }
            EnemyCubeOrientation before = m_cubeOrientation;
            float rollDuration = duration * 0.725f;
            float rollElapsed = 0f;
            while (rollElapsed < rollDuration)
            {
                rollElapsed += Time.deltaTime;
                m_cubeMeshVisual.AnimateTileRoll(before, direction, rollElapsed / rollDuration, out float travel, out m_cubeOrientation);
                transform.position = Vector3.Lerp(startPosition, targetPosition, travel);
                yield return null;
            }
            // 프레임 간격과 무관하게 정확히 세 번의 90도 회전과 도착을 확정합니다.
            m_cubeMeshVisual.AnimateTileRoll(before, direction, 1f, out _, out m_cubeOrientation);
            transform.position = targetPosition;
            ShowCubeOrientation();
            yield break;
        }
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
                if (UsesMeshCube) m_cubeMeshVisual.AnimateRoll(m_cubeOrientation, direction, progress);
                else m_cubeVisual.AnimateRoll(m_cubeOrientation, nextOrientation, direction, progress);
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
        ShowCubeOrientation();
    }

    public void StopMovement()
    {
        m_isMoving = false;
        m_isSpawning = false;
        m_spawnRoutine = null;
        StopAllCoroutines();
        ShowCubeOrientation();

        if (TileManager.Instance != null) TileManager.Instance.RemoveEnemyFromPath(m_health, out _);
    }

    private void ShowCubeOrientation()
    {
        if (UsesMeshCube) m_cubeMeshVisual.ShowOrientation(m_cubeOrientation);
        else if (m_cubeVisual != null) m_cubeVisual.ShowOrientation(m_cubeOrientation);
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

        // 0번(Start)도 평범한 칸이라 특수 타일과 디버프존의 효과를 받습니다.
        if (TileManager.Instance == null) return;

        // 2. 현재 멈춰있는 타일 타입 확인
        PathTileBuffType tileType = TileManager.Instance.GetTileTypeAt(m_currentTileIndex);

        // 3. 타일 타입별 버프 분기 처리
        switch (tileType)
        {
            case PathTileBuffType.MoveTile:
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
