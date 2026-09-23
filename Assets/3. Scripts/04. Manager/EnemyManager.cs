using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필드에 등록된 적을 관리하고, 한 번의 적 턴에서 소환과 이동을 순서대로 처리합니다.
/// 타일 효과와 디버프 턴 진행은 GameManager가 정한 단계에서 별도로 호출합니다.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    #region Singleton and State

    public static EnemyManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private TilePath m_tilePath;

    public List<EnemyMoveController> activeEnemies = new List<EnemyMoveController>();

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

        if (m_tilePath == null)
        {
            m_tilePath = FindFirstObjectByType<TilePath>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Enemy Turn Processing

    /// <summary>
    /// 필요하면 이번 턴의 적을 먼저 소환한 뒤, 살아 있는 모든 적의 이동 완료를 기다립니다.
    /// </summary>
    public IEnumerator ProcessEnemyTurnRoutine(bool shouldSpawn)
    {
        // 웨이브 1~5턴에만 사이클별 소환 수만큼 적을 추가합니다.
        if (shouldSpawn && WaveManager.Instance != null)
        {
            WaveManager.Instance.SpawnWaveEnemiesForTurn();
        }

        // 새로 소환된 적을 포함해 현재 필드의 모든 적이 동시에 이동을 시작합니다.
        foreach (EnemyMoveController enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                StartCoroutine(enemy.ProcessTurnMoveRoutine());
            }
        }

        // 모든 적의 이동이 끝날 때까지 대기
        bool anyMoving = true;
        while (anyMoving)
        {
            anyMoving = false;
            foreach (EnemyMoveController enemy in activeEnemies)
            {
                if (enemy != null && enemy.IsMoving)
                {
                    anyMoving = true;
                    break;
                }
            }
            yield return null;
        }

        // 파괴되거나 풀로 돌아간 적 목록 정리
        activeEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
    }

    #endregion

    #region Turn Effects

    public void ApplyAllTileBuffs()
    {
        foreach (EnemyMoveController enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.CheckAndApplyTileBuff();
            }
        }
    }

    public void AdvanceAllDebuffTurns()
    {
        foreach (EnemyMoveController enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.TryGetComponent(out EnemyDebuffController debuff))
            {
                debuff.AdvanceDebuffTurn();
            }
        }
    }

    #endregion

    #region Registration

    public void RegisterEnemy(EnemyMoveController enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    #endregion
}
