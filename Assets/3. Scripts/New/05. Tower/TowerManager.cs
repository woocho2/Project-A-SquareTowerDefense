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
        private static readonly int[] s_sellGems = { 0, 1, 3, 9, 27, 81 };

        [SerializeField] private TextAsset m_towerCsv;
        [SerializeField] private TowerController m_towerPrefab;
        [SerializeField] private Transform m_towerParent;
        [SerializeField] private List<TowerEmblem> m_selectedEmblems = new List<TowerEmblem>();

        public TowerDatabase Database { get; private set; }
        public ColorUpgradeState ColorLevels { get; private set; }
        public int BuildCost { get; private set; } = StartBuildCost;
        public TowerController Selected { get; private set; }

        public static int GetSellGem(int tier) => tier >= 1 && tier <= 5 ? s_sellGems[tier] : 0;

        public event Action<TowerController> TowerAdded;
        public event Action<int> BuildCostChanged;
        public event Action<TowerController> SelectionChanged;
        public event Action<TowerController> TowerRemoved;
        public event Action<TowerController, TowerController> TowerReplaced;

        private TowerFactory m_factory;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            Database = new TowerDatabase();
            Database.Load(m_towerCsv.text);
            ColorLevels = new ColorUpgradeState();

            ColorLevels.LevelChanged += HandleColorLevelChanged;
        }

        private void Start()
        {
            m_factory = new TowerFactory(m_towerPrefab, m_towerParent, BoardManager.Instance, Database, ColorLevels);
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

        public void Select(TowerController tower)
        {
            if (Selected == tower) return;
            Selected = tower;
            SelectionChanged?.Invoke(tower);
        }

        public bool TrySell(TowerController tower)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[TowerManager] 플레이어 턴이 아니라 팔 수 없음");
                return false;
            }
            if (tower == null) return false;
            if (TowerId.IsSynergy(tower.Definition.Id))
            {
                Debug.Log("[TowerManager] 시너지 타워는 팔 수 없음");
                return false;
            }

            int gem = GetSellGem(tower.Definition.Tier);
            Debug.Log($"[TowerManager] {tower.name} 판매, 젬 +{gem}");
            RemoveTower(tower);
            CurrencyManager.Instance.AddGem(gem);
            return true;
        }

        private void RemoveTower(TowerController tower)
        {
            if (Selected == tower) Select(null);
            tower.OnRemoved();
            m_factory.Destroy(tower);
            TowerRemoved?.Invoke(tower);
        }

        public bool TryMerge(TowerController target, MergeMode mode)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[TowerManager] 플레이어 턴이 아니라 합성할 수 없음");
                return false;
            }
            if (target == null) return false;

            var materials = new List<TowerController>();
            if (!TowerProgression.TryFindMaterials(target, mode, BoardManager.Instance.Slots, materials))
            {
                Debug.Log($"[TowerManager] {mode} 합성 재료가 모자람 (같은 티어 {TowerProgression.MergeCount}기)");
                return false;
            }

            List<TowerDefinition> candidates = TowerProgression.GetResultCandidates(target.Definition, mode, Database, m_selectedEmblems);
            if (candidates.Count == 0)
            {
                Debug.Log("[TowerManager] 합성 결과가 CSV에 없음 (재료는 그대로)");
                return false;
            }

            TowerDefinition result = candidates[Random.Range(0, candidates.Count)];
            TowerController created = m_factory.Replace(target, result, false);
            if (created == null) return false;

            foreach (TowerController material in materials)
            {
                RemoveTower(material);
            }

            Debug.Log($"[TowerManager] {mode} 합성 → {created.name}");
            TowerReplaced?.Invoke(target, created);
            TowerAdded?.Invoke(created);
            Select(created);
            return true;
        }

        public bool TryTierUpgrade(TowerController tower)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (tower == null) return false;

            int cost = TowerProgression.GetTierUpgradeCost(tower.Definition.Tier);
            if (cost <= 0 || TowerId.IsSynergy(tower.Definition.Id))
            {
                Debug.Log("[TowerManager] 더 강화할 수 없는 타워 (5티어·시너지)");
                return false;
            }
            if (CurrencyManager.Instance.Gem < cost)
            {
                Debug.Log($"[TowerManager] 젬이 모자람 ({CurrencyManager.Instance.Gem} / {cost})");
                return false;
            }
            if (!Database.TryGet(TowerId.GetNextTier(tower.Definition.Id), out TowerDefinition next))
            {
                Debug.Log("[TowerManager] 다음 티어가 CSV에 없음");
                return false;
            }

            TowerController created = m_factory.Replace(tower, next, true);
            if (created == null) return false;

            CurrencyManager.Instance.TrySpendGem(cost);
            Debug.Log($"[TowerManager] 티어 강화 → {created.name}, 젬 −{cost}");
            TowerReplaced?.Invoke(tower, created);
            if (Selected == tower) Select(created);
            return true;
        }

        public bool TryUpgradeColor(TowerColor color)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (!ColorLevels.CanUpgrade(color))
            {
                Debug.Log($"[TowerManager] {color}는 이미 최대 레벨");
                return false;
            }

            int cost = ColorLevels.GetCost(color);
            if (cost <= 0 || CurrencyManager.Instance.Gem < cost)
            {
                Debug.Log($"[TowerManager] 젬이 모자람 ({CurrencyManager.Instance.Gem} / {cost})");
                return false;
            }

            ColorLevels.TryUpgrade(color);
            CurrencyManager.Instance.TrySpendGem(cost);
            Debug.Log($"[TowerManager] {color} 컬러 강화 → 레벨 {ColorLevels.GetLevel(color)}, 젬 −{cost}");
            return true;
        }

        private void HandleColorLevelChanged(TowerColor color, int level)
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower == null || tower.Definition.Color != color) continue;

                tower.RecalculateStats();
            }
        }

        public bool TryMoveOrSwap(TowerController tower, int toSlot)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (tower == null || tower.SlotIndex == toSlot) return false;

            TowerSlots slots = BoardManager.Instance.Slots;
            int fromSlot = tower.SlotIndex;
            TowerController other = slots.Get(toSlot);

            if (other == null)
            {
                if (!slots.TryMove(fromSlot, toSlot)) return false;
                PlaceAt(tower, toSlot);
            }
            else
            {
                if (!slots.TrySwap(fromSlot, toSlot)) return false;
                PlaceAt(tower, toSlot);
                PlaceAt(other, fromSlot);
            }

            Debug.Log($"[TowerManager] {tower.name} {fromSlot} → {toSlot}{(other != null ? $" (교환: {other.name})" : "")}");
            return true;
        }

        private void PlaceAt(TowerController tower, int slot)
        {
            BoardManager board = BoardManager.Instance;
            tower.SetSlot(slot, board.GetWorldPosition(board.Grid.GetSpawnCell(slot)));
        }
    }
}
