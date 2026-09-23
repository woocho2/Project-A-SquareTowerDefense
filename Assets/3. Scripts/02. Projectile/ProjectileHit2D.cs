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
    public bool isFireMeteor;
}

/// <summary>
/// 물리 엔진 없이 순수 수학적 거리 계산으로 명중을 판정하는 최적화 투사체
/// </summary>
public class ProjectileHit2D : MonoBehaviour
{
    [Header("Target Layers")]
    [SerializeField] public LayerMask m_enemyLayer;

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
    private Vector3 m_originalLocalScale;

    private void Awake()
    {
        // 프리팹마다 원래 크기가 다를 수 있으므로 각자의 기준 크기를 보관합니다.
        m_originalLocalScale = transform.localScale;

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
        if (m_stats.hitEffectID == 203 || m_stats.hitEffectID == 205)
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
        SetupVisualEffects(stats.hitEffectID);
    }

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

    public void Launch(Vector3 dir, float speed, bool rotateProjectile, float lifeTimeOverride = -1f, Transform targetEnemy = null)
    {
        if (m_lifeCo != null) StopCoroutine(m_lifeCo);

        m_isLaunched = true;

        // 타겟이 넘어왔다면 유도 대상으로 지정
        m_homingTarget = targetEnemy;

        // 타겟이 없거나 사망했을 때를 대비한 기본 방향 설정
        m_lastDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
        if (rotateProjectile && m_stats.hitEffectID != 203 && m_stats.hitEffectID != 205) RotateToDirection(m_lastDirection);

        // 발사 시점에 트레일 초기화 후 꼬리 및 파티클 방출 시작
        StartVisualEffects();

        float lt = (lifeTimeOverride > 0f) ? lifeTimeOverride : lifeTime;
        m_lifeCo = StartCoroutine(CoLife(lt));
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
        if (IsSplash)
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
                            (m_stats.hitEffectID == 205) ? 1.5f : 3.0f;
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

        TilePath path = TilePath.Instance;
        if (path == null)
        {
            Debug.LogWarning("[ProjectileHit2D] TilePath가 없어 스플래시 피해를 계산할 수 없습니다.");
            ReturnToPool();
            return;
        }

        // 실제 피해 대상은 물리 탐색이 아니라 패스 타일별 적 목록에서만 결정합니다.
        List<EnemyHealthController> damagedEnemies = path.GetEnemiesInSquare(transform.position, tileRadius);
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

    private void DealDamage(EnemyHealthController health, float damage, bool countsAsPrimaryTargetHit = false)
    {
        if (health == null || health.CurrentHP <= 0f) return;
        if (m_stats.isFireMeteor && health.TryGetComponent(out EnemyDebuffController debuff))
        {
            damage *= debuff.GetFireMeteorDamageMultiplier();
        }
        health.ApplyDamage(damage, m_stats.isCritical, m_stats.armorPenetrationPercent, m_stats.ownerTower);
        if (countsAsPrimaryTargetHit)
        {
            m_stats.ownerTower?.NotifyFireTargetHit();
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
        TilePath path = TilePath.Instance;
        if (path == null) return null;

        List<EnemyHealthController> candidates = path.GetAllActiveEnemies();
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

        m_trailRenderer.colorGradient = GetElementalGradient(effectId);
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
        colorOverLifetime.color = GetElementalParticleGradient(effectId);

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

    private Gradient GetElementalGradient(int effectId)
    {
        int element = effectId % 100;
        Gradient g = new Gradient();
        Color headColor, midColor, tailColor;

        switch (element)
        {
            case 7: // Fire
                headColor = new Color(1f, 1f, 1f, 1f);
                midColor  = new Color(0.98f, 0.45f, 0.08f, 1f); // #f97316
                tailColor = new Color(0.86f, 0.15f, 0.15f, 1f); // #dc2626
                break;
            case 8: // Ice
                headColor = new Color(1f, 1f, 1f, 1f);
                midColor  = new Color(0.22f, 0.74f, 0.97f, 1f); // #38bdf8
                tailColor = new Color(0.01f, 0.52f, 0.78f, 1f); // #0284c7
                break;
            case 9: // Electric
                headColor = new Color(1f, 1f, 1f, 1f);
                midColor  = new Color(0.99f, 0.88f, 0.28f, 1f); // #fde047
                tailColor = new Color(0.79f, 0.54f, 0.02f, 1f); // #ca8a04
                break;
            case 10: // Wind
                headColor = new Color(0.80f, 0.98f, 0.95f, 1f); // #ccfbf1
                midColor  = new Color(0.18f, 0.83f, 0.75f, 1f); // #2dd4bf
                tailColor = new Color(0.02f, 0.59f, 0.41f, 1f); // #059669
                break;
            case 11: // Earth
                headColor = new Color(1f, 0.95f, 0.78f, 1f);    // #fef3c7
                midColor  = new Color(0.96f, 0.62f, 0.04f, 1f); // #f59e0b
                tailColor = new Color(0.57f, 0.25f, 0.05f, 1f); // #92400e
                break;
            case 12: // Light
                headColor = new Color(1f, 1f, 1f, 1f);
                midColor  = new Color(1f, 0.94f, 0.54f, 1f);    // #fef08a
                tailColor = new Color(0.98f, 0.80f, 0.08f, 1f); // #facc15
                break;
            case 13: // Darkness
                headColor = new Color(0.95f, 0.91f, 1f, 1f);    // #f3e8ff
                midColor  = new Color(0.75f, 0.52f, 0.99f, 1f); // #c084fc
                tailColor = new Color(0.35f, 0.11f, 0.53f, 1f); // #581c87
                break;
            default: // Physical
                headColor = new Color(1f, 1f, 1f, 1f);
                midColor  = new Color(0.89f, 0.91f, 0.94f, 1f);
                tailColor = new Color(0.58f, 0.64f, 0.72f, 1f);
                break;
        }

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

    private ParticleSystem.MinMaxGradient GetElementalParticleGradient(int effectId)
    {
        int element = effectId % 100;
        Gradient g = new Gradient();
        Color pColor;

        switch (element)
        {
            case 7:  pColor = new Color(0.98f, 0.45f, 0.08f); break; // Fire
            case 8:  pColor = new Color(0.22f, 0.74f, 0.97f); break; // Ice
            case 9:  pColor = new Color(0.99f, 0.88f, 0.28f); break; // Electric
            case 10: pColor = new Color(0.18f, 0.83f, 0.75f); break; // Wind
            case 11: pColor = new Color(0.96f, 0.62f, 0.04f); break; // Earth
            case 12: pColor = new Color(1f, 0.95f, 0.6f);     break; // Light
            case 13: pColor = new Color(0.75f, 0.52f, 0.99f); break; // Darkness
            default: pColor = new Color(0.82f, 0.88f, 0.95f); break; // Physical (Metallic Silver)
        }

        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(pColor, 0.4f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );

        return new ParticleSystem.MinMaxGradient(g);
    }

    #endregion
}
