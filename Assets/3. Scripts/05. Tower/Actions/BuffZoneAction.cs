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
            BuffRule rule = SkillRegistry.GetBuffRule(zone.Emblem);
            receiver.State.Modifiers.RemoveBySource(source);
            foreach (StatModifier modifier in rule.GetModifiers(zone, receiver, source))
                receiver.State.Modifiers.Add(modifier);
            receiver.State.ReceivedZoneBuffs[zone] = source;
            rule.OnGiveBuff(zone, receiver);                // 확률로 터지는 버프 (바람·빛)
        }

        public static void TakeBuff(Zone zone, TowerController receiver)
        {
            if (!receiver.State.ReceivedZoneBuffs.TryGetValue(zone, out string source)) return;
            receiver.State.Modifiers.RemoveBySource(source);
            receiver.State.ReceivedZoneBuffs.Remove(zone);
            SkillRegistry.GetBuffRule(zone.Emblem).OnTaken(zone, receiver);     // 확률로 터진 시간 버프도 같이 끝남
        }

        // ⓪ 적 턴마다: 단계가 도는 존(불·전기)의 시간을 줄이고, 단계가 바뀌었으면 존 위 타워의 버프를 바로 다시 줌
        public static void RunZoneTurns(ZoneRegistry zones, TowerSlots slots)
        {
            foreach (Zone zone in new List<Zone>(zones.All))
            {
                if (!zone.IsBuff || !zone.IsPlaced) continue;
                if (!SkillRegistry.GetBuffRule(zone.Emblem).OnZoneTurn(zone)) continue;

                zones.NotifyChanged(zone);
                TowerController receiver = slots.Get(zone.CellIndex);
                if (receiver != null && receiver.IsAttackTower) GiveBuff(zone, receiver);
            }
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
