using System.Collections.Generic;
using UnityEngine;


public class GlobalProjectileManager : MonoBehaviour
{
    public static GlobalProjectileManager Instance { get; private set; }

    [Header("All Projectiles Setup")]
    [Tooltip("게임 내에서 사용할 모든 투사체 데이터(ProjectileData SO)를 여기에 등록하세요.")]
    [SerializeField] private List<ProjectileData> m_allProjectiles;

    private Dictionary<int, ProjectileData> m_projectileDB = new Dictionary<int, ProjectileData>();
    private Dictionary<int, ProjectileObjectPool2D> m_poolDictionary = new Dictionary<int, ProjectileObjectPool2D>();

    [Header("Global Pool Settings")]
    [Tooltip("각 투사체 풀의 기본 사전 생성 개수")]
    [SerializeField] private int m_defaultInitialSize = 20;

    [Tooltip("각 투사체 풀의 최대 보관 개수 상한")]
    [SerializeField] private int m_defaultMaxSize = 200;

    private void Awake()
    {
        if (m_allProjectiles == null || m_allProjectiles.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeProjectileDatabase();

    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeProjectileDatabase()
    {
        if (m_allProjectiles == null || m_allProjectiles.Count == 0)
        {

            Debug.LogWarning("[GlobalProjectileManager] 등록된 투사체 에셋이 없습니다.");
            return;
        }

        foreach (var proj in m_allProjectiles)
        {
            if (proj == null) continue;

            int key = proj.projectileID;

            if (!m_projectileDB.ContainsKey(key))
            {
                m_projectileDB.Add(key, proj);
            }
            else
            {
                Debug.LogWarning($"[GlobalProjectileManager] 중복된 총알 키가 발견되었습니다: {key}");
            }

            if (!m_poolDictionary.ContainsKey(proj.projectileID))
            {
                GameObject poolObj = new GameObject($"Pool_{proj.projectileName}");
                poolObj.transform.SetParent(this.transform);

                ProjectileObjectPool2D newPool = poolObj.AddComponent<ProjectileObjectPool2D>();

                ProjectileHit2D hitPrefab = proj.prefab.GetComponent<ProjectileHit2D>();
                if (hitPrefab != null)
                {
                    newPool.InitPool(hitPrefab, m_defaultInitialSize, m_defaultMaxSize, true, poolObj.transform);
                    m_poolDictionary.Add(proj.projectileID, newPool);
                }
                else
                {
                    Debug.LogError($"[GlobalProjectileManager] {proj.projectileName}의 프리팹에 ProjectileHit2D 컴포넌트가 없습니다!");
                }
            }
        }
        Debug.Log($"[GlobalProjectileManager] 총 {m_projectileDB.Count}개의 투사체 데이터베이스 로드 완료.");
    }

    public ProjectileData GetMatchingProjectile(int towerID)
    {
        int searchKey = towerID;

        if (m_projectileDB.TryGetValue(searchKey, out ProjectileData matchedData))
        {
            return matchedData;
        }

        Debug.LogError($"[데이터 누락] {searchKey} 조합에 해당하는 총알 에셋이 매니저에 없습니다!");
        return null;
    }

    public ProjectileHit2D SpawnProjectile(int projectileID, Vector3 position, float speed, bool rotateProjectile)
    {
        if (m_poolDictionary.TryGetValue(projectileID, out ProjectileObjectPool2D targetPool))
        {
            return targetPool.Spawn(position, speed, rotateProjectile);
        }

        Debug.LogError($"[GlobalProjectileManager] ID가 {projectileID}인 투사체 풀을 찾을 수 없습니다.");
        return null;
    }
}