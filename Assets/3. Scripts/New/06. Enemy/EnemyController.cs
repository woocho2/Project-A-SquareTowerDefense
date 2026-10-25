using UnityEngine;

namespace SquareTD
{
    public class EnemyController : MonoBehaviour, ITarget
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyMovement Movement { get; private set; }
        public EnemyHealth Health { get; } = new EnemyHealth();
        public EnemyPool Pool { get; private set; }
        public int SpawnWave { get; private set; }
        public float MaxHp => Health.Max;
        public EnemyStats Stats { get; private set; }
        public Transform Transform => transform;
        public int PathIndex => Movement.PathIndex;
        public bool IsAlive => !Health.IsDead;
        public bool IsBoss => Definition.Kind != EnemyKind.Normal;
        public bool IsDummy => false;
        public EnemyController Body => this;
        public float CurrentHp => Health.Current;
        public float PreviewDamage(DamageInfo info) => DamageCalculator.Calculate(info, Stats.Get(StatType.Defense), Stats.Get(StatType.DamageTaken), Stats.Get(StatType.FixedDamageRatio));
        public void SetSortingOrder(int order) => Movement.SetSortingOrder(order);

        private void Awake()
        {
            Movement = GetComponent<EnemyMovement>();
        }

        public void Setup(EnemyDefinition definition, EnemyPool pool, int spawnWave)
        {
            Definition = definition;
            Pool = pool;
            SpawnWave = spawnWave;
            Stats = new EnemyStats(definition, spawnWave);
            Health.Reset(definition.MaxHP * WaveTable.GetHpMultiplier(spawnWave, definition.Kind));
        }

        public void TakeDamage(DamageInfo info)
        {
            float damage = PreviewDamage(info);
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
