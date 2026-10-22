namespace SquareTD
{
    public class EnemyHealth
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        public void Reset(float max)
        {
            Max = max;
            Current = Max;
            IsDead = false;
        }

        public bool TakeDamage(float amount)
        {
            if (IsDead) return false;

            Current -= amount;
            if (Current > 0f) return false;

            Current = 0f;
            IsDead = true;
            return true;
        }
    }
}
