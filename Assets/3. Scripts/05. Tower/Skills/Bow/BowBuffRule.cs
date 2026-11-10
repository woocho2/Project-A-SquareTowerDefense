using System.Collections.Generic;

namespace SquareTD
{
    // 활 버프 — 엘븐 사이트 · 스피릿 보우 (거리 비례 추가 피해, 스피릿 보우 사거리 +1, 찌르기는 항상 최대치)
    public class BowBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier> { StatModifier.Flat(source, StatType.DistanceDamage, cast.Rate) };
            if (cast.IsSkill2) modifiers.Add(StatModifier.Flat(source, StatType.Range, 1f));
            if (IsSynergy(cast, receiver)) modifiers.Add(StatModifier.Flat(source, StatType.DistanceDamageBonus, 1f));
            return modifiers;
        }
    }
}
