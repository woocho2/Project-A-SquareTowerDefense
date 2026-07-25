using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// "풀링되는 2D 투사체" 스크립트
/// - ProjectilePool2D에서 꺼내 Spawn/Launch로 발사됨
/// - 충돌하거나 수명이 끝나면 ReturnToPool()로 풀에 반환됨
/// </summary>
public class ProjectileHit2D : MonoBehaviour
{
    [Header("Target Layers")]
    [SerializeField] public LayerMask m_enemyLayer;
    [SerializeField] public LayerMask m_endLayer;

    [Header("Audio / ETC")]
    [SerializeField] AudioSource m_audioSource;
    [SerializeField] LayerMask m_sfxLayerMask;

    private ProjectileStats m_stats;
    public float lifeTime = 1.5f;
    // 발사 후 이 시간이 지나면 자동으로 풀로 반환(수명)

    private Rigidbody2D rb;
    private ProjectileObjectPool2D ownerPool;
    private Coroutine lifeCo;
    private bool IsReturning;
    public bool IsSplash = false;

    private Transform m_homingTarget;
    private Vector3 m_lastDirection;
    private bool m_isHomingMode = false;

    private Vector2 m_targetPosition;

    private void Awake()
    {
        // Rigidbody2D 캐싱(매번 GetComponent 하지 않게)
        rb = GetComponent<Rigidbody2D>();

        if (m_audioSource == null)
        {
            TryGetComponent(out m_audioSource);
        }
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        if (IsSplash && !IsReturning && m_stats.speed > 0f)
        {
            float moveDistanceThisFrame = m_stats.speed * Time.fixedDeltaTime;
            float distanceToTarget = Vector2.Distance(transform.position, m_targetPosition);

            if (distanceToTarget <= moveDistanceThisFrame || distanceToTarget < 0.2f)
            {
                rb.linearVelocity = Vector2.zero;
                ExplodeSplash();
                return;
            }
        }

        if (m_isHomingMode && !IsReturning)
        {
            if (m_homingTarget != null && m_homingTarget.gameObject.activeInHierarchy)
            {
                float moveDistanceThisFrame = m_stats.speed * Time.fixedDeltaTime;
                float distanceToTarget = Vector2.Distance(transform.position, m_homingTarget.position);

                if (distanceToTarget <= moveDistanceThisFrame)
                {

                    if (m_homingTarget.TryGetComponent<EnemyController>(out var enemy))
                    {
                        enemy.TakeDamage(m_stats.damage, m_stats.isCritical);

                        if (m_stats.dotDamage > 0f)
                        {
                            enemy.ApplyDebuff(DebuffTarget.DotDamage, m_stats.dotDamage, m_stats.dotDuration);
                        }

                        if (m_stats.chainCount > 0f)
                        {
                            TriggerChain(enemy, (int)m_stats.chainCount, m_stats.chainDamage);
                        }
                    }
                    ReturnToPool();
                    return;
                }

                m_lastDirection = (m_homingTarget.position - transform.position).normalized;

                float angle = Mathf.Atan2(m_lastDirection.y, m_lastDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                rb.linearVelocity = m_lastDirection * m_stats.speed;
            }
            else
            {
                rb.linearVelocity = m_lastDirection * m_stats.speed;
            }
        }
    }

    public void Init(ProjectileStats stats)
    {
        m_stats = stats;
    }

    public void SetPool(ProjectileObjectPool2D pool)
    {
        ownerPool = pool;
    }

    public void SetTargetPosition(Vector2 targetPos)
    {
        m_targetPosition = targetPos;
    }

    /// <summary>
    /// 풀에서 꺼낸 직후 호출되는 발사 함수
    /// - 물리 상태를 리셋하고 속도를 부여
    /// - 옵션에 따라 투사체 스프라이트를 진행 방향으로 회전
    /// - 수명 타이머 코루틴 시작(시간 지나면 자동 반환)
    /// </summary>
    /// <param name="dir">발사 방향(가능하면 normalized 권장)</param>
    /// <param name="speed">발사 속도</param>
    /// <param name="rotateProjectile">진행 방향으로 회전할지</param>
    /// <param name="lifeTimeOverride">수명 오버라이드(양수면 이 값 사용, 아니면 기본 lifeTime)</param>
    public void Launch(Vector3 dir, float speed, bool rotateProjectile, float lifeTimeOverride = -1f, Transform targetEnemy = null)
    {
        IsReturning = false;

        if (lifeCo != null)
        {
            StopCoroutine(lifeCo);
            lifeCo = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (targetEnemy != null && !IsSplash)
        {
            m_isHomingMode = true;
            m_homingTarget = targetEnemy;
            m_lastDirection = (m_homingTarget.position - transform.position).normalized;
        }
        else
        {
            m_isHomingMode = false;
            m_homingTarget = null;

            if (rb != null)
            {
                Vector3 nd = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
                rb.linearVelocity = dir * speed;
            }
        }

        if (rotateProjectile && !m_isHomingMode)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

        }
        else if (!m_isHomingMode)
        {
            transform.rotation = Quaternion.identity;
        }

        float lt = (lifeTimeOverride > 0f) ? lifeTimeOverride : lifeTime;

        lifeCo = StartCoroutine(CoLife(lt));
    }

    private IEnumerator CoLife(float t)
    {
        yield return new WaitForSeconds(t);
        ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsReturning) return;

        if (CommonUtil.ContainsLayer(m_endLayer, collision.gameObject.layer))
        {
            ReturnToPool();
            return;
        }

        if (CommonUtil.ContainsLayer(m_enemyLayer, collision.gameObject.layer))
        {
            GetComponent<Collider2D>().enabled = false;

            if (IsSplash && m_stats.speed == 0f)
            {
                ExplodeSplash();
                return;
            }

            if (IsSplash && m_stats.speed > 0f)
            {
                GetComponent<Collider2D>().enabled = true;
                return;
            }

            if (collision.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.TakeDamage(m_stats.damage, m_stats.isCritical);

                if (m_stats.dotDamage > 0f)
                {
                    enemy.ApplyDebuff(DebuffTarget.DotDamage, m_stats.dotDamage, m_stats.dotDuration);
                }

                if (m_stats.chainCount > 0f)
                {
                    TriggerChain(enemy, (int)m_stats.chainCount, m_stats.chainDamage);
                }

                EffectManager.Instance.PlayEffect(m_stats.hitEffectID, transform.position, Quaternion.identity);
            }
            ReturnToPool();
        }
    }

    private void ExplodeSplash()
    {
        if (EffectManager.Instance != null)
        {
            float effectScale = m_stats.SplashRadius;
            EffectManager.Instance.PlayEffect(m_stats.hitEffectID, transform.position, Quaternion.identity, effectScale);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, m_stats.SplashRadius, m_enemyLayer);

        foreach (Collider2D hit in hitEnemies)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.TakeDamage(m_stats.damage, m_stats.isCritical);

                if (m_stats.debuffTarget != DebuffTarget.None)
                {
                    enemy.ApplyDebuff(m_stats.debuffTarget, m_stats.abilityValue, m_stats.duration);
                }

                if (m_stats.dotDamage > 0f)
                {
                    enemy.ApplyDebuff(DebuffTarget.DotDamage, m_stats.dotDamage, m_stats.dotDuration);
                }

                if (m_stats.chainCount > 0f)
                {
                    TriggerChain(enemy, (int)m_stats.chainCount, m_stats.chainDamage);
                }
            }
        }

        ReturnToPool();
    }

    private void TriggerChain(EnemyController initialEnemy, int remainingCount, float damage)
    {
        if (remainingCount <= 0) return;

        // 현재 적 주변의 다른 적 탐색
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(initialEnemy.transform.position, 2.5f, m_enemyLayer);

        foreach (var hit in hitEnemies)
        {
            EnemyController nextEnemy = hit.GetComponentInParent<EnemyController>();

            // 1. 적이 있고, 방금 맞은 적(initialEnemy)이 아닐 경우
            if (nextEnemy != null && nextEnemy != initialEnemy)
            {
                // 2. 즉발 데미지 적용
                nextEnemy.TakeDamage(damage, false);

                // 3. 전기 이펙트 생성 (EffectManager 사용)
                //EffectManager.Instance.PlayEffect("LightningEffectID", initialEnemy.transform.position, Quaternion.identity);

                // 4. 연쇄 재귀 호출
                TriggerChain(nextEnemy, remainingCount - 1, damage);
                break; // 한 번에 하나씩만 연쇄
            }
        }
    }

    // ----------------------------
    // 풀 반환 처리
    // ----------------------------

    /// <summary>
    /// 투사체를 풀로 되돌리는 공통 함수
    /// - 코루틴 정리
    /// - 풀에 연결되어 있으면 Release, 아니면 Destroy(안전장치)
    /// </summary>
    private void ReturnToPool()
    {
        // 중복 호출 방지(충돌+수명만료 동시 등)
        if (IsReturning) return;
        IsReturning = true;

        // 수명 코루틴이 돌고 있으면 정리
        if (lifeCo != null)
        {
            StopCoroutine(lifeCo);
            lifeCo = null;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }

        m_isHomingMode = false;
        m_homingTarget = null;
        m_lastDirection = Vector3.zero;

        // 풀 참조가 있으면 풀로 반환
        // 없으면(테스트 중이거나 SetPool을 깜빡한 경우) 파괴해서 누수 방지
        if (ownerPool != null)
            ownerPool.Release(this);
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        // 물리값 리셋: 다음에 다시 꺼냈을 때 이전 속도가 남아있지 않도록
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 혹시 남아있는 코루틴 정리(이중 안전장치)
        if (lifeCo != null)
        {
            StopCoroutine(lifeCo);
            lifeCo = null;
        }

        // 다음 재사용 시 정상 동작하도록 플래그 초기화
        //IsReturning = false;
    }
}
