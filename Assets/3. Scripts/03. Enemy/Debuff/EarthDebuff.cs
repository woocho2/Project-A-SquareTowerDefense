using UnityEngine;

// 10. Earth: 속박(기절) + 최대 체력 비례 데미지
public class EarthDebuff : DebuffBase
{
    public EarthDebuff(float duration, float maxHPDamagePercent)
        : base(DebuffTarget.Earth, duration, maxHPDamagePercent) { }

    public override void OnApply(EnemyHealthController health, EnemyMovementController movement)
    {
        movement.SetSpeedMultiplier(0f); // 속박
        health.ApplyDamage(health.MaxHP * (Value / 100f), false); // 최대 체력 비례 데미지
    }

    public override void OnRemove(EnemyHealthController health, EnemyMovementController movement)
    {
        movement.SetSpeedMultiplier(1f);
    }
}
