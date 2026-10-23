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
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.CountChanged -= OnEnemyCountChanged;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= OnCurrencyChanged;
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
        }

        private static readonly MergeMode[] s_autoMergeOrder = { MergeMode.Exact, MergeMode.Color, MergeMode.Emblem };

        private void AutoPlayerTurn()
        {
            for (int round = 0; round < 50; round++)                  // 합성으로 칸이 비면 다시 생성 → 다시 합성 (멈추지 않게 횟수 제한)
            {
                bool built = AutoBuildAll();
                bool merged = AutoMergeAll();
                if (!built && !merged) break;
            }
            // 수업 20: 3티어 타워가 처음 나온 컬러를 젬으로 컬러 강화 (ColorUpgradeState가 생기면)
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
