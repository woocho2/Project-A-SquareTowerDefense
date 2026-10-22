using UnityEngine;

namespace SquareTD
{
    public class EnemyController : MonoBehaviour
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyMovement Movement { get; private set; }
        public EnemyHealth Health { get; } = new EnemyHealth();
        public EnemyPool Pool { get; private set; }
        public int SpawnWave { get; private set; }

        public float MaxHp => Health.Max;
        public float Defense { get; private set; }

        private void Awake()
        {
            Movement = GetComponent<EnemyMovement>();
        }

        public void Setup(EnemyDefinition definition, EnemyPool pool, int spawnWave)
        {
            Definition = definition;
            Pool = pool;
            SpawnWave = spawnWave;
            Defense = definition.Defense * WaveTable.GetDefenseMultiplier(spawnWave, definition.Kind);
            Health.Reset(definition.MaxHP * WaveTable.GetHpMultiplier(spawnWave, definition.Kind));
        }

        public void TakeDamage(DamageInfo info)
        {
            float damage = DamageCalculator.Calculate(info, Defense, 1f, 0f);
            if (Health.TakeDamage(damage))
            {
                KillInfo kill = new KillInfo
                {
                    Enemy = this,
                    Damage = info,
                    SpawnWave = SpawnWave,
                    Kind = Definition.Kind,
                };
                EnemyManager.Instance.HandleDeath(kill);
            }
        }
    }
}
