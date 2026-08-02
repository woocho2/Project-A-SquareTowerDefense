using UnityEngine;

public class BuffAction : TowerAttackAction
{
    // base() 매개변수를 data 1개로 맞춥니다.
    public BuffAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(towerTransform.position, currentStats.range, m_data.targetLayer);

        if (targets.Length == 0) return false;

        float buffPower = currentStats.abilityValue * 1f;

        foreach (Collider2D hit in targets)
        {
            ApplyBuffToTarget(hit, buffPower, currentStats);
        }
        return true;
    }

    private void ApplyBuffToTarget(Collider2D hit, float buffPower, TowerStats buffStats)
    {
        TowerController targetTower = hit.GetComponent<TowerController>();
        if (targetTower != null)
        {
            float auraCooldown = buffStats.duration > 0 ? 1f / buffStats.duration : 1f;
            float duration = auraCooldown + 0.1f;

            targetTower.AddBuffStat(m_data.buffTarget, buffPower, buffStats.damage, duration, buffStats.duration);
        }
    }
}