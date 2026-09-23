using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 문양별로 공유하는 투사체 풀을 생성하고 투사체 인스턴스를 제공합니다.
/// 활성 투사체 수는 GameManager가 공격 연출 종료를 기다릴 때 사용합니다.
/// </summary>
public class GlobalProjectileManager : MonoBehaviour
{
    #region Singleton and Pool Settings

    public static GlobalProjectileManager Instance { get; private set; }

    private readonly Dictionary<int, ProjectileObjectPool2D> m_poolDictionary = new Dictionary<int, ProjectileObjectPool2D>();

    [Header("Global Pool Settings")]
    [Tooltip("각 투사체 풀의 기본 사전 생성 개수")]
    [SerializeField] private int m_defaultInitialSize = 20;

    [Tooltip("각 투사체 풀의 최대 보관 개수 상한")]
    [SerializeField] private int m_defaultMaxSize = 200;

    [Tooltip("모든 투사체 프리팹의 원본 크기에 곱하는 전역 배율입니다.")]
    [SerializeField, Range(0.1f, 2f)] private float m_projectileScaleMultiplier = 0.5f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // TowerManager가 CSV 데이터를 로드한 뒤 투사체 프리팹 목록으로 풀을 구성합니다.
        InitializeProjectileDatabase();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Pool Initialization

    public void InitializeProjectileDatabase()
    {
        TowerData[] allTowers = TowerManager.Instance.GetTowerDataArray();

        if (allTowers == null || allTowers.Length == 0) return;

        foreach (var tower in allTowers)
        {
            if (tower.projectilePrefab == null) continue;

            // 티어 자리만 제거하여 같은 문양의 모든 티어가 하나의 풀을 공유합니다. (1101 -> 101)
            int sharedKey = tower.towerID % 1000;

            if (!m_poolDictionary.ContainsKey(sharedKey))
            {
                GameObject poolObj = new GameObject($"Pool_{sharedKey}_Proj");
                poolObj.transform.SetParent(this.transform);

                ProjectileObjectPool2D newPool = poolObj.AddComponent<ProjectileObjectPool2D>();

                ProjectileHit2D hitPrefab = tower.projectilePrefab.GetComponent<ProjectileHit2D>();

                if (hitPrefab != null)
                {
                    newPool.InitPool(hitPrefab, m_defaultInitialSize, m_defaultMaxSize, true, poolObj.transform);
                    m_poolDictionary.Add(sharedKey, newPool);
                }
            }
        }
        Debug.Log($"[GlobalProjectileManager] 총 {m_poolDictionary.Count}개의 투사체 오브젝트 풀 생성 완료.");
    }

    #endregion

    #region Projectile Access

    public ProjectileHit2D SpawnProjectile(int towerID, Vector3 position, float speed, bool rotateProjectile)
    {
        int sharedKey = towerID % 1000;

        if (m_poolDictionary.TryGetValue(sharedKey, out ProjectileObjectPool2D targetPool))
        {
            ProjectileHit2D projectile = targetPool.Spawn(position);
            projectile?.SetVisualScale(m_projectileScaleMultiplier);
            return projectile;
        }

        Debug.LogError($"[GlobalProjectileManager] ID가 {sharedKey}인 투사체 풀을 찾을 수 없습니다.");
        return null;
    }

    #endregion

    #region Active Projectile Queries

    public bool HasActiveProjectiles()
    {
        return GetActiveProjectileCount() > 0;
    }

    public int GetActiveProjectileCount()
    {
        return FindObjectsByType<ProjectileHit2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    #endregion
}
