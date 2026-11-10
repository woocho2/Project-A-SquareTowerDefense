namespace SquareTD
{
    // 얼음 디버프 — 프로스트바이트 · 딥 프리즈 (한기 스택, 딥 프리즈는 한기를 모두 없애고 빙결, 그동안 한기·빙결인 적 받는 피해 +50%)
    public class IceDebuffRule : DebuffRule
    {
        public const float DeepFreezeDamageTaken = 0.5f;

        public override void OnApply(SupportCast cast, EnemyController enemy)
        {
            if (!cast.IsSkill2)
            {
                ChillEffect.Add(enemy, Resist(enemy, cast.Amount));
                return;
            }

            if (!ChillEffect.IsChilled(enemy)) return;
            enemy.Status.Remove(ChillEffect.ChillKey);
            enemy.Status.Add(new FreezeEffect());
        }

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) =>
            cast.IsSkill2 ? new IceDebuffEffect(cast, Resist(enemy, DeepFreezeDamageTaken)) : null;
    }

    public class IceDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_damageTaken;

        public IceDebuffEffect(SupportCast cast, float damageTaken)
            : base(cast, $"한기·빙결인 동안 받는 피해 +{damageTaken * 100f:0.#}%")
        {
            m_damageTaken = damageTaken;
        }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (!ChillEffect.IsChilled(enemy) && !FreezeEffect.IsFrozen(enemy)) return damage;
            float taken = enemy.Stats.Get(StatType.DamageTaken);
            return taken > 0f ? damage * (taken + m_damageTaken) / taken : damage;
        }
    }
}
