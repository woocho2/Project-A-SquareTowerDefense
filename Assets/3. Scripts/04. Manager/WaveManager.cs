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
    public void SpawnNextWaveEnemyForTurn()
    {
        if (m_currentWave > 40) return;

        int subWave = m_currentWave % 10;
        if (subWave == 0) subWave = 10;

        // 보스 웨이브(subWave == 10)일 때는 일반 잡몹 대신 보스 스폰 처리 등 분기 가능
        EnemyType currentEnemyType = DetermineEnemyType(m_currentWave, m_activeEnemyCount);
        int poolIndex = (int)currentEnemyType;

        if (poolIndex >= m_enemyPool.Length || m_enemyPool[poolIndex] == null)
        {
            Debug.LogError($"[WaveManager] {currentEnemyType}에 해당하는 오브젝트 풀이 없습니다!");
            return;
        }

        // 스탯 배율 계산
        GetMultiplier(out float hpMulti, out float defendMulti);

        EnemyObjectPool2D targetPool = m_enemyPool[poolIndex];
        // 0번 타일 위치에 스폰 요청
        targetPool.Spawn(m_tilePath.GetWorldPosition(0), hpMulti, defendMulti, m_tilePath);

        m_activeEnemyCount++;
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
        CurrencyManager.Instance?.AddGold((m_currentWave - 1) * 100);
        UpdateSpecialBossButtonUI();
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
                Debug.Log("최종 40웨이브 Boss 처치! 승리하셨습니다.");
            }
        }
    }
}