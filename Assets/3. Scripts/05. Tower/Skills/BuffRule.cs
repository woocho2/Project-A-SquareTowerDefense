using System.Collections.Generic;

namespace SquareTD
{
    // 버프 규칙의 베이스 (문양마다 하나)
    public abstract class BuffRule
    {
        public abstract List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source);

        public virtual bool UsesOwnerStack => false;

        public virtual int GetLimitModifier(TowerController owner, int baseLimit) => 0;

        public virtual void OnGiveBuff(Zone zone, TowerController receiver) { }

        public virtual void OnTaken(Zone zone, TowerController receiver) { }

        public virtual void OnReceiverActed(Zone zone, TowerController receiver) { }

        public virtual void OnReceiverHit(Zone zone, TowerController receiver, ITarget target) { }

        public virtual bool OnZoneTurn(Zone zone) => false;

        protected static bool IsSynergy(Zone zone, TowerController receiver)
        {
            AttackAttribute attribute = zone.Owner.Definition.Attribute;
            if (attribute != AttackAttribute.None) return receiver.Definition.Attribute == attribute;
            return receiver.Definition.Emblem == zone.Emblem;
        }

        public static ZoneStage ReceivedStage(TowerController tower, TowerEmblem emblem)
        {
            ZoneStage best = ZoneStage.None;
            if (tower == null || tower.State == null) return best;

            foreach (Zone zone in tower.State.ReceivedZoneBuffs.Keys)
            {
                if (zone.Emblem != emblem || zone.Stage == ZoneStage.Discharge) continue;
                if (zone.Stage > best) best = zone.Stage;
            }
            return best;
        }
    }
}
