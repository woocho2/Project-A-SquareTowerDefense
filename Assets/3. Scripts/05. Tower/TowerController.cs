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
            if (IsAttackTower) State.Stack.Add(Stats.AttackCount);
            m_skill?.OnOwnerActed(this);
            yield return m_action.Act(this, Stats, handle);
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

        // 광역: 기본 공격이 한 발 떨어질 때마다 1씩, Duration 발이 되면 스킬 2를 줄 세우고 0부터 다시 셈. 줄 세웠으면 true
        public bool OnSplashShotLanded(ITarget aimed)
        {
            if (m_skill == null || !IsSplash) return false;
            State.SkillShotCount++;
            if (State.SkillShotCount < Stats.Duration) return false;

            State.SkillShotCount = 0;
            m_pendingSkills.Enqueue(aimed);         // 착탄 범위를 쓰는 스킬의 중심은 이 발의 대상
            return true;
        }

        // 단일: 그 적이 맞은 수가 Duration 이상이면 그 적의 수를 0으로 하고 스킬 2를 줄 세움
        public void OnBasicHitLanded(ITarget target)
        {
            if (m_skill == null) return;
            m_skill.OnBasicHit(this, target);

            if (!IsTarget || !target.IsAlive || target.Body == null) return;

            TowerEmblem emblem = Definition.Emblem;
            if (target.Body.HitCounter.Get(emblem) < Stats.Duration) return;
            target.Body.HitCounter.Reset(emblem);
            m_pendingSkills.Enqueue(target);
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
                        Debug.Log($"[Skill] {name} 스킬 2 발동 (착탄 {Stats.Duration}발) → {aliveBefore - AliveEnemyCount()}마리 죽음, 남은 적 {AliveEnemyCount()}");   // ── 확인용 임시 로그 ──
                        return true;
                    }

                    State.SkillShotCount = Stats.Duration;      // 맞힐 적이 없음: 다 찬 채로 남겨 두고 다음 착탄에 다시 (스킬을 버리지 않음)
                    m_pendingSkills.Clear();
                    return false;
                }

                if (!target.IsAlive) continue;                  // 그사이 기본 공격에 죽음. 그 적에게만 쏘는 스킬이라 넘어감
                m_skill.Fire(this, target);
                Debug.Log($"[Skill] {name} → {target.Body.name} 스킬 2 발동 (적중 {Stats.Duration}회) → {(target.IsAlive ? $"버팀, 체력 {target.CurrentHp:0.0}" : "죽음")}");    // ── 수업 23 확인용 임시 로그 ──
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
