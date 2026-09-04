using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private TilePath m_tilePath;

    public List<EnemyMovementController> activeEnemies = new List<EnemyMovementController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (m_tilePath == null)
        {
            m_tilePath = FindObjectOfType<TilePath>();
        }
    }

    // 에너미 턴 시작 시 호출: 신규 몬스터 스폰 + 전체 몬스터 주사위 이동 및 타일 검사 통합
    public IEnumerator ProcessEnemyTurnRoutine()
    {
        // 1. WaveManager를 통해 새 적 스폰 (웨이브 당 1마리씩 생성 구조라면 여기서 호출)
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.SpawnNextWaveEnemyForTurn();
        }

        // 2. 살아있는 모든 적 이동 처리 (0번은 1칸, 나머지는 주사위)
        List<Coroutine> runningCoroutines = new List<Coroutine>();
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                runningCoroutines.Add(StartCoroutine(enemy.ProcessTurnMoveRoutine()));
            }
        }

        // 모든 적의 이동이 끝날 때까지 대기
        bool anyMoving = true;
        while (anyMoving)
        {
            anyMoving = false;
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && enemy.IsMoving)
                {
                    anyMoving = true;
                    break;
                }
            }
            yield return null;
        }

        // 특수 타일 효과는 GameManager의 ProcessTileBuffs 단계에서 적용합니다.

        // 파괴되거나 풀로 돌아간 적 목록 정리
        activeEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
    }

    public void ApplyAllTileBuffs()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.CheckAndApplyTileBuff();
            }
        }
    }

    public void AdvanceAllDebuffTurns()
    {
        foreach (EnemyMovementController enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.TryGetComponent(out EnemyDebuffController debuff))
            {
                debuff.AdvanceDebuffTurn();
            }
        }
    }

    public void RegisterEnemy(EnemyMovementController enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }
}
