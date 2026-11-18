using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SquareTD
{
    // 타워 생성·판매·합성·경험치·이동을 맡는 매니저
    public class TowerManager : Singleton<TowerManager>
    {
        public const int FixedBuildCost = 250;
        private static readonly int[] s_sellGold = { 0, 200, 600, 1800, 5400 };

        [SerializeField] private TextAsset m_towerCsv;
        [SerializeField] private TowerController m_towerPrefab;
        [SerializeField] private Transform m_towerParent;
        [SerializeField] private List<TowerEmblem> m_selectedEmblems = new List<TowerEmblem>();
        [SerializeField] private float m_mergeDelay = 0.5f;

        public TowerDatabase Database { get; private set; }
        public ColorUpgradeState ColorLevels { get; private set; }
        public int BuildCost => FixedBuildCost;
        public int BuildCount { get; private set; }
        public IReadOnlyList<TowerEmblem> SelectedEmblems => m_selectedEmblems;
        public TowerController Selected { get; private set; }

        public static int GetSellGold(int tier) => tier >= 1 && tier <= TowerId.MaxTier ? s_sellGold[tier] : 0;

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

            if (StartSettings.IsSet && StartSettings.Emblems.Count > 0)
            {
                m_selectedEmblems.Clear();
                m_selectedEmblems.AddRange(StartSettings.Emblems);
            }

            ColorLevels.LevelChanged += HandleColorLevelChanged;
        }

        private void Start()
        {
            m_factory = new TowerFactory(m_towerPrefab, m_towerParent, BoardManager.Instance, Database, ColorLevels);
            m_mergeEffect = EffectSpace.Make<TowerMergeEffect>(transform, "Tower Merge Effect");

            Synergy = new SynergyBoard(BoardManager.Instance);
            Synergy.Spawn(m_factory, m_selectedEmblems);
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.EnemyTurnEnded += HandleEnemyTurnEnded;
                TurnManager.Instance.StateChanged += HandleTurnStateChanged;
            }
            if (GameSession.Instance != null) GameSession.Instance.BossMechanics.Blocker = Synergy.TryBlockMechanic;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (TurnManager.Instance == null) return;
            TurnManager.Instance.EnemyTurnEnded -= HandleEnemyTurnEnded;
            TurnManager.Instance.StateChanged -= HandleTurnStateChanged;
        }

        public SynergyBoard Synergy { get; private set; }

        private void HandleEnemyTurnEnded(int turn) => Synergy?.OnEnemyTurnEnded();

        private void HandleTurnStateChanged(ITurnState state)
        {
            if (state is PlayerTurnState) Synergy?.OnPlayerTurnStarted();
        }

        public static bool TryGetBuildSlot(BoardManager board, out TowerPlace place, out int index)
        {
            place = TowerPlace.Board;
            if (board.Slots.TryGetFirstEmpty(out index)) return true;
            place = TowerPlace.Bench;
            return board.Bench != null && board.Bench.TryGetFirstEmpty(out index);
        }

        public bool TryBuild()
        {
            if (!TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[TowerManager] 플레이어 턴이 아니라 만들 수 없음");
                return false;
            }
            if (!TryGetBuildSlot(BoardManager.Instance, out TowerPlace place, out int slotIndex))
            {
                Debug.Log("[TowerManager] 판과 대기석에 빈 칸이 없음");
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

            TowerController tower = m_factory.Create(definition, slotIndex, place);
            if (tower == null) return false;

            CurrencyManager.Instance.TrySpendGold(BuildCost);
            BuildCount++;
            Debug.Log($"[TowerManager] {tower.name} → {(place == TowerPlace.Bench ? "대기석 " : "")}{slotIndex}번 칸, 골드 −{BuildCost}");
            TowerAdded?.Invoke(tower);
            BuildCostChanged?.Invoke(BuildCost);
            RequestAutoMerge();
            return true;
        }

        public TowerController SpawnForDebug(TowerDefinition definition)
        {
            if (!TryGetBuildSlot(BoardManager.Instance, out TowerPlace place, out int slotIndex))
            {
                Debug.Log("[TowerManager] 판과 대기석에 빈 칸이 없음");
                return null;
            }

            TowerController tower = m_factory.Create(definition, slotIndex, place);
            if (tower != null)
            {
                TowerAdded?.Invoke(tower);
                RequestAutoMerge();
            }
            return tower;
        }

        public static TowerColor GetGuaranteedColor(int buildIndex)
        {
            switch (buildIndex)
            {
                case 0: return TowerColor.Red;
                case 1: return TowerColor.Blue;
                default: return TowerColor.None;
            }
        }

        private bool TryPickRandomTier1(out TowerDefinition definition)
        {
            definition = null;
            if (m_selectedEmblems.Count == 0) return false;

            TowerEmblem emblem = m_selectedEmblems[Random.Range(0, m_selectedEmblems.Count)];
            TowerColor color = GetGuaranteedColor(BuildCount);
            if (color == TowerColor.None)
            {
                TowerColor last = SimulationRunner.AttackOnlyBuilds ? TowerColor.Blue : TowerColor.Purple;
                color = (TowerColor)Random.Range((int)TowerColor.Red, (int)last + 1);
            }
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

            int gold = GetSellGold(tower.Definition.Tier);
            Debug.Log($"[TowerManager] {tower.name} 판매, 골드 +{gold}");
            RemoveTower(tower);
            CurrencyManager.Instance.AddGold(gold);
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
            BoardManager board = BoardManager.Instance;
            while (TowerProgression.TryFindMergeGroup(board.GetBoardAndBenchTowers(), IsMergingTower, out TowerController keep, out List<TowerController> materials, RequiredMergeCount))
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
            if (GameSession.Instance != null) GameSession.Instance.Results.Merge(keep, materials, created);

            Debug.Log($"[TowerManager] 자동 합성 → {created.name} ({created.SlotIndex}번 칸)");
            TowerReplaced?.Invoke(keep, created);
            TowerAdded?.Invoke(created);
            if (wasSelected) Select(created);
            if (m_mergeEffect != null) m_mergeEffect.PlayPop(created);
        }

        public bool TryBuyXp()
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (ColorLevels.IsMax)
            {
                Debug.Log("[TowerManager] 이미 최대 레벨");
                return false;
            }

            int cost = BalanceTables.XpPurchaseCost;
            if (!CurrencyManager.Instance.TrySpendGold(cost))
            {
                Debug.Log($"[TowerManager] 골드가 모자람 ({CurrencyManager.Instance.Gold} / {cost})");
                return false;
            }

            ColorLevels.AddXp(BalanceTables.XpPerPurchase);
            Debug.Log($"[TowerManager] 경험치 +{BalanceTables.XpPerPurchase} → 레벨 {ColorLevels.Level} ({ColorLevels.Xp} / {ColorLevels.XpToNext}), 골드 −{cost}");
            return true;
        }

        private void HandleColorLevelChanged(int level)
        {
            BoardManager board = BoardManager.Instance;
            foreach (TowerController tower in board.GetBoardAndBenchTowers())
            {
                tower.RecalculateStats();
                if (tower.IsOnBoard) board.Zones.EnsureZonesFor(tower);
            }
        }

        public bool TryMoveOrSwap(TowerController tower, int toSlot) => TryMoveTo(tower, TowerPlace.Board, toSlot);

        public bool TryMoveTo(TowerController tower, TowerPlace place, int toSlot)
        {
            if (!TurnManager.Instance.Current.CanPlayerAct) return false;
            if (tower == null || place == TowerPlace.Synergy || tower.Place == TowerPlace.Synergy) return false;
            if (tower.Place == place && tower.SlotIndex == toSlot) return false;

            BoardManager board = BoardManager.Instance;
            TowerSlots to = board.GetSlots(place);
            if (to == null || toSlot < 0 || toSlot >= to.Count) return false;

            TowerPlace fromPlace = tower.Place;
            int fromSlot = tower.SlotIndex;
            TowerSlots from = board.GetSlots(fromPlace);
            TowerController other = to.Get(toSlot);
            if (IsMergingTower(tower) || IsMergingTower(other))
            {
                Debug.Log("[TowerManager] 합성 중인 타워는 옮길 수 없음");
                return false;
            }

            if (fromPlace == place)
            {
                bool done = other == null ? to.TryMove(fromSlot, toSlot) : to.TrySwap(fromSlot, toSlot);
                if (!done) return false;
            }
            else
            {
                from.Remove(fromSlot);
                if (other != null) to.Remove(toSlot);
                to.TryPlace(tower, toSlot);
                if (other != null) from.TryPlace(other, fromSlot);
            }

            Relocate(tower, place, toSlot);
            if (other != null) Relocate(other, fromPlace, fromSlot);

            Debug.Log($"[TowerManager] {tower.name} {PlaceName(fromPlace)}{fromSlot} → {PlaceName(place)}{toSlot}{(other != null ? $" (교환: {other.name})" : "")}");
            if (fromPlace != place && GameSession.Instance != null) GameSession.Instance.RefreshTowerLimits();
            return true;
        }

        private static string PlaceName(TowerPlace place) => place == TowerPlace.Bench ? "대기석 " : "";

        private void Relocate(TowerController tower, TowerPlace place, int slot)
        {
            BoardManager board = BoardManager.Instance;
            bool wasOnBoard = tower.IsOnBoard;
            if (wasOnBoard && place != TowerPlace.Board)
            {
                tower.OnRemoved();
                SupportBuffs.ClearZones(tower);
            }
            tower.SetPlace(place, slot, board.GetPlaceWorldPosition(place, slot));
            if (!wasOnBoard && place == TowerPlace.Board) board.Zones.EnsureZonesFor(tower);
        }

        public bool TryChangeForBoss(TowerController tower, TowerDefinition definition, IReadOnlyList<int> zoneCells = null)
        {
            if (tower == null || definition == null || IsMergingTower(tower)) return false;
            if (BoardManager.Instance.GetSlots(tower.Place).Get(tower.SlotIndex) != tower) return false;
            m_factory.ChangeDefinition(tower, definition, zoneCells);
            TowerReplaced?.Invoke(tower, tower);
            if (Selected == tower) SelectionChanged?.Invoke(tower);
            return true;
        }

        public TowerDefinition PickBossTransformation(TowerController tower)
        {
            var options = new List<TowerDefinition>();
            foreach (TowerDefinition candidate in Database.All)
            {
                if (candidate.Tier == tower.Definition.Tier && candidate.Id != tower.Definition.Id &&
                    !TowerId.IsSynergy(candidate.Id) && m_selectedEmblems.Contains(candidate.Emblem)) options.Add(candidate);
            }
            return options.Count == 0 ? null : options[Random.Range(0, options.Count)];
        }

        public void RefreshAfterBossEffects()
        {
            if (Selected != null) SelectionChanged?.Invoke(Selected);
            RequestAutoMerge();
        }

        public void DestroyAllForBoss()
        {
            if (m_mergeRoutine != null) StopCoroutine(m_mergeRoutine);
            m_mergeRoutine = null;
            m_merging.Clear();
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null) RemoveTower(tower);
            }
        }
    }
}
