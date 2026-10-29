namespace SquareTD
{
    // 디버프 규칙의 베이스 (문양마다 하나)
    public abstract class DebuffRule
    {
        public abstract StatusEffect CreateEffect(Zone zone, EnemyController enemy);

        public virtual void OnStep(Zone zone, EnemyController enemy) { }

        protected static float Resist(EnemyController enemy, float value) => enemy.Resistance.ResistWeak(value);
    }
}
