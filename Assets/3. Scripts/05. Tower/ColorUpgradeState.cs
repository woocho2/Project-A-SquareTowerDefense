using System;

namespace SquareTD
{
    // 플레이어 레벨 (경험치를 채워 올림) — 네 컬러의 강화 레벨로 함께 쓰임
    public class ColorUpgradeState
    {
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpToNext => BalanceTables.GetXpToNext(Level);
        public bool IsMax => Level >= BalanceTables.MaxColorLevel;

        public event Action<int> LevelChanged;
        public event Action XpChanged;

        public int GetLevel(TowerColor color) => Level;

        public int AddXp(int amount)
        {
            if (amount <= 0 || IsMax) return 0;

            int levels = 0;
            Xp += amount;
            while (!IsMax && Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                levels++;
            }
            if (IsMax) Xp = 0;

            XpChanged?.Invoke();
            if (levels > 0) LevelChanged?.Invoke(Level);
            return levels;
        }
    }
}
