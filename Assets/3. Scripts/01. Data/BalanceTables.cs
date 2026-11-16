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
        public readonly float AbilityValue;

        public TierRow(int action, float power, int attackCount, float maxStack, float stackValue, int mastery, float skillCount, float abilityValue)
        {
            Action = action;
            Power = power;
            AttackCount = attackCount;
            MaxStack = maxStack;
            StackValue = stackValue;
            Mastery = mastery;
            SkillCount = skillCount;
            AbilityValue = abilityValue;
        }

        public static readonly TierRow Identity = new TierRow(0, 1f, 0, 1f, 1f, 0, 1f, 1f);
    }

    public readonly struct ColorLevelRow
    {
        public readonly float AbilityValue;
        public readonly float StackValue;
        public readonly float MaxStack;
        public readonly int Mastery;

        public ColorLevelRow(float abilityValue, float stackValue, float maxStack, int mastery)
        {
            AbilityValue = abilityValue;
            StackValue = stackValue;
            MaxStack = maxStack;
            Mastery = mastery;
        }

        public static readonly ColorLevelRow Identity = new ColorLevelRow(1f, 1f, 1f, 0);
    }

    public static class BalanceTables
    {
        public const int MaxColorLevel = 5;

        private static readonly TierRow[] s_attackTierRows =
        {
            TierRow.Identity,
            new TierRow(0, 1f, 0, 1f, 1f, 0, 1f, 1f),
            new TierRow(0, 1.5f, 0, 1f, 1f, 0, 0.95f, 1.25f),
            new TierRow(0, 2.45f, 1, 1f, 1f, 0, 0.9f, 5f / 2f),
            new TierRow(-1, 6.15f, 1, 0.75f, 2f, 1, 0.8f, 25f / 4f),
        };

        private static readonly TierRow[] s_supportTierRows =
        {
            TierRow.Identity,
            new TierRow(0, 1f, 0, 1f, 1f, 0, 1f, 1f),
            new TierRow(0, 1.5f, 0, 1f, 1f, 0, 0.95f, 1.25f),
            new TierRow(0, 2f, 1, 1f, 1f, 0, 0.9f, 5f / 2f),
            new TierRow(-1, 4f, 1, 0.75f, 2f, 1, 0.8f, 25f / 4f),
        };

        private static readonly ColorLevelRow[] s_attackColorLevelRows =
        {
            ColorLevelRow.Identity,
            new ColorLevelRow(1f, 1f, 1f, 0),
            new ColorLevelRow(1.25f, 1.1f, 1f, 0),
            new ColorLevelRow(1.5f, 1.2f, 1f, 0),
            new ColorLevelRow(2f, 1.5f, 1/2f, 0),
            new ColorLevelRow(3f, 2f, 1/2f, 1),
        };

        private static readonly ColorLevelRow[] s_supportColorLevelRows =
        {
            ColorLevelRow.Identity,
            new ColorLevelRow(1f, 1f, 1f, 0),
            new ColorLevelRow(1.25f, 1.1f, 1f, 0),
            new ColorLevelRow(1.5f, 1.2f, 1f, 0),
            new ColorLevelRow(2f, 1.5f, 0.8f, 0),
            new ColorLevelRow(3f, 2f, 0.7f, 1),
        };

        private static readonly int[] s_colorUpgradeCosts = { 0, 10, 20, 40, 80, 0 };

        public static bool IsSupport(TowerColor color) => color == TowerColor.Yellow || color == TowerColor.Purple;

        public static TierRow GetTierRow(int tier) => GetTierRow(tier, TowerColor.Red);

        public static TierRow GetTierRow(int tier, TowerColor color)
        {
            TierRow[] rows = IsSupport(color) ? s_supportTierRows : s_attackTierRows;
            if (tier < 1 || tier >= rows.Length) return TierRow.Identity;
            return rows[tier];
        }

        public static ColorLevelRow GetColorLevelRow(int level, TowerColor color)
        {
            level = Mathf.Clamp(level, 1, MaxColorLevel);
            return (IsSupport(color) ? s_supportColorLevelRows : s_attackColorLevelRows)[level];
        }

        public static int GetColorUpgradeCost(int level)
        {
            if (level < 1 || level >= MaxColorLevel) return 0;
            return s_colorUpgradeCosts[level];
        }
    }
}
