using System;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 적 소환·죽음·목록을 맡는 매니저
    public class EnemyManager : Singleton<EnemyManager>
    {
        [SerializeField] EnemySpawnEntry m_normal, m_middleBoss, m_finalBoss;
        public EnemyRegistry Registry { get; private set; }
        public BossTimer BossTimer { get; private set; }
        private EnemySpawner m_spawner;
        private DeathResolver m_deathResolver;
        public event Action<int> CountChanged;
        public event Action<KillInfo> EnemyDied;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            Registry = new EnemyRegistry();
            BossTimer = new BossTimer();
        }

        private void Start()
        {
            m_spawner = new EnemySpawner(BoardManager.Instance, Registry, m_normal, m_middleBoss, m_finalBoss, BossTimer, GameSession.Instance != null ? GameSession.Instance.Stage : null);
            m_deathResolver = new DeathResolver(BoardManager.Instance, Registry, BossTimer, CurrencyManager.Instance, GameSession.Instance);
        }

        public List<EnemyController> SpawnWave(int wave)
        {
            List<EnemyController> spawned = m_spawner.SpawnWave(wave);
            CountChanged?.Invoke(Registry.Count);
            return spawned;
        }

        public EnemyController SpawnForDebug(float hp)
        {
            int wave = WaveManager.Instance != null ? Mathf.Max(1, WaveManager.Instance.CurrentWave) : 1;
            EnemyController enemy = m_spawner.SpawnNormalWithHp(wave, hp);
            CountChanged?.Invoke(Registry.Count);
            return enemy;
        }

        public bool IsAnyEnemyMoving()
        {
            foreach (EnemyController enemy in Registry.Alive)
            {
                if (enemy.Movement.IsMoving) return true;
            }
            return false;
        }

        public void HandleDeath(KillInfo kill)
        {
            m_deathResolver.Resolve(kill);
            EnemyDied?.Invoke(kill);
            CountChanged?.Invoke(Registry.Count);
        }
    }
}
