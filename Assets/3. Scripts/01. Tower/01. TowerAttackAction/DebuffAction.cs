using UnityEngine;

public class DebuffAction : TowerAttackAction
{
    // base() 매개변수를 data 1개로 맞춥니다.
    public DebuffAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(towerTransform.position, currentStats.Range, m_data.targetLayer);

        if (targets.Length == 0) return false;

        float debuffValue = 0f;
        float debuffDuration = m_data.duration;

        switch (m_data.debuffTarget)
        {
            case DebuffTarget.Slow:
                debuffValue = currentStats.AbilityValue;
                break; 
            case DebuffTarget.Stun:
                debuffValue = 0f;
                break;
            case DebuffTarget.DotDamage:
                debuffValue = currentStats.AbilityValue * 0.05f;
                Debug.Log(debuffValue);
                break;
            case DebuffTarget.Weak:
                debuffValue = currentStats.AbilityValue * 0.1f;
                Debug.Log(debuffValue);
                break;
        }

        foreach (Collider2D hit in targets)
        {
            ApplyDebuffToEnemy(hit, debuffValue, debuffDuration);
        }
        return true;
    }

    private void ApplyDebuffToEnemy(Collider2D hit, float value, float duration)
    {
        EnemyController enemy = hit.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            enemy.ApplyDebuff(m_data.debuffTarget, value, duration);
        }
    }
}