using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 웨이브와 적 턴의 순서를 돌리는 게임 루프
    public class TurnManager : Singleton<TurnManager>
    {
        public const int EnemyTurnsPerWave = 10;
        public const int LastWaveEnemyTurns = 20;

        public ITurnState Current { get; private set; } = new IdleState();

        public event Action<ITurnState> StateChanged;
        public event Action<int> EnemyTurnEnded;

        private bool m_endTurnRequested;
        [SerializeField] private float m_attackSafetySeconds = 10f;

        private TowerTurnRunner m_towerRunner;
        private EnemyTileEffectApplier m_tileApplier;

        private void Start()
        {
            BoardManager board = BoardManager.Instance;
            m_towerRunner = new TowerTurnRunner(board.Slots, m_attackSafetySeconds, board.Zones, EffectActivity.WaitUntilIdle);
            m_tileApplier = new EnemyTileEffectApplier(board.Grid, board.Tiles, board.Zones);
            StartCoroutine(GameLoop());
        }

        public void OnClickEndTurn()
        {
            if (!Current.CanPlayerAct) return;

            if (TowerManager.Instance != null && TowerManager.Instance.IsMerging)
            {
                Debug.Log("[TurnManager] 자동 합성이 끝난 뒤에 턴을 넘길 수 있음");
                return;
            }

            if (BoardManager.Instance.Zones.HasUnplaced())
            {
                Debug.Log("[TurnManager] 자리를 고르지 않은 존이 있어 턴을 넘길 수 없음 (Z 키)");
                return;
            }

            m_endTurnRequested = true;
        }

        public void EndGame(ITurnState endState)
        {
            ChangeState(endState);
        }

        private void ChangeState(ITurnState next)
        {
            Current.Exit();
            Current = next;
            next.Enter();
            Debug.Log($"[TurnManager] {Current.Name}");
            StateChanged?.Invoke(Current);
        }

        private IEnumerator GameLoop()
        {
            yield return null;

            WaveManager wave = WaveManager.Instance;
            GameSession session = GameSession.Instance;
            while (wave.CurrentWave < WaveManager.LastWave)
            {
                wave.NextWave();
                Debug.Log($"[TurnManager] 웨이브 {wave.CurrentWave} 시작");
                if (!session.CheckBeforeSpawn(EnemyManager.Instance.Registry.Count)) yield break;
                EnemyManager.Instance.SpawnWave(wave.CurrentWave);

                ChangeState(new PlayerTurnState());
                m_endTurnRequested = false;
                while (!m_endTurnRequested) yield return null;

                ChangeState(new EnemyTurnState());
                int turns = wave.IsLastWave ? LastWaveEnemyTurns : EnemyTurnsPerWave;
                for (int turn = 1; turn <= turns; turn++)
                {
                    yield return EnemyTurn(turn);
                    if (session.IsOver) yield break;
                }
            }
            session.GameOver("40웨이브의 제한 턴이 끝남");
        }

        private IEnumerator EnemyTurn(int turn)
        {
            Debug.Log($"[TurnManager] 적 턴 {turn}");
            EnemyTurnStart();
            yield return EffectActivity.WaitUntilIdle();
            yield return MoveAllEnemies();
            yield return SupportTowersPhase();
            TileEffectsPhase();
            yield return AttackTowersPhase();
            DebuffPhase();
            BossTimerPhase();
            EnemyTurnEnded?.Invoke(turn);
        }

        private void EnemyTurnStart()
        {
            m_towerRunner.RunEnemyTurnStart();
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
                enemy.Status.OnEnemyTurnStart();
        }

        private IEnumerator MoveAllEnemies()
        {
            var enemies = new List<EnemyController>(EnemyManager.Instance.Registry.Alive);
            foreach (EnemyController enemy in enemies)
            {
                enemy.StartCoroutine(enemy.Movement.TakeTurn());
            }

            while (EnemyManager.Instance.IsAnyEnemyMoving()) yield return null;
        }

        private IEnumerator AttackTowersPhase() { yield return m_towerRunner.RunAttackTowers(); }
        private IEnumerator SupportTowersPhase() { yield return m_towerRunner.RunSupportTowers(); }
        private void TileEffectsPhase()
        {
            m_tileApplier.ApplyAll(EnemyManager.Instance.Registry.Alive);
        }

        private void DebuffPhase()
        {
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
            {
                if (enemy.IsAlive) enemy.Status.OnTurn();
            }

            BoardManager.Instance.Grounds.RunTurn();
        }

        private void BossTimerPhase()
        {
            if (EnemyManager.Instance.BossTimer.Tick())
            {
                GameSession.Instance.GameOver("보스 제한 턴이 끝남");
            }
        }
    }
}
