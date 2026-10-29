using System.Collections.Generic;

namespace SquareTD
{
    // 문양 없는 존이 쓰는 시험용 버프·디버프 규칙
    public class TestBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            float value = zone.Stage == ZoneStage.Skill2 ? 0.2f : 0.1f;
            return new List<StatModifier> { StatModifier.Percent(source, StatType.Power, value) };
        }
    }

    public class TestDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            float value = Resist(enemy, zone.Stage == ZoneStage.Skill2 ? 0.2f : 0.1f);
            string key = EmblemDebuffEffect.KeyOf(zone.Emblem);
            return new EmblemDebuffEffect(zone.Emblem, zone.Stage, zone.Owner.Stats.Duration, value,
                $"방어 −{value * 100f:0.#}%",
                StatModifier.Percent(key, StatType.Defense, -value));
        }
    }
}
