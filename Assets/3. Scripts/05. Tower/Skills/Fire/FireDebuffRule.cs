namespace SquareTD
{
    // 불 디버프 — 이그나이트 · 인페르노 (화상 스택, 인페르노 동안 화상 피해 2배, 불 타워가 치면 화상 +Tier × 레벨)
    public class FireDebuffRule : DebuffRule
    {
        public override void OnApply(SupportCast cast, EnemyController enemy) => BurnEffect.Add(enemy, Resist(enemy, cast.Amount));

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) =>
            cast.IsSkill2 ? new FireDebuffEffect(cast) : null;
    }

    public class FireDebuffEffect : EmblemDebuffEffect
    {
        public FireDebuffEffect(SupportCast cast)
            : base(cast, "화상의 매 턴 피해 2배, 불 타워에게 맞으면 화상 +(그 타워의 Tier × 레벨)") { }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            TowerController attacker = info.Attacker;
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Fire || !attacker.IsAttackTower) return;
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return;
            BurnEffect.Add(enemy, attacker.Definition.Tier * attacker.Stats.ColorLevel);
        }
    }
}
