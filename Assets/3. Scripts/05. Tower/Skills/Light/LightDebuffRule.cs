using System;
using UnityEngine;

namespace SquareTD
{
    // 빛 디버프 — 잔영(스킬 1) · 잔상(스킬 2) (20번 4-15장)
    //   존을 밟은 적의 더미(분신)를 그 칸에 세움. 더미 체력 = 본체 최대 체력 × (0.001 × Power × AbilityValue × 스택)%
    //   더미가 받은 피해의 2배(잔영) / 4배(잔상)가 본체에게 바로 들어감 (저항력만큼 줄어듦). 한 적에게 더미는 하나
    //   잔영의 더미는 Duration이 끝나면 사라짐, 잔상의 더미는 처치되거나 본체가 죽을 때까지. 처치된 뒤 다시 밟으면 새로 세움
    public class LightDebuffRule : DebuffRule
    {
        public const float ShadowRelay = 2f;
        public const float AfterimageRelay = 4f;

        private readonly Func<PathOccupancy> m_occupancy;                   // 더미가 들어갈 점유 (게임에서는 BoardManager의 것)
        private readonly Func<int, Vector3> m_cellPosition;                 // 길 칸 번호 → 더미를 세울 자리

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

    // 잔영·잔상이 적에게 걸려 있는 동안 — 더미를 세우고 치움
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

        // 더미가 이미 있으면(다시 밟음) 그 더미를 그대로 두고 배율만 새 단계로. 없으면(처음, 또는 처치된 뒤) 그 칸에 새로 세움
        //   끝나는 때(Duration·정화)는 더미가 본체의 효과 목록을 보고 스스로 사라짐 (DummyTarget)
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
