using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    public class GameSession : Singleton<GameSession>
    {
        [SerializeField] private GameMode m_mode = GameMode.Normal;

        public GameMode Mode => m_mode;
        public int BaseLimit { get; private set; }                  // 사이클의 기본 한도 (WaveTable)
        public int CurrentLimit => BaseLimit + SumLimitModifiers(); // 기본 + 보정의 합 (도끼 버프 −, 수업 31 방패 방어막 +)
        public bool IsOver { get; private set; }
        public EmblemSharedState Shared { get; } = new EmblemSharedState();        // 어둠의 카운트 (수업 40)

        public event Action<int> LimitChanged;

        // 한도 보정: 출처 → 더하는 값 (수업 28). 바뀌면 값과 LimitChanged는 바로, 패배 판정은 여전히 소환 직전만 (20번 1장)
        private readonly Dictionary<string, int> m_limitModifiers = new Dictionary<string, int>();
        private readonly HashSet<string> m_towerLimitSources = new HashSet<string>();   // 버프 타워 규칙이 넣은 출처 (다시 셀 때 이것만 지움)
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
            BoardManager.Instance.Zones.ZoneChanged += OnZoneChanged;      // 방패의 이지스가 펴지면 (존이 스킬 2로)
            EnemyManager.Instance.EnemyDied += OnEnemyDied;                // 어둠의 카운트 (수업 40)
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
            RefreshTowerLimits();                   // 도끼가 깎는 양은 기본 한도에 비례
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

        // 한도를 바꾸는 일을 하고, 실제로 바뀌었으면 알림
        private void ChangeLimit(Action change)
        {
            int before = CurrentLimit;
            change();
            if (CurrentLimit == before) return;

            Debug.Log($"[GameSession] 한도 {CurrentLimit} (기본 {BaseLimit}{(m_limitModifiers.Count > 0 ? $", 보정 {SumLimitModifiers():+0;-0}" : "")})");
            LimitChanged?.Invoke(CurrentLimit);
        }

        // 판 위 버프 타워의 한도 보정을 다시 셈: 문양마다 그 문양 버프 타워 중 값이 가장 큰 하나 (도끼는 여러 기여도 더하지 않음, 20번 4-4장)
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
