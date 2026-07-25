using UnityEngine;
using UnityEngine.Pool; // (추가) UnityEngine.Pool 사용
using System.Collections.Generic;

/// <summary>
/// 2D 투사체(총알 등)를 재사용하기 위한 오브젝트 풀(ObjectPool 기반)
/// - Prewarm(initialSize) 지원
/// - expandable=false면 풀에 남은 게 없을 때 null 반환
/// - maxSize를 "총 생성 상한"처럼 사용(기존 코드 의도 유지)
/// </summary>
public class ProjectileObjectPool2D : MonoBehaviour
{
    // ----------------------------
    // Inspector 설정값들
    // ----------------------------

    [Header("Prefab")]
    [SerializeField] private ProjectileHit2D projectilePrefab;

    [Header("Pool Settings")]
    [SerializeField] private int initialSize = 20;
    [SerializeField] private int maxSize = 200;
    [SerializeField] private bool expandable = true;

    [Header("Hierarchy")]
    [SerializeField] private Transform container;

    // ----------------------------
    // 내부 상태값들
    // ----------------------------

    private ObjectPool<ProjectileHit2D> m_pool; // (변경) Queue -> ObjectPool
    private int _createdCount = 0;                  // (유지) "총 생성 상한" 용

    // ----------------------------
    // Unity LifeCycle
    // ----------------------------

    public void InitPool(ProjectileHit2D prefab, int initSize, int max, bool canExpand, Transform poolContainer)
    {
        projectilePrefab = prefab;
        initialSize = initSize;
        maxSize = max;
        expandable = canExpand;
        container = poolContainer;

        if (projectilePrefab == null)
        {
            Debug.LogError("[ProjectilePool2D] projectilePrefab이 비어있습니다.");
            enabled = false;
            return;
        }

        // (추가) UnityEngine.Pool ObjectPool 구성
        // - defaultCapacity: 초기 내부 용량(프리웜 개수 힌트)
        // - maxSize: "풀에 보관 가능한(비활성) 최대 개수" (Unity 기본 의미)
        //   여기서는 기존 코드와 맞추기 위해 maxSize를 그대로 넣되,
        //   생성 상한은 _createdCount로 별도로 막습니다.
        m_pool = new ObjectPool<ProjectileHit2D>(
            createFunc: CreateNew,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyPooled,
            collectionCheck: true,
            defaultCapacity: Mathf.Max(0, initialSize),
            maxSize: Mathf.Max(1, maxSize)
        );
        Prewarm();
    }

    private void OnDestroy()
    {
        // (추가) 필요 시 풀 정리
        m_pool?.Dispose();
        m_pool = null;
    }

    /// <summary>
    /// initialSize 만큼 미리 생성해서 풀에 넣어두는 작업
    /// </summary>
    private void Prewarm()
    {
        if (initialSize <= 0) return;

        if (initialSize > maxSize)
        {
            Debug.LogWarning(
                $"[ProjectilePool2D] initialSize({initialSize})가 maxSize({maxSize})보다 큽니다. maxSize까지만 Prewarm합니다.");
        }

        int count = Mathf.Min(initialSize, maxSize);

        // Prewarm 중에는 Get으로 꺼낸 뒤 다시 Release 해서 비활성 상태로 쌓아둡니다.
        // (ObjectPool 내부 CountAll / CountInactive도 자연스럽게 세팅됨)
        var temp = new List<ProjectileHit2D>(count);
        for (int i = 0; i < count; i++)
        {
            var p = TryGet(); // (변경) 생성 상한/expandable 체크 포함
            if (p == null) break;
            temp.Add(p);
        }

        for (int i = 0; i < temp.Count; i++)
            m_pool.Release(temp[i]);
    }

    /// <summary>
    /// 프리팹을 Instantiate 해서 새 투사체를 하나 생성합니다.
    /// (활성화는 하지 않음)
    /// </summary>
    private ProjectileHit2D CreateNew()
    {
        // (중요) createFunc에서는 상한 체크를 하지 않습니다.
        // 상한은 TryGet()에서 막아서 createFunc가 호출되지 않게 합니다.
        var p = Instantiate(projectilePrefab, container);
        p.gameObject.SetActive(false);

        // 투사체가 자기 자신을 풀로 되돌릴 수 있도록
        p.SetPool(this);

        _createdCount++;
        return p;
    }

    private void OnGetFromPool(ProjectileHit2D p)
    {
        // (추가) Get 시점에 "안전 리셋"이 필요하면 여기서 처리
        // 현재는 Spawn에서 SetActive(true)/Launch를 하므로 최소한만 보장
        if (p == null) return;
        p.gameObject.SetActive(false); // 실수로 활성 상태로 들어온 경우 방지
    }

    private void OnReleaseToPool(ProjectileHit2D p)
    {
        if (p == null) return;

        p.gameObject.SetActive(false);
        // (중요) 풀로 돌아온 투사체는 container 아래로 이동시켜서 계층 정리
        p.transform.SetParent(container, false);
    }

    private void OnDestroyPooled(ProjectileHit2D p)
    {
        if (p == null) return;
        Destroy(p.gameObject);
    }

    // ----------------------------
    // 외부에서 사용하는 핵심 API
    // ----------------------------

    /// <summary>
    /// 탄을 꺼내서 -> 위치 세팅 -> 활성화 -> Launch까지 한 번에 처리
    /// </summary>
    public ProjectileHit2D Spawn(
        Vector3 position,
        float speed,
        bool rotateProjectile,
        float lifeTimeOverride = -1f)
    {
        var p = TryGet();              // (변경) _pool.Get() 직접 호출 대신 TryGet()
        if (p == null) return null;

        // 발사 중인 투사체는 container 영향 제거
        //p.transform.SetParent(null, true);

        p.transform.position = position;
        p.gameObject.SetActive(true);

        return p;
    }

    /// <summary>
    /// 풀에서 투사체를 하나 꺼냅니다.
    /// - 비활성 재고가 있으면 Get()
    /// - 없으면 expandable/maxSize(총 생성 상한) 조건에 따라 생성 or null
    /// </summary>
    private ProjectileHit2D TryGet()
    {
        if (m_pool == null) return null;

        // 비활성 재고가 있으면 바로 가져오기
        if (m_pool.CountInactive > 0)
            return m_pool.Get();

        // 재고가 없을 때 확장 금지면 실패
        if (!expandable) return null;

        // 기존 코드 의도 유지: "총 생성 상한"으로 maxSize 사용
        if (_createdCount >= maxSize) return null;

        // 생성 허용
        return m_pool.Get();
    }

    /// <summary>
    /// 사용이 끝난 탄을 풀로 되돌립니다.
    /// (투사체 스크립트에서 충돌/수명 종료 시 호출)
    /// </summary>
    public void Release(ProjectileHit2D projectile)
    {
        if (projectile == null) return;

        m_pool.Release(projectile); // (변경) Queue.Enqueue 대신 ObjectPool.Release
    }
}