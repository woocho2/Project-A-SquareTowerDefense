using UnityEngine;

// 11. Darkness: 중심부 끌어당김 + 초당 0.5% 데미지
public class DarknessDebuff : DebuffBase
{
    private Vector3 m_centerPos;
    private float m_tickTimer = 0f;

    public DarknessDebuff(float duration, Vector3 centerPos)
        : base(DebuffTarget.Darkness, duration, 0.5f)
    {
        m_centerPos = centerPos;
    }

    public override void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        base.OnUpdate(health, movement, deltaTime);

        // 중심부로 흡인
        Vector3 dir = (m_centerPos - health.transform.position).normalized;
        float step = movement.CurrentSpeed * 1.5f * deltaTime;
        health.transform.position += dir * step;

        // 초당 0.5% 지속 피해
        m_tickTimer += deltaTime;
        if (m_tickTimer >= 1.0f)
        {
            m_tickTimer -= 1.0f;
            health.ApplyDamage(health.MaxHP * 0.005f, false);
        }
    }
}
