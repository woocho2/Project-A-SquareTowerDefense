using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 타워에서 발사되는 투사체의 최종 스탯 정보 구조체
/// </summary>
[System.Serializable]
public struct ProjectileStats
{
    public int projectileID;
    public string projectileName;
    public float damage;
    public float speed;
    public bool isCritical;
    public float criticalRate;
    public float criticalDamage;
    public int SplashRadius;
    public int targetPathIndex;
    public int additionalHitCount;
    public float armorPenetrationPercent;
    public int iceAdditionalTargetCount;
    public int chainCount;
    public float extraHitChance;
    public TowerController ownerTower;
    public float duration;
    public float abilityValue;
    public int hitEffectID;
    public DebuffTarget debuffTarget;
    // 스킬이 발사한 투사체일 때만 채웁니다. 값이 있으면 명중 처리를 이 스킬이 맡습니다.
    public TowerSkill skill;
}

/// <summary>
/// 물리 엔진 없이 순수 수학적 거리 계산으로 명중을 판정하는 최적화 투사체
/// </summary>
public class ProjectileHit2D : MonoBehaviour
{
    [Header("Target Layers")]
    [SerializeField] public LayerMask m_enemyLayer;

    [Header("Projectile Artwork")]
    [Tooltip("프리팹의 전용 이미지를 사용하고 타워 문양과 색으로 덮어쓰지 않습니다.")]
    [SerializeField] private bool m_usePrefabVisual;
    [Tooltip("전용 이미지의 원래 색을 유지하면서 잔상과 파티클에 적용할 색입니다.")]
    [SerializeField] private Color m_prefabEffectColor = Color.white;

    [Header("Visual Effects (Trail & Particle)")]
    [SerializeField] private bool m_enableTrail = true;
    [SerializeField] private bool m_enableParticles = true;
    [SerializeField] private TrailRenderer m_trailRenderer;
    [SerializeField] private ParticleSystem m_particleSystem;

    private ProjectileStats m_stats;
    public float lifeTime = 1.5f;

    private ProjectileObjectPool2D m_ownerPool;
    private Coroutine m_lifeCo;
    public bool IsSplash = false;

    private Transform m_homingTarget;
    private Vector2 m_targetPosition;
    private Vector3 m_lastDirection;
    private bool m_isLaunched;
    private bool m_renderersHidden;
    private Vector3 m_originalLocalScale;
    private SpriteRenderer m_spriteRenderer;
    private Sprite m_originalSprite;
    private Color m_originalColor;

    // 발사 전에 이 탄이 깎기로 예약해 둔 피해입니다. 명중하거나 회수될 때 반드시 풀어 줍니다.
    private EnemyHealthController m_reservedTarget;
    private float m_reservedDamage;

    // 활성 투사체(대기 중인 위성 포함)의 목록입니다. 턴 진행이 "공격이 다 끝났는지" 물을 때 씁니다.
    // 켜지고 꺼질 때 스스로 등록·해제하므로 씬 전체를 검색하지 않아도 됩니다.
    private static readonly HashSet<ProjectileHit2D> s_activeProjectiles = new HashSet<ProjectileHit2D>();
    public static int ActiveCount => s_activeProjectiles.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveProjectiles()
    {
        s_activeProjectiles.Clear();
    }

    private void OnEnable()
    {
        s_activeProjectiles.Add(this);
    }

    private void OnDisable()
    {
        s_activeProjectiles.Remove(this);
        ReleaseReservedDamage();
    }

    private void Awake()
    {
        // 프리팹마다 원래 크기가 다를 수 있으므로 각자의 기준 크기를 보관합니다.
        m_originalLocalScale = transform.localScale;
        SpriteRenderer renderer = GetSpriteRenderer();
        if (renderer != null)
        {
            m_originalSprite = renderer.sprite;
            m_originalColor = renderer.color;
        }

        if (m_trailRenderer == null) m_trailRenderer = GetComponent<TrailRenderer>();
        if (m_particleSystem == null) m_particleSystem = GetComponent<ParticleSystem>();
        ResetVisualEffects();
    }

    private void Update()
    {
        // 위성 위치에 대기 중인 투사체는 Launch가 호출될 때까지 이동하거나 명중하지 않습니다.
        if (!m_isLaunched) return;

        float moveStep = m_stats.speed * Time.deltaTime;

        // 1. 유효한 타겟이 있으면 타겟 위치, 없거나 스플래시면 목표 좌표를 목적지로 지정
        Vector2 destination = (m_homingTarget != null && m_homingTarget.gameObject.activeInHierarchy)
            ? (Vector2)m_homingTarget.position
            : m_targetPosition;

        float distanceToTarget = Vector2.Distance(transform.position, destination);

        // 2. 목적지 도달 시 명중 처리
        if (distanceToTarget <= moveStep || distanceToTarget < 0.2f)
        {
            OnHit();
            return;
        }

        // 3. 목적지를 향해 회전 및 이동
        m_lastDirection = ((Vector3)destination - transform.position).normalized;
        if (m_stats.hitEffectID == 205 || m_stats.hitEffectID == 202)
        {
            // 캡틴 아메리카 방패 및 올라프 투척 도끼 고속 회전 투척 (초당 3바퀴 회전)
            transform.Rotate(0, 0, -1080f * Time.deltaTime);
        }
        else
        {
            RotateToDirection(m_lastDirection);
        }
        transform.position = Vector2.MoveTowards(transform.position, destination, moveStep);
    }

    public void Init(ProjectileStats stats)
    {
        m_stats = stats;
        ApplyTowerVisual(stats.ownerTower);
        SetupVisualEffects(stats.hitEffectID);
    }

    private void ApplyTowerVisual(TowerController ownerTower)
    {
        if (m_usePrefabVisual)
        {
            RestorePrefabVisual();
            return;
        }

        if (ownerTower == null) return;

        TowerVisual visual = ownerTower.GetComponentInChildren<TowerVisual>(true);
        SpriteRenderer renderer = GetSpriteRenderer();
        if (visual == null || renderer == null) return;

        // 풀에서 재사용할 때도 소유 타워의 현재 문양과 색을 다시 적용합니다.
        if (visual.EmblemSprite != null) renderer.sprite = visual.EmblemSprite;
        renderer.color = visual.EmblemColor;
    }

    private SpriteRenderer GetSpriteRenderer()
    {
        if (m_spriteRenderer == null) m_spriteRenderer = GetComponent<SpriteRenderer>();
        return m_spriteRenderer;
    }

    private void RestorePrefabVisual()
    {
        SpriteRenderer renderer = GetSpriteRenderer();
        if (renderer == null) return;
        renderer.sprite = m_originalSprite;
        renderer.color = m_originalColor;
    }

    public ProjectileStats Stats => m_stats;
    public void SetPool(ProjectileObjectPool2D pool) => m_ownerPool = pool;
    public void SetTargetPosition(Vector2 targetPos) => m_targetPosition = targetPos;
    public void SetVisualScale(float multiplier)
    {
        transform.localScale = m_originalLocalScale * Mathf.Max(0.01f, multiplier);
    }

    /// <summary>
    /// 투사체를 활성 상태로 타워 주위에 대기시킵니다.
    /// 발사 코루틴이 중단되더라도 preparationLifetime 뒤에는 풀로 돌아갑니다.
    /// </summary>
    public void PrepareAsSatellite(float preparationLifetime)
    {
        if (m_lifeCo != null) StopCoroutine(m_lifeCo);
        // 풀에서 꺼낸 직후부터 전용 총알 이미지로 위성을 표시합니다.
        if (m_usePrefabVisual) RestorePrefabVisual();

        m_isLaunched = false;
        m_homingTarget = null;
        m_lastDirection = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // 대기 중에는 트레일 및 파티클 방출 중단
        ResetVisualEffects();

        m_lifeCo = StartCoroutine(CoLife(Mathf.Max(0.1f, preparationLifetime)));
    }

    public void CancelPreparedProjectile()
    {
        ReturnToPool();
    }

    /// <summary>발사 전에 이 탄이 대상에게 줄 예상 피해를 예약합니다. 다음 탄의 대상 선택에 반영됩니다.</summary>
    public void ReserveDamage(EnemyHealthController target, float expectedDamage)
    {
        ReleaseReservedDamage();
        if (target == null || expectedDamage <= 0f) return;

        m_reservedTarget = target;
        m_reservedDamage = expectedDamage;
        target.ReserveDamage(expectedDamage);
    }

    private void ReleaseReservedDamage()
    {
        if (m_reservedTarget != null) m_reservedTarget.ReleaseReservedDamage(m_reservedDamage);
        m_reservedTarget = null;
        m_reservedDamage = 0f;
    }

    public void Launch(Vector3 dir, float speed, bool rotateProjectile, float lifeTimeOverride = -1f, Transform targetEnemy = null)
    {
        if (m_lifeCo != null) StopCoroutine(m_lifeCo);

        m_isLaunched = true;

        // 타겟이 넘어왔다면 유도 대상으로 지정
        m_homingTarget = targetEnemy;

        // 타겟이 없거나 사망했을 때를 대비한 기본 방향 설정
        m_lastDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
        if (rotateProjectile && m_stats.hitEffectID != 205 && m_stats.hitEffectID != 202) RotateToDirection(m_lastDirection);

        // 발사 시점에 트레일 초기화 후 꼬리 및 파티클 방출 시작
        StartVisualEffects();

        float lt = (lifeTimeOverride > 0f) ? lifeTimeOverride : GetTravelLifetime(speed, targetEnemy);
        m_lifeCo = StartCoroutine(CoLife(lt));
    }

    // 느린 탄이 목표에 닿기 전에 수명으로 사라지지 않도록, 날아갈 거리와 속도로 수명을 정합니다.
    private float GetTravelLifetime(float speed, Transform targetEnemy)
    {
        Vector2 destination = targetEnemy != null ? (Vector2)targetEnemy.position : m_targetPosition;
        float travelTime = Vector2.Distance(transform.position, destination) / Mathf.Max(0.01f, speed);
        return Mathf.Max(lifeTime, travelTime + 0.5f);
    }

    private void RotateToDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 도달 시 타입에 따라 명중 처리 분기
    private void OnHit()
    {
        if (m_stats.skill != null)
        {
            HandleSkillImpact();
        }
        else if (IsSplash)
        {
            ExplodeSplash();
        }
        else
        {
            HitSingleTarget(m_homingTarget);
        }
    }

    // 단일 타겟 명중 시 처리
    private void HitSingleTarget(Transform target)
    {
        // 예약은 "아직 날아가는 탄"만 세는 값이므로 실제 피해를 주기 직전에 풉니다.
        ReleaseReservedDamage();

        if (target != null)
        {
            if (target.TryGetComponent<EnemyHealthController>(out var health))
            {
                int hitCount = 1 + Mathf.Max(0, m_stats.additionalHitCount);
                for (int i = 0; i < hitCount; i++)
                {
                    if (health.CurrentHP <= 0f) break;

                    DealDamage(health, m_stats.damage, true);

                    if (m_stats.debuffTarget != DebuffTarget.None && target.TryGetComponent<EnemyDebuffController>(out var debuff))
                    {
                        ApplyDebuffToObject(debuff);
                    }
                }

                ApplyIceAdditionalHits(health);
                ApplyChainHits(health.transform.position, new List<EnemyHealthController> { health });

                if (UnityEngine.Random.Range(0f, 100f) < m_stats.extraHitChance && health.CurrentHP > 0f)
                {
                    DealDamage(health, m_stats.damage);
                }
            }
        }

        Quaternion effectRot = (m_stats.hitEffectID == 104 || m_stats.hitEffectID == 204) ? transform.rotation : Quaternion.identity;
        float effectScale = (m_stats.hitEffectID == 104 || m_stats.hitEffectID == 204) ? 1.0f :
                            (m_stats.hitEffectID == 202) ? 1.5f : 3.0f;
        EffectManager.Instance?.PlayEffect(m_stats.hitEffectID, transform.position, effectRot, effectScale);
        ReturnToPool();
    }

    // 스플래시 범위 폭발 처리
    private void ExplodeSplash()
    {
        int splashRange = Mathf.Max(1, m_stats.SplashRadius);
        int tileRadius = splashRange - 1;
        int tileWidth = tileRadius * 2 + 1;

        Quaternion splashEffectRot = (m_stats.hitEffectID == 104 || m_stats.hitEffectID == 204) ? transform.rotation : Quaternion.identity;
        float splashEffectScale = (m_stats.hitEffectID == 104 || m_stats.hitEffectID == 204) ? (tileWidth * 1.2f) : tileWidth;
        EffectManager.Instance?.PlayEffect(m_stats.hitEffectID, transform.position, splashEffectRot, splashEffectScale);

        if (TileManager.Instance == null)
        {
            Debug.LogWarning("[ProjectileHit2D] TileManager가 없어 스플래시 피해를 계산할 수 없습니다.");
            ReturnToPool();
            return;
        }

        // 실제 피해 대상은 투사체 위치가 아니라, 발사할 때 정한 착탄 타일 인덱스 주변의 적 목록에서만 결정합니다.
        List<EnemyHealthController> damagedEnemies = new List<EnemyHealthController>();
        List<int> splashPathIndices = TileManager.Instance.GetPathIndicesInSquare(m_stats.targetPathIndex, tileRadius);
        for (int i = 0; i < splashPathIndices.Count; i++)
        {
            damagedEnemies.AddRange(TileManager.Instance.GetLivingEnemiesAtPathIndex(splashPathIndices[i]));
        }

        for (int i = 0; i < damagedEnemies.Count; i++)
        {
            EnemyHealthController health = damagedEnemies[i];
            DealDamage(health, m_stats.damage);

            if (UnityEngine.Random.Range(0f, 100f) < m_stats.extraHitChance && health.CurrentHP > 0f)
            {
                DealDamage(health, m_stats.damage);
            }

            if (m_stats.debuffTarget != DebuffTarget.None && health.TryGetComponent(out EnemyDebuffController debuff))
            {
                ApplyDebuffToObject(debuff);
            }
        }

        ApplyChainHits(transform.position, damagedEnemies);

        ReturnToPool();
    }

    // 스킬이 발사한 투사체는 명중 처리를 스킬에 맡깁니다. 투사체는 어떤 스킬인지 알지 못합니다.
    private void HandleSkillImpact()
    {
        IEnumerator followUp = m_stats.skill.OnSkillProjectileImpact(this);
        if (followUp == null)
        {
            ReturnToPool();
            return;
        }

        // 스킬의 후속 처리가 끝날 때까지 투사체를 보이지 않는 활성 상태로 두어, 다음 타워가 그 도중에 행동하지 않게 합니다.
        if (m_lifeCo != null) StopCoroutine(m_lifeCo);
        m_isLaunched = false;
        ResetVisualEffects();
        SetRenderersVisible(false);
        m_lifeCo = StartCoroutine(CoSkillFollowUp(followUp));
    }

    private IEnumerator CoSkillFollowUp(IEnumerator followUp)
    {
        yield return followUp;

        // ReturnToPool이 실행 중인 자신을 멈추지 않도록 핸들을 먼저 비웁니다.
        m_lifeCo = null;
        ReturnToPool();
    }

    private void SetRenderersVisible(bool visible)
    {
        m_renderersHidden = !visible;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = visible;
        }
    }

    private void DealDamage(EnemyHealthController health, float damage, bool countsAsPrimaryTargetHit = false)
    {
        if (health == null || health.CurrentHP <= 0f) return;
        health.ApplyDamage(damage, m_stats.isCritical, m_stats.armorPenetrationPercent, m_stats.ownerTower);
        if (countsAsPrimaryTargetHit)
        {
            m_stats.ownerTower?.NotifyPrimaryTargetHit();
        }
    }

    // Ice 버프를 받은 단일 타겟 공격은 명중 적의 인접 적을 추가로 타격합니다.
    private void ApplyIceAdditionalHits(EnemyHealthController primaryTarget)
    {
        int remainingHits = Mathf.Max(0, m_stats.iceAdditionalTargetCount);
        if (remainingHits == 0 || primaryTarget == null) return;

        List<EnemyHealthController> excluded = new List<EnemyHealthController> { primaryTarget };
        for (int i = 0; i < remainingHits; i++)
        {
            EnemyHealthController nextTarget = FindClosestEnemy(primaryTarget.transform.position, 1.5f, excluded);
            if (nextTarget == null) break;

            DealDamage(nextTarget, m_stats.damage);
            excluded.Add(nextTarget);
        }
    }

    // Light 버프: 공격 지점에서 가까운 적에게 원래 피해의 20%를 연쇄합니다.
    private void ApplyChainHits(Vector3 origin, List<EnemyHealthController> excluded)
    {
        int remainingChains = Mathf.Max(0, m_stats.chainCount);
        Vector3 chainOrigin = origin;

        for (int i = 0; i < remainingChains; i++)
        {
            EnemyHealthController nextTarget = FindClosestEnemy(chainOrigin, 2f, excluded);
            if (nextTarget == null) break;

            DealDamage(nextTarget, m_stats.damage * .2f);
            excluded.Add(nextTarget);
            chainOrigin = nextTarget.transform.position;
        }
    }

    private EnemyHealthController FindClosestEnemy(Vector3 origin, float searchRadius, List<EnemyHealthController> excluded)
    {
        if (TileManager.Instance == null) return null;

        List<EnemyHealthController> candidates = TileManager.Instance.GetAllLivingEnemies();
        EnemyHealthController closest = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyHealthController candidate = candidates[i];
            if (candidate == null || candidate.CurrentHP <= 0f || excluded.Contains(candidate)) continue;

            float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;
            if (sqrDistance <= searchRadius * searchRadius && sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = candidate;
            }
        }

        return closest;
    }

    // 디버프 객체 생성 및 에너미 등록
    private void ApplyDebuffToObject(EnemyDebuffController debuffController)
    {
        DebuffBase debuff = m_stats.debuffTarget switch
        {
            DebuffTarget.Fire => new FireDebuff(m_stats.duration, m_stats.abilityValue),
            DebuffTarget.Ice => new IceDebuff(m_stats.duration, m_stats.abilityValue),
            DebuffTarget.Wind => new WindDebuff(m_stats.duration, m_stats.abilityValue),
            _ => null
        };

        if (debuff != null)
        {
            debuffController.AddDebuff(debuff);
        }
    }

    private IEnumerator CoLife(float t)
    {
        yield return new WaitForSeconds(t);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (m_lifeCo != null)
        {
            StopCoroutine(m_lifeCo);
            m_lifeCo = null;
        }

        m_homingTarget = null;
        m_lastDirection = Vector3.zero;
        m_isLaunched = false;
        ReleaseReservedDamage();

        // 메테오 연쇄 도중에 회수되더라도 다음 사용 때 보이도록 되돌립니다.
        if (m_renderersHidden) SetRenderersVisible(true);

        ResetVisualEffects();

        gameObject.SetActive(false);

        if (m_ownerPool != null) m_ownerPool.Release(this);
        else Destroy(gameObject);
    }

    #region Visual Effects (Trail & Particles)

    public void ResetVisualEffects()
    {
        if (m_trailRenderer != null)
        {
            m_trailRenderer.emitting = false;
            m_trailRenderer.Clear();
        }
        if (m_particleSystem != null)
        {
            m_particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void StartVisualEffects()
    {
        if (m_enableTrail && m_trailRenderer != null)
        {
            m_trailRenderer.Clear();
            m_trailRenderer.emitting = true;
        }
        if (m_enableParticles && m_particleSystem != null)
        {
            m_particleSystem.Clear();
            m_particleSystem.Play();
        }
    }

    public void SetupVisualEffects(int effectId)
    {
        if (m_enableTrail) SetupTrailRenderer(effectId);
        if (m_enableParticles) SetupParticleSystem(effectId);
    }

    private void SetupTrailRenderer(int effectId)
    {
        if (m_trailRenderer == null)
        {
            m_trailRenderer = GetComponent<TrailRenderer>();
            if (m_trailRenderer == null)
            {
                m_trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
        }

        m_trailRenderer.time = 0.20f;
        m_trailRenderer.minVertexDistance = 0.04f;
        m_trailRenderer.autodestruct = false;
        m_trailRenderer.emitting = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            m_trailRenderer.sortingLayerID = sr.sortingLayerID;
            m_trailRenderer.sortingOrder = sr.sortingOrder - 1;
            if (sr.sharedMaterial != null)
            {
                m_trailRenderer.sharedMaterial = sr.sharedMaterial;
            }
        }

        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(new Keyframe(0f, 0.38f));
        widthCurve.AddKey(new Keyframe(1f, 0f));
        m_trailRenderer.widthCurve = widthCurve;
        m_trailRenderer.widthMultiplier = 1f;

        m_trailRenderer.colorGradient = GetProjectileTrailGradient();
    }

    private void SetupParticleSystem(int effectId)
    {
        if (m_particleSystem == null)
        {
            m_particleSystem = GetComponent<ParticleSystem>();
            if (m_particleSystem == null)
            {
                m_particleSystem = gameObject.AddComponent<ParticleSystem>();
            }
        }

        var main = m_particleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.maxParticles = 50;

        var emission = m_particleSystem.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 14f;

        var shape = m_particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var colorOverLifetime = m_particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = GetProjectileParticleGradient();

        var sizeOverLifetime = m_particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var psRenderer = GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                psRenderer.sortingLayerID = sr.sortingLayerID;
                psRenderer.sortingOrder = sr.sortingOrder - 1;
                if (sr.sharedMaterial != null)
                {
                    psRenderer.sharedMaterial = sr.sharedMaterial;
                }
            }
        }

        m_particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private Color GetProjectileColor()
    {
        if (m_usePrefabVisual) return m_prefabEffectColor;
        SpriteRenderer renderer = GetSpriteRenderer();
        return renderer != null ? renderer.color : Color.white;
    }

    private Gradient GetProjectileTrailGradient()
    {
        Gradient g = new Gradient();
        Color midColor = GetProjectileColor();
        Color headColor = Color.Lerp(midColor, Color.white, 0.55f);
        Color tailColor = midColor * 0.65f;

        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(headColor, 0f),
                new GradientColorKey(midColor, 0.35f),
                new GradientColorKey(tailColor, 0.8f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.85f, 0.25f),
                new GradientAlphaKey(0.4f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        return g;
    }

    private ParticleSystem.MinMaxGradient GetProjectileParticleGradient()
    {
        Gradient g = new Gradient();
        Color pColor = GetProjectileColor();

        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(pColor, 0.4f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );

        return new ParticleSystem.MinMaxGradient(g);
    }

    #endregion
}
