using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SquareTD
{
    // 타워 생성·판매·합성·강화·이동을 맡는 매니저
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
        [SerializeField] private float m_mergeDelay = 0.5f;

        public TowerDatabase Database { get; private set; }
        public ColorUpgradeState ColorLevels { get; private set; }
        public StackModeSettings StackModes { get; private set; } = new StackModeSettings();
        public int BuildCost { get; private set; } = StartBuildCost;
        public TowerController Selected { get; private set; }

        public static int GetSellGem(int tier) => tier >= 1 && tier <= 5 ? s_sellGems[tier] : 0;

        public event Action<TowerController> TowerAdded;
        public event Action<int> BuildCostChanged;
        public event Action<TowerController> SelectionChanged;
        public event Action<TowerController> TowerRemoved;
        public event Action<TowerController, TowerController> TowerReplaced;

        private TowerFactory m_factory;
        private TowerMergeEffect m_mergeEffect;
        private Coroutine m_mergeRoutine;
        private readonly HashSet<TowerController> m_merging = new HashSet<TowerController>();

        public int RequiredMergeCount { get; private set; } = TowerProgression.MergeCount;

        public void ConfigureMergeCount(int count)
        {
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
            RequiredMergeCount = count;
        }

        public bool IsMerging => m_mergeRoutine != null;
        public bool IsMergingTower(TowerController tower) => tower != null && m_merging.Contains(tower);

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
            m_factory = new TowerFactory(m_towerPrefab, m_towerParent, BoardManager.Instance, Database, ColorLevels, StackModes);
            m_mergeEffect = EffectSpace.Make<TowerMergeEffect>(transform, "Tower Merge Effect");
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
            RequestAutoMerge();
            return true;
        }

        public TowerController SpawnForDebug(TowerDefinition definition)
        {
            if (!BoardManager.Instance.Slots.TryGetFirstEmpty(out int slotIndex))
            {
                Debug.Log("[TowerManager] 빈 칸이 없음");
                return null;
            }

            TowerController tower = m_factory.Create(definition, slotIndex);
            if (tower != null)
            {
                TowerAdded?.Invoke(tower);
                RequestAutoMerge();
            }
            return tower;
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
            if (IsMergingTower(tower))
            {
                Debug.Log("[TowerManager] 합성 중인 타워는 팔 수 없음");
                return false;
            }
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

        private void RequestAutoMerge()
        {
            if (m_mergeRoutine == null && isActiveAndEnabled) m_mergeRoutine = StartCoroutine(AutoMergeLoop());
        }

        private IEnumerator AutoMergeLoop()
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            while (TowerProgression.TryFindMergeGroup(slots, IsMergingTower, out TowerController keep, out List<TowerController> materials, RequiredMergeCount))
            {
                if (!Database.TryGet(TowerId.GetNextTier(keep.Definition.Id), out TowerDefinition next))
                {
                    Debug.LogError($"[TowerManager] {keep.name}의 다음 티어가 CSV에 없어 합성하지 못함");
                    break;
                }

                m_merging.Add(keep);
                foreach (TowerController material in materials) m_merging.Add(material);

                yield return new WaitForSeconds(m_mergeDelay);
                if (m_mergeEffect != null)
                {
                    m_mergeEffect.PlayGather(materials, keep.transform.position);
                    yield return new WaitForSeconds(m_mergeEffect.FlyDuration);
                    m_mergeEffect.PlayCharge(keep);
                    yield return new WaitForSeconds(m_mergeEffect.ChargeDuration);
                }

                MergeInto(keep, materials, next);
                m_merging.Clear();
            }
            m_mergeRoutine = null;
        }

        private void MergeInto(TowerController keep, List<TowerController> materials, TowerDefinition next)
        {
            bool wasSelected = Selected == keep || materials.Contains(Selected);
            foreach (TowerController material in materials) RemoveTower(material);

            TowerController created = m_factory.Replace(keep, next, true);
            if (created == null) return;

            Debug.Log($"[TowerManager] 자동 합성 → {created.name} ({created.SlotIndex}번 칸)");
            TowerReplaced?.Invoke(keep, created);
            TowerAdded?.Invoke(created);
            if (wasSelected) Select(created);
            if (m_mergeEffect != null) m_mergeEffect.PlayPop(created);
        }

        public bool TryTierUpgrade(TowerController tower)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (tower == null || IsMergingTower(tower)) return false;

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
            RequestAutoMerge();
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

                ZoneRegistry zones = BoardManager.Instance.Zones;
                if (level == 4)
                {
                    foreach (Zone zone in zones.GetByOwner(tower)) zone.ResetIfSkill1();
                }
                zones.EnsureZonesFor(tower);
            }
        }

        public bool TryMoveOrSwap(TowerController tower, int toSlot)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (tower == null || tower.SlotIndex == toSlot) return false;

            TowerSlots slots = BoardManager.Instance.Slots;
            int fromSlot = tower.SlotIndex;
            TowerController other = slots.Get(toSlot);
            if (IsMergingTower(tower) || IsMergingTower(other))
            {
                Debug.Log("[TowerManager] 합성 중인 타워는 옮길 수 없음");
                return false;
            }

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
            tower.SetSpawnTile(board.Tiles.GetSpawnTile(slot));
        }
    }
}
