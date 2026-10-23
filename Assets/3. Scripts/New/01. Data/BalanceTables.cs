using UnityEngine;

namespace SquareTD
{
    // 티어 하나가 1티어 값에 주는 변화 (브론즈에 견준 누적값)
    public readonly struct TierRow
    {
        public readonly int Action;             // 더함 (음수면 빨라짐)
        public readonly float Power;            // 곱함
        public readonly int AttackCount;        // 더함
        public readonly float MaxStack;         // 곱함
        public readonly float StackValue;       // 곱함
        public readonly int Mastery;            // 더함


        public TierRow(int action, float power, int attackCount, float maxStack, float stackValue, int mastery)
        {
            Action = action;
            Power = power;
            AttackCount = attackCount;
            MaxStack = maxStack;
            StackValue = stackValue;
            Mastery = mastery;
        }

        public static readonly TierRow Identity = new TierRow(0, 1f, 0, 1f, 1f, 0);      // 아무것도 바꾸지 않는 줄
    }

    // 컬러 강화 레벨 하나가 주는 변화 (레벨 1에 견준 누적값)
    public readonly struct ColorLevelRow
    {
        public readonly int AttackCount;        // 더함 (광역 기준. 단일은 GetColorLevelRow가 두 배로 돌려줌)
        public readonly float AbilityValue;     // 곱함
        public readonly float StackValue;       // 곱함
        public readonly float MaxStack;         // 곱함
        public readonly int Mastery;            // 더함

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

    // 20번 10장 [티어가 올리는 스탯] · 8장 [컬러 강화 — 젬]의 표. 밸런스 숫자는 여기에만 둠
    public static class BalanceTables
    {
        public const int MaxColorLevel = 5;             // 젬으로 갈 수 있는 끝
        public const int MaxColorLevelWithBonus = 6;    // 대지의 만개로만 닿음

        // 인덱스 = 티어 (0번은 안 씀)
        private static readonly TierRow[] s_tierRows =
        {
            TierRow.Identity,                                   // 0 (안 씀)
            new TierRow(0, 1f, 0, 1f, 1f, 0),                   // 1 브론즈
            new TierRow(-1, 1f, 0, 1f, 1f, 0),                  // 2 실버
            new TierRow(-1, 3f, 0, 1f, 1f, 1),                  // 3 골드
            new TierRow(-2, 3f, 1, 1/2f, 2f, 1),                // 4 미스릴
            new TierRow(-5, 15f, 1, 1/2f, 2f, 3),               // 5 다이아
        };

        // 인덱스 = 레벨 (0번은 안 씀)
        private static readonly ColorLevelRow[] s_colorLevelRows =
        {
            ColorLevelRow.Identity,
            new ColorLevelRow(0, 1f, 1f, 1f, 0),                // 1
            new ColorLevelRow(0, 2f, 1f, 1f, 0),                // 2
            new ColorLevelRow(1, 2f, 1f, 1f, 0),                // 3
            new ColorLevelRow(1, 2f, 2f, 1/2f, 0),              // 4
            new ColorLevelRow(2, 4f, 2f, 1/2f, 1),              // 5
            new ColorLevelRow(2, 6f, 2f, 1/2f, 2),              // 6
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
                case TowerColor.Red:                    // 광역·단일: 스킬 2가 자주 나오게 줄어듦
                case TowerColor.Blue:
                    return s_attackDuration[tier];

                case TowerColor.Purple:                 // 디버프: 효과가 오래 남게 늘어남
                    return s_debuffDuration[tier];

                default:                                // 버프(옐로우)는 Duration을 쓰지 않음
                    return 0;
            }
        }

        public static ColorLevelRow GetColorLevelRow(int level, TowerColor color)
        {
            level = Mathf.Clamp(level, 1, MaxColorLevelWithBonus);     // 0 이하는 1, 7 이상은 6으로
            ColorLevelRow row = s_colorLevelRows[level];

            if (color != TowerColor.Blue) return row;

            // 단일은 AttackCount만 두 배. 필드가 readonly라 고칠 수 없어서 새로 만듦
            return new ColorLevelRow(row.AttackCount * 2, row.AbilityValue, row.StackValue, row.MaxStack, row.Mastery);
        }

        public static int GetColorUpgradeCost(int level)
        {
            if (level < 1 || level >= MaxColorLevel) return 0;         // 레벨 5 이상(또는 잘못된 값)은 더 못 올림
            return s_colorUpgradeCosts[level];
        }
    }
}
