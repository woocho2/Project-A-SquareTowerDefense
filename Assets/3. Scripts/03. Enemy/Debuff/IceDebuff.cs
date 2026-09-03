using UnityEngine;

// 7. Ice: 둔화 (5스택 시 빙결 -> 이동불가 + 해제 시 빙결 중 누적 데미지의 20% 추가 피해)
public class IceDebuff : DebuffBase
{
    private const int MAX_STACK = 5;
    private bool m_isFrozen = false;
    private float m_accumulatedDamage = 0f;
    private float m_lastObservedHP;

    public IceDebuff(float duration, float slowPercent)
        : base(DebuffTarget.Ice, duration, slowPercent) { }

    public override void OnApply(EnemyHealthController health, EnemyMovementController movement)
    {
        UpdateSlow(movement);
    }

    public override void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        base.OnRefresh(health, movement, newDuration, newValue);

        if (!m_isFrozen)
        {
            Stack++;
            if (Stack >= MAX_STACK)
            {
                // 1초간 빙결 상태로 전환
                m_isFrozen = true;
                Duration = 1.0f;
                movement.SetSpeedMultiplier(0f);
                m_lastObservedHP = health.CurrentHP;
                return;
            }
            UpdateSlow(movement);
        }
    }

    public override void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        base.OnUpdate(health, movement, deltaTime);

        // 빙결 상태 동안 받은 데미지 추적
        if (m_isFrozen && health.CurrentHP < m_lastObservedHP)
        {
            m_accumulatedDamage += (m_lastObservedHP - health.CurrentHP);
            m_lastObservedHP = health.CurrentHP;
        }
    }

    private void UpdateSlow(EnemyMovementController movement)
    {
        float slowMultiplier = 100f / (100f + (Value * Stack));
        movement.SetSpeedMultiplier(slowMultiplier);
    }

    public override void OnRemove(EnemyHealthController health, EnemyMovementController movement)
    {
        movement.SetSpeedMultiplier(1f);

        // 빙결 해제 시 누적 데미지의 20% 추가 피해
        if (m_isFrozen && m_accumulatedDamage > 0f && health.CurrentHP > 0f)
        {
            health.ApplyDamage(m_accumulatedDamage * 0.2f, false);
        }
    }
}