namespace SquareTD
{
    // 도끼 디버프 — 사형 선고(스킬 1) · 사형 집행(스킬 2) (20번 4-4장)
    //   체력이 최대 체력의 (0.001 × Power × AbilityValue × 스택)% 미만이면 즉사. 피해를 받을 때마다, 피해가 들어간 뒤의 체력으로 판정
    //   사형 선고는 대신 적에게 이득: 최대 체력 +20% (현재 체력도 같은 비율로), 최대 이동력 +1. 사형 집행이 되면 이득은 전부 없어짐
    //   베기 시너지: 베기 공격에 맞을 때는 즉사 기준 +5%(선고) / +10%(집행)
    //   즉사 기준과 베기 보너스는 적의 저항력만큼 줄어듦 (보스도 같은 식). 이득은 줄지 않음
    public class AxeDebuffRule : DebuffRule
    {
        public const float HpBonus = 0.2f;
        public const int SpeedBonus = 1;

        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float threshold = Resist(enemy, SkillFormula.ZoneValue(zone));
            float slashBonus = Resist(enemy, isSkill2 ? 0.1f : 0.05f);
            return isSkill2
                ? new AxeDebuffEffect(zone, threshold, slashBonus, 0f, 0)
                : new AxeDebuffEffect(zone, threshold, slashBonus, HpBonus, SpeedBonus);
        }
    }

    // 사형 선고·사형 집행이 적에게 걸려 있는 동안 하는 일. 값은 AxeDebuffRule이 저항까지 계산해서 넘김
    public class AxeDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_threshold;         // 즉사 기준 (0.012 = 최대 체력의 1.2%)
        private readonly float m_slashBonus;        // 베기 공격일 때 기준에 더함
        private readonly float m_hpBonus;           // 사형 선고: 최대 체력을 늘리는 비율 (사형 집행은 0)

        public AxeDebuffEffect(Zone zone, float threshold, float slashBonus, float hpBonus, int speedBonus)
            : base(TowerEmblem.Axe, zone.Stage, zone.Owner.Stats.Duration, threshold,
                   $"체력 {threshold * 100f:0.##}% 미만이면 즉사 (베기면 {(threshold + slashBonus) * 100f:0.##}%)" + (hpBonus > 0f ? $", 최대 체력 +{hpBonus * 100f:0}%, 최대 이동력 +{speedBonus}" : ""),
                   SpeedModifiers(speedBonus))
        {
            m_threshold = threshold;
            m_slashBonus = slashBonus;
            m_hpBonus = hpBonus;
        }

        // 최대 이동력 +n은 스탯 수정자로 (걸려 있는 동안만, 출처는 효과의 Key)
        private static StatModifier[] SpeedModifiers(int speedBonus) => speedBonus > 0
            ? new[] { StatModifier.Flat(KeyOf(TowerEmblem.Axe), StatType.SpeedMax, speedBonus) }
            : new StatModifier[0];

        public override void OnAdded(EnemyController enemy)
        {
            if (m_hpBonus > 0f) enemy.Health.ScaleMax(1f + m_hpBonus);
        }

        public override void OnRemoved(EnemyController enemy)
        {
            if (m_hpBonus > 0f) enemy.Health.ScaleMax(1f / (1f + m_hpBonus));
        }

        // 피해가 들어간 뒤: 남은 체력이 기준 미만이면 즉사. 즉사를 부른 공격(info)이 처치한 것으로 침
        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!enemy.IsAlive) return;

            float threshold = m_threshold;
            if (info.Attribute == AttackAttribute.Slash) threshold += m_slashBonus;
            if (enemy.CurrentHp < enemy.MaxHp * threshold) enemy.Execute(info);
        }
    }
}
