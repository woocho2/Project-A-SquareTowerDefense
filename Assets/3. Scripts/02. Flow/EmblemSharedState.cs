using System;

namespace SquareTD
{
    // 판 전체에서 함께 쓰는 값: 어둠의 침식 카운트, 빛의 초월 카운트와 초월 웨이브
    public class EmblemSharedState
    {
        public const int BlackHoleKillCount = 5;
        public const float TranscendGoal = 20f;
        public const float TranscendPerWave = 1f;

        public float DarknessCount { get; private set; }
        public float TranscendCount { get; private set; }
        public bool IsTranscendWave { get; private set; }

        private bool m_transcendNext;
        private bool m_extendNext;

        public event Action<float> DarknessChanged;
        public event Action<float> TranscendChanged;

        public void AddDarkness(float amount)
        {
            if (amount <= 0f) return;
            DarknessCount += amount;
            DarknessChanged?.Invoke(DarknessCount);
        }

        public static float GetKillCount(KillInfo kill)
        {
            if (kill.Damage.Source == DamageSource.Ground && kill.Damage.Emblem == TowerEmblem.Darkness) return BlackHoleKillCount;
            return 0f;
        }

        public void AddTranscend(float amount)
        {
            if (amount <= 0f) return;
            TranscendCount += amount;
            if (TranscendCount >= TranscendGoal)
            {
                TranscendCount = 0f;
                m_transcendNext = true;
            }
            TranscendChanged?.Invoke(TranscendCount);
        }

        public void ExtendTranscend()
        {
            if (IsTranscendWave) m_extendNext = true;
        }

        public void OnWaveStarted()
        {
            IsTranscendWave = m_transcendNext || m_extendNext;
            m_transcendNext = false;
            m_extendNext = false;
            AddTranscend(TranscendPerWave);
        }

        private static EmblemSharedState s_outsideGame = new EmblemSharedState();

        public static EmblemSharedState Current => GameSession.Instance != null ? GameSession.Instance.Shared : s_outsideGame;

        public static void ResetOutsideGame() => s_outsideGame = new EmblemSharedState();
    }
}
