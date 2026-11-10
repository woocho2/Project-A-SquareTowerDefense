using System.Collections.Generic;

namespace SquareTD
{
    // 버프 규칙의 베이스 (문양마다 하나). 스킬 1·스킬 2의 값은 SupportCast로 받음
    public abstract class BuffRule
    {
        public abstract List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source);

        public virtual int GetLimitModifier(int baseLimit) => 0;

        public virtual void OnSkill2(SupportCast cast) { }

        public virtual void OnOwnerActed(TowerController owner) { }

        public virtual void OnGiveSkill1(SupportCast cast, TowerController receiver) { }

        public virtual void OnReceiverActed(SupportCast cast, TowerController receiver) { }

        public virtual void OnReceiverHit(SupportCast cast, TowerController receiver, ITarget target) { }

        protected static bool IsSynergy(SupportCast cast, TowerController receiver)
        {
            if (!cast.IsSkill2) return false;
            if (cast.Attribute != AttackAttribute.None) return receiver.Definition.Attribute == cast.Attribute;
            return receiver.Definition.Emblem == cast.Emblem;
        }

        public static bool HasSkill2(TowerController tower, TowerEmblem emblem)
        {
            SupportCast? cast = SupportBuffs.GetActive(tower, emblem);
            return cast.HasValue && cast.Value.IsSkill2;
        }
    }
}
