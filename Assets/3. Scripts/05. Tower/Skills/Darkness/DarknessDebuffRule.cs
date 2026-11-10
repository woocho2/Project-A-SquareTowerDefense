using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 디버프 — 미아즈마 · 다크 플레이그 (침식 스택과 같은 양의 침식 카운트, 다크 플레이그 동안 침식인 적 추가 피해 +10%,
    //                                       어둠 타워가 치면 그 타일의 모든 적 침식 +1)
    public class DarknessDebuffRule : DebuffRule
    {
        public const float PlagueBonusDamage = 0.1f;
        public const float DarkTileErosion = 1f;

        public override void OnApply(SupportCast cast, EnemyController enemy)
        {
            ErosionEffect.Add(enemy, cast.Amount);
            EmblemSharedState.Current.AddDarkness(cast.Amount);
        }

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) =>
            cast.IsSkill2 ? new DarknessDebuffEffect(cast) : null;
    }

    public class DarknessDebuffEffect : EmblemDebuffEffect
    {
        public DarknessDebuffEffect(SupportCast cast)
            : base(cast, "침식인 동안 추가 피해 +10%, 어둠 타워에게 맞으면 그 타일의 모든 적 침식 +1") { }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            if (info.Attacker == null || ErosionEffect.Get(enemy) <= 0f) return damage;
            return damage * (1f + DarknessDebuffRule.PlagueBonusDamage);
        }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            TowerController attacker = info.Attacker;
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Darkness || !attacker.IsAttackTower) return;
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return;
            if (BoardManager.Instance == null) return;

            foreach (ITarget other in new List<ITarget>(BoardManager.Instance.Occupancy.GetAt(enemy.PathIndex)))
            {
                EnemyController body = other.Body;
                if (body != null && body.IsAlive && !other.IsDummy) ErosionEffect.Add(body, DarknessDebuffRule.DarkTileErosion);
            }
        }
    }
}
