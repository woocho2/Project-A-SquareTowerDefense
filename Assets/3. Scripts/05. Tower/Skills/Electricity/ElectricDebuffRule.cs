using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 전기 디버프 — 스태틱 필드 · 스태틱 스톰 (전하 부여, 스태틱 스톰이 발동할 때 적의 전하 2배,
    //                                         전기 타워가 전하인 적을 치면 Tier × 레벨마리에게 연쇄, 피해 × Tier × 5%)
    public class ElectricDebuffRule : DebuffRule
    {
        public const float ChainRatePerTier = 0.05f;

        public override void OnApply(SupportCast cast, EnemyController enemy) => ChargeEffect.Add(enemy, cast.Amount);

        public override void OnSkill2(SupportCast cast)
        {
            if (EnemyManager.Instance == null) return;
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
            {
                float charge = ChargeEffect.Get(enemy);
                if (charge > 0f) ChargeEffect.Add(enemy, charge);
            }
        }

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) =>
            cast.IsSkill2 ? new ElectricDebuffEffect(cast) : null;
    }

    public class ElectricDebuffEffect : EmblemDebuffEffect
    {
        public ElectricDebuffEffect(SupportCast cast)
            : base(cast, "전기 타워에게 맞으면 (그 타워의 Tier × 레벨)마리에게 연쇄") { }

        public override void OnDamageTaken(EnemyController enemy, DamageInfo info, float damage)
        {
            TowerController attacker = info.Attacker;
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Electricity || !attacker.IsAttackTower) return;
            if (info.Source != DamageSource.Basic && info.Source != DamageSource.Skill) return;
            if (damage <= 0f || ChargeEffect.Get(enemy) <= 0f || EnemyManager.Instance == null) return;

            int chains = attacker.Definition.Tier * attacker.Stats.ColorLevel;
            float chainDamage = damage * attacker.Definition.Tier * ElectricDebuffRule.ChainRatePerTier;

            var others = new List<EnemyController>(EnemyManager.Instance.Registry.Alive);
            others.Remove(enemy);
            others.Sort((a, b) =>
            {
                int byDistance = Mathf.Abs(a.PathIndex - enemy.PathIndex).CompareTo(Mathf.Abs(b.PathIndex - enemy.PathIndex));
                return byDistance != 0 ? byDistance : ChargeEffect.Get(b).CompareTo(ChargeEffect.Get(a));
            });

            for (int i = 0; i < chains && i < others.Count; i++)
            {
                others[i].TakeDamage(new DamageInfo { Attacker = attacker, Source = DamageSource.Relay, Emblem = TowerEmblem.Electricity, BaseDamage = chainDamage, ForceFixed = true });
            }
        }
    }
}
