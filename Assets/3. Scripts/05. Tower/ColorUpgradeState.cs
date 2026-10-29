using System;

namespace SquareTD
{
    // 컬러별 강화 레벨
    public class ColorUpgradeState
    {
        private readonly int[] m_levels = { 1, 1, 1, 1, 1 };

        public event Action<TowerColor, int> LevelChanged;

        public int GetLevel(TowerColor color) => m_levels[(int)color];

        public int GetCost(TowerColor color) => BalanceTables.GetColorUpgradeCost(GetLevel(color));
        public bool CanUpgrade(TowerColor color) => GetLevel(color) < BalanceTables.MaxColorLevel;

        public bool TryUpgrade(TowerColor color)
        {
            if (!CanUpgrade(color)) return false;

            m_levels[(int)color]++;

            LevelChanged?.Invoke(color, GetLevel(color));
            return true;
        }
    }
}
