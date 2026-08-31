using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Pool & Spawn Reference")]
    [SerializeField] private EnemyObjectPool2D[] m_enemyPool;  // 인덱스: 0=Normal, 1=Speed, 2=Depend, 3=SpecialBoss, 4=Boss
    [SerializeField] private Transform m_spawnPoint;

    [Header("Wave Base Settings")]
    [SerializeField] private float m_waveDuration = 50f;
    [SerializeField] private int m_enemiesPerNormalWave = 30;
    [SerializeField] private float m_baseSpawnInterval = 1.5f;
    [SerializeField] private int m_currentWave = 1;
    [SerializeField] private Button btn_specialBoss;

    private int m_activeEnemyCount = 0;
    private Coroutine m_waveCoroutine;

    public float currentWaveTimer { get; private set; }

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
        if (Instance == this) Instance = null;
        if (m_waveCoroutine != null) StopCoroutine(m_waveCoroutine);
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

                if (m_currentWave == 8 || m_currentWave == 18 || m_currentWave == 28 || m_currentWave == 38)
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
                case 5: currentHPMultiplier = 1.75f; currentDefendMultiplier = 2.0f; break;
                case 6: currentHPMultiplier = 2.0f; currentDefendMultiplier = 2.4f; break;
                case 7: currentHPMultiplier = 2.25f; currentDefendMultiplier = 2.6f; break;
                case 8: currentHPMultiplier = 2.45f; currentDefendMultiplier = 2.9f; break;
                case 9: currentHPMultiplier = 2.7f; currentDefendMultiplier = 3.5f; break;
                case 10: currentHPMultiplier = 1.0f; currentDefendMultiplier = 1.0f; break;
            }

            currentHPMultiplier *= m_currentWave switch
            {
                > 30 => 8.0f,
                > 20 => 4.0f,
                > 10 => 2.0f,
                _ => 1.0f
            };

            currentDefendMultiplier *= m_currentWave switch
            {
                > 30 => 2.5f,
                > 20 => 1.7f,
                > 10 => 1.2f,
                _ => 1.0f
            };

            int spawnCount = (subWave == 10) ? 1 : m_enemiesPerNormalWave;

            StartCoroutine(SpawnEnemiesRoutine(spawnCount, currentHPMultiplier, currentDefendMultiplier));

            currentWaveTimer = m_waveDuration;

            while (currentWaveTimer > 0f)
            {
                currentWaveTimer -= Time.deltaTime;
                yield return null;
            }

            if (m_currentWave % 2 == 0)
            {
                CurrencyManager.Instance?.AddGem(1);
            }

            m_currentWave++;
            CurrencyManager.Instance?.AddGold((m_currentWave - 1) * 100);
            Debug.Log($"{m_currentWave - 1}Wave 클리어! {(m_currentWave - 1) * 100} 골드 지급");
        }
    }

    private IEnumerator SpawnEnemiesRoutine(int spawnCount, float hpMulti, float defendMulti)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            EnemyType currentEnemyType = DetermineEnemyType(m_currentWave, i);
            int poolIndex = (int)currentEnemyType;

            if (poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
            {
                Debug.LogError($"[WaveManager] {currentEnemyType}에 해당하는 오브젝트 풀이 없습니다!");
                yield break;
            }

            EnemyObjectPool2D targetPool = m_enemyPool[poolIndex];
            targetPool.Spawn(m_spawnPoint.position, hpMulti, defendMulti);

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
            case 2:
            case 6:
            case 7:
                return EnemyType.Normal;
            case 3:
            case 8:
                return (spawnIndex > 10) ? EnemyType.Normal : EnemyType.Speed;
            case 4:
            case 9:
                return (spawnIndex > 10) ? EnemyType.Normal : EnemyType.Depend;
            case 5:
                return (spawnIndex % 2 == 0) ? EnemyType.Speed : EnemyType.Depend;
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
            >= 15 and <= 17 => 2.0f,
            >= 25 and <= 27 => 4.0f,
            >= 35 and <= 37 => 8.0f,
            _ => 1.0f
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

    public int GetEnemyCount() => m_activeEnemyCount;
    public int GetWave() => m_currentWave;

    public void OnEnemyDied(bool isBoss = false)
    {
        m_activeEnemyCount--;

        if (m_activeEnemyCount < 0) m_activeEnemyCount = 0;

        if (isBoss && m_currentWave >= 40)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameClear();
                if (m_waveCoroutine != null) StopCoroutine(m_waveCoroutine);
                Debug.Log("최종 40웨이브 Boss 처치! 승리하셨습니다.");
            }
        }
    }
}