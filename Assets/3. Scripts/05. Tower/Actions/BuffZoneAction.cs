using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SquareTD
{
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
            bool ownerStack = SkillRegistry.GetBuffRule(tower.Definition.Emblem).UsesOwnerStack;
            if (ownerStack) tower.State.Stack.Add(stats.AttackCount);      // 방패: 행동마다 타워 자신에게 한 번 (존 수와 무관)

            foreach (Zone zone in m_zones.GetByOwner(tower))
            {
                if (!zone.IsPlaced) continue;

                ZoneStage before = zone.Stage;

                if (ownerStack) zone.SetStack(tower.State.Stack.Current, stats.Stack);
                else zone.AddStack(stats.AttackCount, stats.Stack);

                if (zone.Stage != before) m_zones.NotifyChanged(zone);

                TowerController receiver = m_slots.Get(zone.CellIndex);
                if (receiver != null && receiver.IsAttackTower) GiveBuff(zone, receiver);
            }
            yield break;
        }

        public override void OnRemoved(TowerController tower)
        {
            foreach (Zone zone in m_zones.GetByOwner(tower))
            {
                for (int i = 0; i < m_slots.Count; i++)
                {
                    TowerController receiver = m_slots.Get(i);
                    if (receiver != null) TakeBuff(zone, receiver);
                }
            }
            m_zones.RemoveByOwner(tower);
        }

        public static string SourceOf(Zone zone) => $"{EmblemNames.GetEmblemName(zone.Emblem)} 버프존";

        public static void GiveBuff(Zone zone, TowerController receiver)
        {
            string source = SourceOf(zone);
            receiver.State.Modifiers.RemoveBySource(source);
            foreach (StatModifier modifier in SkillRegistry.GetBuffRule(zone.Emblem).GetModifiers(zone, receiver, source))
                receiver.State.Modifiers.Add(modifier);
            receiver.State.ReceivedZoneBuffs[zone] = source;
        }

        public static void TakeBuff(Zone zone, TowerController receiver)
        {
            if (!receiver.State.ReceivedZoneBuffs.TryGetValue(zone, out string source)) return;
            receiver.State.Modifiers.RemoveBySource(source);
            receiver.State.ReceivedZoneBuffs.Remove(zone);
        }

        public static void RefreshReceived(TowerController tower, ZoneRegistry zones)
        {
            var ended = new List<Zone>();
            foreach (Zone zone in tower.State.ReceivedZoneBuffs.Keys)
            {
                if (!zones.All.Contains(zone) || zone.CellIndex != tower.SlotIndex) ended.Add(zone);
            }
            foreach (Zone zone in ended) TakeBuff(zone, tower);
        }
    }
}
