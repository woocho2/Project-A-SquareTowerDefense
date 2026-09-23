using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 구성, 적 소환 순서, 웨이브별 능력치 배율과 중간 보스 소환 상태를 관리합니다.
/// 실제 10턴 진행 순서는 GameManager가 담당합니다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    #region Singleton and Inspector

    public static WaveManager Instance;

    [Header("Pool Reference")]
    [SerializeField] private EnemyObjectPool2D[] m_enemyPool;  // 0=Normal, 1=Speed, 2=Depend, 3=MiddleBoss, 4=Boss
    [SerializeField] private TilePath m_tilePath;                // 타일 경로 참조

    [Header("Wave Base Settings")]
    [SerializeField] private int m_currentWave = 1;

    #endregion

    #region Runtime State and Public Read-only State

    private int m_waveSpawnedCount = 0;
    private bool m_middleBossSpawned;

    public event Action MiddleBossSummonableChanged;

    public int CurrentWave => m_currentWave;
    public int CurrentCycle => Mathf.Clamp(((m_currentWave - 1) / 10) + 1, 1, 4);
    public int CurrentSubWave => ((m_currentWave - 1) % 10) + 1;
    public bool CanSummonMiddleBoss => IsMiddleBossSummonWindow(m_currentWave) && !m_middleBossSpawned;

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
        if (m_enemyPool == null || m_tilePath == null)
        {
            Debug.LogError("[WaveManager] 필수 컴포넌트(Pool 또는 TilePath)가 누락되었습니다.");
            return;
        }

        MiddleBossSummonableChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Wave Composition and Spawning

    /// <summary>
    /// 웨이브의 첫 5턴 동안 호출됩니다.
    /// 현재 사이클에 맞는 수의 적을 소환 대기열에서 꺼내 반환합니다.
    /// </summary>
    public int SpawnWaveEnemiesForTurn()
    {
        List<EnemyType> waveEnemies = GetWaveEnemyComposition(m_currentWave);
        if (m_waveSpawnedCount >= waveEnemies.Count || m_enemyPool == null || m_tilePath == null) return 0;

        int spawnCount = CurrentSubWave == 10 ? 1 : CurrentCycle * 2;
        int spawnedCount = 0;
        GetEnemyStatMultipliers(out float hpMulti, out float defendMulti);

        for (int i = 0; i < spawnCount && m_waveSpawnedCount < waveEnemies.Count; i++)
        {
            EnemyType currentEnemyType = waveEnemies[m_waveSpawnedCount];
            int poolIndex = (int)currentEnemyType;

            if (poolIndex < 0 || poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
            {
                Debug.LogError($"[WaveManager] {currentEnemyType}에 해당하는 오브젝트 풀이 없습니다!");
                break;
            }

            m_enemyPool[poolIndex].Spawn(m_tilePath.GetWorldPosition(0), hpMulti, defendMulti, m_tilePath);

            m_waveSpawnedCount++;
            spawnedCount++;
        }

        return spawnedCount;
    }

    /// <summary>
    /// 10웨이브 주기의 적 구성을 생성합니다.
    /// 10·20·30·40웨이브는 보스 1마리로 고정합니다.
    /// </summary>
    private List<EnemyType> GetWaveEnemyComposition(int wave)
    {
        List<EnemyType> enemies = new List<EnemyType>();
        if (wave < 1 || wave > 40) return enemies;

        int baseWave = wave % 10;
        int cycle = ((wave - 1) / 10) + 1;
        

        if (baseWave == 0)
        {
            enemies.Add(EnemyType.Boss);
            return enemies;
        }

        int turnPerWave = 5;
        int enemiesPerTurn = 2 * cycle;

        int singleTypeCount = turnPerWave * enemiesPerTurn;
        int mixedTypeCount = turnPerWave * cycle;

        switch (baseWave)
        {
            case 1:
            case 2:
            case 3:
                AddEnemies(enemies, EnemyType.Normal, singleTypeCount);
                break;

            case 4:
            case 5:
                AddMixedEnemies(enemies, EnemyType.Normal, EnemyType.Speed, mixedTypeCount);
                break;

            case 6:
            case 7:
                AddMixedEnemies(enemies, EnemyType.Normal, EnemyType.Depend, mixedTypeCount);
                break;

            case 8:
            case 9:
                AddMixedEnemies(enemies, EnemyType.Speed, EnemyType.Depend, mixedTypeCount);
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

    private static void AddMixedEnemies(List<EnemyType> enemies, EnemyType firstType, EnemyType secondType, int countPerType)
    {
        for (int i = 0; i < countPerType; i++)
        {
            enemies.Add(firstType);
            enemies.Add(secondType);
        }
    }

    #endregion

    #region Enemy Stat Scaling

    /// <summary>
    /// 일반 적에게 적용할 최종 배율을 계산합니다.
    /// 최종 배율은 사이클 내부 단계 배율과 10웨이브 단위 사이클 배율의 곱입니다.
    /// </summary>
    private void GetEnemyStatMultipliers(out float hpMulti, out float defendMulti)
    {
        int subWave = CurrentSubWave;
      
        float subWaveHp = subWave switch
        {
            1 => 1.0f,
            2 => 1.5f,
            3 => 2.0f,
            4 => 2.5f,
            5 => 3.0f,
            6 => 3.5f,
            7 => 4.0f,
            8 => 4.5f,
            9 => 5.0f,
            10 => 1.0f,
            _ => 1.0f
        };

        float subWaveDefend = subWave switch
        {
            1 => 1.0f,
            2 => 1.0f,
            3 => 1.0f,
            4 => 1.0f,
            5 => 1.0f,
            6 => 2.0f,
            7 => 2.0f,
            8 => 2.0f,
            9 => 2.0f,
            10 => 1.0f,
            _ => 1.0f
        };

        GetCycleMultipliers(m_currentWave, out float cycleHp, out float cycleDefend);
        
        hpMulti = subWaveHp * cycleHp;
        defendMulti = subWaveDefend * cycleDefend;
    }

    private static void GetCycleMultipliers(int wave, out float hpMulti, out float defendMulti)
    {
        hpMulti = wave switch
        {
            >30 => 8.0f,
            >20 => 4.0f,
            >10 => 2.0f,
            _ => 1.0f
        };

        defendMulti = wave switch
        {
            >30 => 4.0f,
            >20 => 3.0f,
            >10 => 2.0f,
            _ => 1.0f
        };
    }

    #endregion

    #region Middle Boss

    private static bool IsMiddleBossSummonWindow(int wave)
    {
        return (wave >= 5 && wave <= 7)
            || (wave >= 15 && wave <= 17)
            || (wave >= 25 && wave <= 27)
            || (wave >= 35 && wave <= 37);
    }

    public bool TrySpawnMiddleBoss()
    {
        if (!CanSummonMiddleBoss) return false;

        int poolIndex = (int)EnemyType.MiddleBoss;
        if (m_enemyPool == null || poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null) return false;

        GetCycleMultipliers(m_currentWave, out float middleBossHPMulti, out float middleBossDefendMulti);
        
        EnemyObjectPool2D targetPool = m_enemyPool[poolIndex];
        targetPool.Spawn(m_tilePath.GetWorldPosition(0), middleBossHPMulti, middleBossDefendMulti, m_tilePath);
        m_middleBossSpawned = true;
        MiddleBossSummonableChanged?.Invoke();
        return true;
    }

    #endregion

    #region Wave Progression and Game Clear

    /// <summary>
    /// 10번째 적 턴이 끝난 뒤 다음 웨이브 상태로 전환합니다.
    /// </summary>
    public void NextWave()
    {
        m_currentWave++;
        m_waveSpawnedCount = 0;

        if (m_currentWave == 5 || m_currentWave == 15 || m_currentWave == 25 || m_currentWave == 35)
        {
            m_middleBossSpawned = false;
        }

        MiddleBossSummonableChanged?.Invoke();
    }

    public void OnEnemyDied(bool isBoss = false)
    {
        if (isBoss && m_currentWave >= 40)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameClear();
                Debug.Log("최종 40웨이브 Boss 처치! 승리하셨습니다.");
            }
        }
    }

    #endregion
}
