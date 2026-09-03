using UnityEngine;

// 5. Hammer: 방어력 감소 (수치만큼 방어력 배율 감소)
public class HammerDebuff : DebuffBase
{
    public HammerDebuff(float duration, float defenseReductionPercent)
        : base(DebuffTarget.Hammer, duration, defenseReductionPercent) { }

    public override void OnApply(EnemyHealthController health, EnemyMovementController movement)
    {
        float multiplier = Mathf.Clamp(1f - (Value / 100f), 0.1f, 1f);
        health.SetDefendMultiplier(multiplier);
    }

    public override void OnRemove(EnemyHealthController health, EnemyMovementController movement)
    {
        health.SetDefendMultiplier(1f);
    }
}