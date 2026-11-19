using UnityEngine;

namespace SquareTD
{
    // 티어·플레이어 레벨이 스탯에 주는 값, 경험치, 상점 가격·티어 확률 표
    public readonly struct TierRow
    {
        public readonly int Action;
        public readonly float Power;
        public readonly int AttackCount;
        public readonly int Mastery;
        public readonly float SkillCount;
        public readonly float AbilityValue;

        public TierRow(int action, float power, int attackCount, int mastery, float skillCount, float abilityValue)
        {
            Action = action;
            Power = power;
            AttackCount = attackCount;
            Mastery = mastery;
            SkillCount = skillCount;
            AbilityValue = abilityValue;
        }

        public static readonly TierRow Identity = new TierRow(0, 1f, 0, 0, 1f, 1f);
    }

    public readonly struct ColorLevelRow
    {
        public readonly float AbilityValue;
        public readonly int Mastery;

        public ColorLevelRow(float abilityValue, int mastery)
        {
            AbilityValue = abilityValue;
            Mastery = mastery;
        }

        public static readonly ColorLevelRow Identity = new ColorLevelRow(1f, 0);
    }

    public static class BalanceTables
    {
        public const int MaxColorLevel = 10;

        private static readonly TierRow[] s_attackTierRows =
        {
            TierRow.Identity,
            new TierRow(0, 1f, 0, 0, 1f, 1f),
            new TierRow(0, 1.5f, 0, 0, 0.95f, 1.25f),
            new TierRow(0, 2.45f, 1, 0, 0.9f, 5f / 2f),
            new TierRow(-1, 6.15f, 1, 1, 0.8f, 25f / 4f),
        };

        private static readonly TierRow[] s_supportTierRows =
        {
            TierRow.Identity,
            new TierRow(0, 1f, 0, 0, 1f, 1f),
            new TierRow(0, 1.5f, 0, 0, 0.95f, 2.5f),
            new TierRow(0, 2f, 1, 0, 0.9f, 5f),
            new TierRow(-1, 4f, 1, 1, 0.8f, 10f),
        };

        private static readonly ColorLevelRow[] s_colorLevelRows =
        {
            ColorLevelRow.Identity,
            new ColorLevelRow(1f, 0),
            new ColorLevelRow(1.1f, 0),
            new ColorLevelRow(1.2f, 0),
            new ColorLevelRow(1.35f, 0),
            new ColorLevelRow(1.5f, 1),
            new ColorLevelRow(1.75f, 1),
            new ColorLevelRow(2f, 1),
            new ColorLevelRow(2.5f, 1),
            new ColorLevelRow(3f, 1),
            new ColorLevelRow(4f, 2),
        };

        private static readonly int[] s_xpToNext = { 0, 4, 8, 15, 20, 25, 55, 75, 110, 140, 0 };

        public const int GoldPerXp = 100;
        public const int XpPerPurchase = 1;
        public const int XpPurchaseCost = GoldPerXp * XpPerPurchase;

        public const int ShopSize = 5;
        public const int ShopRerollCost = 50;
        public const float SellRatio = 0.8f;

        private static readonly int[] s_shopPrices = { 0, 250, 600, 1500, 4000 };

        private static readonly float[][] s_tierChances =
        {
            new[] { 1f, 0f, 0f, 0f },
            new[] { 1f, 0f, 0f, 0f },
            new[] { 0.95f, 0.05f, 0f, 0f },
            new[] { 0.85f, 0.15f, 0f, 0f },
            new[] { 0.75f, 0.22f, 0.03f, 0f },
            new[] { 0.6f, 0.35f, 0.05f, 0f },
            new[] { 0.45f, 0.45f, 0.095f, 0.005f },
            new[] { 0.3f, 0.55f, 0.14f, 0.01f },
            new[] { 0.2f, 0.5f, 0.25f, 0.05f },
            new[] { 0.1f, 0.35f, 0.45f, 0.1f },
            new[] { 0f, 0.2f, 0.4f, 0.4f },
        };

        public static int GetShopPrice(int tier) => tier >= 1 && tier < s_shopPrices.Length ? s_shopPrices[tier] : 0;

        public static int GetSellGold(int paidGold) => Mathf.FloorToInt(Mathf.Max(0, paidGold) * SellRatio);

        public static float GetTierChance(int level, int tier)
        {
            if (tier < 1 || tier > 4) return 0f;
            return s_tierChances[Mathf.Clamp(level, 1, MaxColorLevel)][tier - 1];
        }

        public static int RollTier(int level, float roll)
        {
            float[] chances = s_tierChances[Mathf.Clamp(level, 1, MaxColorLevel)];
            float sum = 0f;
            for (int i = 0; i < chances.Length; i++)
            {
                sum += chances[i];
                if (roll < sum) return i + 1;
            }
            for (int i = chances.Length - 1; i >= 0; i--)
                if (chances[i] > 0f) return i + 1;
            return 1;
        }

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
            return s_colorLevelRows[level];
        }

        public static int GetXpToNext(int level)
        {
            if (level < 1 || level >= MaxColorLevel) return 0;
            return s_xpToNext[level];
        }
    }
}
