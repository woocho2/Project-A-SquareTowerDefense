using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public enum EnemyType
{
    Normal,
    Speed,
    Depend,
    SpecialBoss,
    Boss
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Pool & Spawn Reference")]
    [SerializeField] private EnemyObjectPool2D[] m_enemyPool;  // 적 오브젝트 풀 참조
    [SerializeField] private Transform m_spawnPoint;           // 적이 생성될 시작 위치

    [Header("Wave Base Settings")]
    [SerializeField] private float m_waveDuration = 50;
    [SerializeField] private int m_enemiesPerNormalWave = 30;  // 웨이브당 기본 소환 마리 수
    [SerializeField] private float m_baseSpawnInterval = 1.5f; // 기본 소환 간격
    [SerializeField] private int m_currentWave = 1;            // 현재 진행 중인 웨이브 인덱스
    [SerializeField] Button btn_specialBoss;
    private int m_activeEnemyCount = 0;
    private Coroutine m_waveCoroutine;

    public float currentWaveTimer { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 이미 다른 인스턴스가 존재하는 경우, 현재 게임 오브젝트를 파괴하여 싱글톤 패턴을 유지합니다.   
            Destroy(gameObject);
            return;
        }

        // 현재 인스턴스를 싱글톤 인스턴스로 설정합니다.
        Instance = this;

        // 씬이 변경되어도 이 게임 오브젝트가 파괴되지 않도록 설정합니다.
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (m_enemyPool == null || m_spawnPoint == null)
        {
            Debug.LogError("[WaveManager] 필수 컴포넌트(Pool 또는 SpawnPoint)가 누락되었습니다.");
            return;
        }

        if (btn_specialBoss != null)
        {
            btn_specialBoss.onClick.AddListener(SpawnSpecialBoss);
            btn_specialBoss.gameObject.SetActive(false);
        }

        m_waveCoroutine = StartCoroutine(WaveLoopRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (m_waveCoroutine != null)
        {
            StopCoroutine(m_waveCoroutine);
        }
    }

    private IEnumerator WaveLoopRoutine()
    {
        while (m_currentWave <= 40)
        {
            int subWave = m_currentWave % 10;
            if (subWave == 0) subWave = 10;

            if (btn_specialBoss != null)
            {
                if (m_currentWave == 5 || m_currentWave == 15 || m_currentWave == 25 || m_currentWave == 35)
                {
                    btn_specialBoss.gameObject.SetActive(true);
                    btn_specialBoss.interactable = true;
                }

                if (m_currentWave == 8 || m_currentWave == 18 || m_currentWave == 28 || m_currentWave == 388)
                {
                    btn_specialBoss.gameObject.SetActive(false);
                    btn_specialBoss.interactable = false;
                }
            }

            float currentHPMultiplier = 1f;
            float currentDefendMultiplier = 1f;

            switch (subWave)
            {
                case 1: currentHPMultiplier = 1.0f; currentDefendMultiplier = 1.0f; break;
                case 2: currentHPMultiplier = 1.1f; currentDefendMultiplier = 1.0f; break;
                case 3: currentHPMultiplier = 1.25f; currentDefendMultiplier = 1.2f; break;
                case 4: currentHPMultiplier = 1.45f; currentDefendMultiplier = 1.6f; break;
                case 5: currentHPMultiplier = 1.75f; currentDefendMultiplier = 2.0f; break; // 5라운드에서 2배로 점프
                case 6: currentHPMultiplier = 2.0f; currentDefendMultiplier = 2.4f; break;
                case 7: currentHPMultiplier = 2.25f; currentDefendMultiplier = 2.6f; break;
                case 8: currentHPMultiplier = 2.45f; currentDefendMultiplier = 2.9f; break;
                case 9: currentHPMultiplier = 2.7f; currentDefendMultiplier = 3.5f; break;
                case 10: currentHPMultiplier = 1.0f; currentDefendMultiplier = 1.0f; break; // 10라운드 보스 체력 기준
            }

            currentHPMultiplier *= m_currentWave switch
            {
                > 30 => 8.0f, // 30웨이브 초과 시 8배
                > 20 => 4.0f, // 20웨이브 초과 시 4배 (단, 30웨이브 이하여야 함)
                > 10 => 2.0f, // 10웨이브 초과 시 2배 (단, 20웨이브 이하여야 함)
                _ => 1.0f     // 10웨이브 이하일 경우 배율을 곱하지 않음 (1.0배)
            };

            currentDefendMultiplier *= m_currentWave switch
            {
                > 30 => 2.5f, // 30웨이브 초과 시 2.5배
                > 20 => 1.7f, // 20웨이브 초과 시 1.7배
                > 10 => 1.2f, // 10웨이브 초과 시 1.2배
                _ => 1.0f     // 기본 방어력 배율 유지
            };

            int spawnCount = (subWave == 10) ? 1 : m_enemiesPerNormalWave;

            StartCoroutine(SpawnEnemiesRoutine(spawnCount, currentHPMultiplier, currentDefendMultiplier));

            currentWaveTimer = m_waveDuration;

            while (currentWaveTimer > 0)
            {
                currentWaveTimer -= Time.deltaTime;
                yield return null;
            }

            if (m_currentWave % 2 == 0)
            {
                CurrencyManager.Instance.AddGem(1);
            }
                        
            m_currentWave++;
            CurrencyManager.Instance.AddGold((m_currentWave-1) * 100);
            Debug.Log($"{m_currentWave - 1}Wave 클리어! {(m_currentWave - 1) * 10} 골드 지급");
        }
    }



    private IEnumerator SpawnEnemiesRoutine(int spawnCount, float hpMulti, float DependMulti)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            EnemyType currentEnemyType = DetermineEnemyType(m_currentWave, i);
            int poolIndex = (int)currentEnemyType;

            if (poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
            {
                Debug.LogError($"[WaveManager] {currentEnemyType}에 해당하는 오브젝트 풀이 배열에 없습니다!");
                yield break;
            }

            EnemyObjectPool2D targetPool = m_enemyPool[poolIndex];
            targetPool.Spawn(m_spawnPoint.position, hpMulti, DependMulti);

            m_activeEnemyCount++;

            yield return new WaitForSeconds(m_baseSpawnInterval);
        }
    }

    private EnemyType DetermineEnemyType(int wave, int spawnIndex)
    {
        int subWave = wave % 10;
        if (subWave == 0) subWave = 10;

        switch (subWave)
        {
            case 1:
                return EnemyType.Normal;
            case 2:
                return EnemyType.Normal;
            case 3:
                return (spawnIndex > 10) ? EnemyType.Normal : EnemyType.Speed;
            case 4:
                return (spawnIndex > 10) ? EnemyType.Normal : EnemyType.Depend;
            case 5:
                return (spawnIndex % 2 == 0) ? EnemyType.Speed : EnemyType.Depend;
            case 6:
                return EnemyType.Normal;
            case 7:
                return EnemyType.Normal;
            case 8:
                return (spawnIndex > 10) ? EnemyType.Normal : EnemyType.Speed;
            case 9:
                return (spawnIndex > 10) ? EnemyType.Normal : EnemyType.Depend;
            case 10:
                return EnemyType.Boss;
            default:
                return EnemyType.Normal;
        }
    }

    private void SpawnSpecialBoss()
    {
        if (btn_specialBoss != null)
        {
            btn_specialBoss.interactable = false;
            btn_specialBoss.gameObject.SetActive(false);
        }

        int poolIndex = (int)EnemyType.SpecialBoss;

        if (poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
        {
            Debug.LogError("[WaveManager] Special Boss에 해당하는 오브젝트 풀이 배열에 없습니다.");
            return;
        }

        float specialHPMulti = m_currentWave switch
        {
            >= 15 and <= 17 => 2.0f, // 15 이상 17 이하 (15, 16, 17 웨이브)
            >= 25 and <= 27 => 4.0f, // 25 이상 27 이하 (25, 26, 27 웨이브)
            >= 35 and <= 37 => 8.0f, // 35 이상 37 이하 (35, 36, 37 웨이브)
            _ => 1.0f                // 해당 범위가 아닐 때의 기본 배율
        };

        float specialDefendMulti = m_currentWave switch
        {
            >= 15 and <= 17 => 1.2f,
            >= 25 and <= 27 => 1.5f,
            >= 35 and <= 37 => 3.0f,
            _ => 1.0f
        };

        EnemyObjectPool2D targetPool = m_enemyPool[poolIndex];
        targetPool.Spawn(m_spawnPoint.position, specialHPMulti, specialDefendMulti);

        m_activeEnemyCount++;

        Debug.Log($"[WaveManager] {m_currentWave}웨이브 Special Boss 출현!");


    }

    public int GetEnemyCount()
    {
        return m_activeEnemyCount;
    }

    public int GetWave()
    {
        return m_currentWave;
    }

    public void OnEnemyDied(bool isBoss = false)
    {
        m_activeEnemyCount--;

        if (m_activeEnemyCount < 0)
        {
            m_activeEnemyCount = 0;
        }

        if (isBoss && m_currentWave >= 40)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameClear();

                if (m_waveCoroutine != null)
                {
                    StopCoroutine(m_waveCoroutine);
                }

                Debug.Log("최종 40웨이브 Boss 처치! 승리하셨습니다.");
            }
        }
    }
}
