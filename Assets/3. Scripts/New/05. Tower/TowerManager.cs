using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SquareTD
{
    public class TowerManager : Singleton<TowerManager>
    {
        public const int StartBuildCost = 50;
        public const int BuildCostStep = 5;
        public const int MaxBuildCost = 1500;

        [SerializeField] private TextAsset m_towerCsv;
        [SerializeField] private TowerController m_towerPrefab;
        [SerializeField] private Transform m_towerParent;
        [SerializeField] private List<TowerEmblem> m_selectedEmblems = new List<TowerEmblem>();

        public TowerDatabase Database { get; private set; }
        public int BuildCost { get; private set; } = StartBuildCost;

        public event Action<TowerController> TowerAdded;
        public event Action<int> BuildCostChanged;

        private TowerFactory m_factory;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            Database = new TowerDatabase();
            Database.Load(m_towerCsv.text);
        }

        private void Start()
        {
            m_factory = new TowerFactory(m_towerPrefab, m_towerParent, BoardManager.Instance);
        }

        public bool TryBuild()
        {
            if (!TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[TowerManager] 플레이어 턴이 아니라 만들 수 없음");
                return false;
            }
            if (!BoardManager.Instance.Slots.TryGetFirstEmpty(out int slotIndex))
            {
                Debug.Log("[TowerManager] 빈 칸이 없음");
                return false;
            }
            if (CurrencyManager.Instance.Gold < BuildCost)
            {
                Debug.Log($"[TowerManager] 골드가 모자람 ({CurrencyManager.Instance.Gold} / {BuildCost})");
                return false;
            }
            if (!TryPickRandomTier1(out TowerDefinition definition))
            {
                Debug.LogError("[TowerManager] 1티어 타워를 고르지 못함 (고른 문양이 비었거나 CSV에 없는 ID)");
                return false;
            }

            TowerController tower = m_factory.Create(definition, slotIndex);
            if (tower == null) return false;

            CurrencyManager.Instance.TrySpendGold(BuildCost);
            BuildCost = Mathf.Min(BuildCost + BuildCostStep, MaxBuildCost);
            Debug.Log($"[TowerManager] {tower.name} → {slotIndex}번 칸, 다음 비용 {BuildCost}");
            TowerAdded?.Invoke(tower);
            BuildCostChanged?.Invoke(BuildCost);
            return true;
        }

        private bool TryPickRandomTier1(out TowerDefinition definition)
        {
            definition = null;
            if (m_selectedEmblems.Count == 0) return false;

            TowerEmblem emblem = m_selectedEmblems[Random.Range(0, m_selectedEmblems.Count)];
            TowerColor color = (TowerColor)Random.Range((int)TowerColor.Red, (int)TowerColor.Purple + 1);
            return Database.TryGet(TowerId.Make(1, color, emblem), out definition);
        }
    }
}
