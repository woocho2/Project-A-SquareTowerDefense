using UnityEngine;

namespace SquareTD
{
    // 티어·컬러 강화 레벨이 스탯에 주는 값 표
    public readonly struct TierRow
    {
        public readonly int Action;
        public readonly float Power;
        public readonly int AttackCount;
        public readonly float MaxStack;
        public readonly float StackValue;
        public readonly int Mastery;
        public readonly float SkillCount;

        public TierRow(int action, float power, int attackCount, float maxStack, float stackValue, int mastery, float skillCount)
        {
            Action = action;
            Power = power;
            AttackCount = attackCount;
            MaxStack = maxStack;
            StackValue = stackValue;
            Mastery = mastery;
            SkillCount = skillCount;
        }

        public static readonly TierRow Identity = new TierRow(0, 1f, 0, 1f, 1f, 0, 1f);
    }

    public readonly struct ColorLevelRow
    {
        public readonly int AttackCount;
        public readonly float AbilityValue;
        public readonly float StackValue;
        public readonly float MaxStack;
        public readonly int Mastery;

        public ColorLevelRow(int attackCount, float abilityValue, float stackValue, float maxStack, int mastery)
        {
            AttackCount = attackCount;
            AbilityValue = abilityValue;
            StackValue = stackValue;
            MaxStack = maxStack;
            Mastery = mastery;
        }

        public static readonly ColorLevelRow Identity = new ColorLevelRow(0, 1f, 1f, 1f, 0);
    }

    public static class BalanceTables
    {
        public const int MaxColorLevel = 5;

        private static readonly TierRow[] s_tierRows =
        {
            TierRow.Identity,
            new TierRow(0, 1f, 0, 1f, 1f, 0, 1f),
            new TierRow(-1, 2f, 0, 0.95f, 1f, 0, 0.95f),
            new TierRow(-2, 4f, 1, 0.9f, 1f, 0, 0.9f),
            new TierRow(-3, 8f, 1, 0.8f, 1.5f, 0, 0.8f),
            new TierRow(-4, 16f, 2, 0.5f, 2.5f, 1, 0.5f),
        };

        private static readonly ColorLevelRow[] s_colorLevelRows =
        {
            ColorLevelRow.Identity,
            new ColorLevelRow(0, 1f, 1f, 1f, 0),
            new ColorLevelRow(0, 2f, 1.5f, 1f, 0),
            new ColorLevelRow(1, 4f, 2f, 1f, 0),
            new ColorLevelRow(1, 8f, 2.5f, 1/2f, 0),
            new ColorLevelRow(2, 16f, 4f, 1/2f, 1),
        };

        private static readonly int[] s_colorUpgradeCosts = { 0, 4, 12, 48, 240, 0 };

        public static TierRow GetTierRow(int tier)
        {
            if (tier < 1 || tier >= s_tierRows.Length) return TierRow.Identity;
            return s_tierRows[tier];
        }

        public static ColorLevelRow GetColorLevelRow(int level, TowerColor color)
        {
            level = Mathf.Clamp(level, 1, MaxColorLevel);
            ColorLevelRow row = s_colorLevelRows[level];

            if (color != TowerColor.Blue) return row;

            return new ColorLevelRow(row.AttackCount * 2, row.AbilityValue, row.StackValue, row.MaxStack, row.Mastery);
        }

        public static int GetColorUpgradeCost(int level)
        {
            if (level < 1 || level >= MaxColorLevel) return 0;
            return s_colorUpgradeCosts[level];
        }
    }
}
