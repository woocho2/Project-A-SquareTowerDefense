using System.Collections.Generic;

namespace SquareTD
{
    // 창 버프 — 원 포인트 · 원 스트라이크 (모아 찌르기: AttackCount 1, 공격력 × 원래 AttackCount. 원 스트라이크 치명타 확률·방어 관통 +10%, 찌르기는 맞은 적 받는 치유 -40%)
    public class SpearBuffRule : BuffRule
    {
        public const float StrikeCritRate = 0.1f;
        public const float StrikeArmorPenetration = 0.1f;
        public const float PierceHealingCut = 0.4f;
        public const string HealingCutKey = "원 스트라이크 치유 감소";

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            var modifiers = new List<StatModifier>
            {
                StatModifier.Flat(source, StatType.AttackCountLock, 1f),
                StatModifier.Percent(source, StatType.Power, cast.Rate),
            };
            if (cast.IsSkill2)
            {
                modifiers.Add(StatModifier.Flat(source, StatType.CritRate, StrikeCritRate));
                modifiers.Add(StatModifier.Flat(source, StatType.ArmorPenetration, StrikeArmorPenetration));
            }
            return modifiers;
        }

        public override void OnReceiverHit(SupportCast cast, TowerController receiver, ITarget target)
        {
            if (!IsSynergy(cast, receiver)) return;
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive) return;
            enemy.Status.Add(new TimedModifierEffect(HealingCutKey, cast.Duration, PierceHealingCut, "받는 치유 −40%",
                StatModifier.Flat(HealingCutKey, StatType.HealingTaken, -PierceHealingCut)));
        }
    }
}
