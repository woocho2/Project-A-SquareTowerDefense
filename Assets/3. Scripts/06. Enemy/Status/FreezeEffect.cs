namespace SquareTD
{
    // 빙결 — 다음 행동 한 번 행동 불가, 그동안 받은 피해의 30%를 풀릴 때 한 번에
    public class FreezeEffect : ImmobilizeEffect
    {
        public const float ThawRate = 0.3f;

        public float DamageWhileFrozen { get; private set; }

        public FreezeEffect() : base("빙결") { }

        public static bool IsFrozen(ITarget target)
        {
            EnemyStatusEffects status = target?.Body != null ? target.Body.Status : target?.Status;
            return status != null && status.Find(ImmobilizeKey) is FreezeEffect;
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

        public override string Describe() => $"빙결: 다음 행동 한 번 0 / 0, 풀릴 때 받은 피해 {DamageWhileFrozen:0.#}의 30%";
    }
}
