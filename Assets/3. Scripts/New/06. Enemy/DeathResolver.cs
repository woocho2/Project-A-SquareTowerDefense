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
    }
}
