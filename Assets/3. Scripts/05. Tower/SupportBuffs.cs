using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 공격 타워가 받는 문양별 버프 (스킬 1은 존에 서 있는 동안, 스킬 2는 남은 행동 수만큼. 같은 문양이면 스킬 2만)
    public class SupportBuffEntry
    {
        public Zone Zone;
        public SupportCast? Skill2;
        public int Skill2Left;

        public bool IsEmpty => Zone == null && Skill2Left <= 0;

        public SupportBuffEntry Clone() => new SupportBuffEntry { Zone = Zone, Skill2 = Skill2, Skill2Left = Skill2Left };
    }

    public static class SupportBuffs
    {
        public static string SourceOf(TowerEmblem emblem) => $"{EmblemNames.GetEmblemName(emblem)} 버프";

        public static SupportCast? GetActive(TowerController tower, TowerEmblem emblem)
        {
            if (tower == null || tower.State == null) return null;
            if (!tower.State.SupportBuffs.TryGetValue(emblem, out SupportBuffEntry entry)) return null;
            if (entry.Skill2.HasValue && entry.Skill2Left > 0) return entry.Skill2;
            if (entry.Zone != null && entry.Zone.Owner != null && entry.Zone.Owner.State != null) return SupportCast.Skill1(entry.Zone.Owner);
            return null;
        }

        public static void GiveSkill1(Zone zone, TowerController receiver)
        {
            Entry(receiver, zone.Emblem).Zone = zone;
            Rebuild(receiver, zone.Emblem);
        }

        public static void TakeSkill1(Zone zone, TowerController receiver)
        {
            if (!receiver.State.SupportBuffs.TryGetValue(zone.Emblem, out SupportBuffEntry entry) || entry.Zone != zone) return;
            entry.Zone = null;
            Rebuild(receiver, zone.Emblem);
        }

        public static void GiveSkill2(SupportCast cast, TowerController receiver)
        {
            SupportBuffEntry entry = Entry(receiver, cast.Emblem);
            entry.Skill2 = cast;
            entry.Skill2Left = cast.Duration;
            Rebuild(receiver, cast.Emblem);
            if (GameSession.Instance != null) GameSession.Instance.RefreshTowerLimits();
        }

        public static void CountDown(TowerController receiver)
        {
            foreach (TowerEmblem emblem in new List<TowerEmblem>(receiver.State.SupportBuffs.Keys))
            {
                SupportBuffEntry entry = receiver.State.SupportBuffs[emblem];
                if (entry.Skill2Left <= 0) continue;

                entry.Skill2Left--;
                if (entry.Skill2Left > 0) continue;

                entry.Skill2 = null;
                Rebuild(receiver, emblem);
                if (GameSession.Instance != null) GameSession.Instance.RefreshTowerLimits();
            }
        }

        public static void Rebuild(TowerController receiver, TowerEmblem emblem)
        {
            string source = SourceOf(emblem);
            receiver.State.Modifiers.RemoveBySource(source);

            SupportCast? active = GetActive(receiver, emblem);
            if (active == null)
            {
                if (receiver.State.SupportBuffs.TryGetValue(emblem, out SupportBuffEntry entry) && entry.IsEmpty)
                    receiver.State.SupportBuffs.Remove(emblem);
                return;
            }

            foreach (StatModifier modifier in SkillRegistry.GetBuffRule(emblem).GetModifiers(active.Value, receiver, source))
                receiver.State.Modifiers.Add(modifier);
        }

        public static void RebuildAll(TowerController receiver)
        {
            foreach (TowerEmblem emblem in new List<TowerEmblem>(receiver.State.SupportBuffs.Keys)) Rebuild(receiver, emblem);
        }

        public static void ForEachActive(TowerController receiver, Action<BuffRule, SupportCast> call)
        {
            foreach (TowerEmblem emblem in new List<TowerEmblem>(receiver.State.SupportBuffs.Keys))
            {
                SupportCast? active = GetActive(receiver, emblem);
                if (active.HasValue) call(SkillRegistry.GetBuffRule(emblem), active.Value);
            }
        }

        public static void RefreshZones(TowerController receiver, ZoneRegistry zones)
        {
            foreach (SupportBuffEntry entry in new List<SupportBuffEntry>(receiver.State.SupportBuffs.Values))
            {
                Zone zone = entry.Zone;
                if (zone == null) continue;
                if (!zones.GetByOwner(zone.Owner).Contains(zone) || zone.CellIndex != receiver.SlotIndex) TakeSkill1(zone, receiver);
            }
        }

        private static SupportBuffEntry Entry(TowerController receiver, TowerEmblem emblem)
        {
            if (!receiver.State.SupportBuffs.TryGetValue(emblem, out SupportBuffEntry entry))
            {
                entry = new SupportBuffEntry();
                receiver.State.SupportBuffs[emblem] = entry;
            }
            return entry;
        }
    }
}
