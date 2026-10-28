using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 광역 — 블리자드 (20번 4-13장)
    //   맵 전체의 적에게 Power × AbilityValue (스택을 쓰면 ×). 동상·저체온증인 적을 빙결 (확률 없이 — 강한 CC라 적은 지금 저항력%로만 막음)
    //   이미 얼어 있는 적에게는 아무 일도 없음 (같은 키라 Status.Add가 거절)
    public class IceSplashSkill : AttackSkill
    {
        public IceSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool freezes = enemy != null && EmblemDebuffEffect.StageOn(target, TowerEmblem.Ice) != ZoneStage.None;

            DamageInfo blizzard = hit;
            blizzard.ChangesMovement = freezes;
            ProjectileHitResolver.HitSingle(target, blizzard);

            // [B] freezes이고 그 적이 아직 살아 있으면 빙결을 검 — IceDebuffEffect.OnTurn의 Add와 같은 모양 (조건만 freezes && enemy.IsAlive)
        }
    }
}
