using System.Collections.Generic;

namespace SquareTD
{
    // 물 단일 — 급류
    public class WaterTargetSkill : AttackSkill
    {
        public WaterTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool isWet = EmblemDebuffEffect.StageOn(target, TowerEmblem.Water) != ZoneStage.None;
            bool pushes = enemy != null && enemy.IsAlive && Chance.Roll(enemy.Resistance.ResistWeak(1f));

            DamageInfo torrent = hit;
            torrent.ChangesMovement = pushes;
            ProjectileHitResolver.HitSingle(target, torrent);

            if (pushes && enemy.IsAlive) enemy.Status.Add(new ForcedMoveEffect(isWet ? -2 : -1));
        }
    }
}
