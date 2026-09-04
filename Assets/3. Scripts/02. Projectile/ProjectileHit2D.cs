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
}

/// <summary>
/// 물리 엔진 없이 순수 수학적 거리 계산으로 명중을 판정하는 최적화 투사체
/// </summary>
public class ProjectileHit2D : MonoBehaviour
{
    [Header("Target Layers")]
    [SerializeField] public LayerMask m_enemyLayer;

    private ProjectileStats m_stats;
    public float lifeTime = 1.5f;

    private ProjectileObjectPool2D m_ownerPool;
    private Coroutine m_lifeCo;
    public bool IsSplash = false;

    private Transform m_homingTarget;
    private Vector2 m_targetPosition;
    private Vector3 m_lastDirection;

    private void Update()
    {
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
        RotateToDirection(m_lastDirection);
        transform.position = Vector2.MoveTowards(transform.position, destination, moveStep);
    }

    public void Init(ProjectileStats stats) => m_stats = stats;
    public void SetPool(ProjectileObjectPool2D pool) => m_ownerPool = pool;
    public void SetTargetPosition(Vector2 targetPos) => m_targetPosition = targetPos;

    public void Launch(Vector3 dir, float speed, bool rotateProjectile, float lifeTimeOverride = -1f, Transform targetEnemy = null)
    {
        if (m_lifeCo != null) StopCoroutine(m_lifeCo);

        // 타겟이 넘어왔다면 유도 대상으로 지정
        m_homingTarget = targetEnemy;

        // 타겟이 없거나 사망했을 때를 대비한 기본 방향 설정
        m_lastDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
        if (rotateProjectile) RotateToDirection(m_lastDirection);

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

                    DealDamage(health, m_stats.damage);

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

        EffectManager.Instance?.PlayEffect(m_stats.hitEffectID, transform.position, Quaternion.identity);
        ReturnToPool();
    }

    // 스플래시 범위 폭발 처리
    private void ExplodeSplash()
    {
        int splashRange = Mathf.Max(1, m_stats.SplashRadius);
        int tileRadius = splashRange - 1;
        int tileWidth = tileRadius * 2 + 1;

        EffectManager.Instance?.PlayEffect(m_stats.hitEffectID, transform.position, Quaternion.identity, tileWidth);

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

    private void DealDamage(EnemyHealthController health, float damage)
    {
        if (health == null || health.CurrentHP <= 0f) return;
        health.ApplyDamage(damage, m_stats.isCritical, m_stats.armorPenetrationPercent, m_stats.ownerTower);
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

        gameObject.SetActive(false);

        if (m_ownerPool != null) m_ownerPool.Release(this);
        else Destroy(gameObject);
    }
}
