using System.Collections.Generic;

namespace SquareTD
{
    // 아직 규칙이 없는 문양이 쓰는 시험 규칙 (수업 26의 ZoneTestRules를 BuffRule·DebuffRule 모양으로 옮김)
    //   SkillRegistry 표에 없는 문양만 이것으로. 문양이 다 들어오는 수업 40에서 지움
    //   버프존: 존 위 공격 타워의 위력 +10% (스킬 2면 +20%)
    public class TestBuffRule : BuffRule
    {
        public override List<StatModifier> GetModifiers(Zone zone, TowerController receiver, string source)
        {
            float value = zone.Stage == ZoneStage.Skill2 ? 0.2f : 0.1f;
            return new List<StatModifier> { StatModifier.Percent(source, StatType.Power, value) };
        }
    }

    //   디버프존: 밟은 적의 방어력 −10% (스킬 2면 −20%, 영구). 저항만큼 줄어듦
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
