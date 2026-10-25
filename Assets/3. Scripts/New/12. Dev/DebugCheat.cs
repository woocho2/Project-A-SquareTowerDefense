using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    public class DebugCheat : MonoBehaviour
    {
        private bool m_autoEndTurn;

        private void Start()
        {
            EnemyManager.Instance.CountChanged += OnEnemyCountChanged;
            CurrencyManager.Instance.Changed += OnCurrencyChanged;
            TowerManager.Instance.TowerAdded += OnTowerAdded;
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.CountChanged -= OnEnemyCountChanged;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= OnCurrencyChanged;
            if (TowerManager.Instance != null) TowerManager.Instance.TowerAdded -= OnTowerAdded;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TurnManager.Instance.OnClickEndTurn();                // 턴 종료
            }

            if (keyboard.aKey.wasPressedThisFrame)
            {
                m_autoEndTurn = !m_autoEndTurn;                        // 뒤집기
                Debug.Log($"[DebugCheat] 자동 진행 {(m_autoEndTurn ? "켬" : "끔")}");
            }

            if (m_autoEndTurn && TurnManager.Instance.Current.CanPlayerAct)   // 플레이어가 조작할 수 있는 상태일 때만
            {
                AutoPlayerTurn();
                TurnManager.Instance.OnClickEndTurn();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                TowerManager.Instance.TryBuild();
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                CurrencyManager.Instance.AddGold(1000);
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                TowerManager.Instance.TrySell(TowerManager.Instance.Selected);
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                StartPickAttackCell();
            }

            if (keyboard.digit1Key.wasPressedThisFrame) TowerManager.Instance.TryMerge(TowerManager.Instance.Selected, MergeMode.Exact);
            if (keyboard.digit2Key.wasPressedThisFrame) TowerManager.Instance.TryMerge(TowerManager.Instance.Selected, MergeMode.Color);
            if (keyboard.digit3Key.wasPressedThisFrame) TowerManager.Instance.TryMerge(TowerManager.Instance.Selected, MergeMode.Emblem);
            if (keyboard.uKey.wasPressedThisFrame) TowerManager.Instance.TryTierUpgrade(TowerManager.Instance.Selected);
            if (keyboard.hKey.wasPressedThisFrame) CurrencyManager.Instance.AddGem(100);

            // ── 수업 21 확인용 임시 코드 ── N: 살아 있는 적 전부 이동력 −3 (켜고 끄기), P: 행동 카운트 +1 (켜고 끄기). 수업 22의 특수 타일이 생기면 지움
            if (keyboard.nKey.wasPressedThisFrame) ToggleEnemyModifier("수업 21 확인: 이동력 −3", StatModifier.Flat("수업 21 확인: 이동력 −3", StatType.SpeedMin, -3f),
                                                                                                    StatModifier.Flat("수업 21 확인: 이동력 −3", StatType.SpeedMax, -3f));
            if (keyboard.pKey.wasPressedThisFrame) ToggleEnemyModifier("수업 21 확인: 행동 카운트 +1", StatModifier.Flat("수업 21 확인: 행동 카운트 +1", StatType.ActionGainPerTurn, 1f));

            if (keyboard.lKey.wasPressedThisFrame)
            {
                TowerController selected = TowerManager.Instance.Selected;
                if (selected == null) Debug.Log("[DebugCheat] 컬러 강화할 타워를 먼저 클릭하세요");
                else if (TowerManager.Instance.TryUpgradeColor(selected.Definition.Color))
                {
                    // ── 수업 20 확인용 임시 코드 ── 같은 컬러 타워가 다 바뀌었는지 (수업 24 ClickDebugWindow가 생기면 지움)
                    TowerSlots slots = BoardManager.Instance.Slots;
                    for (int i = 0; i < slots.Count; i++)
                    {
                        TowerController tower = slots.Get(i);
                        if (tower == null) continue;
                        TowerFinalStats s = tower.Stats;
                        Debug.Log($"[DebugCheat] {tower.name}  Lv.{s.ColorLevel}  AbilityValue {s.AbilityValue}  AttackCount {s.AttackCount}  Mastery {s.Mastery}  Stack {s.Stack}");
                    }
                }
            }
        }

        private static void ToggleEnemyModifier(string source, params StatModifier[] modifiers)    // ── 수업 21 확인용 임시 코드 ──
        {
            foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
            {
                if (enemy.Stats.Modifiers.RemoveBySource(source) == 0)
                {
                    foreach (StatModifier modifier in modifiers) enemy.Stats.Modifiers.Add(modifier);
                }
                MoveRange range = enemy.Stats.GetMoveRange();
                Debug.Log($"[DebugCheat] {enemy.name}  이동력 {range.Min} ~ {range.Max}  행동 주기 {range.Action}  " +
                          $"매 턴 카운트 −{1 + Mathf.RoundToInt(enemy.Stats.Get(StatType.ActionGainPerTurn))}");
            }
        }

        private static readonly MergeMode[] s_autoMergeOrder = { MergeMode.Exact, MergeMode.Color, MergeMode.Emblem };

        private void AutoPlayerTurn()
        {
            for (int round = 0; round < 50; round++)                  // 멈추지 않게 횟수 제한
            {
                AutoBuildAll();                                        // 골드가 있는 만큼 생성
                if (BoardManager.Instance.Slots.TryGetFirstEmpty(out _)) break;   // 칸이 남았으면 합성하지 않고 다음 턴까지 기다림
                if (!AutoMergeAll()) break;                            // 꽉 찼으면 합성 → 빈 칸이 생기면 다시 생성부터
            }

            AutoArrangeAttackTowers();
        }

        private const int AutoLongRange = 3;                           // Range가 이 이상이면 긴 사거리, 아래면 짧은 사거리

        // 공격 타워 자리 잡기: 높은 티어부터 남은 칸 중 가장 좋은 칸으로 (그 칸의 타워와 교환)
        //   긴 사거리  사거리 안에 들어오는 길 칸이 가장 많은 곳 (같으면 길에 가까운 곳)
        //   짧은 사거리 길에 가장 가까운 곳 (같으면 사거리 안의 길 칸이 많은 곳)
        private static void AutoArrangeAttackTowers()
        {
            BoardGrid grid = BoardManager.Instance.Grid;
            TowerSlots slots = BoardManager.Instance.Slots;

            var attackers = new List<TowerController>();
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && tower.IsAttackTower) attackers.Add(tower);
            }
            attackers.Sort((a, b) => a.Definition.Tier != b.Definition.Tier
                ? b.Definition.Tier.CompareTo(a.Definition.Tier)
                : b.Stats.Range.CompareTo(a.Stats.Range));

            var placed = new bool[slots.Count];                        // 이미 자리를 잡은 타워가 있는 칸 (다시 빼앗지 않음)
            foreach (TowerController tower in attackers)
            {
                int range = tower.Stats.Range;
                int best = tower.SlotIndex;                            // 같은 점수면 지금 자리에 둠 (쓸데없이 옮기지 않게)
                (int, int) bestScore = GetSlotScore(grid, best, range);

                for (int slot = 0; slot < slots.Count; slot++)
                {
                    if (placed[slot]) continue;
                    (int, int) score = GetSlotScore(grid, slot, range);
                    if (score.CompareTo(bestScore) > 0)
                    {
                        best = slot;
                        bestScore = score;
                    }
                }

                if (best != tower.SlotIndex) TowerManager.Instance.TryMoveOrSwap(tower, best);
                placed[tower.SlotIndex] = true;
            }
        }

        private static (int, int) GetSlotScore(BoardGrid grid, int slot, int range)    // 클수록 좋음 (앞 값 먼저 비교)
        {
            Vector3Int cell = grid.GetSpawnCell(slot);
            int coverage = grid.GetPathIndicesInSquare(cell, range).Count;
            int distance = GetDistanceToPath(grid, cell);

            return range >= AutoLongRange ? (coverage, -distance) : (-distance, coverage);
        }

        private static int GetDistanceToPath(BoardGrid grid, Vector3Int cell)
        {
            int nearest = int.MaxValue;
            for (int i = 0; i < grid.PathCount; i++)
            {
                nearest = Mathf.Min(nearest, BoardGrid.GetTileDistance(grid.GetPathCell(i), cell));
            }
            return nearest;
        }

        private void OnTowerAdded(TowerController tower)                // 자동 진행 중 3티어 타워가 생기면(합성) 그 컬러를 한 번 컬러 강화
        {
            if (!m_autoEndTurn || tower.Definition.Tier != 3) return;

            TowerColor color = tower.Definition.Color;
            ColorUpgradeState levels = TowerManager.Instance.ColorLevels;
            if (!levels.CanUpgrade(color) || CurrencyManager.Instance.Gem < levels.GetCost(color)) return;   // 안 되는 건 로그 없이 넘어감

            TowerManager.Instance.TryUpgradeColor(color);
        }

        private static bool AutoBuildAll()
        {
            TowerManager towers = TowerManager.Instance;
            bool builtAny = false;
            while (CurrencyManager.Instance.Gold >= towers.BuildCost
                   && BoardManager.Instance.Slots.TryGetFirstEmpty(out _)
                   && towers.TryBuild())
            {
                builtAny = true;
            }
            return builtAny;
        }

        private static bool AutoMergeAll()
        {
            bool mergedAny = false;
            for (int count = 0; count < 100; count++)
            {
                if (!AutoMergeOnce()) break;
                mergedAny = true;
            }
            return mergedAny;
        }

        private static bool AutoMergeOnce()                            // 확정 → 컬러 → 문양 순서로 하나라도 되면 true (되면 다시 확정부터)
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            var materials = new List<TowerController>();

            foreach (MergeMode mode in s_autoMergeOrder)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    TowerController tower = slots.Get(i);
                    if (tower == null) continue;
                    if (!TowerProgression.TryFindMaterials(tower, mode, slots, materials)) continue;   // 재료가 없으면 로그 없이 건너뜀
                    if (TowerManager.Instance.TryMerge(tower, mode)) return true;
                }
            }
            return false;
        }

        private void StartPickAttackCell()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || !tower.IsAttackTower || !TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[DebugCheat] 플레이어 턴에 공격 타워를 먼저 클릭하세요");
                return;
            }

            BoardInputController input = FindFirstObjectByType<BoardInputController>();
            if (input != null) input.ChangeMode(new PickAttackCellMode(input, tower));
        }

        private void OnEnemyCountChanged(int count)
        {
            Debug.Log($"[DebugCheat] 적 {count} / 한도 {GameSession.Instance.CurrentLimit}");
        }

        private void OnCurrencyChanged(int gold, int gem)
        {
            Debug.Log($"[DebugCheat] 골드 {gold} / 젬 {gem}");
        }
    }
}
