using UnityEngine;

namespace SquareTD
{
    public class EnemySpawner
    {
        private readonly EnemyPool m_pool;
        private readonly BoardManager m_board;
        private readonly EnemyRegistry m_registry;
        private readonly EnemyDefinition m_normal;

        public EnemySpawner(EnemyPool pool, BoardManager board, EnemyRegistry registry, EnemyDefinition normal)
        {
            m_pool = pool;
            m_board = board;
            m_registry = registry;
            m_normal = normal;
        }

        public void SpawnWave(int wave)
        {
            int spawnCount = WaveTable.GetNormalCount(wave);

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOne(m_normal, wave);
            }
        }

        private void SpawnOne(EnemyDefinition definition, int wave)
        {
            if (definition == null)
            {
                Debug.LogWarning("[EnemySpawner] 적 정의가 비어 있어 소환하지 않음");
                return;
            }

            EnemyController enemy = m_pool.Get();
            enemy.Setup(definition,
            WaveTable.GetHpMultiplier(wave, definition.Kind),
            WaveTable.GetDefenseMultiplier(wave, definition.Kind));
            int index = Random.Range(0, m_board.Grid.PathCount);
            enemy.Movement.Init(index);
            enemy.transform.position = m_board.GetWorldPosition(m_board.Grid.GetPathCell(index));
            enemy.gameObject.SetActive(true);
            m_board.Occupancy.Add(enemy, index);
            m_registry.Register(enemy);
        }
    }
}