using UnityEngine;
using UnityEngine.Pool; // (추가) UnityEngine.Pool 사용
using System.Collections.Generic;


public class EnemyObjectPool2D : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private EnemyController enemyPrefab;


    [Header("Pool Settings")]
    [SerializeField] private int initialSize = 30;
    [SerializeField] private int maxSize = 101;
    [SerializeField] private bool expandable = true;

    [Header("Hierarchy")]
    [SerializeField] private Transform container;

    public Transform spawnPoint;


    private ObjectPool<EnemyController> m_pool;
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

        m_pool = new ObjectPool<EnemyController>(
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

        if (initialSize > maxSize)
        {
            Debug.LogWarning($"[ProjectilePool2D] initialSize({initialSize})가 maxSize({maxSize})보다 큽니다. maxSize까지만 Prewarm합니다.");
        }

        int count = Mathf.Min(initialSize, maxSize);

        var temp = new List<EnemyController>(count);
        for (int i = 0; i < count; i++)
        {
            var p = TryGet();
            if (p == null) break;
            temp.Add(p);
        }

        for (int i = 0; i < temp.Count; i++)
            m_pool.Release(temp[i]);
    }


    private EnemyController CreateNew()
    {
        var e = Instantiate(enemyPrefab, container);
        e.gameObject.SetActive(false);

        e.SetPool(this);

        _createdCount++;
        return e;
    }

    private void OnGetFromPool(EnemyController e)
    {
        if (e == null) return;
        e.gameObject.SetActive(false);
    }

    private void OnReleaseToPool(EnemyController e)
    {
        if (e == null) return;

        e.gameObject.SetActive(false);
        e.transform.SetParent(container, false);
    }

    private void OnDestroyPooled(EnemyController e)
    {
        if (e == null) return;
        Destroy(e.gameObject);
    }


    public EnemyController Spawn(Vector3 position, float hpMultiPlier, float dependMultiplier)
    {
        var e = TryGet();
        if (e == null) return null;

        e.ResetEnemy(position, hpMultiPlier, dependMultiplier);
        e.gameObject.SetActive(true);

        return e;
    }

    private EnemyController TryGet()
    {
        if (m_pool == null) return null;

        if (m_pool.CountInactive > 0)
            return m_pool.Get();

        if (!expandable) return null;

        if (_createdCount >= maxSize) return null;

        return m_pool.Get();
    }

    public void Release(EnemyController enemy)
    {
        if (enemy == null) return;

        m_pool.Release(enemy);
    }
}