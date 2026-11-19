using System;

namespace SquareTD
{
    // 판 전체에서 함께 쓰는 값: 어둠의 침식 카운트, 빛의 초월 카운트와 초월 웨이브, 프레이야의 골드 2배 남은 턴, 브륀힐드의 한도 보호, 뇨르드의 멈춤 칸
    public class EmblemSharedState
    {
        public const int BlackHoleKillCount = 5;
        public const float TranscendGoal = 20f;
        public const float TranscendPerWave = 1f;

        public float DarknessCount { get; private set; }
        public float TranscendCount { get; private set; }
        public bool IsTranscendWave { get; private set; }
        public int GoldDoubleTurns { get; private set; }

        public void StartGoldDouble(int turns) => GoldDoubleTurns = Math.Max(GoldDoubleTurns, turns);

        public void CountDownGoldDouble()
        {
            if (GoldDoubleTurns > 0) GoldDoubleTurns--;
        }

        public int LimitShields { get; private set; }

        public void AddLimitShield() => LimitShields++;

        public bool TryUseLimitShield()
        {
            if (LimitShields <= 0) return false;
            LimitShields--;
            return true;
        }

        private readonly System.Collections.Generic.Dictionary<int, int> m_stopTiles = new System.Collections.Generic.Dictionary<int, int>();

        public System.Collections.Generic.IReadOnlyDictionary<int, int> StopTiles => m_stopTiles;

        public void AddStopTile(int pathIndex, int turns)
        {
            if (pathIndex < 0 || turns <= 0) return;
            m_stopTiles[pathIndex] = m_stopTiles.TryGetValue(pathIndex, out int left) ? Math.Max(left, turns) : turns;
        }

        public bool IsStopTile(int pathIndex) => m_stopTiles.ContainsKey(pathIndex);

        public void CountDownStopTiles()
        {
            foreach (int index in new System.Collections.Generic.List<int>(m_stopTiles.Keys))
            {
                int left = m_stopTiles[index] - 1;
                if (left > 0) m_stopTiles[index] = left;
                else m_stopTiles.Remove(index);
            }
        }

        private bool m_transcendNext;
        private bool m_extendNext;
        public bool IsTranscendQueued => m_transcendNext || m_extendNext;

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
