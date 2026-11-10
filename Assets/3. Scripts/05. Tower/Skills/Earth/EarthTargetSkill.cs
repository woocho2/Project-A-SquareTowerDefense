using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 대지 단일 — 소잉 (쓸 때마다 Power·치명타 피해·치명타 확률·AttackCount 중 무작위 하나가 성장 스택만큼 오름)
    public class EarthTargetSkill : AttackSkill
    {
        public const string GrowthSource = "소잉 성장";
        public const string AttackCountKey = "소잉 AttackCount";

        public EarthTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) => ProjectileHitResolver.HitSingle(target, hit);

        protected override void OnFired(TowerController owner, int hitCount) => Sow(owner, Random.Range(0, 4));

        public static void Sow(TowerController owner, int pick)
        {
            float growth = Growth.Get(owner) * SkillFormula.PercentRate;
            if (growth <= 0f) return;

            switch (pick)
            {
                case 0: Grow(owner, GrowthSource + " Power", StatType.Power, growth); break;
                case 1: GrowFlat(owner, GrowthSource + " 치명타 피해", StatType.CritDamage, growth); break;
                case 2: GrowFlat(owner, GrowthSource + " 치명타 확률", StatType.CritRate, growth); break;
                default:
                    owner.State.AddCounter(AttackCountKey, growth);
                    string source = GrowthSource + " AttackCount";
                    owner.State.Modifiers.RemoveBySource(source);
                    owner.State.Modifiers.Add(StatModifier.Flat(source, StatType.AttackCount, Mathf.Floor(owner.State.GetCounter(AttackCountKey) + 0.0001f)));
                    owner.State.PersistentSources.Add(source);
                    break;
            }
        }

        private static void GrowFlat(TowerController owner, string source, StatType stat, float amount)
        {
            float total = amount;
            foreach (StatModifier modifier in owner.State.Modifiers.All)
            {
                if (modifier.Source == source && modifier.Stat == stat) total += modifier.Value;
            }
            owner.State.Modifiers.RemoveBySource(source);
            owner.State.Modifiers.Add(StatModifier.Flat(source, stat, total));
            owner.State.PersistentSources.Add(source);
        }
    }
}
