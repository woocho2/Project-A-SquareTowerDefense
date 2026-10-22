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

            if (keyboard.kKey.wasPressedThisFrame)
            {
                HitFirstEnemy(60f);
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                HitAllEnemies(99999f);
            }
        }

        private void HitFirstEnemy(float damage)
        {
            IReadOnlyList<EnemyController> alive = EnemyManager.Instance.Registry.Alive;
            if (alive.Count == 0)
            {
                Debug.Log("[DebugCheat] 때릴 적이 없음");
                return;
            }

            EnemyController enemy = alive[0];
            enemy.TakeDamage(new DamageInfo { Source = DamageSource.Basic, BaseDamage = damage });
            Debug.Log($"[DebugCheat] {enemy.name} 체력 {enemy.Health.Current:0.0} / {enemy.Health.Max:0.0}");
        }

        private void HitAllEnemies(float damage)
        {
            foreach (EnemyController enemy in new List<EnemyController>(EnemyManager.Instance.Registry.Alive))
            {
                enemy.TakeDamage(new DamageInfo { Source = DamageSource.Basic, BaseDamage = damage });
            }
            Debug.Log($"[DebugCheat] 모든 적에게 {damage} / 남은 적 {EnemyManager.Instance.Registry.Count}, 보스 타이머 {EnemyManager.Instance.BossTimer.Count}");
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
