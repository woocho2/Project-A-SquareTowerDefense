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
        private AttackSkill m_skill;                        // 💬 스킬 2 (공격 타워만. 버프·디버프 타워는 null)
        public AttackSkill Skill => m_skill;

        public bool IsSplash => Definition.Color == TowerColor.Red;     // 💬 광역 = 스킬 주기(턴)로 발동
        public bool IsTarget => Definition.Color == TowerColor.Blue;    // 💬 단일 = 같은 적이 맞은 횟수로 발동

        public void Init(TowerDefinition definition, int slotIndex, TowerAction action,
                         TowerDefinition tier1Definition = null, ColorUpgradeState colorLevels = null, AttackSkill skill = null)
        {
            m_skill = skill;                                // 💬 맨 뒤 칸(= null)이라 스킬 없이 부르는 예전 테스트들도 그대로 돎
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

        public void SetSpawnTile(SpawnTileType type) => SpawnTileEffects.Apply(State.Modifiers, type);    // 수정자가 바뀌면 Changed → RecalculateStats

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
            m_skill?.OnOwnerActed(this);                    // 💬 훅: 자기 버프의 유지 감소 (도끼·바람 단일, 수업 31·36). 지금은 빈 함수
            yield return m_action.Act(this, Stats, handle);
        }

        // ── 스킬 2 발동 (수업 23) ──

        // 💬 ⓪ 적 턴 시작: 광역은 스킬 주기 카운트 +1
        public void OnEnemyTurnStart()
        {
            if (m_skill == null) return;
            if (IsSplash) State.SkillTurnCount++;
            m_skill.OnEnemyTurnStart(this);                 // 💬 훅 (지금은 빈 함수)
        }

        // 💬 ④ 광역: 카운트가 Duration에 닿았으면 발동하고 0으로. 발동했으면 true
        public bool TryFireTurnSkill()
        {
            if (m_skill == null || !IsSplash) return false;
            if (State.SkillTurnCount < Stats.Duration) return false;    // 💬 아직 주기가 안 됨

            int hitCount = m_skill.Fire(this, null);        // 💬 광역 시험 스킬은 맵 전체라 "발동시킨 적"이 없음 → null
            if (hitCount == 0) return false;                // 💬 맞을 적이 하나도 없으면 카운트를 그대로 두고 다음 턴에 다시 (스킬을 버리지 않음)

            State.SkillTurnCount = 0;
            return true;
        }

        // 💬 기본 공격이 대상에 맞았을 때 (TargetAction · SplashAction의 도착 콜백이 부름)
        //    단일: 그 적의 이 문양 적중 수가 이 타워의 Duration 이상이면 그 적에게 발동하고 그 문양의 수를 0으로
        //    → 하위 타워가 쌓은 수를 상위 타워가 터뜨리는 것도 이 비교 하나로 됨 (20번 4-1장의 예)
        public void OnBasicHitLanded(ITarget target)
        {
            if (m_skill == null) return;
            m_skill.OnBasicHit(this, target);               // 💬 훅: 바람 광역(당기기)·전기 광역(전하)·빛 단일(도깨비불). 지금은 빈 함수

            if (!IsTarget || !target.IsAlive || target.Body == null) return;

            TowerEmblem emblem = Definition.Emblem;
            if (target.Body.HitCounter.Get(emblem) < Stats.Duration) return;   // 💬 아직 덜 맞음

            target.Body.HitCounter.Reset(emblem);           // 💬 먼저 0으로 (스킬 피해로 죽어도 순서가 꼬이지 않게)
            m_skill.Fire(this, target);
            Debug.Log($"[Skill] {name} → {target.Body.name} 스킬 2 발동 (적중 {Stats.Duration}회)");    // ── 수업 23 확인용 임시 로그 ──
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
