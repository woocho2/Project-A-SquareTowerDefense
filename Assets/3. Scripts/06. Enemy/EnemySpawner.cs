using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SquareTD
{
    // 웨이브의 적을 판에 내보냄
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
        private readonly StageDefinition m_stage;

        public EnemySpawner(BoardManager board, EnemyRegistry registry, EnemySpawnEntry normal, EnemySpawnEntry middleBoss, EnemySpawnEntry finalBoss, BossTimer bossTimer, StageDefinition stage = null)
        {
            m_stage = stage;
            m_board = board;
            m_registry = registry;
            m_normal = normal;
            m_middleBoss = middleBoss;
            m_finalBoss = finalBoss;
            m_bossTimer = bossTimer;
        }

        public List<EnemyController> SpawnWave(int wave)
        {
            var spawned = new List<EnemyController>();
            int spawnCount = WaveTable.GetNormalCount(wave);

            for (int i = 0; i < spawnCount; i++)
            {
                AddIfSpawned(spawned, SpawnOne(m_normal, wave));
            }

            if (WaveTable.HasMiddleBoss(wave))
            {
                AddIfSpawned(spawned, SpawnOne(m_middleBoss, wave));
            }

            if (WaveTable.HasFinalBoss(wave))
            {
                AddIfSpawned(spawned, SpawnOne(m_finalBoss, wave));
            }
            return spawned;
        }

        private static void AddIfSpawned(List<EnemyController> list, EnemyController enemy)
        {
            if (enemy != null) list.Add(enemy);
        }

        public EnemyController SpawnNormalWithHp(int wave, float hp)
        {
            EnemyController enemy = SpawnOne(m_normal, wave);
            if (enemy != null) enemy.Health.Reset(hp);
            return enemy;
        }

        private EnemyController SpawnOne(EnemySpawnEntry entry, int wave)
        {
            if (entry.Definition == null || entry.Pool == null)
            {
                Debug.LogWarning("[EnemySpawner] 적 정의가 비어 있어 소환하지 않음");
                return null;
            }

            EnemyController enemy = entry.Pool.Get();
            EnemyDefinition definition = entry.Definition;
            StageEnemyLook look = m_stage != null ? m_stage.GetLook(definition.Kind, WaveTable.GetCycle(wave)) : null;
            EnemyCubeAppearance appearance = look != null && look.Appearance != null ? look.Appearance : definition.Appearance;
            if (appearance != null) enemy.Movement.SetAppearance(appearance);
            enemy.Setup(definition, entry.Pool, wave, look != null ? look.Name : null);
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
            return enemy;
        }
    }
}
