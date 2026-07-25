using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    [SerializeField] EffectLibrary m_effectLibrary;

    [Serializable]

    public class PoolOverride
    {
        public int effectID;
        public int poolSize = 10;
        public int maxSize = 50;

        public bool prewarm = false;
    }

    [SerializeField] List<PoolOverride> m_poolOverrides = new List<PoolOverride>();

    [Header("Hierarchy ManageMent")]
    [SerializeField] private Transform m_effectContainer;

    [SerializeField] int m_poolSize = 10;
    [SerializeField] int m_maxSize = 50;

    private Dictionary<int, ObjectPool<GameObject>> m_pools = new Dictionary<int, ObjectPool<GameObject>>();

    private Dictionary<int, PoolOverride> m_overrideMap = new Dictionary<int, PoolOverride>();

    private readonly Dictionary<int, ObjectPool<GameObject>> m_poolByInstanceID = new Dictionary<int, ObjectPool<GameObject>>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);

        m_overrideMap = new Dictionary<int, PoolOverride>();

        foreach (var pool in m_poolOverrides)
        {
            m_overrideMap[pool.effectID] = pool;
        }

        if (m_effectLibrary == null)
        {
            Debug.LogError("EffectManager.Awake: m_effectLibrary가 할당되지 않았습니다. EffectLibrary를 할당하세요.");
            return;
        }

        foreach (var kv in m_overrideMap)
        {
            if (kv.Value.prewarm)
            {
                var pool = GetOrCreatePool(kv.Key);

                Prewarm(pool, kv.Value.poolSize);
            }
        }

    }
    private ObjectPool<GameObject> GetOrCreatePool(int effectID)
    {
        if (m_pools.TryGetValue(effectID, out var outpool))
        {
            return outpool;
        }

        if (m_effectLibrary == null) return null;

        var prefab = m_effectLibrary.GetEffectPrefab(effectID);

        if (prefab == null)
        {
            Debug.LogWarning($"EffectManager.GetOrCreatePool: EffectLibrary에서 key '{effectID}'에 해당하는 프리팹을 찾을 수 없습니다. 풀을 생성할 수 없습니다.");
            return null;
        }

        int poolSize = m_poolSize;
        int maxSize = m_maxSize;

        if (m_overrideMap.TryGetValue(effectID, out var overrideInfo))
        {
            poolSize = overrideInfo.poolSize;
            maxSize = overrideInfo.maxSize;
        }

        ObjectPool<GameObject> pool = null;

        pool = new ObjectPool<GameObject>(
            createFunc: () => CreateEffectInstance(effectID, prefab, pool),
            actionOnGet: OnGetEffect,
            actionOnRelease: OnReleaseEffect,
            actionOnDestroy: OnDestroyEffect,
            collectionCheck: false,
            defaultCapacity: poolSize,
            maxSize: maxSize
            );

        m_pools[effectID] = pool;

        return pool;
    }

    void Prewarm(ObjectPool<GameObject> pool, int count)
    {
        if (pool == null) return;

        if (count == 0) return;

        var temp = new List<GameObject>(count);

        for (int i = 0; i < count; i++)
        {
            temp.Add(pool.Get());
        }

        for (int i = 0; i < count; i++)
        {
            pool.Release(temp[i]);
        }
    }

    GameObject CreateEffectInstance(int effectID, GameObject prefab, ObjectPool<GameObject> pool)
    {
        if (prefab == null) return null;

        Transform parentTransform = m_effectContainer != null ? m_effectContainer : transform;

        var effect = Instantiate(prefab, parentTransform);
        effect.SetActive(false);
        m_poolByInstanceID[effect.GetInstanceID()] = pool;

        var autoOff = effect.GetComponent<EffectPool2D>();
        if (autoOff != null)
        {
            autoOff.SetPool(pool);
            autoOff.SetDestoryOnFinish(false);
        }
        return effect;
    }

    private void OnGetEffect(GameObject effect)
    {
        if (effect == null) return;
    }

    private void OnReleaseEffect(GameObject effect)
    {
        if (effect == null) return;
        effect.SetActive(false);

        Transform parentTransform = m_effectContainer != null ? m_effectContainer : transform;
        effect.transform.SetParent(parentTransform);        
    }

    private void OnDestroyEffect(GameObject effect)
    {
        if (effect == null) return;
        m_poolByInstanceID.Remove(effect.GetInstanceID());
        Destroy(effect);
    }

    public GameObject GetEffect(int effectID)
    {
        var pool = GetOrCreatePool(effectID);

        if (pool == null)
        {
            Debug.LogWarning($"EffectManager.GetEffect: '{effectID}'에 대한 풀을 가져오거나 생성할 수 없습니다. EffectLibrary에 해당 키가 있는지, 그리고 프리팹이 할당되어 있는지 확인하세요.");
            return null;
        }
        return pool.Get();
    }


    public void PlayEffect(int effectID, Vector3 position, Quaternion rotation, float scale = 3f)
    {
        var effect = GetEffect(effectID);

        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = rotation;

            effect.transform.localScale = new Vector3 (scale, scale, scale);
            effect.SetActive(true);
        }
    }
}

