using UnityEngine;

// 12. Light: 체력이 일정 비율 이하일 때 즉시 산화(처치)
public class LightDebuff : DebuffBase
{
    public LightDebuff(float duration, float executionThresholdPercent)
        : base(DebuffTarget.Light, duration, executionThresholdPercent) { }

    public override void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        base.OnUpdate(health, movement, deltaTime);

        // 체력 비율이 기준치 이하로 떨어지면 즉사 처리
        if (health.CurrentHP > 0f && (health.CurrentHP / health.MaxHP) <= (Value / 100f))
        {
            health.ExecuteInstantKill();
        }
    }
}
