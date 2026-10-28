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

        // 회복 (회복 타일 …): 받는 치유 배율만큼 (출혈·과다출혈이 깎음, 수업 30). 실제로 오른 양을 돌려줌
        public float Heal(float amount) => Health.Heal(amount * Mathf.Max(0f, Stats.Get(StatType.HealingTaken)));

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
            Status.OnDamageTaken(info, damage);             // 맞은 뒤 (사형 집행 등). 죽었어도 부름

            if (!died)
            {
                TryStun(info);
                return;
            }
            Die(info);
        }

        // 기절이 실린 공격 (배쉬 20%, 서프레스·락다운의 타격 시너지 +5% / +10%, 수업 31). 두 확률을 더해 한 번 굴림
        //   기절은 강한 CC라 붙일 때 적이 지금 저항력%로 막을 수 있음 (Status.Add)
        private void TryStun(DamageInfo info)
        {
            if (!IsAlive) return;
            float chance = info.StunChance + Status.GetStunChanceBonus(info);
            if (chance <= 0f) return;
            if (chance < 1f && Random.value >= chance) return;
            Status.Add(new ImmobilizeEffect("기절"));
        }

        // 즉사 (도끼 사형 선고·사형 집행, 수업 28): 피해 계산 없이 남은 체력을 다 깎음. info는 즉사를 부른 그 공격 (보상·처치 기록용)
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
            Status.OnDeath(kill);                           // 풀로 돌아가기 전에 (원석·금맥의 보상 등)
            if (EnemyManager.Instance != null) EnemyManager.Instance.HandleDeath(kill);
        }
    }
}
