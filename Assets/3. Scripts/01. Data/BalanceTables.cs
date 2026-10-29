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

        public TierRow(int action, float power, int attackCount, float maxStack, float stackValue, int mastery)
        {
            Action = action;
            Power = power;
            AttackCount = attackCount;
            MaxStack = maxStack;
            StackValue = stackValue;
            Mastery = mastery;
        }

        public static readonly TierRow Identity = new TierRow(0, 1f, 0, 1f, 1f, 0);
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
        public const int MaxColorLevelWithBonus = 6;

        private static readonly TierRow[] s_tierRows =
        {
            TierRow.Identity,
            new TierRow(0, 1f, 0, 1f, 1f, 0),
            new TierRow(-1, 1f, 0, 1f, 1f, 0),
            new TierRow(-1, 3f, 0, 1f, 1f, 1),
            new TierRow(-2, 3f, 1, 1/2f, 2f, 1),
            new TierRow(-5, 15f, 1, 1/2f, 2f, 3),
        };

        private static readonly ColorLevelRow[] s_colorLevelRows =
        {
            ColorLevelRow.Identity,
            new ColorLevelRow(0, 1f, 1f, 1f, 0),
            new ColorLevelRow(0, 2f, 1f, 1f, 0),
            new ColorLevelRow(1, 2f, 1f, 1f, 0),
            new ColorLevelRow(1, 2f, 2f, 1/2f, 0),
            new ColorLevelRow(2, 4f, 2f, 1/2f, 1),
            new ColorLevelRow(2, 6f, 2f, 1/2f, 2),
        };

        private static readonly int[] s_attackDuration = { 0, 0, -1, -1, -1, -2 };
        private static readonly int[] s_debuffDuration = { 0, 0, 1, 1, 1, 2 };
        private static readonly int[] s_colorUpgradeCosts = { 0, 4, 12, 48, 240, 0 };

        public static TierRow GetTierRow(int tier)
        {
            if (tier < 1 || tier >= s_tierRows.Length) return TierRow.Identity;
            return s_tierRows[tier];
        }

        public static int GetTierDurationDelta(int tier, TowerColor color)
        {
            if (tier < 1 || tier > 5) return 0;

            switch (color)
            {
                case TowerColor.Red:
                case TowerColor.Blue:
                    return s_attackDuration[tier];

                case TowerColor.Purple:
                    return s_debuffDuration[tier];

                default:
                    return 0;
            }
        }

        public static ColorLevelRow GetColorLevelRow(int level, TowerColor color)
        {
            level = Mathf.Clamp(level, 1, MaxColorLevelWithBonus);
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
