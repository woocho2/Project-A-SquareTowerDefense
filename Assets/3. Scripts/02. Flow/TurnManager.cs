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
        public event Action EnemyTurnStarted;

        private bool m_endTurnRequested;
        [SerializeField] private float m_attackSafetySeconds = 10f;
        [SerializeField] private float m_phaseGap = 0.5f;
        private bool m_anyMoved;

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

                ChangeState(new PlayerTurnState());
                m_endTurnRequested = false;
                while (!m_endTurnRequested) yield return null;

                ChangeState(new EnemyTurnState());
                if (!session.CheckBeforeSpawn(EnemyManager.Instance.Registry.Count)) yield break;
                List<EnemyController> spawned = EnemyManager.Instance.SpawnWave(wave.CurrentWave);
                yield return EnemySpawnIntro.Play(spawned);
                yield return Gap(spawned.Count > 0);

                int adaptive = session.RollAdaptive();
                yield return AdaptiveDiceRoll.Play(adaptive, WaveTable.GetAdaptiveMin(session.Mode), WaveTable.GetAdaptiveMax(session.Mode));
                yield return Gap(true);

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
            yield return BuffTowersPhase();
            yield return Gap(m_towerRunner.LastActed > 0);
            yield return AttackTowersPhase();
            yield return Gap(m_towerRunner.LastActed > 0);
            yield return Gap(AdaptivePhase());
            yield return MoveAllEnemies();
            yield return Gap(m_anyMoved);
            yield return DebuffTowersPhase();
            TileEffectsPhase();
            yield return Gap(m_towerRunner.LastActed > 0);
            yield return Gap(DotPhase());
            BossTimerPhase();
            EnemyTurnEnded?.Invoke(turn);
        }

        private IEnumerator Gap(bool happened)
        {
            if (!happened || m_phaseGap <= 0f || SimulationRunner.IsOn) yield break;
            yield return new WaitForSeconds(m_phaseGap);
        }

        private bool AdaptivePhase()
        {
            bool healed = false;
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
            {
                if (enemy.IsAlive && enemy.Movement.ApplyAdaptive()) healed = true;
            }
            return healed;
        }

        private void EnemyTurnStart()
        {
            EnemyTurnStarted?.Invoke();
            m_towerRunner.RunEnemyTurnStart();
        }

        private IEnumerator MoveAllEnemies()
        {
            var enemies = new List<EnemyController>(EnemyManager.Instance.Registry.Alive);
            foreach (EnemyController enemy in enemies)
            {
                enemy.StartCoroutine(enemy.Movement.TakeTurn());
            }

            while (EnemyManager.Instance.IsAnyEnemyMoving()) yield return null;
            m_anyMoved = enemies.Exists(enemy => enemy.Movement.ActedThisTurn);
        }

        private IEnumerator AttackTowersPhase() { yield return m_towerRunner.RunAttackTowers(); }
        private IEnumerator BuffTowersPhase() { yield return m_towerRunner.RunBuffTowers(); }
        private IEnumerator DebuffTowersPhase() { yield return m_towerRunner.RunDebuffTowers(); }
        private void TileEffectsPhase()
        {
            m_tileApplier.ApplyAll(EnemyManager.Instance.Registry.Alive);
        }

        private bool DotPhase()
        {
            bool any = BoardManager.Instance.Grounds.All.Count > 0;
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
            {
                if (!enemy.IsAlive) continue;
                if (enemy.Status.Effects.Count > 0) any = true;
                enemy.Status.OnTurn();
            }

            BoardManager.Instance.Grounds.RunTurn();
            return any;
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
