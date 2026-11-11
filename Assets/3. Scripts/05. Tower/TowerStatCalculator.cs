using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 티어·컬러 레벨·수정자로 최종 스탯을 계산
    public static class TowerStatCalculator
    {
        private static readonly StatType[] s_intStats =
        {
            StatType.Range, StatType.MaxAction, StatType.AttackCount, StatType.Mastery, StatType.MaxStack,
        };

        public static TowerFinalStats Calculate(TowerDefinition definition, TowerDefinition tier1Definition, TowerState state, int colorLevel)
        {
            bool receivesBuffs = definition.Color == TowerColor.Red || definition.Color == TowerColor.Blue;
            int level = Mathf.Clamp(colorLevel, 1, BalanceTables.MaxColorLevel);

            TowerDefinition baseDefinition = tier1Definition ?? definition;
            float[] values = ReadBase(baseDefinition);

            ApplyTier(values, definition.Tier);
            ApplyColorLevel(values, level, definition.Color);

            float[] baseValues = (float[])values.Clone();
            var sources = new List<StatSource>();

            StatModifierList applied = receivesBuffs ? state.Modifiers : AllColorModifiers(state.Modifiers);
            ApplyModifiers(values, applied);
            sources = BuildSources(baseValues, applied);
            ApplyAttackCountLock(values);
            ApplyLimits(values);

            return new TowerFinalStats(values, baseValues, level, sources);
        }

        public static readonly HashSet<string> AllColorSources = new HashSet<string> { Acceleration.Key };

        private static StatModifierList AllColorModifiers(StatModifierList modifiers)
        {
            var list = new StatModifierList();
            foreach (StatModifier modifier in modifiers.All)
            {
                if (AllColorSources.Contains(modifier.Source)) list.Add(modifier);
            }
            return list;
        }

        private static List<StatSource> BuildSources(float[] baseValues, StatModifierList modifiers)
        {
            var sources = new List<StatSource>();

            foreach (StatModifier modifier in modifiers.All)
            {
                float amount;
                if (modifier.IsPercent) amount = baseValues[(int)modifier.Stat] * modifier.Value;
                else amount = modifier.Value;

                sources.Add(new StatSource(modifier.Source, modifier.Stat, amount));
            }

            return sources;
        }

        private static float[] ReadBase(TowerDefinition definition)
        {
            float[] values = new float[TowerFinalStats.StatCount];

            values[(int)StatType.Power] = definition.Power;
            values[(int)StatType.Range] = definition.Range;
            values[(int)StatType.MaxAction] = definition.Action;
            values[(int)StatType.AttackCount] = definition.AttackCount;
            values[(int)StatType.CritRate] = definition.CriticalRate;
            values[(int)StatType.CritDamage] = definition.CriticalDamage;
            values[(int)StatType.Mastery] = definition.Mastery;
            values[(int)StatType.MaxStack] = definition.Stack;
            values[(int)StatType.Duration] = definition.Duration;
            values[(int)StatType.AbilityValue] = definition.AbilityValue;
            values[(int)StatType.StackValue] = 1f;

            return values;
        }

        private static void ApplyTier(float[] values, int tier)
        {
            TierRow row = BalanceTables.GetTierRow(tier);

            values[(int)StatType.MaxAction] += row.Action;
            values[(int)StatType.Power] *= row.Power;
            values[(int)StatType.AttackCount] += row.AttackCount;
            values[(int)StatType.MaxStack] *= row.MaxStack;
            values[(int)StatType.StackValue] *= row.StackValue;
            values[(int)StatType.Mastery] += row.Mastery;
            values[(int)StatType.Duration] *= row.SkillCount;
        }

        private static void ApplyColorLevel(float[] values, int level, TowerColor color)
        {
            ColorLevelRow row = BalanceTables.GetColorLevelRow(level, color);

            values[(int)StatType.AttackCount] += row.AttackCount;
            values[(int)StatType.AbilityValue] *= row.AbilityValue;
            values[(int)StatType.StackValue] *= row.StackValue;
            values[(int)StatType.MaxStack] *= row.MaxStack;
            values[(int)StatType.Mastery] += row.Mastery;
        }

        private static void ApplyModifiers(float[] values, StatModifierList modifiers)
        {
            for (int i = 1; i < values.Length; i++)
            {
                values[i] = modifiers.Apply((StatType)i, values[i]);
            }
        }

        private static void ApplyAttackCountLock(float[] values)
        {
            if (values[(int)StatType.AttackCountLock] <= 0f) return;

            int attackCount = Mathf.Max(1, Mathf.RoundToInt(values[(int)StatType.AttackCount]));
            values[(int)StatType.Power] *= attackCount;
            values[(int)StatType.AttackCount] = 1f;
        }

        private static void ApplyLimits(float[] values)
        {
            foreach (StatType stat in s_intStats)
                values[(int)stat] = Mathf.Round(values[(int)stat]);

            values[(int)StatType.MaxAction] = Mathf.Max(1, values[(int)StatType.MaxAction]);
            values[(int)StatType.Duration] = Mathf.Max(1f, values[(int)StatType.Duration]);
            values[(int)StatType.MaxStack] = Mathf.Max(1, values[(int)StatType.MaxStack]);
            values[(int)StatType.CritRate] = Mathf.Min(1, values[(int)StatType.CritRate]);
            values[(int)StatType.ArmorPenetration] = Mathf.Clamp01(values[(int)StatType.ArmorPenetration]);
        }
    }
}
