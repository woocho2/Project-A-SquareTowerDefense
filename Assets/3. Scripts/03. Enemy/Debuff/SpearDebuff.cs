using UnityEngine;

// 3. Spear: 투창 (종료 시 현재 체력 비례 스택 데미지 폭발)
public class SpearDebuff : DebuffBase
{
    public SpearDebuff(float duration, float damagePerStackPercent)
        : base(DebuffTarget.Spear, duration, damagePerStackPercent) { }

    public override void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        base.OnRefresh(health, movement, newDuration, newValue);
        Stack++;
    }

    public override void OnRemove(EnemyHealthController health, EnemyMovementController movement)
    {
        if (health != null && health.CurrentHP > 0f)
        {
            float explodeDamage = health.CurrentHP * (Value / 100f) * Stack;
            health.ApplyDamage(explodeDamage, false);
        }
    }
}