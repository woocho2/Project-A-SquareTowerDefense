using UnityEngine;

public class BowDebuff : DebuffBase
{
    public BowDebuff(float duration, float value) : base(DebuffTarget.Bow, duration, value) { }

    public override void OnRefresh(EnemyHealthController health, EnemyMovementController movement, float newDuration, float newValue)
    {
        base.OnRefresh(health, movement, newDuration, newValue);
        Stack++;
    }
}
