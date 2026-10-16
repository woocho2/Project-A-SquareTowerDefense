using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TileManager의 패스 인덱스에 등록된 적을 대상으로, 웨이브 시작의 일괄 소환과 적 턴의 이동을 처리합니다.
/// 타일 효과와 디버프 턴 진행은 GameManager가 정한 단계에서 별도로 호출합니다.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    #region Singleton and State

    public static EnemyManager Instance { get; private set; }

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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Enemy Turn Processing

    /// <summary>
    /// 이번 웨이브의 적을 한 번에 소환하고, 모두 착지할 때까지 기다립니다.
    /// </summary>
    public IEnumerator SpawnWaveRoutine()
    {
        if (WaveManager.Instance != null) WaveManager.Instance.SpawnWaveEnemies();

        // 새 큐브가 모두 착지해야 플레이어 턴으로 넘어갑니다.
        List<EnemyHealthController> spawnedEnemies = GetEnemiesOnPath();
        bool anySpawning;
        do
        {
            anySpawning = false;
            foreach (EnemyHealthController enemy in spawnedEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.Movement != null && enemy.Movement.IsSpawning)
                {
                    anySpawning = true;
                    break;
                }
            }
            if (anySpawning) yield return null;
        } while (anySpawning);
    }

    /// <summary>
    /// 살아 있는 모든 적의 이동 완료를 기다립니다.
    /// </summary>
    public IEnumerator ProcessEnemyTurnRoutine()
    {
        // 현재 필드의 모든 적이 동시에 이동을 시작합니다.
        List<EnemyMoveController> movingEnemies = new List<EnemyMoveController>();
        foreach (EnemyHealthController enemy in GetEnemiesOnPath())
        {
            if (!enemy.TryGetComponent(out EnemyMoveController movement)) continue;

            movingEnemies.Add(movement);
            StartCoroutine(movement.ProcessTurnMoveRoutine());
        }

        // 모든 적의 이동이 끝날 때까지 대기
        bool anyMoving = true;
        while (anyMoving)
        {
            anyMoving = false;
            foreach (EnemyMoveController enemy in movingEnemies)
            {
                if (enemy != null && enemy.IsMoving)
                {
                    anyMoving = true;
                    break;
                }
            }
            yield return null;
        }
    }

    #endregion

    #region Turn Effects

    public void ApplyAllTileBuffs()
    {
        foreach (EnemyHealthController enemy in GetEnemiesOnPath())
        {
            if (enemy.gameObject.activeInHierarchy && enemy.TryGetComponent(out EnemyMoveController movement))
            {
                movement.CheckAndApplyTileBuff();
            }
        }
    }

    public void AdvanceAllDebuffTurns()
    {
        foreach (EnemyHealthController enemy in GetEnemiesOnPath())
        {
            if (enemy.gameObject.activeInHierarchy && enemy.TryGetComponent(out EnemyDebuffController debuff))
            {
                debuff.AdvanceDebuffTurn();
            }
        }
    }

    /// <summary>
    /// 모든 보스·중간 보스의 남은 제한 턴을 1 줄입니다. 0이 된 보스가 있으면 true를 반환합니다.
    /// </summary>
    public bool AdvanceAllBossTurnLimits()
    {
        foreach (EnemyHealthController enemy in GetEnemiesOnPath())
        {
            if (!enemy.gameObject.activeInHierarchy || !enemy.IsBoss) continue;

            enemy.AdvanceBossTurnLimit();
            if (enemy.IsBossTurnLimitOver) return true;
        }
        return false;
    }


    // 적 목록의 원본은 TileManager의 패스 인덱스별 점유 목록입니다.
    private static List<EnemyHealthController> GetEnemiesOnPath()
    {
        return TileManager.Instance != null
            ? TileManager.Instance.GetAllEnemiesOnPath()
            : new List<EnemyHealthController>();
    }

    #endregion
}
