using System;

namespace SquareTD
{
    // 문양이 판 전체에서 함께 쓰는 값 (21번 #15, 수업 40) — 어둠의 카운트
    //   판이 끝날 때까지 쌓이고 줄지 않음. 상한 없음. 소수 그대로 쌓고 화면에는 정수로 (20번 4-16장)
    //   주인은 GameSession (판 하나에 하나). 규칙·스킬은 Current로 찾음 — 게임 밖(테스트)에서는 따로 둔 하나
    public class EmblemSharedState
    {
        public const int KillCount = 1;             // 적을 처치하면 +1 (어둠 버프 타워가 판 위에 있을 때만)
        public const int BlackHoleKillCount = 5;    // 블랙홀로 막타를 치면 +5

        public float DarknessCount { get; private set; }

        public event Action<float> DarknessChanged;

        public void AddDarkness(float amount)
        {
            if (amount <= 0f) return;
            DarknessCount += amount;
            DarknessChanged?.Invoke(DarknessCount);
        }

        // 처치 한 번의 카운트: 블랙홀 막타 +5는 어둠 버프 타워가 없어도, 기본 +1은 어둠 버프 타워가 있을 때만
        public static float GetKillCount(KillInfo kill, bool hasDarknessBuffTower)
        {
            float count = 0f;
            if (hasDarknessBuffTower) count += KillCount;
            if (kill.Damage.Source == DamageSource.Ground && kill.Damage.Emblem == TowerEmblem.Darkness) count += BlackHoleKillCount;
            return count;
        }

        private static EmblemSharedState s_outsideGame = new EmblemSharedState();

        public static EmblemSharedState Current => GameSession.Instance != null ? GameSession.Instance.Shared : s_outsideGame;

        public static void ResetOutsideGame() => s_outsideGame = new EmblemSharedState();      // 테스트마다 0부터
    }
}
