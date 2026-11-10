using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 빛 디버프 — 쉬머 · 배니시 (실명 스택, 디버프 타워도 실명을 쌓아 100마다 초월 카운트 +Tier, 초월 웨이브의 배니시는 모든 적 즉사,
    //                           배니시 최대 체력 -10%, 빛 타워가 실명인 적을 치면 최종 피해 +20%)
    public class LightDebuffRule : DebuffRule
    {
        public const string TowerBlindKey = "실명";
        public const float BlindPerTranscend = 100f;
        public const float VanishMaxHpCut = 0.1f;
        public const float LightFinalDamage = 0.2f;

        public override void OnApply(SupportCast cast, EnemyController enemy)
        {
            float amount = Resist(enemy, cast.Amount);
            BlindEffect.Add(enemy, amount);
            AddTowerBlind(cast.Owner, cast.Amount);
        }

        public override void OnSkill2(SupportCast cast)
        {
            if (!EmblemSharedState.Current.IsTranscendWave || EnemyManager.Instance == null) return;
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
                enemy.Execute(new DamageInfo { Attacker = cast.Owner, Source = DamageSource.Skill, Emblem = TowerEmblem.Light });
        }

        public override StatusEffect CreateEffect(SupportCast cast, EnemyController enemy) =>
            cast.IsSkill2 ? new LightDebuffEffect(cast, Mathf.Min(Resist(enemy, VanishMaxHpCut), 0.95f)) : null;

        public static void AddTowerBlind(TowerController tower, float amount)
        {
            if (tower == null || tower.State == null || amount <= 0f) return;
            float before = tower.State.GetCounter(TowerBlindKey);
            tower.State.AddCounter(TowerBlindKey, amount);
            int crossed = Mathf.FloorToInt(tower.State.GetCounter(TowerBlindKey) / BlindPerTranscend) - Mathf.FloorToInt(before / BlindPerTranscend);
            if (crossed > 0) EmblemSharedState.Current.AddTranscend(crossed * tower.Definition.Tier);
        }
    }

    public class LightDebuffEffect : EmblemDebuffEffect
    {
        private readonly float m_maxHpCut;

        public LightDebuffEffect(SupportCast cast, float maxHpCut)
            : base(cast, $"최대 체력 −{maxHpCut * 100f:0.#}%, 빛 타워에게 실명인 동안 최종 피해 +20%")
        {
            m_maxHpCut = maxHpCut;
        }

        public override void OnAdded(EnemyController enemy)
        {
            if (m_maxHpCut > 0f) enemy.Health.ScaleMax(1f - m_maxHpCut);
        }

        public override void OnRemoved(EnemyController enemy)
        {
            if (m_maxHpCut > 0f) enemy.Health.ScaleMax(1f / (1f - m_maxHpCut));
        }

        public override float ModifyIncomingDamage(EnemyController enemy, DamageInfo info, float damage)
        {
            TowerController attacker = info.Attacker;
            if (attacker == null || attacker.Definition.Emblem != TowerEmblem.Light || !BlindEffect.IsBlind(enemy)) return damage;
            return damage * (1f + LightDebuffRule.LightFinalDamage);
        }
    }
}
