namespace SquareTD
{
    // 빙결 (21번 #136, 수업 37) — 기절과 같은 행동 불가(다음 행동 0 / 0, 강한 CC). 키도 같아 기절·속박과 겹치지 않음
    //   해동: 빙결 중에 받은 피해의 25%를 빙결이 끝날 때 한 번 더 받음 (방어 무시)
    //   한기 존에 선 타워의 공격은 빙결된 적에게 +25% (20번 4-13장)
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
