namespace SquareTD
{
    public class DeathResolver
    {
        private readonly BoardManager m_board;
        private readonly EnemyRegistry m_registry;
        private readonly BossTimer m_bossTimer;
        private readonly CurrencyManager m_currency;
        private readonly GameSession m_session;

        public DeathResolver(BoardManager board, EnemyRegistry registry, BossTimer bossTimer,
                             CurrencyManager currency, GameSession session)
        {
            m_board = board;
            m_registry = registry;
            m_bossTimer = bossTimer;
            m_currency = currency;
            m_session = session;
        }

        public void Resolve(KillInfo kill)
        {
            EnemyController enemy = kill.Enemy;

            WaveTable.GetKillReward(kill.Kind, kill.SpawnWave, out int gold, out int gem);
            GetBonusReward(kill, ref gold, ref gem);
            m_currency.AddGold(gold);
            m_currency.AddGem(gem);

            if (kill.Kind != EnemyKind.Normal)
            {
                m_bossTimer.Remove(enemy);
            }

            if (kill.Kind == EnemyKind.FinalBoss && kill.SpawnWave == WaveManager.LastWave)
            {
                m_session.GameClear();
            }

            m_board.Occupancy.Remove(enemy, enemy.Movement.PathIndex);
            m_registry.Unregister(enemy);

            enemy.Pool.Release(enemy);
        }

        // 처치 보상 보정 (수업 32·40): 원석·금맥(+ 타격 막타)은 골드 +%, 금맥은 10%로 젬 1개,
        //   어둠 카운트 600 존에 선 타워가 처치하면 골드·젬 +150%. 골드 증가끼리는 더하고 소수는 버림
        public static void GetBonusReward(KillInfo kill, ref int gold, ref int gem)
        {
            float goldRate = 0f;
            if (kill.Enemy != null && kill.Enemy.Status != null)
            {
                goldRate += kill.Enemy.Status.GetGoldBonusRate(kill);
                gem += kill.Enemy.Status.RollGemBonus(kill);
            }

            TowerController killer = kill.Damage.Attacker;
            float reward = killer != null && killer.Stats != null ? killer.Stats.Get(StatType.RewardBonus) : 0f;

            gold = UnityEngine.Mathf.FloorToInt(gold * (1f + goldRate + reward) + 0.001f);     // + 0.001: 1111.9999 같은 float 오차가 1111로 버려지지 않게
            gem = UnityEngine.Mathf.FloorToInt(gem * (1f + reward) + 0.001f);
        }
    }
}
