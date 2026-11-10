using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 버프 — 프로스트 아머 · 앱솔루트 제로 (최대 Action +4, 공격력·치명타 확률·치명타 피해, 앱솔루트 제로 Mastery +1, 얼음 타워는 Action +2만)
    public class IceBuffRule : BuffRule
    {
        public const int ActionPenalty = 4;
        public const int IceActionPenalty = 2;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            int action = IsSynergy(cast, receiver) ? IceActionPenalty : ActionPenalty;
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.MaxAction, action),
                StatModifier.Percent(source, StatType.Power, cast.Rate),
                StatModifier.Flat(source, StatType.CritRate, cast.Rate),
                StatModifier.Flat(source, StatType.CritDamage, cast.Rate),
            };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.Mastery, 1f));
            return modifiers;
        }
    }
}
