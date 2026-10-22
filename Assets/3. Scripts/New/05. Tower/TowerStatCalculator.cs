namespace SquareTD
{
    public static class TowerStatCalculator
    {
        public static TowerFinalStats Calculate(TowerDefinition definition, TowerState state)
        {
            return new TowerFinalStats(definition);
        }
    }
}
