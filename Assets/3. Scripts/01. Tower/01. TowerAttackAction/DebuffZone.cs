using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DebuffZone : MonoBehaviour
{
    // 장판 내부의 적을 추적하는 리스트 (외부에서 읽기 가능)
    public List<EnemyController> EnemiesInZone { get; private set; } = new List<EnemyController>();

    // 적의 진입/이탈 시 DebuffAction으로 신호를 보내기 위한 이벤트
    public Action<EnemyController> OnEnemyEnterEvent;
    public Action<EnemyController> OnEnemyExitEvent;

    [Header("Shield Tower (길막용)")]
    public GameObject obstacleCollider; // 물리적으로 길을 막는 콜라이더

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyController enemy = collision.GetComponentInParent<EnemyController>();
        if (enemy != null && !EnemiesInZone.Contains(enemy))
        {
            EnemiesInZone.Add(enemy);
            OnEnemyEnterEvent?.Invoke(enemy); // Action에 진입 알림
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        EnemyController enemy = collision.GetComponentInParent<EnemyController>();
        if (enemy != null && EnemiesInZone.Contains(enemy))
        {
            EnemiesInZone.Remove(enemy);
            OnEnemyExitEvent?.Invoke(enemy); // Action에 이탈 알림
        }
    }

    private void Update()
    {
        // 장판 위에서 적이 사망하거나 비활성화되었을 때 리스트에서 안전하게 제거 (역순 순회)
        for (int i = EnemiesInZone.Count - 1; i >= 0; i--)
        {
            if (EnemiesInZone[i] == null || !EnemiesInZone[i].gameObject.activeInHierarchy)
            {
                OnEnemyExitEvent?.Invoke(EnemiesInZone[i]);
                EnemiesInZone.RemoveAt(i);
            }
        }
    }

    // Shield 타워의 주기적인 길막 기능 온오프
    public void ToggleObstacle(bool isActive)
    {
        if (obstacleCollider != null)
            obstacleCollider.SetActive(isActive);
    }
}