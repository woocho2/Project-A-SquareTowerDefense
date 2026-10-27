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
        public EnemyHitCounter HitCounter { get; } = new EnemyHitCounter();     // 단일 타워 스킬 2용, 문양별 적중 수
        public EnemyResistance Resistance { get; private set; }                 // 기본·지금 저항력 (수업 25)
        public EnemyStatusEffects Status { get; private set; }                  // 걸린 효과 목록 (수업 25)
        public Transform Transform => transform;
        public int PathIndex => Movement.PathIndex;
        public bool IsAlive => !Health.IsDead;
        public bool IsBoss => Definition.Kind != EnemyKind.Normal;
        public bool IsDummy => false;
        public EnemyController Body => this;
        public float CurrentHp => Health.Current;
        // 들어갈 피해 (방어·받는 피해·고정 피해 → 상태 효과가 끼어듦). 타워가 "이 적에게 얼마가 갈까"를 미리 셀 때도 이것
        public float PreviewDamage(DamageInfo info)
        {
            float damage = DamageCalculator.Calculate(info, Stats.Get(StatType.Defense), Stats.Get(StatType.DamageTaken), Stats.Get(StatType.FixedDamageRatio));
            return Status != null ? Status.ModifyIncomingDamage(info, damage) : damage;
        }
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
            HitCounter.Clear();
            Resistance = new EnemyResistance(definition.BaseResistance);                   // 소환마다 새로 (풀에서 다시 꺼낸 적이 지난번 저항·효과를 들고 있지 않게)
            Status = new EnemyStatusEffects(this, Stats.Modifiers, Resistance);
            Health.Reset(definition.MaxHP * WaveTable.GetHpMultiplier(spawnWave, definition.Kind));
        }

        // 맞기 전 훅 → 피해 계산(+ 상태 효과) → 체력 → 맞은 뒤 훅 → 죽었으면 사망 훅 → 사망 처리
        public void TakeDamage(DamageInfo info)
        {
            Status.OnBeforeHit(ref info);                   // 맞기 전 (락 온: 치명 확률 등)
            float damage = PreviewDamage(info);
            bool died = Health.TakeDamage(damage);
            Status.OnDamageTaken(info, damage);             // 맞은 뒤 (처형 등). 죽었어도 부름

            if (!died) return;

            KillInfo kill = new KillInfo
            {
                Enemy = this,
                Damage = info,
                SpawnWave = SpawnWave,
                Kind = Definition.Kind,
            };
            Status.OnDeath(kill);                           // 풀로 돌아가기 전에 (원석·금맥의 보상 등)
            if (EnemyManager.Instance != null) EnemyManager.Instance.HandleDeath(kill);
        }
    }
}
