namespace SquareTD
{
    // 도끼 디버프 — 사형 선고 · 사형 집행
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

    public class AxeDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_threshold;
        private readonly float m_slashBonus;
        private readonly float m_hpBonus;

        public AxeDebuffEffect(Zone zone, float threshold, float slashBonus, float hpBonus, int speedBonus)
            : base(TowerEmblem.Axe, zone.Stage, zone.Owner.Stats.Duration, threshold,
                   $"체력 {threshold * 100f:0.##}% 미만이면 즉사 (베기면 {(threshold + slashBonus) * 100f:0.##}%)" + (hpBonus > 0f ? $", 최대 체력 +{hpBonus * 100f:0}%, 최대 이동력 +{speedBonus}" : ""),
                   SpeedModifiers(speedBonus))
        {
            m_threshold = threshold;
            m_slashBonus = slashBonus;
            m_hpBonus = hpBonus;
        }

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

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!enemy.IsAlive) return;

            float threshold = m_threshold;
            if (info.Attribute == AttackAttribute.Slash) threshold += m_slashBonus;
            if (enemy.CurrentHp < enemy.MaxHp * threshold) enemy.Execute(info);
        }
    }
}
