namespace SquareTD
{
    public class TowerFinalStats
    {
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

        public TowerFinalStats(TowerDefinition definition)
        {
            Power = definition.Power;
            Range = definition.Range;
            Action = definition.Action;
            AttackCount = definition.AttackCount;
            CriticalRate = definition.CriticalRate;
            CriticalDamage = definition.CriticalDamage;
            Mastery = definition.Mastery;
            Stack = definition.Stack;
            Duration = definition.Duration;
            AbilityValue = definition.AbilityValue;
        }
    }
}
