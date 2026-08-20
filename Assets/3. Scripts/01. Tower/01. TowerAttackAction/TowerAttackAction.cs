using UnityEngine;

public abstract class TowerAttackAction
{
    protected TowerData m_data;

    public TowerAttackAction(TowerData data)
    {
        m_data = data;
    }

    public abstract bool ExecuteAction(Transform towerTransform, TowerStats finalStats);

    protected virtual bool TryFindTarget(Vector2 origin, float range, LayerMask targetLayer, TargetPriority priority, out EnemyController targetEnemy)
    {
        targetEnemy = null;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(origin, range, targetLayer);

        if (hitEnemies.Length == 0)
        {
            return false;
        }

        float minSqrDistance = Mathf.Infinity;
        float maxDistanceTraveled = -1f;
        float minDistanceTraveled = Mathf.Infinity;
        float maxHp = -1f;
        float minHp = Mathf.Infinity;

        foreach (Collider2D hit in hitEnemies)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            switch (priority)
            {
                // 1. 타워와 물리적 거리가 가장 가까운 적
                case TargetPriority.Closest:
                    Vector2 direction = (Vector2)hit.transform.position - origin;
                    float sqrDistance = direction.sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetEnemy = enemy;
                    }
                    break;

                // 2. 경로상 가장 앞선 적 (진행 거리가 가장 긴 적)
                case TargetPriority.First:
                    // EnemyController에 이동 거리/웨이포인트 진행도(DistanceTraveled) 속성이 있다고 가정
                    if (enemy.DistanceTraveled > maxDistanceTraveled)
                    {
                        maxDistanceTraveled = enemy.DistanceTraveled;
                        targetEnemy = enemy;
                    }
                    break;

                // 3. 경로상 가장 뒤처진 적 (스폰된 지 얼마 안 된 적)
                case TargetPriority.Last:
                    if (enemy.DistanceTraveled < minDistanceTraveled)
                    {
                        minDistanceTraveled = enemy.DistanceTraveled;
                        targetEnemy = enemy;
                    }
                    break;

                // 4. 체력이 가장 높거나 보스/엘리트인 적
                case TargetPriority.Strongest:
                    // 보스가 있다면 무조건 최우선 타겟 지정 처리 가능
                    if (enemy.CurrentHp > maxHp)
                    {
                        maxHp = enemy.CurrentHp;
                        targetEnemy = enemy;
                    }
                    break;

                // 5. 체력이 가장 낮아 빠르게 정리 가능한 적
                case TargetPriority.Weakest:
                    if (enemy.CurrentHp < minHp)
                    {
                        minHp = enemy.CurrentHp;
                        targetEnemy = enemy;
                    }
                    break;
            }
        }
        return targetEnemy != null;
    }
}