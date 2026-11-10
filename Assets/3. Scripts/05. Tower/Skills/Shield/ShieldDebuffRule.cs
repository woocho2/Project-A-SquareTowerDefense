namespace SquareTD
{
    // 방패 디버프 — 서프레스 · 락다운 (저항력 감소, 락다운 최대 이동력 -2, 타격에 맞으면 받는 최종 피해 +5%)
    public class ShieldDebuffRule : DebuffRule
    {
        public const int LockdownSpeedCut = 2;
        public const float BluntDamageTaken = 0.05f;

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy)
        {
            float cut = Resist(enemy, cast.Amount);
            float bluntTaken = cast.IsSkill2 ? Resist(enemy, BluntDamageTaken) : 0f;
            return new ShieldDebuffEffect(cast, cut, bluntTaken);
        }
    }

    public class ShieldDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_cut;
        private readonly float m_bluntTaken;

        public ShieldDebuffEffect(SupportCast cast, float cut, float bluntTaken)
            : base(cast, $"저항력 −{cut:0.##}%p" + (cast.IsSkill2 ? $", 최대 이동력 −{ShieldDebuffRule.LockdownSpeedCut}, 타격에 받는 최종 피해 +{bluntTaken * 100f:0.#}%" : ""),
                   cast.IsSkill2 ? new[] { StatModifier.Flat(KeyOf(TowerEmblem.Shield), StatType.SpeedMax, -ShieldDebuffRule.LockdownSpeedCut) } : new StatModifier[0])
        {
            m_cut = cut;
            m_bluntTaken = bluntTaken;
        }

        public override void OnAdded(EnemyController enemy) => enemy.Resistance.AddCut(m_cut);
        public override void OnRemoved(EnemyController enemy) => enemy.Resistance.RemoveCut(m_cut);

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (m_bluntTaken <= 0f || info.Attribute != AttackAttribute.Blunt) return damage;
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            return taken > 0f ? damage * (taken + m_bluntTaken) / taken : damage;
        }
    }
}
