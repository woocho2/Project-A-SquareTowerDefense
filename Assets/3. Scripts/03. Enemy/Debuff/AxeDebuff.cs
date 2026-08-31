using UnityEngine;

// 4. Axe: 취약 (스택당 받는 피해 증가, 최대 5스택)
public class AxeDebuff : DebuffBase
{
    private const int MAX_STACK = 5;

    public AxeDebuff(float duration, float vulnerabilityPercent)
        : base(DebuffTarget.Axe, duration, vulnerabilityPercent) { }

    public override void OnApply(EnemyHealthController health, EnemyMovementController movement)
    {
        UpdateVulnerability(health);
    }

    public override void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        base.OnRefresh(health, movement, newDuration, newValue);
        if (Stack < MAX_STACK) Stack++;
        UpdateVulnerability(health);
    }

    private void UpdateVulnerability(EnemyHealthController health)
    {
        float multiplier = 1f + ((Value * Stack) / 100f);
        health.SetVulnerability(multiplier);
    }

    public override void OnRemove(EnemyHealthController health, EnemyMovementController movement)
    {
        health.SetVulnerability(1f);
    }
}
