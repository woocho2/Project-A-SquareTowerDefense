using System;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    public class GameSession : Singleton<GameSession>
    {
        [SerializeField] private GameMode m_mode = GameMode.Normal;

        public GameMode Mode => m_mode;
        public int CurrentLimit { get; private set; }
        public bool IsOver { get; private set; }

        public event Action<int> LimitChanged;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            DOTween.SetTweensCapacity(500, 200);
        }

        private void Start()
        {
            WaveManager.Instance.WaveChanged += OnWaveChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (WaveManager.Instance != null) WaveManager.Instance.WaveChanged -= OnWaveChanged;
        }

        private void OnWaveChanged(int wave)
        {
            int limit = WaveTable.GetEnemyLimit(m_mode, WaveTable.GetCycle(wave));
            if (limit == CurrentLimit) return;

            CurrentLimit = limit;
            Debug.Log($"[GameSession] 한도 {CurrentLimit}");
            LimitChanged?.Invoke(CurrentLimit);
        }

        public bool CheckBeforeSpawn(int aliveCount)
        {
            if (aliveCount > CurrentLimit)
            {
                GameOver($"남은 적 {aliveCount} > 한도 {CurrentLimit}");
                return false;
            }
            return true;
        }

        public void GameOver(string reason)
        {
            if (IsOver) return;
            IsOver = true;
            Debug.Log($"[GameSession] 게임 오버: {reason}");
            TurnManager.Instance.EndGame(new GameOverState());
        }

        public void GameClear()
        {
            if (IsOver) return;
            IsOver = true;
            Debug.Log($"[GameSession] 게임 클리어");
            TurnManager.Instance.EndGame(new GameClearState());
        }
    }
}
