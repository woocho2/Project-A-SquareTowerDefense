using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 공격 타워의 스킬 2 베이스 (20번 4-1장, 21번 4-4) — 템플릿 메서드
    //   Fire가 순서를 쥠: 스택을 쓸지 → 피해(공통 식) → 치명 1번 → 범위 → 대상마다 → 스택 비우기
    //   문양 스킬(수업 27부터)은 빈칸 둘(abstract)을 꼭 채우고, 훅(virtual)은 필요한 것만 덮어씀
    public abstract class AttackSkill
    {
        protected readonly BoardGrid m_grid;
        protected readonly PathOccupancy m_occupancy;
        private readonly StackModeSettings m_stackModes;

        protected AttackSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
        {
            m_grid = grid;
            m_occupancy = occupancy;
            m_stackModes = stackModes;
        }

        // ── 템플릿: 순서는 여기서만 정함. 문양 스킬은 이 함수를 덮어쓰지 않음 ──
        public int Fire(TowerController owner, ITarget mainTarget)     // 💬 void → int: 맞힌 대상 수를 돌려줌 (4b에서 바꿈)
        {
            TowerFinalStats stats = owner.Stats;
            TowerStack stack = owner.State.Stack;

            bool useStack = m_stackModes != null && m_stackModes.ShouldUseStack(owner.Definition, stack);

            bool keepHalf = stats.Get(StatType.StackKeepHalf) > 0f;    // 해류 존: 스택을 써도 절반에서 다시 (수업 34)

            if (CastInstead(owner, mainTarget, useStack))   // 피해 대신 다른 일을 하는 스킬(광폭화: 자기 버프, 화살비: 장판)은 여기서 끝 (수업 28·29)
            {
                if (useStack) stack.Consume(keepHalf);
                SkillEffectTable.PlayCast(owner);
                return 1;
            }

            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            float baseDamage = damage;                      // ── 확인용 임시 로그에 씀 ──
            if (useStack) damage = SkillFormula.WithStack(damage, stack.Max, stats.StackValue);

            bool isCritical = RollCritical(owner, stats, mainTarget);
            List<ITarget> area = GetArea(owner, mainTarget);
            int hitCount = 0;                               // 💬 몇 명을 맞혔나 셈
            float minDealt = float.MaxValue, maxDealt = 0f; // ── 확인용 임시 로그에 씀 ──
            var hitPositions = new List<Vector3>();         // 연출에 넘길 맞은 위치 (피해 전에 모음: 죽으면 풀로 돌아가 자리가 바뀜)
            Vector3? mainPosition = Application.isPlaying && mainTarget != null && mainTarget.Transform != null
                ? mainTarget.Transform.position : (Vector3?)null;

            foreach (ITarget target in area)
            {
                if (!target.IsAlive) continue;

                DamageInfo hit = MakeSkillHit(owner, stats, target, damage * ModifyOutgoingDamage(owner, target), isCritical);

                float dealt = target.PreviewDamage(hit);    // ── 확인용: 방어까지 깎인 실제 피해 ──
                minDealt = Mathf.Min(minDealt, dealt);
                maxDealt = Mathf.Max(maxDealt, dealt);
                if (Application.isPlaying && target.Transform != null) hitPositions.Add(target.Transform.position);

                OnHitTarget(owner, target, hit);
                hitCount++;                                 // 💬 한 명 맞힐 때마다 +1
            }

            if (hitCount > 0) SkillEffectTable.Play(owner, mainPosition, hitPositions);   // 연출을 부를 자리: 스킬은 "누가, 어디를 맞혔나"만 넘김

            // ── 확인용 임시 로그 (스킬 피해가 얼마인지) ── 문양 스킬이 자리 잡으면 지움
            if (hitCount > 0)
            {
                string stackText = useStack ? $" → 스택 {stack.Max:0}개 ×{damage / baseDamage:0.##} = {damage:0.##}" : " (스택 안 씀)";
                string critText = isCritical ? $" → 치명 ×{CriticalRoll.Multiplier(stats.CriticalDamage):0.##}" : "";
                string dealtText = Mathf.Approximately(minDealt, maxDealt) ? $"{minDealt:0.##}" : $"{minDealt:0.##} ~ {maxDealt:0.##}";
                Debug.Log($"[Skill] {owner.name} 스킬 피해: 위력 {stats.Power:0.##} × 능력치 {stats.AbilityValue:0.##} = {baseDamage:0.##}{stackText}{critText} → 방어 뒤 실제 {dealtText} ({hitCount}명)");
            }
            if (useStack && hitCount > 0) stack.Consume(keepHalf);  // 💬 아무도 못 맞혔으면 다 찬 스택을 버리지 않고 남김
            if (hitCount > 0) OnFired(owner, hitCount);     // 쓰고 난 뒤 (대지: 쓸 때마다 자람, 수업 35)
            return hitCount;                                // 💬 0이면 광역 타워가 "발동 안 한 것"으로 보고 다음 턴에 다시 함
        }

        protected abstract List<ITarget> GetArea(TowerController owner, ITarget mainTarget);           // 맵 전체 / 착탄 / 줄 / 하나 / 연쇄
        protected abstract void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit);     // 피해 + 부가 효과(기절, 화상 …)

        // 단일 타워: 줄 선 사이에 발동시킨 적이 죽으면 쏘지 않음 (그 적에게만 쏘는 스킬). 자기 버프·맵의 여러 적을 치는 스킬은 false
        public virtual bool NeedsLivingTarget => true;

        protected virtual bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack) => false;   // 피해 대신 다른 일 (도끼 광폭화: 자기 버프, 활 화살비: 장판). true면 쓴 것으로 침
        protected virtual bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) =>   // 도끼 대회전·바람 칼바람: 반드시 치명
            CriticalRoll.Roll(CriticalRoll.RateAgainst(owner, stats, mainTarget));
        protected virtual float ModifyOutgoingDamage(TowerController owner, ITarget target) => 1f;    // 도끼(끝 칸)·불·바람·대지·전기·빛·어둠: 대상에 따른 배율
        public virtual void OnBasicHit(TowerController owner, ITarget target) { }                     // 바람 광역(당기기), 전기 광역(전하), 빛 단일(도깨비불)
        public virtual void OnOwnerActed(TowerController owner) { }                                   // 그 타워가 행동할 때 (시간 버프의 유지 감소는 TimedTowerBuff가 따로)
        public virtual void OnEnemyTurnStart(TowerController owner) { }                               // ⓪ (빛 단일: 도깨비불 비우기)
        public virtual float ModifyBasicDamage(TowerController owner, ITarget target) => 1f;          // 이 타워의 기본 공격 피해 배율 (불 단일: 화상 +10%, 빛 단일: 도깨비불)
        protected virtual void OnFired(TowerController owner, int hitCount) { }                       // 스킬 2를 쏜 뒤 (대지: 쓸 때마다 자람, 수업 35)

        // 그 타워 자신에게 영구로 쌓이는 강화 (대지의 분노·씨뿌리기, 수업 35). 출처 하나에 합계를 넣어 두고, 티어 강화해도 이어받음
        protected static void Grow(TowerController owner, string source, StatType stat, float amount)
        {
            float total = amount;
            foreach (StatModifier modifier in owner.State.Modifiers.All)
            {
                if (modifier.Source == source && modifier.Stat == stat) total += modifier.Value;
            }
            owner.State.Modifiers.RemoveBySource(source);
            owner.State.Modifiers.Add(StatModifier.Percent(source, stat, total));
            owner.State.PersistentSources.Add(source);
        }

        protected DamageInfo MakeSkillHit(TowerController owner, TowerFinalStats stats, ITarget target, float damage, bool isCritical)
        {
            float critMultiplier = isCritical ? CriticalRoll.Multiplier(stats.CriticalDamage) : 1f;
            Vector3Int from = m_grid.GetSpawnCell(owner.SlotIndex);
            Vector3Int to = m_grid.GetPathCell(target.PathIndex);
            int distance = BoardGrid.GetTileDistance(from, to);

            return new DamageInfo
            {
                Attacker = owner,
                Source = DamageSource.Skill,
                Emblem = owner.Definition.Emblem,
                Attribute = owner.Definition.Attribute,
                BaseDamage = DamageCalculator.GetOutgoing(damage, stats.GetDistanceMultiplier(distance), critMultiplier) * stats.FinalDamageMultiplier,
                IsCritical = isCritical,
                CritMultiplier = critMultiplier,
                ArmorPenetration = stats.ArmorPenetration,
                Distance = distance,
                ForceFixed = stats.ForcesFixedDamage,
            };
        }
    }
}
