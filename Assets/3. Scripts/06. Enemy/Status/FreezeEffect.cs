namespace SquareTD
{
    // 빙결 — 다음 행동 한 번 못 움직임
    public class FreezeEffect : ImmobilizeEffect
    {
        public const float ThawRate = 0.25f;
        public const float ColdBonus = 0.25f;

        public float DamageWhileFrozen { get; private set; }

        public FreezeEffect() : base("빙결") { }

        public static bool IsFrozen(ITarget target)
        {
            EnemyStatusEffects status = target?.Body != null ? target.Body.Status : target?.Status;
            return status != null && status.Find(ImmobilizeKey) is FreezeEffect;
        }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (info.Attacker == null) return damage;
            if (BuffRule.ReceivedStage(info.Attacker, TowerEmblem.Ice) != ZoneStage.Skill2) return damage;
            return damage * (1f + ColdBonus);
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            if (damage > 0f) DamageWhileFrozen += damage;
        }

        public override void OnExpire(EnemyController enemy)
        {
            if (!enemy.IsAlive || DamageWhileFrozen <= 0f) return;
            enemy.TakeDamage(new DamageInfo
            {
                Source = DamageSource.Dot,
                Emblem = TowerEmblem.Ice,
                BaseDamage = DamageWhileFrozen * ThawRate,
                ForceFixed = true,
            });
        }

        public override string Describe() => $"빙결: 다음 행동 한 번 0 / 0, 풀릴 때 받은 피해 {DamageWhileFrozen:0.#}의 25%";
    }
}
