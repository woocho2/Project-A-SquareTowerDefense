using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 더미 — 적의 분신 (빛 디버프)
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
        public EnemyStatusEffects Status => Body.Status;
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

        private void OnBodyStatusChanged()
        {
            if (Body.Status.Find(EmblemDebuffEffect.KeyOf(TowerEmblem.Light)) == null) Despawn();
        }

        public static DummyTarget Find(EnemyController body) =>
            body != null && s_byBody.TryGetValue(body, out DummyTarget dummy) ? dummy : null;

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
            else Object.DestroyImmediate(m_object);
        }

        public float PreviewDamage(DamageInfo info) =>
            DamageCalculator.Calculate(info, Stats.Get(StatType.Defense), Stats.Get(StatType.DamageTaken), Stats.Get(StatType.FixedDamageRatio));

        public void AddPrism(int stacks) => m_prism += System.Math.Max(0, stacks);

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
