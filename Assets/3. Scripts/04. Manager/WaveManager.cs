using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 구성(중간 보스·최종 보스 포함), 적 소환 순서, 웨이브별 능력치 배율과 처치 보상을 관리합니다.
/// 실제 10턴 진행 순서는 GameManager가 담당합니다.
/// </summary>
public class WaveManager : MonoBehaviour
{
    #region Singleton and Inspector

    public static WaveManager Instance;

    [Header("Pool Reference")]
    [SerializeField] private EnemyObjectPool2D[] m_enemyPool;  // 0=Normal, 1=Speed, 2=Depend, 3=MiddleBoss, 4=Boss

    [Header("Wave Base Settings")]
    [SerializeField] private int m_currentWave = 1;
    public int CurrentWave => m_currentWave;

    public event Action<int> CurrentWaveChanged;

    #endregion

    #region Runtime State and Public Read-only State

    private int m_waveSpawnedCount = 0;

    public int CurrentCycle => Mathf.Clamp(((m_currentWave - 1) / 10) + 1, 1, 4);
    public int CurrentSubWave => ((m_currentWave - 1) % 10) + 1;

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
        if (m_enemyPool == null || TileManager.Instance == null)
        {
            Debug.LogError("[WaveManager] 필수 컴포넌트(Pool 또는 TileManager)가 누락되었습니다.");
            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Wave Composition and Spawning

    /// <summary>
    /// 웨이브 시작 때 한 번 호출됩니다.
    /// 이번 웨이브 구성의 적을 전부 소환하고, 실제로 소환한 수를 반환합니다.
    /// </summary>
    public int SpawnWaveEnemies()
    {
        List<EnemyType> waveEnemies = GetWaveEnemyComposition(m_currentWave);
        if (m_waveSpawnedCount >= waveEnemies.Count || m_enemyPool == null || TileManager.Instance == null) return 0;

        int spawnedCount = 0;
        GetEnemyStatMultipliers(out float hpMulti, out float defendMulti);

        while (m_waveSpawnedCount < waveEnemies.Count)
        {
            // 경로가 없는 경우에는 소환할 수 없습니다.
            if (!TileManager.Instance.TryGetRandomEnemySpawnIndex(out int spawnIndex)) break;
            EnemyType currentEnemyType = waveEnemies[m_waveSpawnedCount];
            int poolIndex = (int)currentEnemyType;

            if (poolIndex < 0 || poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
            {
                Debug.LogError($"[WaveManager] {currentEnemyType}에 해당하는 오브젝트 풀이 없습니다!");
                break;
            }

            // 풀이 가득 차 소환에 실패하면 세지 않고 멈춥니다.
            if (m_enemyPool[poolIndex].Spawn(spawnIndex, hpMulti, defendMulti) == null)
            {
                Debug.LogWarning($"[WaveManager] {currentEnemyType} 소환에 실패했습니다. 오브젝트 풀의 최대 크기를 확인하세요.");
                break;
            }

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

        switch (baseWave)
        {
            case 1:
            case 2:
            case 3:
            case 4:
                AddEnemies(enemies, EnemyType.Normal, 8 * cycle);
                break;
            case 5:
                enemies.Add(EnemyType.MiddleBoss);
                break;

            case 6:
            case 7:
            case 8:
            case 9:
                AddEnemies(enemies, EnemyType.Normal, 10 * cycle);
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

    #endregion

    #region Enemy Stat Scaling

    /// <summary>
    /// 웨이브 구성으로 소환하는 적에게 적용할 최종 배율을 계산합니다.
    /// 최종 배율은 사이클 내부 단계 배율, 10웨이브 단위 사이클 배율(1 / 2 / 3 / 4), 모드 배율의 곱입니다.
    /// 순서 5(중간 보스)와 10(최종 보스)은 단계 배율이 1이라 사이클 배율만 받습니다.
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
            5 => 1.0f,
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
            2 => 1.2f,
            3 => 1.4f,
            4 => 1.6f,
            5 => 1.0f,
            6 => 2.0f,
            7 => 2.2f,
            8 => 2.4f,
            9 => 2.6f,
            10 => 1.0f,
            _ => 1.0f
        };

        GetCycleMultipliers(m_currentWave, out float cycleHp, out float cycleDefend);

        // 하드 모드는 모든 적의 체력·방어력이 1.5배입니다. 스토리 모드(0.75배)는 21번 5-2에서.
        float modeMulti = GameModeSession.SelectedDifficulty == GameDifficulty.Hard ? 1.5f : 1f;

        hpMulti = subWaveHp * cycleHp * modeMulti;
        defendMulti = subWaveDefend * cycleDefend * modeMulti;
    }

    private static void GetCycleMultipliers(int wave, out float hpMulti, out float defendMulti)
    {
        hpMulti = wave switch
        {
            > 30 => 4.0f,
            > 20 => 3.0f,
            > 10 => 2.0f,
            _ => 1.0f
        };

        defendMulti = wave switch
        {
            > 30 => 4.0f,
            > 20 => 3.0f,
            > 10 => 2.0f,
            _ => 1.0f
        };
    }

    #endregion

    #region Wave Progression and Game Clear

    /// <summary>
    /// 10번째 적 턴이 끝난 뒤 다음 웨이브 상태로 전환합니다.
    /// </summary>
    public void NextWave()
    {
        m_currentWave++;
        CurrentWaveChanged?.Invoke(m_currentWave);
        m_waveSpawnedCount = 0;
    }

    // 보상 기준표: Resources/WavePerEnemies&Gold&Gem.xlsx
    private const int NormalGoldPerCycle = 10;
    private const int BossGoldPerWave = 100;
    private const int BossGemPerWave = 2;
    private const int MiddleBossGoldPerWave = 50;
    private const int MiddleBossGemPerWave = 1;
    private const int FinalWave = 40;

    /// <summary>
    /// 적 처치 보상을 계산합니다. 기준은 적이 죽은 웨이브가 아니라 소환된 웨이브입니다.
    /// 일반 적은 웨이브에 비례하고, 보스 보상은 여기에 사이클(10웨이브 단위) 배율이 한 번 더 곱해집니다.
    /// 중간 보스는 실제 소환 웨이브와 무관하게 그 사이클의 기준 웨이브(5/15/25/35)로 계산합니다.
    /// </summary>
    public static void GetKillReward(EnemyType enemyType, int spawnWave, out int gold, out int gem)
    {
        int wave = Mathf.Clamp(spawnWave, 1, FinalWave);
        int cycle = ((wave - 1) / 10) + 1;

        switch (enemyType)
        {
            case EnemyType.Boss:
                gold = BossGoldPerWave * wave * cycle;
                gem = BossGemPerWave * wave * cycle;
                break;

            case EnemyType.MiddleBoss:
                int baseWave = ((cycle - 1) * 10) + 5;
                gold = MiddleBossGoldPerWave * baseWave * cycle;
                gem = MiddleBossGemPerWave * baseWave * cycle;
                break;

            default:
                gold = NormalGoldPerCycle * cycle;
                gem = 0;
                break;
        }
    }

    /// <summary>40웨이브에 소환된 최종 보스가 죽었을 때만 게임 클리어입니다. 중간 보스나 이전 웨이브 보스는 해당하지 않습니다.</summary>
    public void OnEnemyDied(EnemyType enemyType, int spawnWave)
    {
        if (enemyType == EnemyType.Boss && spawnWave >= FinalWave)
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
