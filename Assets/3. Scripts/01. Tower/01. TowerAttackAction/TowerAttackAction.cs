using UnityEngine;


public abstract class TowerAttackAction
{
    protected TowerData m_data;
    protected ProjectileData m_projectileData;
    
    public TowerAttackAction(TowerData data, ProjectileData projectileData)
    {
        m_data = data;
        m_projectileData = projectileData;
    }

    public abstract bool ExecuteAction(Transform towerTransform, TowerStats finalStats);

    protected virtual bool TryFindClosestTarget(Vector2 origin, float range, LayerMask targetLayer, out EnemyController targetEnemy)
    {
        targetEnemy = null;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(origin, range, targetLayer);

        if (hitEnemies.Length == 0)
        {
            return false;
        }

        float minSqrDistance = Mathf.Infinity;

        foreach (Collider2D hit in hitEnemies)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                Vector2 direction = (Vector2)hit.transform.position - origin;
                float sqrDistance = direction.sqrMagnitude;

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    targetEnemy = enemy;
                }
            }
        }        
        return targetEnemy != null;
    }
}