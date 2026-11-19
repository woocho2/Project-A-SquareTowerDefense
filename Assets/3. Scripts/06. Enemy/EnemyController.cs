using UnityEngine;

namespace SquareTD
{
    // 적 하나: 체력, 스탯, 상태 효과, 피해 받기
    public class EnemyController : MonoBehaviour, ITarget
    {
        public EnemyDefinition Definition { get; private set; }
        public EnemyCubeAppearance Appearance => Movement != null && Movement.Appearance != null ? Movement.Appearance : Definition != null ? Definition.Appearance : null;
        private EnemyMovement m_movement;
        public EnemyMovement Movement
        {
            get => m_movement != null ? m_movement : m_movement = GetComponent<EnemyMovement>();
            private set => m_movement = value;
        }
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
        public event System.Action<EnemyController> BossSkillTriggered;
        public BossMechanicKind BossMechanic { get; private set; }
        public int DeathCount { get; private set; }

        public bool TryTriggerBossSkill(EnemyCubeFaceAsset face)
        {
            if (!IsAlive || Definition == null || !EnemyCubeFaces.IsBossSkillFace(face, Definition.Kind)) return false;
            GameSession session = GameSession.Instance;
            if (session != null)
                session.BossMechanics.Trigger(this, EarthDebuffRule.AdaptiveFor(this, session.AdaptiveValue), session.HelDeathTarget);
            BossSkillTriggered?.Invoke(this);
            return true;
        }

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
            m_movement = GetComponent<EnemyMovement>();
        }

        public void Setup(EnemyDefinition definition, EnemyPool pool, int spawnWave, string displayName = null,
                          BossMechanicKind bossMechanic = BossMechanicKind.None)
        {
            Definition = definition;
            Pool = pool;
            SpawnWave = spawnWave;
            BossMechanic = definition.Kind == EnemyKind.FinalBoss ? bossMechanic : BossMechanicKind.None;
            DeathCount = 0;
            DisplayName = string.IsNullOrEmpty(displayName) ? definition.DisplayName : displayName;
            ModeMultiplier = WaveTable.GetModeStatMultiplier(GameSession.CurrentMode);
            Stats = new EnemyStats(definition, spawnWave, ModeMultiplier);
            Resistance = new EnemyResistance(definition.BaseResistance);
            Status = new EnemyStatusEffects(this, Stats.Modifiers, Resistance);
            Health.Reset(SpawnMaxHp);
            Movement?.SetFinalBoss(definition.Kind == EnemyKind.FinalBoss);
        }

        public bool AddDeathCount(int dice, int target)
        {
            if (dice <= 0 || target <= 0) return false;
            DeathCount += dice;
            if (DeathCount < target) return false;
            DeathCount = 0;
            return true;
        }

        public string DisplayName { get; private set; }
        public float ModeMultiplier { get; private set; } = 1f;
        public float SpawnMaxHp => Definition.MaxHP * WaveTable.GetHpMultiplier(SpawnWave, Definition.Kind) * ModeMultiplier;

        public void TakeDamage(DamageInfo info)
        {
            float damage = PreviewDamage(info);
            BalanceRecorder.OnDamage(info, Mathf.Min(damage, CurrentHp));
            if (IsAlive && GameSession.Instance != null) GameSession.Instance.Results.Record(info, Mathf.Min(damage, CurrentHp));
            bool died = Health.TakeDamage(damage);
            Status.OnDamageTaken(info, damage);

            if (!died)
            {
                UllrSkill.TryFollowUp(this, info, damage);
                if (IsAlive) TryStun(info);
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

        public bool TryExecute(DamageInfo info)
        {
            if (!IsAlive || !Resistance.TryStrongCC()) return false;
            Execute(info);
            return true;
        }

        public void Execute(DamageInfo info)
        {
            if (!IsAlive) return;
            if (GameSession.Instance != null) GameSession.Instance.Results.Record(info, Health.Current);
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
