using System.Collections.Generic;
using UnityEngine;

public class GlobalProjectileManager : MonoBehaviour
{
    public static GlobalProjectileManager Instance { get; private set; }

    private Dictionary<int, ProjectileObjectPool2D> m_poolDictionary = new Dictionary<int, ProjectileObjectPool2D>();

    [Header("Global Pool Settings")]
    [Tooltip("각 투사체 풀의 기본 사전 생성 개수")]
    [SerializeField] private int m_defaultInitialSize = 20;

    [Tooltip("각 투사체 풀의 최대 보관 개수 상한")]
    [SerializeField] private int m_defaultMaxSize = 200;

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
        // [핵심] TowerManager의 Start() (CSV 로드)가 끝난 직후에 풀을 생성해야 합니다.
        // 스크립트 실행 순서(Script Execution Order)를 TowerManager보다 늦게 설정하거나 Start에서 호출하십시오.
        InitializeProjectileDatabase();
    }

    public void InitializeProjectileDatabase()
    {
        // TowerManager에 로드된 모든 타워 데이터를 가져옵니다.
        TowerData[] allTowers = TowerManager.Instance.GetTowerDataArray(); // TowerManager에 이 함수(배열 반환)를 하나 만들어주셔야 합니다.

        if (allTowers == null || allTowers.Length == 0) return;

        foreach (var tower in allTowers)
        {
            // 투사체가 없는 타워라면 건너뜁니다.
            if (tower.projectilePrefab == null) continue;

            // 천의 자리를 자른 공유 ID 생성 (1101 -> 101)
            int sharedKey = tower.towerID % 1000;

            // 공유 ID로 이미 풀(Pool)이 만들어져 있다면 중복 생성하지 않고 넘어갑니다.
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

    public ProjectileHit2D SpawnProjectile(int towerID, Vector3 position, float speed, bool rotateProjectile)
    {
        // 발사 요청이 들어왔을 때도 천의 자리를 떼어내고 공유 풀에서 투사체를 꺼냅니다.
        int sharedKey = towerID % 1000;

        if (m_poolDictionary.TryGetValue(sharedKey, out ProjectileObjectPool2D targetPool))
        {
            return targetPool.Spawn(position);
        }

        Debug.LogError($"[GlobalProjectileManager] ID가 {sharedKey}인 투사체 풀을 찾을 수 없습니다.");
        return null;
    }
}