using System;

namespace SquareTD
{
    // 문양이 판 전체에서 함께 쓰는 값 (어둠의 카운트)
    public class EmblemSharedState
    {
        public const int KillCount = 1;
        public const int BlackHoleKillCount = 5;

        public float DarknessCount { get; private set; }

        public event Action<float> DarknessChanged;

        public void AddDarkness(float amount)
        {
            if (amount <= 0f) return;
            DarknessCount += amount;
            DarknessChanged?.Invoke(DarknessCount);
        }

        public static float GetKillCount(KillInfo kill, bool hasDarknessBuffTower)
        {
            float count = 0f;
            if (hasDarknessBuffTower) count += KillCount;
            if (kill.Damage.Source == DamageSource.Ground && kill.Damage.Emblem == TowerEmblem.Darkness) count += BlackHoleKillCount;
            return count;
        }

        private static EmblemSharedState s_outsideGame = new EmblemSharedState();

        public static EmblemSharedState Current => GameSession.Instance != null ? GameSession.Instance.Shared : s_outsideGame;

        public static void ResetOutsideGame() => s_outsideGame = new EmblemSharedState();
    }
}
