using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 버프 타워의 행동: 존 위 타워에게 스킬 1, 스킬 카운트가 차면 아군 공격 타워 전체에게 스킬 2
    public class BuffZoneAction : TowerAction
    {
        private readonly ZoneRegistry m_zones;
        private readonly TowerSlots m_slots;

        public BuffZoneAction(BoardGrid grid, PathOccupancy occupancy, ZoneRegistry zones, TowerSlots slots)
            : base(grid, occupancy, null)
        {
            m_zones = zones;
            m_slots = slots;
        }

        public override bool CanAct(TowerController tower, TowerFinalStats stats) => true;

        public override IEnumerator Act(TowerController tower, TowerFinalStats stats, AttackHandle handle)
        {
            BuffRule rule = SkillRegistry.GetBuffRule(tower.Definition.Emblem);
            rule.OnOwnerActed(tower);

            foreach (Zone zone in m_zones.GetByOwner(tower))
            {
                if (!zone.IsPlaced) continue;
                TowerController receiver = m_slots.Get(zone.CellIndex);
                if (receiver == null || !receiver.IsAttackTower) continue;

                SupportBuffs.GiveSkill1(zone, receiver);
                rule.OnGiveSkill1(SupportCast.Skill1(tower), receiver);
            }

            int fires = SupportSkillCount.Advance(tower);
            for (int i = 0; i < fires; i++) FireSkill2(tower);
            yield break;
        }

        public void FireSkill2(TowerController tower)
        {
            SupportCast cast = SupportSkillCount.TakeSkill2Cast(tower);
            BuffRule rule = SkillRegistry.GetBuffRule(tower.Definition.Emblem);

            int count = 0;
            for (int i = 0; i < m_slots.Count; i++)
            {
                TowerController receiver = m_slots.Get(i);
                if (receiver == null || !receiver.IsAttackTower) continue;
                SupportBuffs.GiveSkill2(cast, receiver);
                count++;
            }
            rule.OnSkill2(cast);
            Debug.Log($"[Buff] {tower.name} 스킬 2 발동 → 아군 공격 타워 {count}기, 값 {cast.Amount:0.##}, {cast.Duration}번 행동 동안");
        }

        public override void OnRemoved(TowerController tower)
        {
            foreach (Zone zone in m_zones.GetByOwner(tower))
            {
                for (int i = 0; i < m_slots.Count; i++)
                {
                    TowerController receiver = m_slots.Get(i);
                    if (receiver != null) SupportBuffs.TakeSkill1(zone, receiver);
                }
            }
            m_zones.RemoveByOwner(tower);
        }

        public static void RefreshReceived(TowerController tower, ZoneRegistry zones) => SupportBuffs.RefreshZones(tower, zones);
    }

    // 버프·디버프 타워의 공통: 행동마다 차지 +1, 스킬 카운트 +AttackCount (치명이면 × (2 + CriticalDamage))
    public static class SupportSkillCount
    {
        public static int Advance(TowerController tower) => Advance(tower, Random.value);

        public static int Advance(TowerController tower, float critRoll)
        {
            TowerFinalStats stats = tower.Stats;
            tower.State.Stack.Add(1f);

            float gain = stats.AttackCount;
            if (CriticalRoll.Roll(stats.CriticalRate, critRoll)) gain *= CriticalRoll.Multiplier(stats.CriticalDamage);
            return tower.AddSkillCount(gain);
        }

        public static SupportCast TakeSkill2Cast(TowerController tower)
        {
            bool useCharge = tower.State.Stack.IsFull;
            SupportCast cast = SupportCast.Skill2(tower, useCharge);
            if (useCharge) tower.State.Stack.Consume();
            return cast;
        }
    }
}
