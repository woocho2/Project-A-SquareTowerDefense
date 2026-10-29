namespace SquareTD
{
    // 웨이브·사이클별 적 체력 배율과 적 수 한도 표
    public static class WaveTable
    {
        private static readonly float[] waveMulti = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f };
        private static readonly int[] cycleMulti = { 1, 2, 4, 8 };

        public static int GetCycle(int wave)
        {
            switch (wave)
            {
                case >= 1 and <= 10:
                    return 1;

                case >= 11 and <= 20:
                    return 2;

                case >= 21 and <= 30:
                    return 3;

                case >= 31 and <= 40:
                    return 4;

                default:
                    return 0;
            }
        }

        public static int GetOrder(int wave)
        {
            return ((wave - 1) % 10) + 1;
        }

        public static int GetNormalCount(int wave)
        {
            switch (GetOrder(wave))
            {
                case >= 1 and <= 4:
                    return 8 * GetCycle(wave);

                case >= 6 and <= 9:
                    return 10 * GetCycle(wave);

                case 5:
                case 10:
                    return 0;

                default:
                    return 0;
            }
        }

        public static bool HasMiddleBoss(int wave)
        {
            return GetOrder(wave) == 5;
        }

        public static bool HasFinalBoss(int wave)
        {
            return GetOrder(wave) == 10;
        }

        public static float GetHpMultiplier(int wave, EnemyKind kind)
        {
            float order = waveMulti[GetOrder(wave) - 1];
            int cycle = cycleMulti[GetCycle(wave) - 1];

            switch (kind)
            {
                case EnemyKind.None:
                    return 0;

                case EnemyKind.Normal:
                    return order * cycle;

                case EnemyKind.MiddleBoss:
                case EnemyKind.FinalBoss:
                    return cycle;

                default:
                    return 0;
            }
        }

        public static float GetDefenseMultiplier(int wave, EnemyKind kind)
        {
            float order = waveMulti[GetOrder(wave) - 1];
            int cycle = cycleMulti[GetCycle(wave) - 1];

            switch (kind)
            {
                case EnemyKind.None:
                    return 0;

                case EnemyKind.Normal:
                    return order * cycle;

                case EnemyKind.MiddleBoss:
                case EnemyKind.FinalBoss:
                    return cycle;

                default:
                    return 0;
            }
        }

        public static float GetModeStatMultiplier(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.None:
                    return 0;

                case GameMode.Story:
                    return 0.75f;

                case GameMode.Normal:
                    return 1;

                case GameMode.Hard:
                    return 1.5f;

                default:
                    return 0;
            }
        }

        public static int GetEnemyLimit(GameMode mode, int cycle)
        {
            switch (mode)
            {
                case GameMode.None:
                    return 0;

                case GameMode.Story:
                    return cycle * 25;

                case GameMode.Normal:
                    return cycle * 20;

                case GameMode.Hard:
                    return cycle * 15;

                default:
                    return 0;
            }
        }

        public static void GetKillReward(EnemyKind kind, int spawnWave, out int gold, out int gem)
        {
            int cycle = GetCycle(spawnWave);
            int rewardMulti = cycle * cycle;

            switch (kind)
            {
                case EnemyKind.None:
                    gold = 0;
                    gem = 0;
                    return;

                case EnemyKind.Normal:
                    gold = 10 * GetCycle(spawnWave);
                    gem = 0;
                    return;

                case EnemyKind.MiddleBoss:
                    gold = 500 * rewardMulti;
                    gem = 10 * rewardMulti;
                    return;

                case EnemyKind.FinalBoss:
                    gold = 1000 * rewardMulti;
                    gem = 20 * rewardMulti;
                    return;

                default:
                    gold = 0;
                    gem = 0;
                    return;
            }
        }
    }
}
