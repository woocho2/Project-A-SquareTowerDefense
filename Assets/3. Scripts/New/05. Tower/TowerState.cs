namespace SquareTD
{
    public class TowerState
    {
        public int RemainingAction { get; set; }
        public TargetPriority TargetPriority { get; set; }

        public TowerState(TowerDefinition definition)
        {
            RemainingAction = definition.Action;
            TargetPriority = definition.TargetPriority;
        }
    }
}
