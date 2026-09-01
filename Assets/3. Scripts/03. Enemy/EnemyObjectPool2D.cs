using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyObjectPool2D : MonoBehaviour
{
    [Header("Data & Prefab")]
    [SerializeField] private EnemyData m_enemyData;             // 이 풀이 스폰할 전용 ScriptableObject
    [SerializeField] private EnemyHealthController enemyPrefab;
    [SerializeField] private EnemyType m_poolEnemyType;         // 이 풀의 적 타입

    [Header("Pool Settings")]
    [SerializeField] private int initialSize = 30;
    [SerializeField] private int maxSize = 101;
    [SerializeField] private bool expandable = true;

    [Header("Hierarchy")]
    [SerializeField] private Transform container;

    private ObjectPool<EnemyHealthController> m_pool;
    private int _createdCount = 0;

    private void Awake()
    {
        if (container == null) container = transform;

        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemyPool2D] EnemyPrefab이 비어있습니다.");
            enabled = false;
            return;
        }

        if (m_enemyData == null)
        {
            Debug.LogError($"[EnemyPool2D] {gameObject.name}에 EnemyData ScriptableObject가 할당되지 않았습니다.");
        }

        m_pool = new ObjectPool<EnemyHealthController>(
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
        m_pool?.Dispose();
        m_pool = null;
    }

    private void Prewarm()
    {
        if (initialSize <= 0) return;

        int count = Mathf.Min(initialSize, maxSize);
        var temp = new List<EnemyHealthController>(count);

        for (int i = 0; i < count; i++)
        {
            var p = TryGet();
            if (p == null) break;
            temp.Add(p);
        }

        for (int i = 0; i < temp.Count; i++)
        {
            m_pool.Release(temp[i]);
        }
    }

    private EnemyHealthController CreateNew()
    {
        var e = Instantiate(enemyPrefab, container);
        e.gameObject.SetActive(false);
        e.SetPool(this);
        _createdCount++;
        return e;
    }

    private void OnGetFromPool(EnemyHealthController e)
    {
        if (e == null) return;
        e.gameObject.SetActive(false);
    }

    private void OnReleaseToPool(EnemyHealthController e)
    {
        if (e == null) return;
        e.gameObject.SetActive(false);
        e.transform.SetParent(container, false);
    }

    private void OnDestroyPooled(EnemyHealthController e)
    {
        if (e == null) return;
        Destroy(e.gameObject);
    }

    public EnemyHealthController Spawn(Vector3 position, float hpMultiplier, float defendMultiplier, TilePath tilePath)
    {
        if (m_enemyData == null)
        {
            Debug.LogError($"[EnemyObjectPool2D] {gameObject.name}에 EnemyData가 없어 스폰을 중단합니다.");
            return null;
        }

        var health = TryGet();
        if (health == null) return null;

        // 위치 지정 및 오브젝트 활성화 (누락되었던 부분)
        health.transform.position = position;
        health.gameObject.SetActive(true);

        // 1. 디버프 상태 초기화[cite: 22]
        if (health.TryGetComponent<EnemyDebuffController>(out var debuff))
        {
            debuff.ClearAllDebuffs();
        }

        // 2. 턴제 이동 데이터 초기화 (EnemyData 및 TilePath 전달)[cite: 22]
        if (health.TryGetComponent<EnemyMovementController>(out var movement))
        {
            movement.InitMovement(m_enemyData, tilePath);
            // 생성된 적을 EnemyManager에 등록[cite: 22]
            EnemyManager.Instance?.RegisterEnemy(movement);
        }

        // 3. 체력 초기화[cite: 22]
        float finalMaxHP = m_enemyData.MaxHP * hpMultiplier;
        float finalDefend = m_enemyData.Defend * defendMultiplier;
        health.InitHealth(finalMaxHP, finalDefend, m_poolEnemyType);

        return health;
    }

    private EnemyHealthController TryGet()
    {
        if (m_pool == null) return null;
        if (m_pool.CountInactive > 0) return m_pool.Get();
        if (!expandable || _createdCount >= maxSize) return null;
        return m_pool.Get();
    }

    public void Release(EnemyHealthController enemy)
    {
        if (enemy == null) return;
        m_pool.Release(enemy);
    }
}