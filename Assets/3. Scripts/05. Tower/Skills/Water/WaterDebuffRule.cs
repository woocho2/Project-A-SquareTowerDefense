namespace SquareTD
{
    // 물 디버프 — 젖음(스킬 1) · 침수(스킬 2) (20번 4-10장)
    //   이동력에 변화를 주는 공격(ChangesMovement)에 받는 피해 +(0.001 × Power × AbilityValue × 스택)%, 일반 공격에는 그 절반
    //   다른 받는 피해 증가(락 온, 침식 …)와는 더함. 저항력만큼 줄어듦
    //   침수의 고유 효과: 맵 버프(길 위의 특수 타일)를 50%만 받음 — 이 50%도 저항력만큼 줄어듦 (저항 50%인 보스는 75% 받음)
    public class WaterDebuffRule : DebuffRule
    {
        public const float MapBuffCut = 0.5f;

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float damageTaken = Resist(enemy, SkillFormula.ZoneValue(zone));
            float mapCut = isSkill2 ? Resist(enemy, MapBuffCut) : 0f;
            return new WaterDebuffEffect(zone, damageTaken, mapCut);
        }
    }

    // 젖음·침수가 적에게 걸려 있는 동안. 값은 WaterDebuffRule이 저항까지 계산해서 넘김
    public class WaterDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_damageTaken;       // 이동력을 바꾼 공격에 더하는 받는 피해 (일반 공격은 절반)

        public WaterDebuffEffect(Zone zone, float damageTaken, float mapCut)
            : base(TowerEmblem.Water, zone.Stage, zone.Owner.Stats.Duration, damageTaken,
                   $"이동력을 바꾼 공격에 받는 피해 +{damageTaken * 100f:0.##}% (일반 공격 +{damageTaken * 50f:0.##}%)" + (mapCut > 0f ? $", 맵 버프 −{mapCut * 100f:0.#}%" : ""),
                   MapModifiers(mapCut))
        {
            m_damageTaken = damageTaken;
        }

        private static StatModifier[] MapModifiers(float mapCut) => mapCut > 0f
            ? new[] { StatModifier.Flat(KeyOf(TowerEmblem.Water), StatType.MapBuffRate, -mapCut) }
            : new StatModifier[0];

        // 받는 피해 배율에 더함: 피해 × (배율 + 증가) ÷ 배율 (곱하지 않고 다른 증가와 더해지게)
        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            float increase = info.ChangesMovement ? m_damageTaken : m_damageTaken * 0.5f;
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            return taken > 0f ? damage * (taken + increase) / taken : damage;
        }
    }
}
