namespace SquareTD
{
    // 수업 26의 시험 규칙 두 개 (23번 수업 26). 수업 27부터 문양마다 BuffRule / DebuffRule이 대신하고 이 파일은 지움
    //   버프존: 존 위 공격 타워의 위력 +10% (스킬 2면 +20%)
    //   디버프존: 밟은 적의 방어력 −10% (스킬 2면 −20%, 영구). 약한 CC기라 기본 저항력만큼 줄어듦
    public static class ZoneTestRules
    {
        public static StatModifier[] GetBuffModifiers(Zone zone, string source)
        {
            float value = zone.Stage == ZoneStage.Skill2 ? 0.2f : 0.1f;
            return new[] { StatModifier.Percent(source, StatType.Power, value) };
        }

        public static StatusEffect CreateDebuff(Zone zone, EnemyController enemy)
        {
            float raw = zone.Stage == ZoneStage.Skill2 ? 0.2f : 0.1f;
            float value = enemy.Resistance.ResistWeak(raw);                          // 약한 CC기: × (1 − 기본 저항력%)
            string key = EmblemDebuffEffect.KeyOf(zone.Emblem);
            return new EmblemDebuffEffect(zone.Emblem, zone.Stage, zone.Owner.Stats.Duration, value,
                $"방어 −{value * 100f:0.#}%",
                StatModifier.Percent(key, StatType.Defense, -value));
        }
    }
}
