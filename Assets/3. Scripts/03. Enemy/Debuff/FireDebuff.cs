using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

// 6. Fire: 지속 화염 피해 (DoT)
public class FireDebuff : DebuffBase
{
    private float m_tickTimer = 0f;
    private const float TICK_INTERVAL = 0.5f;

    public FireDebuff(float duration, float damagePerTick)
        : base(DebuffTarget.Fire, duration, damagePerTick) { }

    public override void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        base.OnUpdate(health, movement, deltaTime);

        m_tickTimer += deltaTime;
        if (m_tickTimer >= TICK_INTERVAL)
        {
            m_tickTimer -= TICK_INTERVAL;
            health.ApplyDamage(Value, false);
        }
    }
}
