using UnityEngine;

public abstract class TowerAttackAction
{
    protected TowerData m_data;

    public TowerAttackAction(TowerData data)
    {
        m_data = data;
    }

    public abstract bool ExecuteAction(Transform towerTransform, TowerStats finalStats);

    // priority 매개변수에 기본값(= TargetPriority.Closest)을 지정하여 함수 1개로 통합
    protected virtual bool TryFindTarget(
        Vector2 origin,
        float range,
        LayerMask targetLayer,
        out EnemyHealthController targetEnemy,
        TargetPriority priority = TargetPriority.Closest)
    {
        targetEnemy = null;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(origin, range, targetLayer);

        if (hitEnemies.Length == 0)
        {
            return false;
        }

        float minSqrDistance = Mathf.Infinity;
        float maxWaveIndex = -1f;
        float minWaveIndex = Mathf.Infinity;
        float maxHp = -1f;
        float minHp = Mathf.Infinity;

        for (int i = 0; i < hitEnemies.Length; i++)
        {
            Collider2D hit = hitEnemies[i];

            EnemyHealthController health = hit.GetComponentInParent<EnemyHealthController>();
            if (health == null || !health.gameObject.activeInHierarchy || health.CurrentHP <= 0f) continue;

            hit.transform.parent.TryGetComponent<EnemyMovementController>(out var movement);
            if (movement == null)
            {
                health.TryGetComponent<EnemyMovementController>(out movement);
            }

            switch (priority)
            {
                case TargetPriority.Closest:
                case TargetPriority.Default:
                    Vector2 direction = (Vector2)hit.transform.position - origin;
                    float sqrDistance = direction.sqrMagnitude;

                    if (sqrDistance < minSqrDistance)
                    {
                        minSqrDistance = sqrDistance;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.First:
                    if (movement != null && movement.WavePointIndex > maxWaveIndex)
                    {
                        maxWaveIndex = movement.WavePointIndex;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.Last:
                    if (movement != null && movement.WavePointIndex < minWaveIndex)
                    {
                        minWaveIndex = movement.WavePointIndex;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.Strongest:
                    if (health.IsBoss)
                    {
                        targetEnemy = health;
                        return true;
                    }

                    if (health.CurrentHP > maxHp)
                    {
                        maxHp = health.CurrentHP;
                        targetEnemy = health;
                    }
                    break;

                case TargetPriority.Weakest:
                    if (health.CurrentHP < minHp)
                    {
                        minHp = health.CurrentHP;
                        targetEnemy = health;
                    }
                    break;
            }
        }

        return targetEnemy != null;
    }
}