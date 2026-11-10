using System;
using System.Collections.Generic;

namespace SquareTD
{
    // 어둠 버프 — 디바우어 · 글러트니 (침식 카운트 +값, 카운트 단계표, 글러트니 타격마다 0.5% 즉사(+50), 어둠 타워는 1%)
    public class DarknessBuffRule : BuffRule
    {
        public const float ExecuteChance = 0.005f;
        public const float DarkExecuteChance = 0.01f;
        public const float ExecuteCount = 50f;

        private readonly Func<float> m_count;

        public DarknessBuffRule(Func<float> count)
        {
            m_count = count;
        }

        public override List<StatModifier> GetModifiers(SupportCast cast, TowerController receiver, string source)
        {
            float value = cast.Rate;
            float count = m_count();
            var modifiers = new List<StatModifier>();
            if (count >= 100f) modifiers.Add(StatModifier.Percent(source, StatType.Power, value));
            if (count >= 200f) modifiers.Add(StatModifier.Flat(source, StatType.CritRate, value));
            if (count >= 300f) modifiers.Add(StatModifier.Flat(source, StatType.CritDamage, value));
            if (count >= 400f) modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, 1f));
            if (count >= 500f) modifiers.Add(StatModifier.Flat(source, StatType.MaxAction, -1f));
            if (count >= 600f) modifiers.Add(StatModifier.Flat(source, StatType.RewardBonus, 1.5f));
            if (count >= 700f) modifiers.Add(StatModifier.Percent(source, StatType.AbilityValue, value));
            if (count >= 800f) modifiers.Add(StatModifier.Flat(source, StatType.Mastery, 1f));
            if (count >= 900f) modifiers.Add(StatModifier.Flat(source, StatType.ForceFixedDamage, 1f));
            if (count >= 1000f) modifiers.Add(StatModifier.Flat(source, StatType.FinalDamage, value));
            return modifiers;
        }

        public override void OnOwnerActed(TowerController owner) => EmblemSharedState.Current.AddDarkness(SupportCast.Skill1(owner).Amount);

        public override void OnSkill2(SupportCast cast) => EmblemSharedState.Current.AddDarkness(cast.Amount);

        public override void OnReceiverActed(SupportCast cast, TowerController receiver) => SupportBuffs.Rebuild(receiver, TowerEmblem.Darkness);

        public override void OnReceiverHit(SupportCast cast, TowerController receiver, ITarget target)
        {
            if (!cast.IsSkill2) return;
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive) return;

            float chance = receiver.Definition.Emblem == TowerEmblem.Darkness ? DarkExecuteChance : ExecuteChance;
            if (!Chance.Roll(chance)) return;
            enemy.Execute(new DamageInfo { Attacker = receiver, Source = DamageSource.Skill, Emblem = TowerEmblem.Darkness });
            EmblemSharedState.Current.AddDarkness(ExecuteCount);
        }
    }
}
