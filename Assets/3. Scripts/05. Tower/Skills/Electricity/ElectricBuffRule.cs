using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 전기 버프 — 펄스 · 하이퍼 펄스 (AttackCount ×3, 값 브론즈 20: 공격력 (값 − 100)%, 타격마다 전하 +값, 치명타 확률 −4%p (티어가 오르면 풀림),
    //                                하이퍼 펄스 공격마다 스킬 카운트 +1, 전기 타워는 전하 2배)
    public class ElectricBuffRule : BuffRule
    {
        public const float ElectricChargeMultiplier = 2f;
        public const float HyperSkillCount = 1f;
        public const string HyperReadyKey = "하이퍼 펄스 스킬 카운트";
        public const float BronzeValue = 20f;
        public const float PenaltyBase = 100f;
        public const float BronzeCritRate = -0.04f;

        public static float Value(SupportCast cast) => cast.Bronze(BronzeValue);
        public static float PowerRate(SupportCast cast) => (Value(cast) - PenaltyBase) * SkillFormula.PercentRate;

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            int attackCount = Mathf.Max(1, Mathf.RoundToInt(receiver.Stats.GetBase(StatType.AttackCount)));
            return new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.AttackCount, attackCount * 2),
                StatModifier.Percent(source, StatType.Power, PowerRate(cast)),
                StatModifier.Flat(source, StatType.CritRate, cast.Penalty(BronzeCritRate)),
            };
        }

        public override void OnReceiverActed(SupportCast cast, TowerController receiver)
        {
            if (cast.IsSkill2) receiver.State.Counters[HyperReadyKey] = 1f;
        }

        public override void OnReceiverHit(SupportCast cast, TowerController receiver, ITarget target)
        {
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive) return;

            float charge = Value(cast) * (IsSynergy(cast, receiver) ? ElectricChargeMultiplier : 1f);
            ChargeEffect.Add(enemy, charge);

            if (cast.IsSkill2 && receiver.State.GetCounter(HyperReadyKey) > 0f)
            {
                receiver.State.Counters[HyperReadyKey] = 0f;
                receiver.AdvanceSkillCount(HyperSkillCount, target);
            }
        }

        public static float ChargeMultiplier(TowerController tower) =>
            HasSkill2(tower, TowerEmblem.Electricity) && tower.Definition.Emblem == TowerEmblem.Electricity ? ElectricChargeMultiplier : 1f;
    }
}
