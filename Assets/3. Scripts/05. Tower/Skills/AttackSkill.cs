using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 공격 타워 스킬 2의 베이스 (템플릿 메서드)
    public abstract class AttackSkill
    {
        protected readonly BoardGrid m_grid;
        protected readonly PathOccupancy m_occupancy;

        protected AttackSkill(BoardGrid grid, PathOccupancy occupancy)
        {
            m_grid = grid;
            m_occupancy = occupancy;
        }

        public int Fire(TowerController owner, ITarget mainTarget)
        {
            TowerFinalStats stats = owner.Stats;
            TowerStack stack = owner.State.Stack;

            bool useStack = stack.IsFull;

            if (CastInstead(owner, mainTarget, useStack))
            {
                if (useStack) stack.Consume();
                SkillEffectTable.PlayCast(owner);
                return 1;
            }

            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            float baseDamage = damage;
            if (useStack) damage = SkillFormula.WithStack(damage, stack.Max, stats.StackValue) * (1f + stats.Get(StatType.ChargeBonus));

            bool isCritical = RollCritical(owner, stats, mainTarget);
            List<ITarget> area = GetArea(owner, mainTarget);
            int hitCount = 0;
            float minDealt = float.MaxValue, maxDealt = 0f;
            var hitPositions = new List<Vector3>();
            Vector3? mainPosition = Application.isPlaying && mainTarget != null && mainTarget.Transform != null
                ? mainTarget.Transform.position : (Vector3?)null;

            foreach (ITarget target in area)
            {
                if (!target.IsAlive) continue;

                bool targetCritical = isCritical || IsCriticalAgainst(owner, target);
                DamageInfo hit = MakeSkillHit(owner, stats, target, damage * ModifyOutgoingDamage(owner, target), targetCritical);

                float dealt = target.PreviewDamage(hit);
                minDealt = Mathf.Min(minDealt, dealt);
                maxDealt = Mathf.Max(maxDealt, dealt);
                if (Application.isPlaying && target.Transform != null) hitPositions.Add(target.Transform.position);

                OnHitTarget(owner, target, hit);
                hitCount++;
            }

            if (hitCount > 0) SkillEffectTable.Play(owner, mainPosition, hitPositions);

            if (hitCount > 0)
            {
                string stackText = useStack ? $" → 스택 {stack.Max:0}개 ×{damage / baseDamage:0.##} = {damage:0.##}" : " (스택 안 씀)";
                string critText = isCritical ? $" → 치명 ×{CriticalRoll.Multiplier(stats.CriticalDamage):0.##}" : "";
                string dealtText = Mathf.Approximately(minDealt, maxDealt) ? $"{minDealt:0.##}" : $"{minDealt:0.##} ~ {maxDealt:0.##}";
                Debug.Log($"[Skill] {owner.name} 스킬 피해: 위력 {stats.Power:0.##} × 능력치 {stats.AbilityValue:0.##} = {baseDamage:0.##}{stackText}{critText} → 방어 뒤 실제 {dealtText} ({hitCount}명)");
            }
            if (useStack && hitCount > 0) stack.Consume();
            if (hitCount > 0) OnFired(owner, hitCount);
            return hitCount;
        }

        protected abstract List<ITarget> GetArea(TowerController owner, ITarget mainTarget);
        protected abstract void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit);

        public virtual bool NeedsLivingTarget => true;

        public virtual bool BlocksBasicAttack(TowerController owner) => false;

        protected virtual bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack) => false;
        protected virtual bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) => false;
        protected virtual bool IsCriticalAgainst(TowerController owner, ITarget target) => false;

        protected static float ChargedAbility(TowerFinalStats stats, bool useStack) =>
            useStack ? SkillFormula.ChargedAbility(stats.AbilityValue, stats.Stack, stats.StackValue) * (1f + stats.Get(StatType.ChargeBonus))
                     : stats.AbilityValue;

        protected static ITarget HighestHp(List<ITarget> candidates)
        {
            ITarget best = null;
            foreach (ITarget target in candidates)
            {
                if (!target.IsAlive) continue;
                if (best == null || target.CurrentHp > best.CurrentHp) best = target;
            }
            return best;
        }

        protected static int EffectDuration(TowerController owner) => SkillFormula.EffectDuration(owner);
        protected virtual float ModifyOutgoingDamage(TowerController owner, ITarget target) => 1f;
        public virtual void OnEnemyTurnStart(TowerController owner) { }
        protected virtual void OnFired(TowerController owner, int hitCount) { }

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
                BaseDamage = DamageCalculator.GetOutgoing(damage, stats.GetDistanceMultiplier(distance), critMultiplier) * stats.FinalDamageMultiplier * TowerAction.LostHpMultiplier(stats, target),
                IsCritical = isCritical,
                CritMultiplier = critMultiplier,
                ArmorPenetration = stats.ArmorPenetration,
                Distance = distance,
                ForceFixed = stats.ForcesFixedDamage,
            };
        }
    }
}
