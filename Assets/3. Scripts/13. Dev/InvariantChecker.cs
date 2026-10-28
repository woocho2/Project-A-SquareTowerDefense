using System.Linq;
using UnityEngine;

namespace SquareTD
{
    public class InvariantChecker : MonoBehaviour
    {
        private void Start()
        {
            TurnManager.Instance.EnemyTurnEnded += OnEnemyTurnEnded;               // 아래 함수 이름 (괄호 없이)
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnEnded -= OnEnemyTurnEnded;
        }

        private void OnEnemyTurnEnded(int turn)
        {
            if (ProjectileManager.Instance.FlyingCount > 0)
                Debug.LogError($"[InvariantChecker] 적 턴이 끝났는데 날아가는 탄 {ProjectileManager.Instance.FlyingCount}발");
            EnemyRegistry registry = EnemyManager.Instance.Registry;
            PathOccupancy occupancy = BoardManager.Instance.Occupancy;
            int pathCount = BoardManager.Instance.Grid.PathCount;

            if (EnemyManager.Instance.IsAnyEnemyMoving())
                Debug.LogError("[InvariantChecker] 적 턴이 끝났는데 움직이는 적이 있음");

            int activeCount = FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
            if (activeCount != registry.Count)                                    // 명단 수
                Debug.LogError($"[InvariantChecker] 켜진 적 {activeCount} ≠ 명단 {registry.Count}");

            int occupiedCount = 0;
            for (int i = 0; i < pathCount; i++)
            {
                occupiedCount += occupancy.GetAt(i).Count;               // 그 칸에 선 적 수
            }
            if (occupiedCount != registry.Count + DummyTarget.Count)               // 더미(빛 잔영·잔상)도 점유에 들어감, 명단에는 없음
                Debug.LogError($"[InvariantChecker] 점유 {occupiedCount} ≠ 명단 {registry.Count} + 더미 {DummyTarget.Count}");

            foreach (EnemyController enemy in registry.Alive)
            {
                int index = enemy.Movement.PathIndex;
                if (!occupancy.GetAt(index).Contains(enemy))
                    Debug.LogError($"[InvariantChecker] {enemy.name}이 {index}번 칸 점유에 없음");
            }
        }
    }
}
