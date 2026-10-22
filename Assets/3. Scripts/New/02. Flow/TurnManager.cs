using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SquareTD
{
    public class TurnManager : Singleton<TurnManager>
    {
        public const int EnemyTurnsPerWave = 10;
        public const int LastWaveEnemyTurns = 20;

        public ITurnState Current { get; private set; } = new IdleState();
        public event Action<ITurnState> StateChanged;
        public event Action<int> EnemyTurnEnded;

        private bool m_endTurnRequested;

        private void Start()
        {
            StartCoroutine(GameLoop());
        }

        public void OnClickEndTurn()
        {
            if (!Current.CanPlayerAct) return;
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
            yield return MoveAllEnemies();
            SupportTowersPhase();
            TileEffectsPhase();
            yield return AttackTowersPhase();
            DebuffPhase();
            BossTimerPhase();
            EnemyTurnEnded?.Invoke(turn);
        }

        private void EnemyTurnStart() { }

        private IEnumerator MoveAllEnemies()
        {
            var enemies = new List<EnemyController>(EnemyManager.Instance.Registry.Alive);
            foreach (EnemyController enemy in enemies)
            {
                enemy.StartCoroutine(enemy.Movement.TakeTurn());
            }

            while (EnemyManager.Instance.IsAnyEnemyMoving()) yield return null;
        }

        private void SupportTowersPhase()
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null || tower.IsAttackTower) continue;

                if (tower.TickAction()) Debug.Log($"[TurnManager] ② {tower.name} 행동");
            }
        }
        private void TileEffectsPhase() { }
        private IEnumerator AttackTowersPhase()
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null || !tower.IsAttackTower) continue;

                if (tower.TickAction()) Debug.Log($"[TurnManager] ② {tower.name} 행동");
            }
            yield break;
        }
        private void DebuffPhase() { }
        private void BossTimerPhase()
        {
            if (EnemyManager.Instance.BossTimer.Tick())
            {
                GameSession.Instance.GameOver("보스 제한 턴이 끝남");
            }
        }
    }
}
