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

    // WaveManager는 위치와 배율만 전달
    public EnemyHealthController Spawn(Vector3 position, float hpMultiplier, float defendMultiplier)
    {
        if (m_enemyData == null)
        {
            Debug.LogError($"[EnemyObjectPool2D] {gameObject.name}에 EnemyData가 없어 스폰을 중단합니다.");
            return null;
        }

        var health = TryGet();
        if (health == null) return null;

        health.transform.position = position;
        health.transform.rotation = Quaternion.identity;

        if (health.TryGetComponent<EnemyMovementController>(out var movement))
        {
            movement.InitMovement(m_enemyData.Speed);
        }

        if (health.TryGetComponent<EnemyDebuffController>(out var debuff))
        {
            debuff.ClearAllDebuffs();
        }

        float finalMaxHP = m_enemyData.MaxHP * hpMultiplier;
        float finalDefend = m_enemyData.Defend * defendMultiplier;
        health.InitHealth(finalMaxHP, finalDefend, m_poolEnemyType);

        health.gameObject.SetActive(true);
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