namespace SquareTD
{
    public class TowerDefinition
    {
        public int Id { get; }
        public float Power { get; }
        public int Range { get; }
        public int Action { get; }
        public int AttackCount { get; }
        public float CriticalRate { get; }
        public float CriticalDamage { get; }
        public int Mastery { get; }
        public int Stack { get; }
        public int Duration { get; }
        public float AbilityValue { get; }
        public float ProjectileSpeed { get; }
        public TargetPriority TargetPriority { get; }

        public int Tier => TowerId.GetTier(Id);
        public TowerColor Color => TowerId.GetColor(Id);
        public TowerEmblem Emblem => TowerId.GetEmblem(Id);
        public AttackAttribute Attribute => TowerId.GetAttribute(Emblem);

        public TowerDefinition(int id, float power, int range, int action, int attackCount,
                               float criticalRate, float criticalDamage, int mastery, int stack,
                               int duration, float abilityValue, float projectileSpeed, TargetPriority targetPriority)
        {
            Id = id;
            Power = power;
            Range = range;
            Action = action;
            AttackCount = attackCount;
            CriticalRate = criticalRate;
            CriticalDamage = criticalDamage;
            Mastery = mastery;
            Stack = stack;
            Duration = duration;
            AbilityValue = abilityValue;
            ProjectileSpeed = projectileSpeed;
            TargetPriority = targetPriority;
        }
    }
}
