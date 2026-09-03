using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("Pool Reference")]
    [SerializeField] private EnemyObjectPool2D[] m_enemyPool;  // 0=Normal, 1=Speed, 2=Depend, 3=SpecialBoss, 4=Boss
    [SerializeField] private TilePath m_tilePath;                // 타일 경로 참조

    [Header("Wave Base Settings")]
    [SerializeField] private int m_currentWave = 1;

    [SerializeField] private Button btn_specialBoss;

    private int m_activeEnemyCount = 0;
    private int m_spawnedEnemyCountInWave = 0;

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
        if (m_enemyPool == null || m_tilePath == null)
        {
            Debug.LogError("[WaveManager] 필수 컴포넌트(Pool 또는 TilePath)가 누락되었습니다.");
            return;
        }

        if (btn_specialBoss != null)
        {
            btn_specialBoss.onClick.AddListener(SpawnSpecialBoss);
            btn_specialBoss.gameObject.SetActive(false);
        }

        UpdateSpecialBossButtonUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 턴제 에너미 턴에서 호출될 몬스터 스폰 함수
    // 에너미 서브턴마다 호출됩니다. 현재 웨이브의 소환 대기열에서 적 1마리를 소환합니다.
    public bool SpawnNextWaveEnemyForTurn()
    {
        List<EnemyType> waveEnemies = GetWaveEnemyComposition(m_currentWave);
        if (m_spawnedEnemyCountInWave >= waveEnemies.Count) return false;

        EnemyType currentEnemyType = waveEnemies[m_spawnedEnemyCountInWave];
        int poolIndex = (int)currentEnemyType;

        if (poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
        {
            Debug.LogError($"[WaveManager] {currentEnemyType}에 해당하는 오브젝트 풀이 없습니다!");
            return false;
        }

        GetMultiplier(out float hpMulti, out float defendMulti);
        m_enemyPool[poolIndex].Spawn(m_tilePath.GetWorldPosition(0), hpMulti, defendMulti, m_tilePath);

        m_activeEnemyCount++;
        m_spawnedEnemyCountInWave++;
        return true;
    }

    /// <summary>
    /// 10웨이브 주기의 적 구성을 생성합니다.
    /// 10·20·30·40웨이브는 보스 1마리로 고정합니다.
    /// </summary>
    private List<EnemyType> GetWaveEnemyComposition(int wave)
    {
        List<EnemyType> enemies = new List<EnemyType>();
        if (wave < 1 || wave > 40) return enemies;

        int baseWave = ((wave - 1) % 10) + 1;
        int cycle = (wave - 1) / 10;

        if (baseWave == 10)
        {
            enemies.Add(EnemyType.Boss);
            return enemies;
        }

        int singleTypeCount = 4 + (cycle * 2);
        int mixedTypeCount = 2 + cycle;

        switch (baseWave)
        {
            case 1:
            case 2:
            case 3:
                AddEnemies(enemies, EnemyType.Normal, singleTypeCount);
                break;

            case 4:
            case 5:
                AddEnemies(enemies, EnemyType.Normal, mixedTypeCount);
                AddEnemies(enemies, EnemyType.Speed, mixedTypeCount);
                break;

            case 6:
            case 7:
                AddEnemies(enemies, EnemyType.Normal, mixedTypeCount);
                AddEnemies(enemies, EnemyType.Depend, mixedTypeCount);
                break;

            case 8:
            case 9:
                AddEnemies(enemies, EnemyType.Speed, mixedTypeCount);
                AddEnemies(enemies, EnemyType.Depend, mixedTypeCount);
                break;
        }

        return enemies;
    }

    private static void AddEnemies(List<EnemyType> enemies, EnemyType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            enemies.Add(type);
        }
    }
    // 웨이브별 스탯 배율 계산 로직
    public void GetMultiplier(out float hpMulti, out float defendMulti)
    {
        int subWave = m_currentWave % 10;
        if (subWave == 0) subWave = 10;

        hpMulti = subWave switch
        {
            1 => 1.0f,
            2 => 1.1f,
            3 => 1.25f,
            4 => 1.45f,
            5 => 1.75f,
            6 => 2.0f,
            7 => 2.25f,
            8 => 2.45f,
            9 => 2.7f,
            10 => 1.0f,
            _ => 1.0f
        };

        defendMulti = subWave switch
        {
            1 => 1.0f,
            2 => 1.0f,
            3 => 1.2f,
            4 => 1.6f,
            5 => 2.0f,
            6 => 2.4f,
            7 => 2.6f,
            8 => 2.9f,
            9 => 3.5f,
            10 => 1.0f,
            _ => 1.0f
        };

        hpMulti *= m_currentWave switch
        {
            > 30 => 8.0f,
            > 20 => 4.0f,
            > 10 => 2.0f,
            _ => 1.0f
        };

        defendMulti *= m_currentWave switch
        {
            > 30 => 2.5f,
            > 20 => 1.7f,
            > 10 => 1.2f,
            _ => 1.0f
        };
    }

    private void UpdateSpecialBossButtonUI()
    {
        if (btn_specialBoss == null) return;

        if (m_currentWave == 5 || m_currentWave == 15 || m_currentWave == 25 || m_currentWave == 35)
        {
            btn_specialBoss.gameObject.SetActive(true);
            btn_specialBoss.interactable = true;
        }
        else if (m_currentWave == 8 || m_currentWave == 18 || m_currentWave == 28 || m_currentWave == 38)
        {
            btn_specialBoss.gameObject.SetActive(false);
            btn_specialBoss.interactable = false;
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
        if (poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null) return;

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
        targetPool.Spawn(m_tilePath.GetWorldPosition(0), specialHPMulti, specialDefendMulti, m_tilePath);
        m_activeEnemyCount++;
    }

    public void NextWave()
    {
        m_currentWave++;
        m_spawnedEnemyCountInWave = 0;
        CurrencyManager.Instance?.AddGold((m_currentWave - 1) * 100);
        UpdateSpecialBossButtonUI();
    }

    public int GetEnemyCount() => m_activeEnemyCount;
    public int GetEnemiesPerWave() => GetWaveEnemyComposition(m_currentWave).Count;
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
                Debug.Log("최종 40웨이브 Boss 처치! 승리하셨습니다.");
            }
        }
    }
}