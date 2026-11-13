using UnityEngine;

namespace SquareTD
{
    // 적 하나: 체력, 스탯, 상태 효과, 피해 받기
    public class EnemyController : MonoBehaviour, ITarget
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyMovement Movement { get; private set; }
        public EnemyHealth Health { get; } = new EnemyHealth();
        public EnemyPool Pool { get; private set; }
        public int SpawnWave { get; private set; }
        public float MaxHp => Health.Max;
        public EnemyStats Stats { get; private set; }
        public EnemyResistance Resistance { get; private set; }
        public EnemyStatusEffects Status { get; private set; }
        public Transform Transform => transform;
        public int PathIndex => Movement.PathIndex;
        public bool IsAlive => !Health.IsDead;
        public bool IsBoss => Definition.Kind != EnemyKind.Normal;
        public EnemyController Body => this;
        public float CurrentHp => Health.Current;
        public float PreviewDamage(DamageInfo info)
        {
            float defense = Status != null ? Status.ModifyDefense(Stats.Get(StatType.Defense)) : Stats.Get(StatType.Defense);
            float damage = DamageCalculator.Calculate(info, defense, Stats.Get(StatType.DamageTaken), Stats.Get(StatType.FixedDamageRatio));
            return Status != null ? Status.ModifyIncomingDamage(info, damage) : damage;
        }
        public void SetSortingOrder(int order) => Movement.SetSortingOrder(order);

        public const float MinHealingTaken = 0.2f;

        public float Heal(float amount) => Health.Heal(amount * Mathf.Max(MinHealingTaken, Stats.Get(StatType.HealingTaken)));

        private void Awake()
        {
            Movement = GetComponent<EnemyMovement>();
        }

        public void Setup(EnemyDefinition definition, EnemyPool pool, int spawnWave, string displayName = null)
        {
            Definition = definition;
            Pool = pool;
            SpawnWave = spawnWave;
            DisplayName = string.IsNullOrEmpty(displayName) ? definition.DisplayName : displayName;
            ModeMultiplier = WaveTable.GetModeStatMultiplier(GameSession.CurrentMode);
            Stats = new EnemyStats(definition, spawnWave, ModeMultiplier);
            Resistance = new EnemyResistance(definition.BaseResistance);
            Status = new EnemyStatusEffects(this, Stats.Modifiers, Resistance);
            Health.Reset(SpawnMaxHp);
        }

        public string DisplayName { get; private set; }
        public float ModeMultiplier { get; private set; } = 1f;
        public float SpawnMaxHp => Definition.MaxHP * WaveTable.GetHpMultiplier(SpawnWave, Definition.Kind) * ModeMultiplier;

        public void TakeDamage(DamageInfo info)
        {
            float damage = PreviewDamage(info);
            BalanceRecorder.OnDamage(info, Mathf.Min(damage, CurrentHp));
            bool died = Health.TakeDamage(damage);
            Status.OnDamageTaken(info, damage);

            if (!died)
            {
                TryStun(info);
                return;
            }
            Die(info);
        }

        private void TryStun(DamageInfo info)
        {
            if (!IsAlive) return;
            float chance = info.StunChance;
            if (chance <= 0f) return;
            if (chance < 1f && Random.value >= chance) return;
            Status.Add(new ImmobilizeEffect("기절"));
        }

        public void Execute(DamageInfo info)
        {
            if (!IsAlive) return;
            Health.TakeDamage(Health.Current);
            Die(info);
        }

        private void Die(DamageInfo info)
        {
            KillInfo kill = new KillInfo
            {
                Enemy = this,
                Damage = info,
                SpawnWave = SpawnWave,
                Kind = Definition.Kind,
            };
            Status.OnDeath(kill);
            if (EnemyManager.Instance != null) EnemyManager.Instance.HandleDeath(kill);
        }
    }
}
