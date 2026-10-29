namespace SquareTD
{
    // 적이 죽었을 때의 보상 계산
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

            gold = UnityEngine.Mathf.FloorToInt(gold * (1f + goldRate + reward) + 0.001f);
            gem = UnityEngine.Mathf.FloorToInt(gem * (1f + reward) + 0.001f);
        }
    }
}
