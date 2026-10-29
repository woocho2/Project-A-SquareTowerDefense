using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 타워 하나: 스탯, 행동, 스킬 2 발동
    [RequireComponent(typeof(TowerVisual))]
    public class TowerController : MonoBehaviour
    {
        public TowerDefinition Definition { get; private set; }
        public TowerState State { get; private set; }
        public TowerFinalStats Stats { get; private set; }
        private TowerAction m_action;

        [SerializeField] private ShotTiming m_shotTiming = ShotTiming.Default;
        public ShotTiming ShotTiming => m_shotTiming;

        private static int s_nextBuildOrder;
        public int BuildOrder { get; private set; }

        public int SlotIndex { get; private set; }
        public bool IsAttackTower => Definition.Color == TowerColor.Red || Definition.Color == TowerColor.Blue;

        private TowerDefinition m_tier1Definition;
        private ColorUpgradeState m_colorLevels;
        private AttackSkill m_skill;
        public AttackSkill Skill => m_skill;

        public bool IsSplash => Definition.Color == TowerColor.Red;
        public bool IsTarget => Definition.Color == TowerColor.Blue;

        public void Init(TowerDefinition definition, int slotIndex, TowerAction action,
                         TowerDefinition tier1Definition = null, ColorUpgradeState colorLevels = null, AttackSkill skill = null)
        {
            m_skill = skill;
            Definition = definition;
            SlotIndex = slotIndex;
            BuildOrder = ++s_nextBuildOrder;
            m_tier1Definition = tier1Definition;
            m_colorLevels = colorLevels;
            State = new TowerState(definition);
            Stats = CalculateStats();
            State.RemainingAction = Stats.Action;
            State.Stack.SetMax(Stats.Stack);
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
            State.Stack.SetMax(Stats.Stack);
        }

        private TowerFinalStats CalculateStats()
        {
            int level = 1;
            if (m_colorLevels != null) level = m_colorLevels.GetLevel(Definition.Color);

            return TowerStatCalculator.Calculate(Definition, m_tier1Definition, State, level);
        }

        public void SetSpawnTile(SpawnTileType type) => SpawnTileEffects.Apply(State.Modifiers, type);

        public bool TickAction()
        {
            int gain = Mathf.RoundToInt(Stats.Get(StatType.ActionGainPerTurn));
            State.RemainingAction = Mathf.Max(0, State.RemainingAction - (1 + gain));
            if (State.RemainingAction > 0) return false;

            if (m_action != null && !m_action.CanAct(this, Stats)) return false;

            State.RemainingAction = Stats.Action;
            return true;
        }

        public IEnumerator Act(AttackHandle handle)
        {
            if (m_action == null) yield break;
            if (IsAttackTower)
            {
                State.Stack.Add(Stats.AttackCount * (1f + Stats.Get(StatType.StackGainRate)));
                foreach (Zone zone in new List<Zone>(State.ReceivedZoneBuffs.Keys))
                    SkillRegistry.GetBuffRule(zone.Emblem).OnReceiverActed(zone, this);
            }
            m_skill?.OnOwnerActed(this);
            yield return m_action.Act(this, Stats, handle);
            TimedTowerBuff.CountDown(State);
        }

        private readonly Queue<ITarget> m_pendingSkills = new Queue<ITarget>();

        public void OnEnemyTurnStart()
        {
            if (m_skill == null) return;
            m_skill.OnEnemyTurnStart(this);
        }

        public bool CountSkillHit(ITarget target)
        {
            if (m_skill == null || !IsAttackTower) return false;
            if (target == null || !target.IsAlive) return false;

            State.SkillHitCount++;
            if (State.SkillHitCount < Stats.Duration) return false;

            State.SkillHitCount = 0;

            m_pendingSkills.Enqueue(target);
            return true;
        }

        public void OnBasicHitLanded(ITarget target)
        {
            m_skill?.OnBasicHit(this, target);
            foreach (Zone zone in new List<Zone>(State.ReceivedZoneBuffs.Keys))
                SkillRegistry.GetBuffRule(zone.Emblem).OnReceiverHit(zone, this, target);
        }

        public void AdvanceSkillCount(int amount, ITarget target)
        {
            if (m_skill == null || amount <= 0 || target == null) return;

            State.SkillHitCount += amount;
            while (State.SkillHitCount >= Stats.Duration)
            {
                State.SkillHitCount -= Stats.Duration;
                m_pendingSkills.Enqueue(target);
            }
        }

        public bool HasPendingSkill => m_skill != null && m_pendingSkills.Count > 0;

        public bool WillFireNextSkill()
        {
            if (m_skill == null) return false;
            while (m_pendingSkills.Count > 0 && IsTarget && m_skill.NeedsLivingTarget && !m_pendingSkills.Peek().IsAlive)
                m_pendingSkills.Dequeue();
            if (m_pendingSkills.Count == 0) return false;
            return !IsSplash || !m_skill.NeedsLivingTarget || AliveEnemyCount() > 0;
        }

        public bool FireNextPendingSkill()
        {
            while (m_skill != null && m_pendingSkills.Count > 0)
            {
                ITarget target = m_pendingSkills.Dequeue();

                if (IsSplash)
                {
                    int aliveBefore = AliveEnemyCount();
                    if (m_skill.Fire(this, target) > 0)
                    {
                        Debug.Log($"[Skill] {name} 스킬 2 발동 (적중 {Stats.Duration}회) → {aliveBefore - AliveEnemyCount()}마리 죽음, 남은 적 {AliveEnemyCount()}");
                        return true;
                    }

                    State.SkillHitCount = Stats.Duration;
                    m_pendingSkills.Clear();
                    return false;
                }

                if (!target.IsAlive && m_skill.NeedsLivingTarget) continue;
                m_skill.Fire(this, target);
                Debug.Log($"[Skill] {name} → {(target.Transform != null ? target.Transform.name : "대상")} 스킬 2 발동 (적중 {Stats.Duration}회) → {(target.IsAlive ? $"버팀, 체력 {target.CurrentHp:0.0}" : "죽음")}");
                return true;
            }
            return false;
        }

        private static int AliveEnemyCount() => EnemyManager.Instance != null ? EnemyManager.Instance.Registry.Count : 0;

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
