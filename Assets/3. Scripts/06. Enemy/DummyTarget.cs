using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 더미 — 적의 분신 (21번 #133, 수업 39 빛 디버프 잔영·잔상). ITarget의 두 번째 구현
    //   본체가 디버프존을 밟은 그 칸에 서서 움직이지 않음. 타워의 타겟이 되고(점유에 들어감), 처치 보상·적 수 한도에는 세지 않음(명단 밖)
    //   체력 = 본체 최대 체력 × 식의 %. 체력 밖의 수치(방어력 등)는 본체와 같음
    //   더미가 받은 피해 × 2(잔영) / × 4(잔상)가 본체에게 바로 들어감 (저항력만큼 줄어듦). 한 적에게 더미는 하나
    //   더미에 남은 프리즘은 기본 공격에 터지며 본체 최대 체력의 (10 × 스택)% — 그 피해도 더미가 받아 본체에게 넘김
    //   그림은 아직 없음 (연출은 Codex) — 탄이 날아갈 자리만 있는 빈 오브젝트
    public class DummyTarget : ITarget
    {
        public const float PrismRatePerStack = 0.1f;

        private static readonly Dictionary<EnemyController, DummyTarget> s_byBody = new Dictionary<EnemyController, DummyTarget>();

        private readonly PathOccupancy m_occupancy;
        private readonly GameObject m_object;
        private int m_prism;

        public EnemyController Body { get; }
        public int PathIndex { get; }
        public float MaxHp { get; }
        public float CurrentHp { get; private set; }
        public float RelayMultiplier { get; set; }

        public Transform Transform => m_object != null ? m_object.transform : null;
        public bool IsAlive => CurrentHp > 0f && Body != null && Body.IsAlive;
        public bool IsBoss => false;
        public bool IsDummy => true;
        public EnemyStats Stats => Body.Stats;
        public EnemyStatusEffects Status => Body.Status;     // 치명 보너스 등은 본체에 걸린 것으로
        public int Prism => m_prism;

        public static int Count => s_byBody.Count;

        private DummyTarget(EnemyController body, PathOccupancy occupancy, int pathIndex, Vector3 position, float hp, float relay)
        {
            Body = body;
            m_occupancy = occupancy;
            PathIndex = pathIndex;
            MaxHp = hp;
            CurrentHp = hp;
            RelayMultiplier = relay;

            m_object = new GameObject($"Dummy of {body.name}");
            m_object.transform.position = position;
            m_occupancy?.Add(this, pathIndex);
            Body.Status.Changed += OnBodyStatusChanged;
        }

        // 본체의 빛 디버프(잔영·잔상)가 없어지면 (Duration 끝, 정화) 더미도 사라짐. 같은 디버프로 바뀐 것(다시 밟음)이면 그대로
        private void OnBodyStatusChanged()
        {
            if (Body.Status.Find(EmblemDebuffEffect.KeyOf(TowerEmblem.Light)) == null) Despawn();
        }

        public static DummyTarget Find(EnemyController body) =>
            body != null && s_byBody.TryGetValue(body, out DummyTarget dummy) ? dummy : null;

        // 본체에게 더미가 없을 때만 세움 (하나뿐). 있으면 그 더미를 돌려줌
        public static DummyTarget Spawn(EnemyController body, PathOccupancy occupancy, Vector3 position, float hp, float relay)
        {
            DummyTarget existing = Find(body);
            if (existing != null) return existing;

            var dummy = new DummyTarget(body, occupancy, body.PathIndex, position, hp, relay);
            s_byBody[body] = dummy;
            return dummy;
        }

        public void Despawn()
        {
            if (s_byBody.TryGetValue(Body, out DummyTarget current) && current == this) s_byBody.Remove(Body);
            if (m_object == null) return;

            Body.Status.Changed -= OnBodyStatusChanged;

            m_occupancy?.Remove(this, PathIndex);
            if (Application.isPlaying) Object.Destroy(m_object);
            else Object.DestroyImmediate(m_object);         // 테스트 (에디트 모드)
        }

        public float PreviewDamage(DamageInfo info) =>
            DamageCalculator.Calculate(info, Stats.Get(StatType.Defense), Stats.Get(StatType.DamageTaken), Stats.Get(StatType.FixedDamageRatio));

        public void AddPrism(int stacks) => m_prism += System.Math.Max(0, stacks);

        // 본체가 한 번 행동할 때 (더미는 움직이지 않으니 본체를 따라 셈)
        public void CountDownPrism()
        {
            if (m_prism > 0) m_prism--;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;

            float damage = PreviewDamage(info);
            if (info.Source == DamageSource.Basic && m_prism > 0)
            {
                damage += Body.Resistance.ResistWeak(Body.MaxHp * PrismRatePerStack * m_prism);
                m_prism = 0;
            }

            float taken = Mathf.Min(damage, CurrentHp);
            CurrentHp -= taken;

            Body.TakeDamage(new DamageInfo
            {
                Attacker = info.Attacker,
                Source = DamageSource.Relay,
                Emblem = TowerEmblem.Light,
                BaseDamage = Body.Resistance.ResistWeak(taken * RelayMultiplier),
                ForceFixed = true,
            });

            if (CurrentHp <= 0f) Despawn();
        }

        public void SetSortingOrder(int order) { }
    }
}
