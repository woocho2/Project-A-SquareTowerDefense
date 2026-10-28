using System.Collections;
using System.Collections.Generic;
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
                State.Stack.Add(Stats.AttackCount * (1f + Stats.Get(StatType.StackGainRate)));     // 물결·해류는 쌓이는 양 +% (수업 34)
                foreach (Zone zone in new List<Zone>(State.ReceivedZoneBuffs.Keys))
                    SkillRegistry.GetBuffRule(zone.Emblem).OnReceiverActed(zone, this);           // 해머 세공의 젬 (수업 32)
            }
            m_skill?.OnOwnerActed(this);
            yield return m_action.Act(this, Stats, handle);
            TimedTowerBuff.CountDown(State);            // 행동이 끝날 때 시간 버프의 남은 행동 −1 (수업 28 광폭화)
        }

        // ── 스킬 2 발동 (수업 23) ──
        //   착탄·적중 때는 "찼다"고 줄만 세움. 쏘는 것은 이번 행동의 기본 공격이 다 떨어진 뒤 TowerTurnRunner가
        //   FireNextPendingSkill로 하나씩 (2026-10-27 사용자: 스킬 연출 중에 같은 타워의 탄·다음 스킬이 겹치지 않게)
        private readonly Queue<ITarget> m_pendingSkills = new Queue<ITarget>();

        public void OnEnemyTurnStart()
        {
            if (m_skill == null) return;
            m_skill.OnEnemyTurnStart(this);
        }

        //   기본 공격이 살아 있는 적을 맞힐 때마다 1 (광역은 범위 안의 적마다, 단일은 한 발에 1). 어느 적이든 타워 하나에 합쳐 셈
        //   Duration이 되면 그 적을 대상(단일)·중심(광역)으로 스킬 2를 줄 세우고 0부터. 줄 세웠으면 true
        //   피해를 주기 전에 부름: 이 탄에 죽는 적도 "살아 있는 적을 맞힌 것". 이미 죽은 적은 세지 않음
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

        // 기본 공격이 맞은 뒤: 문양 스킬의 훅 (바람 광역의 당기기 등)
        public void OnBasicHitLanded(ITarget target)
        {
            m_skill?.OnBasicHit(this, target);
            foreach (Zone zone in new List<Zone>(State.ReceivedZoneBuffs.Keys))
                SkillRegistry.GetBuffRule(zone.Emblem).OnReceiverHit(zone, this, target);         // 얼음 냉기·한기, 전기 과충전 (수업 37·38)
        }

        // 스킬 2까지의 적중 수를 한꺼번에 더함 (바람 칼바람: 역풍·난기류인 적을 맞히면 당김, 수업 36). 다 차면 그 적으로 줄 세움
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

        // 줄 선 스킬 2를 하나 쏨. 쏘았으면 true → TowerTurnRunner가 연출이 끝나길 기다린 뒤 다시 부름
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
                        Debug.Log($"[Skill] {name} 스킬 2 발동 (적중 {Stats.Duration}회) → {aliveBefore - AliveEnemyCount()}마리 죽음, 남은 적 {AliveEnemyCount()}");   // ── 확인용 임시 로그 ──
                        return true;
                    }

                    State.SkillHitCount = Stats.Duration;       // 맞힐 적이 없음: 다 찬 채로 남겨 두고 다음 적중에 다시 (스킬을 버리지 않음)
                    m_pendingSkills.Clear();
                    return false;
                }

                if (!target.IsAlive && m_skill.NeedsLivingTarget) continue;     // 그사이 기본 공격에 죽음. 그 적에게만 쏘는 스킬이면 넘어감 (광폭화·뽑아찢기는 쏨)
                m_skill.Fire(this, target);
                Debug.Log($"[Skill] {name} → {(target.Transform != null ? target.Transform.name : "대상")} 스킬 2 발동 (적중 {Stats.Duration}회) → {(target.IsAlive ? $"버팀, 체력 {target.CurrentHp:0.0}" : "죽음")}");    // ── 수업 23 확인용 임시 로그 ──
                return true;
            }
            return false;
        }

        private static int AliveEnemyCount() => EnemyManager.Instance != null ? EnemyManager.Instance.Registry.Count : 0;   // 확인용 (테스트에는 매니저가 없음)

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
