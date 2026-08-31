using UnityEngine;

public abstract class DebuffBase
{
    public DebuffTarget Target { get; protected set; }
    public float Duration { get; set; }
    public float Value { get; protected set; }
    public int Stack { get; protected set; } = 1;

    public DebuffBase(DebuffTarget target, float duration, float value)
    {
        Target = target;
        Duration = duration;
        Value = value;
        Stack = 1;
    }

    // 디버프 적용 시점
    public virtual void OnApply(EnemyHealthController health, EnemyMovementController movement) { }

    // 매 프레임 실행 (시간 차감 및 도트/변위 계산)
    public virtual void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        Duration -= deltaTime;
    }

    // 동일 디버프 중첩 시 갱신
    public virtual void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        if (newDuration > Duration) Duration = newDuration;
    }

    // 디버프 만료/제거 시점
    public virtual void OnRemove(EnemyHealthController health, EnemyMovementController movement) { }
}