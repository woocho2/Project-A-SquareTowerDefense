using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 한 판의 진행: 적 수 한도(기본 + 보정), 게임 오버·클리어
    public class GameSession : Singleton<GameSession>
    {
        [SerializeField] private GameMode m_mode = GameMode.Normal;

        public GameMode Mode => m_mode;
        public int BaseLimit { get; private set; }
        public int CurrentLimit => BaseLimit + SumLimitModifiers();
        public bool IsOver { get; private set; }
        public EmblemSharedState Shared { get; } = new EmblemSharedState();

        public event Action<int> LimitChanged;

        private readonly Dictionary<string, int> m_limitModifiers = new Dictionary<string, int>();
        private readonly HashSet<string> m_towerLimitSources = new HashSet<string>();
        public IReadOnlyDictionary<string, int> LimitModifiers => m_limitModifiers;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            DOTween.SetTweensCapacity(500, 200);
        }

        private void Start()
        {
            WaveManager.Instance.WaveChanged += OnWaveChanged;
            TowerManager.Instance.TowerAdded += OnTowerChanged;
            TowerManager.Instance.TowerRemoved += OnTowerChanged;
            TowerManager.Instance.TowerReplaced += OnTowerReplaced;
            BoardManager.Instance.Zones.ZoneChanged += OnZoneChanged;
            EnemyManager.Instance.EnemyDied += OnEnemyDied;
        }

        private void OnEnemyDied(KillInfo kill) => Shared.AddDarkness(EmblemSharedState.GetKillCount(kill, HasDarknessBuffTower()));

        private bool HasDarknessBuffTower()
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && tower.Definition.Color == TowerColor.Yellow && tower.Definition.Emblem == TowerEmblem.Darkness) return true;
            }
            return false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (WaveManager.Instance != null) WaveManager.Instance.WaveChanged -= OnWaveChanged;
            if (TowerManager.Instance != null)
            {
                TowerManager.Instance.TowerAdded -= OnTowerChanged;
                TowerManager.Instance.TowerRemoved -= OnTowerChanged;
                TowerManager.Instance.TowerReplaced -= OnTowerReplaced;
            }
            if (BoardManager.Instance != null) BoardManager.Instance.Zones.ZoneChanged -= OnZoneChanged;
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyDied -= OnEnemyDied;
        }

        private void OnWaveChanged(int wave)
        {
            int limit = WaveTable.GetEnemyLimit(m_mode, WaveTable.GetCycle(wave));
            if (limit == BaseLimit) return;

            ChangeLimit(() => BaseLimit = limit);
            RefreshTowerLimits();
        }

        private void OnTowerChanged(TowerController tower) => RefreshTowerLimits();
        private void OnTowerReplaced(TowerController old, TowerController created) => RefreshTowerLimits();
        private void OnZoneChanged(Zone zone) => RefreshTowerLimits();

        public void SetLimitModifier(string source, int amount) => ChangeLimit(() => m_limitModifiers[source] = amount);
        public void RemoveLimitModifier(string source) => ChangeLimit(() => m_limitModifiers.Remove(source));

        private int SumLimitModifiers()
        {
            int sum = 0;
            foreach (int amount in m_limitModifiers.Values) sum += amount;
            return sum;
        }

        private void ChangeLimit(Action change)
        {
            int before = CurrentLimit;
            change();
            if (CurrentLimit == before) return;

            Debug.Log($"[GameSession] 한도 {CurrentLimit} (기본 {BaseLimit}{(m_limitModifiers.Count > 0 ? $", 보정 {SumLimitModifiers():+0;-0}" : "")})");
            LimitChanged?.Invoke(CurrentLimit);
        }

        private void RefreshTowerLimits()
        {
            var best = new Dictionary<TowerEmblem, int>();
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null || tower.Definition.Color != TowerColor.Yellow) continue;

                TowerEmblem emblem = tower.Definition.Emblem;
                int amount = SkillRegistry.GetBuffRule(emblem).GetLimitModifier(tower, BaseLimit);
                if (!best.TryGetValue(emblem, out int current) || amount > current) best[emblem] = amount;
            }

            ChangeLimit(() =>
            {
                foreach (string source in m_towerLimitSources) m_limitModifiers.Remove(source);
                m_towerLimitSources.Clear();

                foreach (KeyValuePair<TowerEmblem, int> pair in best)
                {
                    if (pair.Value == 0) continue;
                    string source = $"{EmblemNames.GetEmblemName(pair.Key)} 버프 타워";
                    m_limitModifiers[source] = pair.Value;
                    m_towerLimitSources.Add(source);
                }
            });
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
