using System.Collections.Generic;

namespace SquareTD
{
    // 수업 23의 시험 스킬: 문양 없이 피해만 줌 (20번 4-1장 [컬러별 틀]의 기본형)
    //   광역: 맵 전체의 적에게 / 단일: 발동시킨 그 적에게
    //   수업 27부터 문양 스킬이 생기면 그 문양은 문양 스킬을 씀. 아직 없는 문양만 이것으로
    public class DamageOnlySkill : AttackSkill
    {
        public DamageOnlySkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (owner.Definition.Color == TowerColor.Red)
                return SkillArea.WholeMap(m_grid, m_occupancy);

            return SkillArea.Single(mainTarget);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            //    문양 스킬(수업 27부터)은 여기에 부가 효과(기절, 화상 …)를 더 씀
            ProjectileHitResolver.HitSingle(target, hit);
        }
    }
}
