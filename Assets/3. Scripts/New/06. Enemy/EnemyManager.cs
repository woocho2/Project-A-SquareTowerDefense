using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SquareTD
{
    public class EnemyManager : Singleton<EnemyManager>
    {
        [SerializeField] EnemyPool m_normalPool;
        [SerializeField] EnemyDefinition m_normalDefinition;
        public EnemyRegistry Registry { get; private set; }
        private EnemySpawner m_spawner;
        public event Action<int> CountChanged;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            Registry = new EnemyRegistry();
        }

        private void Start()
        {
            m_spawner = new EnemySpawner(m_normalPool, BoardManager.Instance, Registry, m_normalDefinition);
        }

        public void SpawnWave(int wave)
        {
            m_spawner.SpawnWave(wave);
            CountChanged?.Invoke(Registry.Count);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.jKey.wasPressedThisFrame)
            {
                SpawnWave(1);
                Debug.Log($"[EnemyManager] 1웨이브 소환/ 살아 있는 적 {Registry.Count}");
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                foreach (EnemyController enemy in Registry.Alive)
                {
                    enemy.StartCoroutine(enemy.Movement.TakeTurn());
                }
                Debug.Log($"[EnemyManager] 적 턴 / 적 {Registry.Count}마리");
            }
        }
    }
}