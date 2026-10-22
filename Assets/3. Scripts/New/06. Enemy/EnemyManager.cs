using System;
using UnityEngine;

namespace SquareTD
{
    public class EnemyManager : Singleton<EnemyManager>
    {
        [SerializeField] EnemySpawnEntry m_normal, m_middleBoss, m_finalBoss;
        public EnemyRegistry Registry { get; private set; }
        public BossTimer BossTimer { get; private set; }
        private EnemySpawner m_spawner;
        public event Action<int> CountChanged;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            Registry = new EnemyRegistry();
            BossTimer = new BossTimer();
        }

        private void Start()
        {
            m_spawner = new EnemySpawner(BoardManager.Instance, Registry, m_normal, m_middleBoss, m_finalBoss, BossTimer);
        }

        public void SpawnWave(int wave)
        {
            m_spawner.SpawnWave(wave);
            CountChanged?.Invoke(Registry.Count);
        }

        public bool IsAnyEnemyMoving()
        {
            foreach (EnemyController enemy in Registry.Alive)
            {
                if (enemy.Movement.IsMoving) return true;
            }
            return false;
        }
    }
}