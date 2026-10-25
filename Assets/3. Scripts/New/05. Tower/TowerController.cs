using System;
using System.Collections;
using UnityEngine;

namespace SquareTD
{
    [RequireComponent(typeof(TowerVisual))]
    public class TowerController : MonoBehaviour
    {
        public TowerDefinition Definition { get; private set; }
        public TowerState State { get; private set; }
        public TowerFinalStats Stats { get; private set; }
        private TowerAction m_action;

        [SerializeField] private SatelliteSettings m_satellite = SatelliteSettings.Default;
        public SatelliteSettings Satellite => m_satellite;

        public int SlotIndex { get; private set; }
        public bool IsAttackTower => Definition.Color == TowerColor.Red || Definition.Color == TowerColor.Blue;

        private TowerDefinition m_tier1Definition;
        private ColorUpgradeState m_colorLevels;

        public void Init(TowerDefinition definition, int slotIndex, TowerAction action,
                         TowerDefinition tier1Definition = null, ColorUpgradeState colorLevels = null)
        {
            Definition = definition;
            SlotIndex = slotIndex;
            m_tier1Definition = tier1Definition;
            m_colorLevels = colorLevels;
            State = new TowerState(definition);
            Stats = CalculateStats();            
            State.RemainingAction = Stats.Action;            
            State.Modifiers.Changed += RecalculateStats;
            name = $"{definition.Id} {EmblemNames.GetConceptName(definition.Id)}";
            GetComponent<TowerVisual>().Apply(definition);
            m_action = action;
        }

        public void RecalculateStats()
        {
            int oldAction = Stats.Action;
            Stats = CalculateStats();
            
            int diff = Stats.Action - oldAction;            
            State.RemainingAction = Mathf.Max(0, State.RemainingAction + diff);
        }

        private TowerFinalStats CalculateStats()
        {
            int level = 1;            
            if (m_colorLevels != null) level = m_colorLevels.GetLevel(Definition.Color);

            return TowerStatCalculator.Calculate(Definition, m_tier1Definition, State, level);
        }

        public bool TickAction()
        {
            if (State.RemainingAction > 0) State.RemainingAction--;
            if (State.RemainingAction > 0) return false;

            if (m_action != null && !m_action.CanAct(this, Stats)) return false;

            State.RemainingAction = Stats.Action;
            return true;
        }

        public IEnumerator Act(AttackHandle handle)
        {
            if (m_action == null) yield break;
            yield return m_action.Act(this, Stats, handle);
        }

        public void SetSlot(int slotIndex, Vector3 position)
        {
            State.AttackCellIndex = -1;
            SlotIndex = slotIndex;
            transform.position = position;
            m_action?.OnMoved(this);
        }

        public void OnRemoved()
        {
            m_action?.OnRemoved(this);
        }

    }
}
