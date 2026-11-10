namespace SquareTD
{
    // 도끼 디버프 — 데스 센텐스 · 엑스큐션 (즉사 기준, 엑스큐션 받는 피해 +10%, 베기에 맞으면 기준 +10%)
    public class AxeDebuffRule : DebuffRule
    {
        public const float ExecutionDamageTaken = 0.1f;
        public const float SlashBonus = 0.1f;

        public override void OnApply(SupportCast cast, EnemyController enemy)
        {
            if (cast.IsSkill2) return;
            float threshold = Resist(enemy, cast.Rate);
            if (enemy.CurrentHp < enemy.MaxHp * threshold)
                enemy.Execute(new DamageInfo { Attacker = cast.Owner, Source = DamageSource.Dot, Emblem = TowerEmblem.Axe });
        }

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            if (!cast.IsSkill2) return null;
            return new AxeDebuffEffect(cast, Resist(enemy, cast.Rate), Resist(enemy, SlashBonus), Resist(enemy, ExecutionDamageTaken));
        }
    }

    public class AxeDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_threshold;
        private readonly float m_slashBonus;

        public AxeDebuffEffect(SupportCast cast, float threshold, float slashBonus, float damageTaken)
            : base(cast, $"체력 {threshold * 100f:0.##}% 미만이면 즉사 (베기면 {(threshold + slashBonus) * 100f:0.##}%), 받는 피해 +{damageTaken * 100f:0.#}%",
                   StatModifier.Flat(KeyOf(TowerEmblem.Axe), StatType.DamageTaken, damageTaken))
        {
            m_threshold = threshold;
            m_slashBonus = slashBonus;
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
