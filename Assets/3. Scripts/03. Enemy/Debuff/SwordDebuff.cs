using UnityEngine;

// 1. Sword: 저주 (최대 10스택, 스택당 MaxHP 2% 감소 -> 10스택 도달 시 영구 주박 전환)
public class SwordDebuff : DebuffBase
{
    private const int MAX_STACK = 10;
    private bool m_isBinding = false; // 10스택 주박 상태 여부
    private float m_reducedMaxHP = 0f;

    public SwordDebuff(float duration, float value) : base(DebuffTarget.Sword, duration, value) { }

    public override void OnApply(EnemyHealthController health, EnemyMovementController movement)
    {
        ApplyCurse(health);
    }

    public override void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        base.OnRefresh(health, movement, newDuration, newValue);

        if (!m_isBinding)
        {
            Stack++;
            if (Stack >= MAX_STACK)
            {
                // [주박]으로 변경: 영구 지속(시간 무제한)
                m_isBinding = true;
                Duration = float.MaxValue;
            }
            ApplyCurse(health);
        }
    }

    private void ApplyCurse(EnemyHealthController health)
    {
        // 스택당 최대 체력 2% 감소 적용
        float reductionPercent = m_isBinding ? 0.2f : (Stack * 0.02f);
        m_reducedMaxHP = health.MaxHP * reductionPercent;
    }
}
