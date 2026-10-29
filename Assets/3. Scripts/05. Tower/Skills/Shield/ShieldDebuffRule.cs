namespace SquareTD
{
    // 방패 디버프 — 서프레스 · 락다운
    public class ShieldDebuffRule : DebuffRule
    {
        public override StatusEffect CreateEffect(Zone zone, EnemyController enemy)
        {
            bool isSkill2 = zone.Stage == ZoneStage.Skill2;
            float defenseCut = Resist(enemy, SkillFormula.ZoneValue(zone));
            float stunBonus = isSkill2 ? 0.1f : 0.05f;
            return new ShieldDebuffEffect(zone, defenseCut, stunBonus, isSkill2 ? 1 : 0);
        }
    }

    public class ShieldDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_stunBonus;

        public ShieldDebuffEffect(Zone zone, float defenseCut, float stunBonus, int actionPenalty)
            : base(TowerEmblem.Shield, zone.Stage, zone.Owner.Stats.Duration, defenseCut,
                   $"방어력 −{defenseCut * 100f:0.##}%" + (actionPenalty > 0 ? $", 행동 주기 +{actionPenalty}" : "") + $", 타격에 맞으면 기절 확률 +{stunBonus * 100f:0}%",
                   Modifiers(defenseCut, actionPenalty))
        {
            m_stunBonus = stunBonus;
        }

        private static StatModifier[] Modifiers(float defenseCut, int actionPenalty)
        {
            string source = KeyOf(TowerEmblem.Shield);
            StatModifier defense = StatModifier.Percent(source, StatType.Defense, -defenseCut);
            if (actionPenalty <= 0) return new[] { defense };
            return new[] { defense, StatModifier.Flat(source, StatType.MaxAction, actionPenalty) };
        }

        public override float GetStunChanceBonus(DamageInfo info)
        {
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return 0f;
            if (info.Attribute != AttackAttribute.Blunt) return 0f;

            return m_stunBonus;
        }
    }
}
