using System;
using UnityEngine;

namespace SquareTD
{
    // 빛 디버프 — 잔영 · 잔상
    public class LightDebuffRule : DebuffRule
    {
        public const float ShadowRelay = 2f;
        public const float AfterimageRelay = 4f;

        private readonly Func<PathOccupancy> m_occupancy;
        private readonly Func<int, Vector3> m_cellPosition;

        public LightDebuffRule(Func<PathOccupancy> occupancy, Func<int, Vector3> cellPosition)
        {
            m_occupancy = occupancy;
            m_cellPosition = cellPosition;
        }

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            float hpRate = SkillFormula.ZoneValue(zone);
            float relay = zone.Stage == ZoneStage.Skill2 ? AfterimageRelay : ShadowRelay;
            return new LightDebuffEffect(zone, hpRate, relay, m_occupancy, m_cellPosition);
        }
    }

    public class LightDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_hpRate;
        private readonly float m_relay;
        private readonly Func<PathOccupancy> m_occupancy;
        private readonly Func<int, Vector3> m_cellPosition;

        public LightDebuffEffect(Zone zone, float hpRate, float relay, Func<PathOccupancy> occupancy, Func<int, Vector3> cellPosition)
            : base(TowerEmblem.Light, zone.Stage, zone.Owner.Stats.Duration, hpRate,
                   $"더미 (본체 최대 체력의 {hpRate * 100f:0.##}%), 더미가 받은 피해 ×{relay:0}을 본체가 받음")
        {
            m_hpRate = hpRate;
            m_relay = relay;
            m_occupancy = occupancy;
            m_cellPosition = cellPosition;
        }

        public override void OnAdded(EnemyController enemy)
        {
            DummyTarget existing = DummyTarget.Find(enemy);
            if (existing != null)
            {
                existing.RelayMultiplier = m_relay;
                return;
            }

            Vector3 position = m_cellPosition != null ? m_cellPosition(enemy.PathIndex) : enemy.transform.position;
            DummyTarget.Spawn(enemy, m_occupancy?.Invoke(), position, enemy.MaxHp * m_hpRate, m_relay);
        }

        public override void OnActed(EnemyController enemy) => DummyTarget.Find(enemy)?.CountDownPrism();

        public override void OnDeath(EnemyController enemy, KillInfo kill) => DummyTarget.Find(enemy)?.Despawn();
    }
}
