using UnityEngine;

// 9. Wind: ³Ë¹é ¹ÐÄ§
public class WindDebuff : DebuffBase
{
    public WindDebuff(float duration, float pushSpeed)
        : base(DebuffTarget.Wind, duration, pushSpeed) { }

    public override void OnUpdate(EnemyHealthController health, EnemyMovementController movement, float deltaTime)
    {
        base.OnUpdate(health, movement, deltaTime);
      //  float step = 0.05f * movement.CurrentSpeed * Value * deltaTime;
       // movement.MoveBackward(step);
    }
}
