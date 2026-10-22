using UnityEngine;

namespace SquareTD
{
    [RequireComponent(typeof(TowerVisual))]
    public class TowerController : MonoBehaviour
    {
        public TowerDefinition Definition { get; private set; }
        public TowerState State { get; private set; }
        public TowerFinalStats Stats { get; private set; }

        public int SlotIndex { get; private set; }
        public bool IsAttackTower => Definition.Color == TowerColor.Red || Definition.Color == TowerColor.Blue;

        public void Init(TowerDefinition definition, int slotIndex)
        {
            Definition = definition;
            SlotIndex = slotIndex;
            State = new TowerState(definition);
            Stats = TowerStatCalculator.Calculate(definition, State);
            name = $"{definition.Id} {EmblemNames.GetConceptName(definition.Id)}";
            GetComponent<TowerVisual>().Apply(definition);
        }

        public bool TickAction()
        {
            State.RemainingAction--;
            if (State.RemainingAction > 0) return false;

            Stats = TowerStatCalculator.Calculate(Definition, State);
            State.RemainingAction = Stats.Action;
            return true;
        }
    }
}
