using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TileManager의 패스 인덱스에 등록된 적을 대상으로, 한 번의 적 턴에서 소환과 이동을 순서대로 처리합니다.
/// 타일 효과와 디버프 턴 진행은 GameManager가 정한 단계에서 별도로 호출합니다.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    #region Singleton and State

    public static EnemyManager Instance { get; private set; }

    [Header("참조")]
    [SerializeField] private TilePath m_tilePath;

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

    // 적 목록의 원본은 TileManager의 패스 인덱스별 점유 목록입니다.
    private static List<EnemyHealthController> GetEnemiesOnPath()
    {
        return TileManager.Instance != null
            ? TileManager.Instance.GetAllEnemiesOnPath()
            : new List<EnemyHealthController>();
    }

    #endregion
}
