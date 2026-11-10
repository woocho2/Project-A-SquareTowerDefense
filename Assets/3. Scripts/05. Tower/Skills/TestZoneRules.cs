using System.Collections.Generic;

namespace SquareTD
{
    // 문양 없는 존이 쓰는 시험용 버프·디버프 규칙 (공격력 +Power% / 방어력 −Power%)
    public class TestBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            return new List<StatModifier> { StatModifier.Percent(source, StatType.Power, cast.Rate) };
        }
    }

    public class TestDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            float value = Resist(enemy, cast.Rate);
            string key = EmblemDebuffEffect.KeyOf(cast.Emblem);
            return new EmblemDebuffEffect(cast, $"방어 −{value * 100f:0.#}%", StatModifier.Percent(key, StatType.Defense, -value));
        }
    }
}
