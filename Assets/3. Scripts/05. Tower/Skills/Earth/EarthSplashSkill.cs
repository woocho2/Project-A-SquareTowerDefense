using System.Collections.Generic;

namespace SquareTD
{
    // 대지 광역 — 대지의 분노 (20번 4-11장)
    //   맵 전체의 적에게 Power × AbilityValue (스택을 쓰면 ×). 쓸 때마다 이 타워의 AbilityValue +1% (복리가 아님, 판 위에 있는 동안 계속, 티어 강화로 이어받음)
    //   대지 타워끼리: 성장 존에 서 있으면 한 번에 +1.5%, 만개 존에서는 +2%
    //     진흙·석화인 적에게는 그 적의 방어력만큼 더 셈: × (1 + 방어력 × 0.1 × 0.01). 방어력 감소 디버프는 이 계산에 넣지 않음
    public class EarthSplashSkill : AttackSkill
    {
        public const string GrowthSource = "대지의 분노 성장";

        public EarthSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            if (EmblemDebuffEffect.StageOn(target, TowerEmblem.Earth) == ZoneStage.None || target.Stats == null) return 1f;
            return 1f + RaisedDefense(target.Stats) * 0.001f;
        }

        // 원래 방어력 + 늘어난 방어력 (진흙·석화, 방어 타일, 큐브 방어 면). 깎은 것(방패벽 …)은 빼지 않음
        public static float RaisedDefense(EnemyStats stats)
        {
            float percent = 0f, flat = 0f;
            foreach (StatModifier modifier in stats.Modifiers.All)
            {
                if (modifier.Stat != StatType.Defense || modifier.Value <= 0f) continue;
                if (modifier.IsPercent) percent += modifier.Value;
                else flat += modifier.Value;
            }
            return stats.GetBase(StatType.Defense) * (1f + percent) + flat;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }

        protected override void OnFired(TowerController owner, int hitCount)
        {
            Grow(owner, GrowthSource, StatType.AbilityValue, GrowthPerUse(owner));
        }

        public static float GrowthPerUse(TowerController owner)
        {
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Earth);
            if (stage == ZoneStage.Skill2) return 0.02f;
            if (stage == ZoneStage.Skill1) return 0.015f;
            return 0.01f;
        }
    }
}
