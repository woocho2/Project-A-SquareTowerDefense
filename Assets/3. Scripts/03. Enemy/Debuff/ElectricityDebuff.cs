using UnityEngine;

// 8. Electricity: 감전 (스택당 4% 둔화 -> 5스택 시 5초간 누전: 이속 20% 감소 + 초당 MaxHP % 피해)
public class ElectricityDebuff : DebuffBase
{
    private const int MAX_STACK = 5;
    private bool m_isShortCircuit = false; // 누전 상태 여부
    private float m_tickTimer = 0f;

    public ElectricityDebuff(float duration, float maxHPDamagePercent)
        : base(DebuffTarget.Electricity, duration, maxHPDamagePercent) { }

    public override void OnApply(EnemyHealthController health, EnemyMovementController movement)
    {
        movement.SetSpeedMultiplier(1f - (0.04f * Stack));
    }

    public override void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        base.OnRefresh(health, movement, newDuration, newValue);

        if (!m_isShortCircuit)
        {
            Stack++;
            if (Stack >= MAX_STACK)
            {
                // [누전] 상태로 전환: 5초 지속
                m_isShortCircuit = true;
                Duration = 5.0f;
                movement.SetSpeedMultiplier(0.8f); // 이동속도 20% 감소
                return;
            }
            movement.SetSpeedMultiplier(1f - (0.04f * Stack));
        }
    }

    public override void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        base.OnUpdate(health, movement, deltaTime);

        // 누전 상태: 초당 최대 체력 비례 피해
        if (m_isShortCircuit)
        {
            m_tickTimer += deltaTime;
            if (m_tickTimer >= 1.0f)
            {
                m_tickTimer -= 1.0f;
                health.ApplyDamage(health.MaxHP * (Value / 100f), false);
            }
        }
    }

    public override void OnRemove(EnemyHealthController health, EnemyMovementController movement)
    {
        movement.SetSpeedMultiplier(1f);
    }
}