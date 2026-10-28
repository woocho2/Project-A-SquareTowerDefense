namespace SquareTD
{
    // 방패 디버프 — 서프레스(스킬 1) · 락다운(스킬 2) (20번 4-7장)
    //   방어력 −(0.001 × Power × AbilityValue × 스택)% (저항력만큼 줄어듦, 다른 방어력 감소와 더함)
    //   락다운은 적의 최대 행동(행동 주기) +1 — 정수형이라 저항할 수 없음
    //   타격 시너지: 타격 공격(방패·해머의 기본 공격·스킬)에 맞으면 기절 확률 +5%(서프레스) / +10%(락다운)
    //     기절 확률이 없는 공격은 그 확률로, 있는 공격(배쉬 20%)은 거기에 더함. 기절 자체를 막는 것은 강한 CC 저항
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

    // 서프레스·락다운이 적에게 걸려 있는 동안 하는 일. 수치는 ShieldDebuffRule이 넘김
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

        // 타워의 기본 공격·스킬이고 그 타워가 타격이면
        public override float GetStunChanceBonus(DamageInfo info)
        {
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return 0f;
            if (info.Attribute != AttackAttribute.Blunt) return 0f;

            return m_stunBonus;            
        }
    }
}
