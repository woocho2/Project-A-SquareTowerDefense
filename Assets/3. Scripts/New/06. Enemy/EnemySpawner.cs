using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SquareTD
{
    [Serializable]
    public class EnemySpawnEntry
    {
        [field: SerializeField] public EnemyDefinition Definition { get; private set; }
        [field: SerializeField] public EnemyPool Pool { get; private set; }
    }

    public class EnemySpawner
    {
        private readonly BoardManager m_board;
        private readonly EnemyRegistry m_registry;
        private readonly EnemySpawnEntry m_normal;
        private readonly EnemySpawnEntry m_middleBoss;
        private readonly EnemySpawnEntry m_finalBoss;
        private readonly BossTimer m_bossTimer;

        public EnemySpawner(BoardManager board, EnemyRegistry registry, EnemySpawnEntry normal, EnemySpawnEntry middleBoss, EnemySpawnEntry finalBoss, BossTimer bossTimer)
        {
            m_board = board;
            m_registry = registry;
            m_normal = normal;
            m_middleBoss = middleBoss;
            m_finalBoss = finalBoss;
            m_bossTimer = bossTimer;
        }

        public void SpawnWave(int wave)
        {
            int spawnCount = WaveTable.GetNormalCount(wave);

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOne(m_normal, wave);
            }

            if (WaveTable.HasMiddleBoss(wave))
            {
                SpawnOne(m_middleBoss, wave);
            }

            if (WaveTable.HasFinalBoss(wave))
            {
                SpawnOne(m_finalBoss, wave);
            }
        }

        private void SpawnOne(EnemySpawnEntry entry, int wave)
        {
            if (entry.Definition == null || entry.Pool == null)
            {
                Debug.LogWarning("[EnemySpawner] 적 정의가 비어 있어 소환하지 않음");
                return;
            }

            EnemyController enemy = entry.Pool.Get();
            EnemyDefinition definition = entry.Definition;
            enemy.Setup(definition, entry.Pool, wave);
            int index = Random.Range(0, m_board.Grid.PathCount);
            enemy.Movement.Init(index);
            enemy.transform.position = m_board.GetWorldPosition(m_board.Grid.GetPathCell(index));
            enemy.gameObject.SetActive(true);
            m_board.Occupancy.Add(enemy, index);
            m_registry.Register(enemy);
            if (definition.Kind != EnemyKind.Normal)
            {
                m_bossTimer.Add(enemy);
            }
        }
    }
}