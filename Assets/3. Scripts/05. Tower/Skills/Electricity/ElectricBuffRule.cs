using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 전기 버프 — 펄스 · 하이퍼 펄스 (AttackCount ×3, 공격력·치명타 확률·치명타 피해 감소, 타격마다 전하,
    //                                하이퍼 펄스 공격마다 스킬 카운트 +1, 전기 타워는 전하 2배)
    public class ElectricBuffRule : BuffRule
    {
        public const float ElectricChargeMultiplier = 2f;
        public const float HyperSkillCount = 1f;
        public const string HyperReadyKey = "하이퍼 펄스 스킬 카운트";

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            int attackCount = Mathf.Max(1, Mathf.RoundToInt(receiver.Stats.GetBase(StatType.AttackCount)));
            return new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.AttackCount, attackCount * 2),
                StatModifier.Percent(source, StatType.Power, -cast.Rate),
                StatModifier.Flat(source, StatType.CritRate, -cast.Rate),
                StatModifier.Flat(source, StatType.CritDamage, -cast.Rate),
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

            float charge = cast.Amount * (IsSynergy(cast, receiver) ? ElectricChargeMultiplier : 1f);
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
